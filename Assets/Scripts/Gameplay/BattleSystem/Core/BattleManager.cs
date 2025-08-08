using System;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Events;
using Gameplay.BattleSystem.Interfaces;
using Gameplay.BattleSystem.States;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.Units;
using Systems.EventBus;
using Systems.StateMachine;
using Systems.StateMachine.Interfaces;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.Core
{
    public class BattleManager : IBattleManager
    {
        private readonly IStateMachine<BattleState> _stateMachine;
        private readonly PlayerUnit _playerUnit;
        private readonly EnemyUnit _enemyUnit;
        private readonly BattleUI _battleUI;

        [Inject]
        private readonly IEventBus _eventBus;

        public PlayerUnit PlayerUnit => _playerUnit;
        public EnemyUnit EnemyUnit => _enemyUnit;
        public BattleUnit Winner { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public event Action<BattleUnit, BattleUnit> OnBattleStarted;
        public event Action<BattleUnit> OnBattleEnded;

        public BattleManager(
            BattleUI battleUI,
            PlayerUnit playerUnit,
            EnemyUnit enemyUnit)
        {
            _battleUI = battleUI ?? throw new ArgumentNullException(nameof(battleUI));
            _playerUnit = playerUnit ?? throw new ArgumentNullException(nameof(playerUnit));
            _enemyUnit = enemyUnit ?? throw new ArgumentNullException(nameof(enemyUnit));

            _stateMachine = new StateMachine<BattleState>(
                initialState: BattleState.BattleStart);
            _stateMachine.AddState(new BattleStartState(this));
            _stateMachine.AddState(new BattleEndState(this, _battleUI));
            _stateMachine.AddState(new PlayerTurnState(_battleUI));
            _stateMachine.AddState(new EnemyTurnState(this, _battleUI));

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
                SetupUIEvents();
                SetupUnitEvents();
                StartStateMachine();
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

        private void SetupUIEvents()
        {
            if (_battleUI != null)
            {
                _battleUI.OnAttackButtonClicked += OnPlayerAttack;
                GameLogger.Debug("Battle UI events connected", LogCategory.Battle);
            }
        }

        private void SetupUnitEvents()
        {
            if (_playerUnit != null)
            {
                _playerUnit.OnUnitDefeated += OnPlayerDefeated;
            }

            if (_enemyUnit != null)
            {
                _enemyUnit.OnUnitDefeated += OnEnemyDefeated;
            }

            GameLogger.Debug("Battle Unit events connected", LogCategory.Battle);
        }

        private void StartStateMachine()
        {
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

        private void PublishBattleStartedEvent()
        {
            var battleStartedEvent = new BattleStartedEvent(_playerUnit, _enemyUnit);
            _eventBus.Publish(battleStartedEvent);

            OnBattleStarted?.Invoke(_playerUnit, _enemyUnit);
        }

        #region Battle Actions

        private void OnPlayerAttack()
        {
            if (!CanExecutePlayerAction())
            {
                return;
            }

            GameLogger.Debug("Player attack action triggered",  LogCategory.Battle);

            // 공격 실행
            _playerUnit.AttackTarget(_enemyUnit);

            // 적이 살아있으면 적 턴으로 전환
            if (_enemyUnit.IsAlive)
            {
                _stateMachine.ChangeState(BattleState.EnemyTurn);
            }
        }

        public void ExecuteEnemyAction()
        {
            if (!CanExecuteEnemyAction())
            {
                return;
            }

            GameLogger.Debug("Player attack action triggered",  LogCategory.Battle);

            // 공격 실행
            _enemyUnit.AttackTarget(_playerUnit);

            // 플레이어가이 살아있으면 적 턴으로 전환
            if (_playerUnit.IsAlive)
            {
                _stateMachine.ChangeState(BattleState.PlayerTurn);
            }
        }

        #endregion

        #region Action Validation

        private bool CanExecutePlayerAction()
        {
            if (!IsInitialized || IsDisposed)
            {
                GameLogger.Warning("BattleManager is not properly initialized", LogCategory.Battle);
                return false;
            }

            if (_stateMachine.CurrentStateType.CurrentValue != BattleState.PlayerTurn)
            {
                GameLogger.Warning("Cannot execute player action: Not player turn", LogCategory.Battle);
                return false;
            }

            if (!_playerUnit.IsAlive || !_enemyUnit.IsAlive)
            {
                GameLogger.Warning("Cannot execute player action: One unit is defeated", LogCategory.Battle);
                return false;
            }

            return true;
        }

        private bool CanExecuteEnemyAction()
        {
            if (!IsInitialized || IsDisposed)
            {
                GameLogger.Warning("BattleManager is not properly initialized", LogCategory.Battle);
                return false;
            }

            if (_stateMachine.CurrentStateType.CurrentValue != BattleState.EnemyTurn)
            {
                GameLogger.Warning("Cannot execute enemy action: Not enemy turn", LogCategory.Battle);
                return false;
            }

            if (!_playerUnit.IsAlive || !_enemyUnit.IsAlive)
            {
                GameLogger.Warning("Cannot execute enemy action: One unit is defeated", LogCategory.Battle);
                return false;
            }

            return true;
        }

        #endregion

        #region Unit Defeat Handlers

        private void OnEnemyDefeated(BattleUnit unit)
        {
            GameLogger.Debug("Enemy has been defeated!", LogCategory.Battle);
            Winner = _playerUnit;
            _stateMachine.ChangeState(BattleState.BattleEnd);
        }

        private void OnPlayerDefeated(BattleUnit unit)
        {
            GameLogger.Debug("Player has been defeated!", LogCategory.Battle);
            Winner = _enemyUnit;
            _stateMachine.ChangeState(BattleState.BattleEnd);
        }

        #endregion

        #region Debugging

        public string GetDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== BattleManager Debug Info ===");
            sb.AppendLine(ZString.Format("Initialized: {0}", IsInitialized));
            sb.AppendLine(ZString.Format("Disposed: {0}", IsDisposed));
            sb.AppendLine(ZString.Format("Player: {0} (Alive: {1})", _playerUnit?.UnitName ?? "null",
                _playerUnit?.IsAlive ?? false));
            sb.AppendLine(ZString.Format("Enemy: {0} (Alive: {1})", _enemyUnit?.UnitName ?? "null",
                _enemyUnit?.IsAlive ?? false));
            sb.AppendLine(ZString.Format("Winner: {0}", Winner?.UnitName ?? "null"));
            sb.AppendLine(ZString.Format("Current State: {0}", _stateMachine?.CurrentStateType.CurrentValue));
            return sb.ToString();
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
                    _battleUI.OnAttackButtonClicked -= OnPlayerAttack;
                }

                if (_playerUnit != null)
                {
                    _playerUnit.OnUnitDefeated -= OnPlayerDefeated;
                }

                if (_enemyUnit != null)
                {
                    _enemyUnit.OnUnitDefeated -= OnEnemyDefeated;
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
