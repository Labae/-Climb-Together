using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Units;
using VContainer;

namespace Gameplay.BattleSystem.Core.Services
{
    /// <summary>
    /// 턴 관리 전담 서비스
    /// </summary>
    public class TurnService : IDisposable
    {
        [Inject] private readonly TurnOrderService _turnOrderService;

        private PlayerUnit _playerUnit;
        private List<EnemyUnit> _enemyUnits;

        public event Action<TurnOrderEntry> OnTurnChanged;
        public event Action<int> OnRoundChanged;
        public event Action<IReadOnlyList<TurnOrderEntry>> OnTurnOrderUpdated;

        public BattleUnit CurrentUnit => _turnOrderService.CurrentUnit;
        public bool IsPlayerTurn => CurrentUnit is PlayerUnit;
        public bool IsEnemyTurn => CurrentUnit is EnemyUnit;

        public EnemyUnit CurrentEnemy => _turnOrderService.CurrentUnit as EnemyUnit;
        public int ActiveEnemyCount => _enemyUnits?.Count(e => e != null && e.Health.IsAlive) ?? 0;

        public void Initialize(PlayerUnit playerUnit, List<EnemyUnit> enemyUnits)
        {
            _playerUnit = playerUnit;
            _enemyUnits = enemyUnits;

            _turnOrderService.Initialize(playerUnit, enemyUnits);

            _turnOrderService.OnTurnChanged += OnTurnChangedInternal;
            _turnOrderService.OnTurnOrderUpdated += OnTurnOrderUpdatedInternal;
            _turnOrderService.OnRoundChanged += OnRoundChangedInternal;

            GameLogger.Info(ZString.Format("Turn Service 초기화 완료 - 플레이어: {0}, 적: {1}"
                , _playerUnit.UnitName, _enemyUnits.Count), LogCategory.Battle);
        }

        public void RefreshActiveEnemies()
        {
            _turnOrderService.RefreshTurnOrder();
        }

        private void OnTurnChangedInternal(TurnOrderEntry turnOrderEntry)
        {
            GameLogger.Debug(
                ZString.Format("턴 변경: {0} ({1})", turnOrderEntry.Unit.UnitName, turnOrderEntry.IsPlayer ? "플레이어" : "적"),
                LogCategory.Battle);
            OnTurnChanged?.Invoke(turnOrderEntry);
        }

        private void OnRoundChangedInternal(int round)
        {
            GameLogger.Debug(
                ZString.Format("라운드 {0} 시작!", round),
                LogCategory.Battle);
            OnRoundChanged?.Invoke(round);
        }

        private void OnTurnOrderUpdatedInternal(IReadOnlyList<TurnOrderEntry> turnOrderEntries)
        {
            OnTurnOrderUpdated?.Invoke(turnOrderEntries);
        }

        public bool HasMoreEnemyTurns()
        {
            return IsEnemyTurn && _turnOrderService.HasValidTurn();
        }

        public void AdvanceToNextTurn()
        {
            _turnOrderService.AdvanceToNextTurn();
        }

        public bool AreAllEnemiesDefeated()
        {
            RefreshActiveEnemies();
            return ActiveEnemyCount == 0;
        }

        public List<EnemyUnit> GetActiveEnemies()
        {
            return _enemyUnits?.Where(e => e != null && e.Health.IsAlive).ToList() ?? new List<EnemyUnit>();
        }

        public void Dispose()
        {
            if (_turnOrderService != null)
            {
                _turnOrderService.OnTurnChanged -= OnTurnChangedInternal;
                _turnOrderService.OnTurnOrderUpdated -= OnTurnOrderUpdatedInternal;
                _turnOrderService.OnRoundChanged -= OnRoundChangedInternal;
            }
        }
    }
}
