using System;
using Cysharp.Text;
using Data.BattleSystem.Combat;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using R3;
using UnityEngine;

namespace Gameplay.BattleSystem.Components
{
    public class ReactiveShieldComponent : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private int _breakDuration;

        // Properties
        public ReactiveProperty<ShieldData> Shield { get; } = new();
        public ReactiveProperty<UnitState> CurrentState { get; } = new();
        public ReactiveProperty<int> BreakTurnRemaining { get; } = new();
        public ReactiveProperty<float> BreakDamageMultiplier { get; } = new();

        // Events
        public Subject<ShieldData> OnShieldChanged { get; } = new();
        public Subject<DamageData> OnShieldDamaged { get; } = new();
        public Subject<Unit> OnUnitBroken { get; } = new();
        public Subject<Unit> OnUnitRecovered { get; } = new();

        public Observable<bool> HasShield => Shield.Select(s => s.HasShield);
        public Observable<bool> IsBroken => CurrentState.Select(state => state == UnitState.Broken);
        public Observable<float> ShieldPercentage => Shield.Select(s => s.Percentage);

        public void Initialize(int maxShield, int breakDuration, float breakDamageMultiplier)
        {
            if (maxShield < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxShield), maxShield, null);
            }

            if (breakDuration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(breakDuration), breakDuration, null);
            }

            var initialData = new ShieldData(maxShield, maxShield);
            Shield.Value = initialData;
            _breakDuration = breakDuration;
            BreakDamageMultiplier.Value = breakDamageMultiplier;
            CurrentState.Value = UnitState.Normal;
            BreakTurnRemaining.Value = 0;

            // 실드 변화 이벤트
            Shield
                .Subscribe(data => OnShieldChanged.OnNext(data))
                .AddTo(_disposables);

            // 실드가 0이 되면 브레이크 상태
            Shield
                .Where(data => data.IsEmpty && CurrentState.Value == UnitState.Normal)
                .Subscribe(_ => EnterBreakState())
                .AddTo(_disposables);

            // 브레이크 상태 이벤트
            CurrentState
                .Where(state => state == UnitState.Broken)
                .Subscribe(_ => OnUnitBroken.OnNext(Unit.Default))
                .AddTo(_disposables);

            // 브레이크 회복 이벤트
            CurrentState
                .Pairwise()
                .Where(pair => pair is { Previous: UnitState.Broken, Current: UnitState.Normal })
                .Subscribe(pair => OnUnitRecovered.OnNext(Unit.Default))
                .AddTo(_disposables);
        }

        public void DamageShield(int amount)
        {
            if (amount <= 0 || Shield.Value.Max == 0)
            {
                return;
            }

            var currentData = Shield.Value;
            var newData = currentData.TakeDamage(amount);

            if (newData == currentData)
            {
                return;
            }

            Shield.Value = newData;
            var actualDamage = currentData.Current - newData.Current;
            var damageData = new DamageData(actualDamage, DamageType.Shield);
            OnShieldDamaged.OnNext(damageData);
        }

        public void DamageShield(int amount, bool isWeaknessHit)
        {
            if (amount <= 0 || Shield.Value.Max == 0)
            {
                return;
            }

            var currentData = Shield.Value;
            var newData = currentData.TakeDamage(amount);

            if (newData == currentData)
            {
                return;
            }

            Shield.Value = newData;
            var actualDamage = currentData.Current - newData.Current;
            var damageData = new DamageData(actualDamage, DamageType.Shield, false, isWeaknessHit);
            OnShieldDamaged.OnNext(damageData);
        }

        public void ProcessBreakTurn()
        {
            if (CurrentState.Value != UnitState.Broken)
            {
                return;
            }

            BreakTurnRemaining.Value = Mathf.Max(0, BreakTurnRemaining.Value - 1);
            GameLogger.Info(ZString.Format("ProcessBreakTurn! 남은 브레이크 턴: {0}", BreakTurnRemaining.Value)
                , LogCategory.Battle);

            if (BreakTurnRemaining.Value <= 0)
            {
                RecoverFromBreak();
            }
        }

        public void ForceRecover()
        {
            if (CurrentState.Value != UnitState.Broken)
            {
                return;
            }

            RecoverFromBreak();
        }

        private void EnterBreakState()
        {
            CurrentState.Value = UnitState.Broken;
            BreakTurnRemaining.Value = _breakDuration;

            GameLogger.Info(ZString.Format("Unit Broken State! {0}턴간 무력화", _breakDuration), LogCategory.Battle);
        }

        private void RecoverFromBreak()
        {
            CurrentState.Value = UnitState.Normal;
            Shield.Value = Shield.Value.Restore();
            BreakTurnRemaining.Value = 0;

            GameLogger.Info("Recover From Break! 실드 복구 완료", LogCategory.Battle);
        }

        public void Dispose()
        {
            Shield?.Dispose();
            CurrentState?.Dispose();
            BreakTurnRemaining?.Dispose();
            BreakDamageMultiplier?.Dispose();

            OnShieldChanged.Dispose();
            OnShieldDamaged.Dispose();
            OnUnitBroken.Dispose();
            OnUnitRecovered.Dispose();

            _disposables.Dispose();
        }

        public override string ToString()
        {
            return ZString.Format("Shield: {0}, State: {1}, Break Turns: {2}",
                Shield.Value, CurrentState.Value, BreakTurnRemaining.Value);
        }
    }
}
