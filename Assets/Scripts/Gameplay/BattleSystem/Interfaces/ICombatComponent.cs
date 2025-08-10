using Data.BattleSystem.Configs;
using Data.BattleSystem.Enums;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Systems.EventBus;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface ICombatComponent
    {
        BattleStats Stats { get; }

        void Initialize(BattleStats stats);
        int CalculateDamage(BattleUnit target, WeaponType weaponType);
    }
}
