using System;
using System.Collections.Generic;
using System.Linq;
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
using VContainer;

namespace Gameplay.BattleSystem.Core
{
    public class BattleManager : IBattleManager
    {
        private readonly IStateMachine<BattleState> _stateMachine;
        private readonly PlayerUnit _playerUnit;
        private readonly List<EnemyUnit> _enemyUnits;
        private readonly BattleUI _battleUI;

        [Inject] private readonly IEventBus _eventBus;

        // 턴 관리
        private int _currentEnemyIndex = 0;
        private List<EnemyUnit> _activeEnemies;

        public PlayerUnit PlayerUnit => _playerUnit;
        public List<EnemyUnit> EnemyUnits => _enemyUnits;

        public EnemyUnit CurrentEnemy
            => _activeEnemies != null &&
               _activeEnemies.Count > _currentEnemyIndex
                ? _activeEnemies[_currentEnemyIndex]
                : null;

        public BattleUnit Winner { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public event Action<PlayerUnit, List<EnemyUnit>> OnBattleStarted;
        public event Action<BattleUnit> OnBattleEnded;

        public BattleManager(
            BattleUI battleUI,
            PlayerUnit playerUnit,
            List<EnemyUnit> enemyUnits)
        {
            _battleUI = battleUI ?? throw new ArgumentNullException(nameof(battleUI));
            _playerUnit = playerUnit ?? throw new ArgumentNullException(nameof(playerUnit));
            _enemyUnits = enemyUnits ?? throw new ArgumentNullException(nameof(enemyUnits));

            _activeEnemies = _enemyUnits.Where(e => e != null && e.IsAlive).ToList();

            _stateMachine = new StateMachine<BattleState>(
                initialState: BattleState.BattleStart);
            _stateMachine.AddState(new BattleStartState(this));
            _stateMachine.AddState(new BattleEndState(this, _battleUI));
            _stateMachine.AddState(new PlayerTurnState(_battleUI));
            _stateMachine.AddState(new EnemyTurnState(this, _battleUI));
            _stateMachine.AddState(new EnemyTurnTransitionState(this));

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
                _battleUI.OnTargetSelected += OnTargetSelected;
                GameLogger.Debug("Battle UI events connected", LogCategory.Battle);
            }
        }

