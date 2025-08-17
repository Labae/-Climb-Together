using System;
using Cysharp.Text;
using Data.BattleSystem.Combat;
using Debugging;
using Gameplay.BattleSystem.Components;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.UI.Base;
using R3;

namespace Gameplay.BattleSystem.EnemyStatus
{
    /// <summary>
    /// Enemy의 UI용 데이터 제공
    /// </summary>
    public class EnemyStatusModel : BaseModel
    {
        private readonly BattleUnit _unit;

        public ReactiveHealthComponent HealthComponent { get; private set; }
        public ReactiveShieldComponent ShieldComponent { get; private set; }

        public ReactiveProperty<string> UnitName { get; } = new();

        public EnemyStatusModel(BattleUnit unit)
        {
            _unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Initialize();
        }

        private void Initialize()
        {
            if (_unit.ReactiveHealth == null || _unit.ReactiveShield == null)
            {
                throw new InvalidOperationException(ZString.Format(
                    "BattleUnit {0}에 Health & Shield 컴포넌트가 없습니다. BattleUnit.Initialize 먼저 호출하세요.", _unit.UnitName));
            }

            HealthComponent = _unit.ReactiveHealth;
            ShieldComponent = _unit.ReactiveShield;
            UnitName.Value = _unit.UnitName;

            SetInitialized();
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

        public override void Dispose()
        {
            UnitName?.Dispose();
            base.Dispose();
        }

        public override string ToString()
        {
            if (IsInitialized.Value)
            {
                return ZString.Format("EnemyStatusModel: {0} - {1} - {2}", UnitName.Value, GetCurrentHealth(), GetCurrentShield());
            }

            return "EnemyStatusModel: Not Initialized";
        }
    }
}
