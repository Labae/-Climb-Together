using System;
using Cysharp.Text;
using Data.BattleSystem.Combat;
using Debugging;
using Gameplay.BattleSystem.Components;
using Gameplay.BattleSystem.Core;
using R3;

namespace Gameplay.BattleSystem.EnemyStatus
{
    /// <summary>
    /// Enemy의 UI용 데이터 제공
    /// </summary>
    public class EnemyStatusModel : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public ReactiveHealthComponent HealthComponent { get; private set; }
        public ReactiveShieldComponent ShieldComponent { get; private set; }

        public ReactiveProperty<string> UnitName { get; } = new();

        public bool IsInitialized => HealthComponent != null && ShieldComponent != null;

        public void Initialize(BattleUnit unit)
        {
            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            HealthComponent = unit.ReactiveHealth;
            ShieldComponent = unit.ReactiveShield;

            if (HealthComponent == null || ShieldComponent == null)
            {
                throw new InvalidOperationException(ZString.Format(
                    "BattleUnit {0}에 Health & Shield 컴포넌트가 없습니다. BattleUnit.Initialize 먼저 호출하세요.", unit.UnitName));
            }

            UnitName.Value = unit.UnitName;
            GameLogger.Info(ZString.Format("EnemyStatusModel 초기화 완료: {0}", UnitName.Value));
        }

        public HealthData GetCurrentHealth()
        {
            return HealthComponent?.Health.Value ??  new HealthData(0, 1);
        }

        public ShieldData GetCurrentShield()
        {
            return ShieldComponent?.Shield.Value ??  new ShieldData(0, 0);
        }

        public void Dispose()
        {
            UnitName?.Dispose();
            _disposables?.Dispose();
        }

        public override string ToString()
        {
            if (IsInitialized)
            {
                return ZString.Format("EnemyStatusModel: {0} - {1} - {2}", UnitName.Value, GetCurrentHealth(), GetCurrentShield());
            }

            return "EnemyStatusModel: Not Initialized";
        }
    }
}
