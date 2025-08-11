using Data.BattleSystem.Configs.Core;
using Data.WeaponSystem;
using NaughtyAttributes;
using UnityEngine;

namespace Data.BattleSystem.Configs.Enemies
{
    [CreateAssetMenu(fileName = "New EnemyUnitConfig", menuName = "Gameplay/Battle System/Configs/EnemyUnitConfig")]
    public class EnemyUnitConfig : UnitConfig
    {
        [Header("Weapon System")] [SerializeField, Required]
        private WeaponData _equippedWeaponType;

        public WeaponData EquippedWeapon => _equippedWeaponType;
    }
}
