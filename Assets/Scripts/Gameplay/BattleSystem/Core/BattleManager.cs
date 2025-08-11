using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Events;
using Gameplay.BattleSystem.Interfaces;
using Gameplay.BattleSystem.States;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.Units;
using R3;
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
        private readonly CompositeDisposable _disposables = new();

        [Inject] private readonly IEventBus _eventBus;

        // Services
        [Inject] private readonly BattleEventService _battleEventService;
        [Inject] private readonly AttackService _attackService;
        [Inject] private readonly TurnOrderService _turnOrderService;
        [Inject] private readonly TurnService _turnService;
        [Inject] private readonly BattleConditionService _conditionService;

        public PlayerUnit PlayerUnit => _playerUnit;
        public IReadOnlyList<EnemyUnit> EnemyUnits => _enemyUnits;

        public TurnOrderEntry CurrentTurn => _turnOrderService.CurrentTurn;
        public BattleUnit CurrentUnit => _turnService.CurrentUnit;
        public bool IsPlayerTurn => _turnService.IsPlayerTurn;
        public bool IsEnemyTurn => _turnService.IsEnemyTurn;

        public EnemyUnit CurrentEnemy => _turnService.CurrentEnemy;
        public BattleUnit Winner { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

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
                InitializeTurnSystem();
                SetupUIEvents();
                SetupStateMachine();
                SubscribeBattleEvents();

                _battleEventService.PublishBattleStarted(_playerUnit, _enemyUnits.ToArray<BattleUnit>());
                OnTurnChanged(_turnOrderService.CurrentTurn);

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

        private void InitializeTurnSystem()
        {
            var aliveEnemies = _enemyUnits.Where(e => e != null && e.Health.IsAlive).ToList();
            _turnService.Initialize(_playerUnit, aliveEnemies);

            _turnService.OnTurnChanged += OnTurnChanged;
            _turnService.OnRoundChanged += OnRoundChanged;
        }

        private void OnTurnChanged(TurnOrderEntry turnEntry)
        {
            GameLogger.Info(ZString.Format("{0}의 턴!", turnEntry
                .Unit.UnitName));

            if (turnEntry.IsPlayer)
            {
                _stateMachine.ChangeState(BattleState.PlayerTurn);
            }
            else
            {
                _stateMachine.ChangeState(BattleState.EnemyTurnTransition);
            }
        }

        private void OnRoundChanged(int round)
        {
            GameLogger.Info(ZString.Format("=== 라운드 {0} 시작!", round));
        }

        private void SubscribeBattleEvents()
        {
            _eventBus.Subscribe<BattleEndedEvent>(endEvent =>
            {
                Winner = endEvent.Winner;
                _stateMachine.ChangeState(BattleState.BattleEnd);
            }).AddTo(_disposables);
        }

        private void SetupStateMachine()
        {
            _stateMachine = new StateMachine<BattleState>(
                initialState: BattleState.BattleStart);
            _stateMachine.AddState(new BattleStartState(this));
            _stateMachine.AddState(new BattleEndState(this, _battleUI));
            _stateMachine.AddState(new PlayerTurnState(_battleUI));
            _stateMachine.AddState(new EnemyTurnTransitionState(_turnService));
            _stateMachine.AddState(new EnemyTurnState(this, _battleUI));

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
                _battleUI.OnAttackButtonClicked += OnPlayerAttackClicked;
                _battleUI.OnTargetSelected += ExecutePlayerAttack;
                _playerUnit.Health.OnUnitDefeated +=
                    () => _battleEventService.PublishBattleEnded(null, "Player defeated");
                GameLogger.Debug("Battle UI events connected", LogCategory.Battle);
            }
        }

        #region Battle Actions

        private void OnPlayerAttackClicked(WeaponData weapon)
        {
            var activeEnemies = _turnService.GetActiveEnemies();
            if (activeEnemies.Count == 1)
            {
                ExecutePlayerAttack(activeEnemies[0], weapon);
            }
            else
            {
                _battleUI.ShowTargetSelection(activeEnemies, weapon);
            }
        }

        private void ExecutePlayerAttack(EnemyUnit target, WeaponData weapon)
        {
            if (!CanExecutePlayerAction())
            {
                return;
            }

            var result = _attackService.ExecuteAttack(_playerUnit, target, weapon);

            if (!result.IsSuccess)
            {
                GameLogger.Warning(result.ErrorMessage, LogCategory.Battle);
                return;
            }

            FinishTurn();
        }

        public void ExecuteEnemyAction()
        {
            if (!CanExecuteEnemyAction())
            {
                return;
            }

            var currentEnemy = _turnService.CurrentUnit;
            if (currentEnemy == null)
            {
                GameLogger.Warning("현재 적이 없습니다!",  LogCategory.Battle);
                FinishTurn();
                return;
            }

            currentEnemy.Turn.OnTurnStart();

            if (currentEnemy.Shield.IsBroken)
            {
                GameLogger.Info(ZString.Format("{0} 브레이크 상태로 턴 건너뜀", currentEnemy.UnitName), LogCategory.Battle);
                currentEnemy.Turn.OnTurnEnd();
                FinishTurn();
                return;
            }

            _ = _attackService.ExecuteAttack(currentEnemy, _playerUnit, currentEnemy.EquippedWeapon);
            currentEnemy.Turn.OnTurnEnd();

            // 다음 적 턴으로 진행
            FinishTurn();
        }

        private void FinishTurn()
        {
            var endCondition = _conditionService.CheckBattleEndCondition(_playerUnit, _turnService);
            if (endCondition.ShouldEndBattle)
            {
                _battleEventService.PublishBattleEnded(endCondition.Winner, endCondition.Reason);
                return;
            }

            _turnService.AdvanceToNextTurn();
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
                    _battleUI.OnAttackButtonClicked -= OnPlayerAttackClicked;
                    _battleUI.OnTargetSelected -= ExecutePlayerAttack;
                }

                _turnService.OnTurnChanged -= OnTurnChanged;
                _turnService.OnRoundChanged -= OnRoundChanged;

                _disposables.Dispose();

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
