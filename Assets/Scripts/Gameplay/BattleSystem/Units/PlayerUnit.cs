using System;
using Data.BattleSystem.Configs.Player;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Player;
using NaughtyAttributes;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.Units
{
    public class PlayerUnit : BattleUnit
    {
        [SerializeField, ReadOnly]
        private PlayerWeaponInventory _weaponInventory;

        public PlayerWeaponInventory WeaponInventory => _weaponInventory;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _weaponInventory = new();
        }

        protected override void InitializeWeaponSystem()
        {
            base.InitializeWeaponSystem();
            if (_unitConfig is PlayerUnitConfig playerUnitConfig)
            {
                _weaponInventory.Initialize(_weaponDatabase, playerUnitConfig.StartingWeapons);

                if (_weaponInventory.WeaponCount > 0)
                {
                    SetEquippedWeapon(_weaponInventory.OwnedWeapons[0]);
                }
            }
            else
            {
                GameLogger.Error("UnitConfig is not PlayerUnitConfig", LogCategory.Battle);
            }
        }
    }
}
