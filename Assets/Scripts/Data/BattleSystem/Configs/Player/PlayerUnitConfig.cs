using System;
using Data.BattleSystem.Configs.Core;
using Data.WeaponSystem;
using Data.WeaponSystem.Enums;
using UnityEngine;

namespace Data.BattleSystem.Configs.Player
{
    [CreateAssetMenu(fileName = "New PlayerUnitConfig",
        menuName = "Gameplay/Battle System/Configs/PlayerUnitConfig")]
    public class PlayerUnitConfig : UnitConfig
    {
        [Header("Weapon System")] [SerializeField]
        private WeaponData[] startingWeapons = Array.Empty<WeaponData>();

        public WeaponData[] StartingWeapons => startingWeapons;
    }
}
