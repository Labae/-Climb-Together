using Data.WeaponSystem.Enums;
using Gameplay.BattleSystem.Enum;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface IWeaknessComponent
    {
        WeaponType[] Weaknesses { get; }
        bool HasWeakness { get; }

        void Initialize(WeaponType[] weaknesses);
        bool IsWeaknessHit(WeaponType weaponType);
    }
}
