using System;
using Data.BattleSystem.Combat;
using R3;

namespace Gameplay.BattleSystem.Components
{
    public class ReactiveHealthComponent : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        // Properties
        public ReactiveProperty<HealthData> Health { get; } = new();

        // Events
        public Subject<HealthData> OnHealthChanged { get; } = new();
        public Subject<DamageData> OnDamageTaken { get; } = new();
        public Subject<HealData> OnHealed { get; } = new();
        public Subject<Unit> OnUnitDefeated { get; } = new();

        public Observable<float> HealthPercentage => Health.Select(h => h.Percentage);
        public Observable<bool>  IsAlive => Health.Select(h => h.IsAlive);
        public Observable<bool> IsLowHealth => Health.Select(h => h.IsLowHealth);
        public Observable<bool> IsCriticalHealth => Health.Select(h => !h.IsCriticalHealth);

        public void Initialize(int maxHealth)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentException("Maximum health cannot be less than 0");
            }

            var initialData = new HealthData(maxHealth, maxHealth);
            Health.Value = initialData;

            // 체력 변화 시 이벤트 발생
            Health
                .Subscribe(data => OnHealthChanged.OnNext(data))
                .AddTo(_disposables);

            // 사망 시 이벤트 발생 (체력 0이 되었을 때)
            Health
                .Pairwise()
                .Where(pair => pair.Previous.IsAlive && !pair.Current.IsAlive)
                .Subscribe(_ => OnUnitDefeated.OnNext(Unit.Default))
                .AddTo(_disposables);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !Health.Value.IsAlive)
            {
                return;
            }

            var currentData = Health.Value;
            var newData = currentData.TakeDamage(damage);

            if (newData == currentData)
            {
                return;
            }

            Health.Value = newData;
            var actualDamage = currentData.Current - newData.Current;
            var damageData = new DamageData(actualDamage, DamageType.Health);
            OnDamageTaken.OnNext(damageData);
        }

        public void TakeDamage(int damage, bool isCritical, bool isWeakness)
        {
            if (damage <= 0 || !Health.Value.IsAlive)
            {
                return;
            }

            var currentData = Health.Value;
            var newData = currentData.TakeDamage(damage);

            if (newData == currentData)
            {
                return;
            }

            Health.Value = newData;
            var actualDamage = currentData.Current - newData.Current;
            var damageData = new DamageData(actualDamage, DamageType.Health, isCritical, isWeakness);
            OnDamageTaken.OnNext(damageData);
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !Health.Value.IsAlive)
            {
                return;
            }

            var currentData = Health.Value;
            var newData = currentData.Heal(amount);

            if (newData == currentData)
            {
                return;
            }

            Health.Value = newData;
            var actualHeal = newData.Current - currentData.Current;
            var healData = new HealData(actualHeal);
            OnHealed.OnNext(healData);
        }

        public void Heal(int amount, HealType healType)
        {
            if (amount <= 0 || !Health.Value.IsAlive)
            {
                return;
            }

            var currentData = Health.Value;
            var newData = currentData.Heal(amount);

            if (newData == currentData)
            {
                return;
            }

            Health.Value = newData;
            var actualHeal = newData.Current - newData.Current;
            var healData = new HealData(actualHeal, healType);
            OnHealed.OnNext(healData);
        }

        public void SetMaxHealth(int newMaxHealth)
        {
            if (newMaxHealth <= 0)
            {
                return;
            }

            var currentData = Health.Value;
            var newData = currentData.SetMaxHealth(newMaxHealth);
            Health.Value = newData;
        }

        public void Dispose()
        {
            Health?.Dispose();

            OnHealthChanged?.Dispose();
            OnDamageTaken?.Dispose();
            OnHealed?.Dispose();
            OnUnitDefeated?.Dispose();

            _disposables?.Dispose();
        }

        public override string ToString()
        {
            return Health.Value.ToString();
        }
    }
}
