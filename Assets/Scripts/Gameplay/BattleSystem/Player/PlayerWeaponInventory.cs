using System;
using System.Collections.Generic;
using Cysharp.Text;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using UnityEngine;

namespace Gameplay.BattleSystem.Player
{
    /// <summary>
    /// 플레이어가 소유한 무기들을 관리하는 시스템
    /// </summary>
    [Serializable]
    public class PlayerWeaponInventory
    {
        [SerializeField] private List<WeaponData> _ownedWeapons = new();

        private WeaponDatabase _weaponDatabase;

        // 속성
        public int WeaponCount => _ownedWeapons.Count;
        public IReadOnlyList<WeaponData> OwnedWeapons => _ownedWeapons;

        // 이벤트
        public event Action<WeaponData> OnWeaponAdded;
        public event Action<WeaponData> OnWeaponRemoved;
        public event Action OnInventoryChanged;

        public void Initialize(WeaponDatabase weaponDatabase, WeaponData[] startingWeapons)
        {
            _weaponDatabase = weaponDatabase;

            if (startingWeapons is { Length: > 0 })
            {
                _ownedWeapons.Clear();
                foreach (var weapon in startingWeapons)
                {
                    _ownedWeapons.Add(weapon);
                }
            }
            else
            {
                _ownedWeapons.Clear();
                _ownedWeapons.Add(weaponDatabase.Default);
            }

            GameLogger.Info(ZString.Format("플레이어 무기 인벤토리 초기화: {0}", ZString.Join(", ", _ownedWeapons)),
                LogCategory.Battle);
        }

        public bool HasWeapon(WeaponData weapon)
        {
            return _ownedWeapons.Contains(weapon);
        }

        public void AddWeapon(WeaponData weapon)
        {
            if (HasWeapon(weapon))
            {
                return;
            }

            _ownedWeapons.Add(weapon);
            OnWeaponAdded?.Invoke(weapon);
            OnInventoryChanged?.Invoke();
            GameLogger.Info(ZString.Format("새 무기 획득: {0}", weapon), LogCategory.Battle);
        }

        public bool RemoveWeapon(WeaponData weapon)
        {
            if (_ownedWeapons.Count <= 1)
            {
                GameLogger.Warning("최소 하나의 무기는 보유해야합니다!", LogCategory.Battle);
                return false;
            }

            if (_ownedWeapons.Remove(weapon))
            {
                OnWeaponRemoved?.Invoke(weapon);
                OnInventoryChanged?.Invoke();
                GameLogger.Info(ZString.Format("무기 제거: {0}", weapon), LogCategory.Battle);
                return true;
            }

            return false;
        }
    }
}
