using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Data.BattleSystem.Enums;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Events;
using Gameplay.BattleSystem.Interfaces;
using Gameplay.BattleSystem.Services;
using Gameplay.BattleSystem.States;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.Units;
using Systems.EventBus;
using Systems.StateMachine;
using Systems.StateMachine.Interfaces;
using VContainer;

namespace Gameplay.BattleSystem.Core
{
    public class BattleManager : IBattleManager
    {
        private IStateMachine<BattleState> _stateMachine;
        private readonly PlayerUnit _playerUnit;
        private readonly IReadOnlyList<EnemyUnit> _enemyUnits;
        private readonly BattleUI _battleUI;

        [Inject] private readonly IEventBus _eventBus;

        // Services
        [Inject]
        private readonly AttackService _attackService;
        [Inject]
        private readonly TurnService _turnService;
        [Inject]
        private readonly BattleConditionService _conditionService;

        public PlayerUnit PlayerUnit => _playerUnit;
        public IReadOnlyList<EnemyUnit> EnemyUnits => _enemyUnits;

        public EnemyUnit CurrentEnemy => _turnService?.CurrentEnemy;
        public BattleUnit Winner { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public event Action<PlayerUnit, IReadOnlyList<EnemyUnit>> OnBattleStarted;
        public event Action<BattleUnit> OnBattleEnded;

        public BattleManager(
            BattleUI battleUI,
            PlayerUnit playerUnit,
            List<EnemyUnit> enemyUnits)
        {
            _battleUI = battleUI ?? throw new ArgumentNullException(nameof(battleUI));
            _playerUnit = playerUnit ?? throw new ArgumentNullException(nameof(playerUnit));
            _enemyUnits = enemyUnits ?? throw new ArgumentNullException(nameof(enemyUnits));


            GameLogger.Debug("BattleManager created", LogCategory.Battle);
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                GameLogger.Warning("BattleManager is already initialized", LogCategory.Battle);
                return;
            }

            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(BattleManager));
            }

