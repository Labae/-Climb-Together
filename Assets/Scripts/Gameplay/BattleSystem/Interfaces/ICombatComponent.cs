using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Systems.EventBus;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface ICombatComponent
    {
        BattleStats Stats { get; }

        void Initialize(BattleStats stats, string unitName, IEventBus eventBus);
        int CalculateDamage(BattleUnit target, WeaponType weaponType);
        void ExecuteAttack(BattleUnit attacker, BattleUnit target, WeaponType weaponType);
    }
}
