using System.Collections.Generic;
using Cysharp.Text;
using Data.WeaponSystem.Enums;
using Debugging;
using UnityEngine;

namespace Data.WeaponSystem
{
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Gameplay/WeaponSystem/WeaponDatabase")]
    public class WeaponDatabase : ScriptableObject
    {
        [SerializeField] private WeaponData[] _weapons;
        [SerializeField] private WeaponData _defaultWeaponData;

        public WeaponData Default => _defaultWeaponData;

        public WeaponData GetWeaponData(WeaponType weaponType)
        {
            foreach (var weapon in _weapons)
            {
                if (weapon.WeaponType == weaponType)
                {
                    return weapon;
                }
            }

            GameLogger.Warning(ZString.Format("WeaponData for {0} not found", weaponType));
            return null;
        }

        public WeaponData[] GetAllWeapons()
        {
            return _weapons;
        }

        private void OnValidate()
        {
            var weaponTypes = new HashSet<WeaponType>();
            foreach (var weapon in _weapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                if (weaponTypes.Contains(weapon.WeaponType))
                {
                    GameLogger.Error(ZString.Format("Duplicate weapon type found: {0}", weapon.WeaponType));
                }

                weaponTypes.Add(weapon.WeaponType);
            }
        }
    }
}
