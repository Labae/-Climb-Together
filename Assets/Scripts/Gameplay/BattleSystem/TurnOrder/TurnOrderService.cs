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
        private readonly CompositeDisposable _disposable = new();

        private ReactiveProperty<List<TurnOrderEntry>> _turnOrder { get; } = new(new List<TurnOrderEntry>());
        private ReactiveProperty<int> _currentTurnIndex { get; } = new(0);
        private ReactiveProperty<int> _roundNumber { get; } = new(1);

        public ReadOnlyReactiveProperty<List<TurnOrderEntry>> TurnOrder => _turnOrder.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<TurnOrderEntry> CurrentTurn { get; }
        public ReadOnlyReactiveProperty<int> CurrentTurnIndex => _currentTurnIndex.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<int> RoundNumber => _roundNumber.ToReadOnlyReactiveProperty();

        public ReadOnlyReactiveProperty<int> ActiveUnitCount { get; }
        public ReadOnlyReactiveProperty<bool> IsPlayerTurn { get; }
        public ReadOnlyReactiveProperty<bool> IsEnemyTurn { get; }

        public Subject<TurnTransition> OnTurnTransition { get; } = new();
        public Subject<RoundChangeInfo> OnRoundStarted { get; } = new();
        public Subject<Unit> OnUnitDefeated { get; } = new();

        public TurnOrderService()
        {
            CurrentTurn = _turnOrder.CombineLatest(_currentTurnIndex,
                (order, index) =>
                {
                    if (order is { Count: > 0 } && index >= 0 && index < order.Count)
                    {
                        return order[index];
                    }

                    return null;
                }
            ).ToReadOnlyReactiveProperty();

            ActiveUnitCount = _turnOrder
                .Select(order => order?.Count(entry => entry.IsValid) ?? 0)
                .ToReadOnlyReactiveProperty();

            IsPlayerTurn = CurrentTurn
                .Select(turn => turn is { IsPlayer: false })
                .ToReadOnlyReactiveProperty();

            IsEnemyTurn = CurrentTurn
                .Select(turn => turn?.IsPlayer ?? false)
                .ToReadOnlyReactiveProperty();

            CurrentTurn
                .Pairwise()
                .Subscribe(pair =>
                {
                    if (pair.Previous != null && pair.Current != null)
                    {
                        var transition = new TurnTransition(pair.Previous, pair.Current);
                        OnTurnTransition.OnNext(transition);
                    }
                })
                .AddTo(_disposable);

            _roundNumber
                .Pairwise()
                .Where(pair => pair.Current > pair.Previous)
                .Subscribe(pair =>
                {
                    var info = new RoundChangeInfo(pair.Previous, pair.Current);
                    OnRoundStarted.OnNext(info);
                }).AddTo(_disposable);
        }

        public void Initialize(PlayerUnit player, List<EnemyUnit> enemyUnits)
        {
            var allUnits = new List<BattleUnit> { player };
            allUnits.AddRange(enemyUnits.Where(e => e != null && e.Health.IsAlive));

            CalculateTurnOrder(allUnits);
            _currentTurnIndex.Value = 0;
            _roundNumber.Value = 1;

            GameLogger.Info(ZString.Format("턴 순서 초기화 완료 - 총 {0} 유닛", allUnits.Count), LogCategory.Battle);
        }

        private void CalculateTurnOrder(List<BattleUnit> units)
        {
            // sort
            var sortedUnits =
                units.Where(unit => unit != null && unit.Health.IsAlive)
                    .OrderByDescending(unit => unit.GetBehaviourSpeed())
                    .ThenBy(unit => UnityEngine.Random.value)
                    .ToList();

            var entries = new  List<TurnOrderEntry>();
            for (var i = 0; i < sortedUnits.Count; i++)
            {
                var unit = sortedUnits[i];
                var entry = new TurnOrderEntry(unit, unit.GetBehaviourSpeed(), i, unit is PlayerUnit);
                entries.Add(entry);
            }

            _turnOrder.OnNext(entries);
        }

        public void AdvanceToNextTurn()
        {
            if (_turnOrder.Value.Count == 0)
            {
                return;
            }

            var newIndex = _currentTurnIndex.Value + 1;

            if (newIndex >= _turnOrder.Value.Count)
            {
                newIndex = 0;
                _roundNumber.Value++;
                RefreshTurnOrder();
            }

            _currentTurnIndex.OnNext(newIndex);
        }

        public void RefreshTurnOrder()
        {
            var aliveEntries = _turnOrder.Value
                .Where(entry => entry is { IsValid: true })
                .ToList();

            if (aliveEntries.Count == _turnOrder.Value.Count)
            {
                return;
            }

            GameLogger.Info(ZString.Format("턴 순서 갱신: {0} -> {1} 유닛", _turnOrder.Value.Count, aliveEntries.Count));

            var currentUnit = CurrentTurn.CurrentValue?.Unit;
            var units = aliveEntries.Select(e => e.Unit).ToList();

            CalculateTurnOrder(units);

            if (currentUnit != null && currentUnit.Health.IsAlive)
            {
                var newIndex = _turnOrder.Value.FindIndex(entry => entry.Unit == currentUnit);
                _currentTurnIndex.OnNext(newIndex >= 0 ? newIndex : 0);
            }
            else
            {
                _currentTurnIndex.OnNext(0);
            }
        }

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

            _disposable?.Dispose();

            CurrentTurn?.Dispose();
            ActiveUnitCount?.Dispose();
            IsPlayerTurn?.Dispose();
            IsEnemyTurn?.Dispose();

            OnTurnTransition?.Dispose();
            OnRoundStarted?.Dispose();
            OnUnitDefeated?.Dispose();
        }
    }

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