            try
            {
                _turnService.Initialize(_enemyUnits.Where(e => e != null && e.Health.IsAlive).ToList());
                SetupUIEvents();
                SetupStateMachine();
                PublishBattleStartedEvent();

                IsInitialized = true;
                GameLogger.Debug("Battle Manager is initialized", LogCategory.Battle);
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Format("Failed to initialize BattleManager: {0}", e.Message),
                    LogCategory.Battle);
                throw;
            }
        }

        private void SetupStateMachine()
        {
            _stateMachine = new StateMachine<BattleState>(
                initialState: BattleState.BattleStart);
            _stateMachine.AddState(new BattleStartState(this));
            _stateMachine.AddState(new BattleEndState(this, _battleUI));
            _stateMachine.AddState(new PlayerTurnState(_battleUI));
            _stateMachine.AddState(new EnemyTurnState(this, _battleUI));
            _stateMachine.AddState(new EnemyTurnTransitionState(this));

            GameLogger.Info("Starting Battle State Machine...", LogCategory.Battle);
            _stateMachine.TrySetInitialState(BattleState.BattleStart);

            if (_stateMachine is { IsInitialized: true })
            {
                GameLogger.Info(
                    ZString.Format("StateMachine started with state: {0}", _stateMachine.CurrentStateType.CurrentValue),
                    LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("StateMachine is not properly initialized.", LogCategory.Battle);
            }
        }

        private void SetupUIEvents()
        {
            if (_battleUI != null)
            {
                _battleUI.OnAttackButtonClicked += (weaponType) =>
                {
                    var activeEnemies = _turnService.GetActiveEnemies();
                    if (activeEnemies.Count == 1)
                    {
                        ExecutePlayerAttack(activeEnemies[0], weaponType);
                    }
                    else
                    {
                        _battleUI.ShowTargetSelection(activeEnemies, weaponType);
                    }
                };

                _battleUI.OnTargetSelected += ExecutePlayerAttack;
                _playerUnit.Health.OnUnitDefeated += () => EndBattle(null, "Player defeated");
                GameLogger.Debug("Battle UI events connected", LogCategory.Battle);
            }
        }

        private void PublishBattleStartedEvent()
        {
            var battleStartedEvent = new BattleStartedEvent(_playerUnit, _enemyUnits);
            _eventBus.Publish(battleStartedEvent);

            OnBattleStarted?.Invoke(_playerUnit, _enemyUnits);
        }

        private void EndBattle(BattleUnit winner, string reason)
        {
            Winner = winner;
            _stateMachine.ChangeState(BattleState.BattleEnd);
            GameLogger.Info(ZString.Format("전투 종료: {0} ({1})", winner?.UnitName ?? "무승부", reason), LogCategory.Battle);
        }

        #region Turn Management

        private void StartEnemyTurns()
        {
            _turnService.ResetTurnIndex();
            _stateMachine.ChangeState(BattleState.EnemyTurn);
        }

        private void AdvanceToNextEnemyTurn()
        {
            _turnService.AdvanceToNextEnemy();
            _stateMachine.ChangeState(BattleState.EnemyTurnTransition);
        }

        public bool HasMoreEnemyTurns()
        {
            return _turnService.HasMoreEnemyTurns();
        }

        public void ResetTurnIndex()
        {
            _turnService.ResetTurnIndex();
        }

        #endregion

        #region Battle Actions

        private void ExecutePlayerAttack(EnemyUnit target, WeaponType weaponType)
        {
            if (!CanExecutePlayerAction())
            {
                return;
            }

            var result = _attackService.ExecuteAttack(_playerUnit, target, weaponType);

            if (!result.IsSuccess)
            {
                GameLogger.Warning(result.ErrorMessage, LogCategory.Battle);
                return;
            }

            var endCondition = _conditionService.CheckBattleEndCondition(_playerUnit, _turnService);
            if (endCondition.ShouldEndBattle)
            {
                EndBattle(endCondition.Winner, endCondition.Reason);
                return;
            }

            // 적 턴 시작 - 첫 번째 적부터!
            StartEnemyTurns();
        }

        public void ExecuteEnemyAction()
        {
            if (!CanExecuteEnemyAction())
            {
                return;
            }

            var currentEnemy = _turnService.CurrentEnemy;
            if (currentEnemy == null)
            {
                AdvanceToNextEnemyTurn();
                return;
            }

            currentEnemy.Turn.OnTurnStart();

            if (currentEnemy.Shield.IsBroken)
            {
                GameLogger.Info(ZString.Format("{0} 브레이크 상태로 턴 건너뜀", currentEnemy.UnitName), LogCategory.Battle);
                currentEnemy.Turn.OnTurnEnd();
                AdvanceToNextEnemyTurn();
                return;
            }

            var result = _attackService.ExecuteAttack(currentEnemy, _playerUnit, WeaponType.Sword);
            currentEnemy.Turn.OnTurnEnd();

            var endCondition = _conditionService.CheckBattleEndCondition(_playerUnit, _turnService);
            if (endCondition.ShouldEndBattle)
            {
                EndBattle(endCondition.Winner, endCondition.Reason);
                return;
            }

            // 다음 적 턴으로 진행
            AdvanceToNextEnemyTurn();
        }

        #endregion

        #region Action Validation

        private bool CanExecutePlayerAction()
        {
            return _conditionService.CanExecuteAction(_playerUnit, _turnService,
                _stateMachine.CurrentStateType.CurrentValue, BattleState.PlayerTurn);
        }

        private bool CanExecuteEnemyAction()
        {
            return _conditionService.CanExecuteAction(_playerUnit, _turnService,
                _stateMachine.CurrentStateType.CurrentValue, BattleState.EnemyTurn);
        }

        #endregion

        #region Debugging

        public string GetDebugInfo()
        {
            return ZString.Format("BattleManager - Player: {0}, Active Enemies: {1}, Current State: {2}",
                _playerUnit?.UnitName ?? "null",
                _turnService?.ActiveEnemyCount ?? 0,
                _stateMachine?.CurrentStateType.CurrentValue);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            GameLogger.Debug("BattleManager disposing...", LogCategory.Battle);
            try
            {
                if (_battleUI != null)
                {
                    _battleUI.OnAttackButtonClicked -= null;
                    _battleUI.OnTargetSelected -= null;
                }

                Winner = null;
                IsInitialized = false;
                IsDisposed = true;
                GameLogger.Info("BattleManager disposed", LogCategory.Battle);
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error during BattleManager disposal: ", e.Message),
                    LogCategory.Battle);
            }
        }

        #endregion
    }
}
