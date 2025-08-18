using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Units;
using R3;

namespace Gameplay.BattleSystem.TurnOrder
{
    /// <summary>
    /// 스피드 기반 턴 순서 계산 서비스
    /// </summary>
    public class TurnOrderService : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        // Core Properties
        private readonly ReactiveProperty<List<TurnOrderEntry>> _turnOrder = new(new List<TurnOrderEntry>());
        private readonly ReactiveProperty<int> _currentTurnIndex = new(0);
        private readonly ReactiveProperty<int> _roundNumber = new(1);

        // Public ReadOnly Properties
        public ReadOnlyReactiveProperty<List<TurnOrderEntry>> TurnOrder { get; }
        public ReadOnlyReactiveProperty<TurnOrderEntry> CurrentTurn { get; }
        public ReadOnlyReactiveProperty<int> CurrentTurnIndex { get; }
        public ReadOnlyReactiveProperty<int> RoundNumber { get; }
        public ReadOnlyReactiveProperty<int> ActiveUnitCount { get; }

        // Events
        public Subject<TurnTransition> OnTurnTransition { get; } = new();
        public Subject<RoundChangeInfo> OnRoundStarted { get; } = new();
        public Subject<Unit> OnUnitDefeated { get; } = new();

        public TurnOrderService()
        {
            // ReadOnly properties 초기화
            TurnOrder = _turnOrder.ToReadOnlyReactiveProperty();
            CurrentTurnIndex = _currentTurnIndex.ToReadOnlyReactiveProperty();
            RoundNumber = _roundNumber.ToReadOnlyReactiveProperty();

            // Computed Properties
            CurrentTurn = _turnOrder.CombineLatest(_currentTurnIndex,
                    (order, index) =>
                    {
                        if (order != null && order.Count > 0 && index >= 0 && index < order.Count)
                            return order[index];
                        return null;
                    })
                .ToReadOnlyReactiveProperty();

            ActiveUnitCount = _turnOrder
                .Select(order => order?.Count(entry => entry.IsValid) ?? 0)
                .ToReadOnlyReactiveProperty();

            // 턴 변경 감지
            CurrentTurn
                .Pairwise()
                .Subscribe(pair =>
                {
                    if (pair.Previous != null && pair.Current != null)
                    {
                        var transition = new TurnTransition(pair.Previous, pair.Current);
                        OnTurnTransition.OnNext(transition);

                        GameLogger.Debug(ZString.Format("턴 전환: {0} → {1}",
                                pair.Previous.Unit.UnitName,
                                pair.Current.Unit.UnitName),
                            LogCategory.Battle);
                    }
                })
                .AddTo(_disposables);

            // 라운드 변경 감지
            _roundNumber
                .Pairwise()
                .Where(pair => pair.Current > pair.Previous)
                .Subscribe(pair =>
                {
                    var info = new RoundChangeInfo(pair.Previous, pair.Current);
                    OnRoundStarted.OnNext(info);

                    GameLogger.Info(ZString.Format("🔄 라운드 {0} 시작!", pair.Current),
                        LogCategory.Battle);
                })
                .AddTo(_disposables);
        }

        public void Initialize(PlayerUnit player, List<EnemyUnit> enemyUnits)
        {
            var allUnits = new List<BattleUnit> { player };
            allUnits.AddRange(enemyUnits.Where(e => e != null && e.Health.IsAlive));

            CalculateTurnOrder(allUnits);
            _currentTurnIndex.Value = 0;
            _roundNumber.Value = 1;

            GameLogger.Info(ZString.Format("턴 순서 초기화 완료 - 총 {0} 유닛", allUnits.Count),
                LogCategory.Battle);
        }

