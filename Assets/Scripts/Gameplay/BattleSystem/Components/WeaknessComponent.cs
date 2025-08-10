using System;
using System.Linq;
using Cysharp.Text;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Interfaces;

namespace Gameplay.BattleSystem.Components
{
    public class WeaknessComponent : IWeaknessComponent
    {
        private WeaponType[] _weaknesses;

        public WeaponType[] Weaknesses => _weaknesses ?? Array.Empty<WeaponType>();
        public bool HasWeakness => _weaknesses is { Length: > 0 };

        public void Initialize(WeaponType[] weaknesses)
        {
            _weaknesses = weaknesses ?? Array.Empty<WeaponType>();
        }

        public bool IsWeaknessHit(WeaponType weaponType)
        {
            if (!HasWeakness)
            {
                return false;
            }

            return _weaknesses.Any(weakness => weakness == weaponType);
        }

        public override string ToString()
        {
            return ZString.Format("Weakness: {0}", HasWeakness ? string.Join(", ", _weaknesses) : "None");
        }
    }
}