        private void SetupUnitEvents()
        {
            if (_playerUnit != null)
            {
                _playerUnit.OnUnitDefeated += OnPlayerDefeated;
            }

            foreach (var enemyUnit in _enemyUnits)
            {
                if (enemyUnit != null)
                {
                    enemyUnit.OnUnitDefeated += OnEnemyDefeated;
                }
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
            var battleStartedEvent = new BattleStartedEvent(_playerUnit, _enemyUnits);
            _eventBus.Publish(battleStartedEvent);

            OnBattleStarted?.Invoke(_playerUnit, _enemyUnits);
        }

        #region Battle Actions

        private void OnPlayerAttack(WeaponType weaponType)
        {
            if (!CanExecutePlayerAction())
            {
                return;
            }

            GameLogger.Debug("Player attack action triggered", LogCategory.Battle);

            if (_activeEnemies.Count > 1)
            {
                GameLogger.Debug(ZString.Format("Showing target selection for {0} enemies", _activeEnemies.Count),  LogCategory.Battle);
                _battleUI.ShowTargetSelection(_activeEnemies, weaponType);
            }
            else if (_activeEnemies.Count == 1)
            {
                ExecutePlayerAttack(_activeEnemies[0], weaponType);
            }
            else
            {
                GameLogger.Warning("No active enemies to attack!", LogCategory.Battle);
            }
        }

        private void OnTargetSelected(EnemyUnit target, WeaponType weaponType)
        {
            ExecutePlayerAttack(target, weaponType);
        }

        private void ExecutePlayerAttack(EnemyUnit target, WeaponType weaponType)
        {
            if (target == null || !target.IsAlive)
            {
                GameLogger.Warning("Invalid target for player attack",  LogCategory.Battle);
                return;
            }

            GameLogger.Debug(ZString.Format("Player attacking {0} with {1}", target.UnitName, weaponType), LogCategory.Battle);

            // 공격 실행
            _playerUnit.AttackTarget(target, weaponType);

            // 타겟이 죽었으면 활성 목록에서 제거
            if (!target.IsAlive)
            {
                _activeEnemies.Remove(target);
            }

            // 모든 적이 죽었는지 확인
            if (_activeEnemies.Count == 0)
            {
                Winner = _playerUnit;
                _stateMachine.ChangeState(BattleState.BattleEnd);
                return;
            }

            // 플레이어가 죽었는지 확인
            if (!_playerUnit.IsAlive)
            {
                Winner = _activeEnemies.Count > 0 ? _activeEnemies[0] : null;
                _stateMachine.ChangeState(BattleState.BattleEnd);
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

            var currentEnemy = CurrentEnemy;
            if (currentEnemy == null || !currentEnemy.IsAlive)
            {
                GameLogger.Warning("현재 적이 null이거나 죽었습니다. 다음 턴으로 진행", LogCategory.Battle);
                AdvanceToNextEnemyTurn();
                return;
            }

            GameLogger.Debug($"{currentEnemy.UnitName}이(가) 플레이어를 공격합니다!", LogCategory.Battle);

            // 적 공격 실행 (기본 검 공격)
            currentEnemy.AttackTarget(_playerUnit, WeaponType.Sword);

            // 플레이어가 죽었으면 게임 종료
            if (!_playerUnit.IsAlive)
            {
                Winner = currentEnemy;
                _stateMachine.ChangeState(BattleState.BattleEnd);
                return;
            }

            // 다음 적 턴으로 진행
            AdvanceToNextEnemyTurn();
        }

        public bool HasMoreEnemyTurns()
        {
            _activeEnemies = _activeEnemies.Where(e => e != null && e.IsAlive).ToList();

            // 모든 적이 죽었으면 false
            if (_activeEnemies.Count == 0)
            {
                Winner = _playerUnit;
                _stateMachine.ChangeState(BattleState.BattleEnd);
                return false;
            }

            // 현재 인덱스가 활성 적 수보다 작으면 더 있음
            bool hasMore = _currentEnemyIndex < _activeEnemies.Count;

            GameLogger.Debug(ZString.Format("HasMoreEnemyTurns: {0} (현재 인덱스: {1}, 활성 적 수: {2})",
                hasMore, _currentEnemyIndex, _activeEnemies.Count), LogCategory.Battle);

            return hasMore;
        }

        public void ResetEnemyTurnIndex()
        {
            _currentEnemyIndex = 0;
            GameLogger.Debug("적 턴 인덱스가 0으로 리셋되었습니다", LogCategory.Battle);
        }

        #endregion

        private void StartEnemyTurns()
        {
            // 죽은 적들 정리
            _activeEnemies = _activeEnemies.Where(e => e != null && e.IsAlive).ToList();

            if (_activeEnemies.Count == 0)
            {
                Winner = _playerUnit;
                _stateMachine.ChangeState(BattleState.BattleEnd);
                return;
            }

            // 첫 번째 적부터 시작
            _currentEnemyIndex = 0;
            GameLogger.Debug(ZString.Format("적 턴 시작: {0}마리의 적이 순서대로 공격", _activeEnemies.Count), LogCategory.Battle);

            // 첫 번째 적 턴으로 전환
            _stateMachine.ChangeState(BattleState.EnemyTurn);
        }

        private void AdvanceToNextEnemyTurn()
        {
            // 죽은 적들을 활성 목록에서 정리
            _activeEnemies = _activeEnemies.Where(e => e != null && e.IsAlive).ToList();

            // 모든 적이 죽었으면 플레이어 승리
            if (_activeEnemies.Count == 0)
            {
                Winner = _playerUnit;
                _stateMachine.ChangeState(BattleState.BattleEnd);
                return;
            }

            _currentEnemyIndex++;
            GameLogger.Debug(ZString.Format("다음 적 턴으로 진행: 인덱스 {0} / {1}",
                _currentEnemyIndex, _activeEnemies.Count), LogCategory.Battle);

            // EnemyTurnTransition 상태로 전환 (여기서 다음 턴 결정)
            _stateMachine.ChangeState(BattleState.EnemyTurnTransition);
        }

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

            if (!_playerUnit.IsAlive || _activeEnemies.Count == 0)
            {
                GameLogger.Warning("Cannot execute player action: Player dead or no enemies", LogCategory.Battle);
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

            if (!_playerUnit.IsAlive || _activeEnemies.Count == 0)
            {
                GameLogger.Warning("Cannot execute enemy action: Invalid state", LogCategory.Battle);
                return false;
            }

            return true;
        }


        #endregion

        #region Unit Defeat Handlers

        private void OnEnemyDefeated(BattleUnit unit)
        {
            var enemyUnit = unit as EnemyUnit;
            if (enemyUnit == null) return;

            GameLogger.Debug(ZString.Concat(enemyUnit.UnitName, " has been defeated!"), LogCategory.Battle);

            // 죽은 적을 활성 목록에서 제거
            _activeEnemies.Remove(enemyUnit);

            // 현재 턴인 적이 죽었으면 인덱스 조정
            if (_currentEnemyIndex >= _activeEnemies.Count)
            {
                _currentEnemyIndex = 0;
            }

            GameLogger.Debug(ZString.Format("Remaining enemies: {0}", _activeEnemies.Count), LogCategory.Battle);

            // 모든 적이 죽었으면 플레이어 승리
            if (_activeEnemies.Count == 0)
            {
                Winner = _playerUnit;
                _stateMachine.ChangeState(BattleState.BattleEnd);
            }
        }

        private void OnPlayerDefeated(BattleUnit unit)
        {
            GameLogger.Debug("Player has been defeated!", LogCategory.Battle);
            Winner = _activeEnemies.Count > 0 ? _activeEnemies[0] : null;
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
            sb.AppendLine(ZString.Format("Player: {0} (Alive: {1})", _playerUnit?.UnitName ?? "null", _playerUnit?.IsAlive ?? false));
            sb.AppendLine(ZString.Format("Total Enemies: {0}", _enemyUnits?.Count ?? 0));
            sb.AppendLine(ZString.Format("Active Enemies: {0}", _activeEnemies?.Count ?? 0));

            if (_activeEnemies != null)
            {
                for (int i = 0; i < _activeEnemies.Count; i++)
                {
                    sb.AppendLine(ZString.Format("  Enemy {0}: {1} (HP: {2})",
                        i, _activeEnemies[i]?.UnitName ?? "null", _activeEnemies[i]?.IsAlive ?? false));
                }
            }

            sb.AppendLine(ZString.Format("Current Enemy Index: {0}", _currentEnemyIndex));
            sb.AppendLine(ZString.Format("Current Enemy: {0}", CurrentEnemy?.UnitName ?? "null"));
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
                    _battleUI.OnTargetSelected -= OnTargetSelected;
                }

                if (_playerUnit != null)
                {
                    _playerUnit.OnUnitDefeated -= OnPlayerDefeated;
                }

                foreach (var enemy in _enemyUnits)
                {
                    if (enemy != null)
                    {
                        enemy.OnUnitDefeated -= OnEnemyDefeated;
                    }
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
