using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Units;

namespace Gameplay.BattleSystem.Core.Services
{
    /// <summary>
    /// 스피드 기반 턴 순서 계산 서비스
    /// </summary>
    public class TurnOrderService
    {
        private List<TurnOrderEntry> _turnOrder = new();
        private int _currentTurnIndex = 0;
        private int _roundNumber = 1;

        public event Action<TurnOrderEntry> OnTurnChanged;
        public event Action<int> OnRoundChanged;
        public event Action<IReadOnlyList<TurnOrderEntry>> OnTurnOrderUpdated;

        public IReadOnlyList<TurnOrderEntry> TurnOrder => _turnOrder;
        public int CurrentTurnIndex => _currentTurnIndex;
        public int RoundNumber => _roundNumber;

        public TurnOrderEntry CurrentTurn
            => _turnOrder != null &&
               _turnOrder.Count > _currentTurnIndex
            ? _turnOrder[_currentTurnIndex]
            : null;

        public BattleUnit CurrentUnit
            => CurrentTurn?.Unit;

        public void Initialize(PlayerUnit player, List<EnemyUnit> enemyUnits)
        {
            var allUnits = new List<BattleUnit> { player };
            allUnits.AddRange(enemyUnits.Where(e => e != null && e.Health.IsAlive));

            CalculateTurnOrder(allUnits);
            _currentTurnIndex = 0;
            _roundNumber = 1;

            GameLogger.Info(ZString.Format("턴 순서 초기화 완료 - 총 {0} 유닛", allUnits.Count), LogCategory.Battle);
        }

        private void CalculateTurnOrder(List<BattleUnit> units)
        {
            _turnOrder.Clear();

            // sort
            var sortedUnits =
                units.Where(unit => unit != null && unit.Health.IsAlive)
                    .OrderByDescending(unit => unit.GetBehaviourSpeed())
                    .ThenBy(unit => UnityEngine.Random.value)
                    .ToList();

            for (int i = 0; i < sortedUnits.Count; i++)
            {
                var unit = sortedUnits[i];
                var entry = new TurnOrderEntry(unit, unit.GetBehaviourSpeed(), i, unit is PlayerUnit);
                _turnOrder.Add(entry);
            }

            OnTurnOrderUpdated?.Invoke(_turnOrder);
        }

        public void AdvanceToNextTurn()
        {
            if (_turnOrder.Count == 0)
            {
                return;
            }

            _currentTurnIndex++;
            if (_currentTurnIndex >= _turnOrder.Count)
            {
                _currentTurnIndex = 0;
                _roundNumber++;
                OnRoundChanged?.Invoke(_roundNumber);
                GameLogger.Info("새로운 라운드 시작!", LogCategory.Battle);
            }

            RefreshTurnOrder();

            if (CurrentTurn != null)
            {
                OnTurnChanged?.Invoke(CurrentTurn);
                GameLogger.Debug(ZString.Format("턴 진행! {0} (스피드: {1})",
                    CurrentTurn.Unit.UnitName, CurrentTurn.Speed), LogCategory.Battle);
            }
        }

        public void RefreshTurnOrder()
        {
            var aliveUnits = _turnOrder
                .Where(entry => entry.Unit != null && entry.Unit.Health.IsAlive)
                .Select(entry => entry.Unit)
                .ToList();

            if (aliveUnits.Count != _turnOrder.Count)
            {
                GameLogger.Info(ZString.Format("턴 순서 갱신: {0} -> {1} 유닛", _turnOrder.Count, aliveUnits.Count));

                var currentUnit = CurrentUnit;
                CalculateTurnOrder(aliveUnits);

                if (currentUnit != null && currentUnit.Health.IsAlive)
                {
                    _currentTurnIndex = _turnOrder.FindIndex(entry => entry.Unit == currentUnit);
                    if (_currentTurnIndex == -1)
                    {
                        _currentTurnIndex = 0;
                    }
                }
                else
                {
                    if (_currentTurnIndex >= _turnOrder.Count)
                    {
                        _currentTurnIndex = 0;
                    }
                }
            }
        }

        public bool HasValidTurn()
        {
            return _turnOrder.Count > 0 && _currentTurnIndex < _turnOrder.Count;
        }
    }

    [Serializable]
    public class TurnOrderEntry
    {
        public BattleUnit Unit { get; private set; }
        public int Speed { get; private set; }
        public int OrderIndex { get; private set; }
        public bool IsPlayer { get; private set; }

        public bool IsValid => Unit != null && Unit.Health.IsAlive;

        public TurnOrderEntry(BattleUnit unit, int speed, int orderIndex, bool isPlayer)
        {
            Unit = unit;
            Speed = speed;
            OrderIndex = orderIndex;
            IsPlayer = isPlayer;
        }
    }
}