        private void CalculateTurnOrder(List<BattleUnit> units)
        {
            var sortedUnits = units
                .Where(unit => unit != null && unit.Health.IsAlive)
                .OrderByDescending(unit => unit.GetBehaviourSpeed())
                .ThenBy(_ => UnityEngine.Random.value)
                .ToList();

            var entries = new List<TurnOrderEntry>();
            for (int i = 0; i < sortedUnits.Count; i++)
            {
                var unit = sortedUnits[i];
                var entry = new TurnOrderEntry(
                    unit,
                    unit.GetBehaviourSpeed(),
                    i,
                    unit is PlayerUnit
                );
                entries.Add(entry);
            }

            _turnOrder.Value = entries;
        }

        public void AdvanceToNextTurn()
        {
            if (_turnOrder.Value.Count == 0) return;

            var newIndex = _currentTurnIndex.Value + 1;

            // 라운드 끝났으면 새 라운드
            if (newIndex >= _turnOrder.Value.Count)
            {
                newIndex = 0;
                _roundNumber.Value++;
                RefreshTurnOrder(); // 죽은 유닛 제거
            }

            _currentTurnIndex.Value = newIndex;
        }

        public void RefreshTurnOrder()
        {
            var aliveEntries = _turnOrder.Value
                .Where(entry => entry.IsValid)
                .ToList();

            if (aliveEntries.Count == _turnOrder.Value.Count)
                return;

            GameLogger.Info(ZString.Format("턴 순서 갱신: {0} → {1} 유닛",
                _turnOrder.Value.Count, aliveEntries.Count));

            // 현재 유닛 유지
            var currentUnit = CurrentTurn.CurrentValue?.Unit;

            // 새 순서로 재계산
            var units = aliveEntries.Select(e => e.Unit).ToList();
            CalculateTurnOrder(units);

            // 현재 유닛의 새 인덱스 찾기
            if (currentUnit != null && currentUnit.Health.IsAlive)
            {
                var newIndex = _turnOrder.Value.FindIndex(e => e.Unit == currentUnit);
                _currentTurnIndex.Value = newIndex >= 0 ? newIndex : 0;
            }
            else
            {
                _currentTurnIndex.Value = 0;
            }
        }

        /// <summary>
        /// 패배한 유닛 제거
        /// </summary>
        public void RemoveDefeatedUnit(BattleUnit unit)
        {
            OnUnitDefeated.OnNext(Unit.Default);
            RefreshTurnOrder();
        }

        /// <summary>
        /// 현재 활성화된 적 유닛들 반환
        /// </summary>
        public List<EnemyUnit> GetActiveEnemies()
        {
            return _turnOrder.Value
                .Where(entry => entry.IsValid && !entry.IsPlayer)
                .Select(entry => entry.Unit as EnemyUnit)
                .Where(enemy => enemy != null)
                .ToList();
        }

        /// <summary>
        /// 유효한 턴이 있는지 확인
        /// </summary>
        public bool HasValidTurn()
        {
            return _turnOrder.Value.Count > 0 &&
                   _currentTurnIndex.Value < _turnOrder.Value.Count &&
                   CurrentTurn.CurrentValue?.IsValid == true;
        }

        public void Dispose()
        {
            _turnOrder?.Dispose();
            _currentTurnIndex?.Dispose();
            _roundNumber?.Dispose();

            CurrentTurn?.Dispose();
            ActiveUnitCount?.Dispose();

            OnTurnTransition?.Dispose();
            OnRoundStarted?.Dispose();
            OnUnitDefeated?.Dispose();

            _disposables?.Dispose();
        }
    }

    // Event Data Structures
    public readonly struct TurnTransition
    {
        public TurnOrderEntry Previous { get; }
        public TurnOrderEntry Current { get; }

        public TurnTransition(TurnOrderEntry previous, TurnOrderEntry current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct RoundChangeInfo
    {
        public int PreviousRound { get; }
        public int NewRound { get; }

        public RoundChangeInfo(int previousRound, int newRound)
        {
            PreviousRound = previousRound;
            NewRound = newRound;
        }
    }

    [Serializable]
    public class TurnOrderEntry
    {
        public BattleUnit Unit { get; }
        public int Speed { get; }
        public int OrderIndex { get; }
        public bool IsPlayer { get; }

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
