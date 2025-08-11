using Data.BattleSystem.Configs.Core;
using Data.WeaponSystem;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Core.Services;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface ICombatComponent
    {
        BattleStats Stats { get; }

        void Initialize(BattleStats stats);
        DamageResult CalculateDamage(BattleUnit target, WeaponData weaponData);
    }
}
