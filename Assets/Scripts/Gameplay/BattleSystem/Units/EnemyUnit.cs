using Data.BattleSystem.Configs.Enemies;
using Gameplay.BattleSystem.Core;

namespace Gameplay.BattleSystem.Units
{
    public class EnemyUnit : BattleUnit
    {
        protected override void InitializeWeaponSystem()
        {
            base.InitializeWeaponSystem();
            if (_unitConfig is EnemyUnitConfig enemyUnitConfig)
            {
                SetEquippedWeapon(enemyUnitConfig.EquippedWeapon);
            }
        }
    }
}
