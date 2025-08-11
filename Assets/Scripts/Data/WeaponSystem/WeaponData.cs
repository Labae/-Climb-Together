using Data.WeaponSystem.Enums;
using NaughtyAttributes;
using UnityEngine;

namespace Data.WeaponSystem
{
    [CreateAssetMenu(fileName = "New Weapon Data", menuName = "Gameplay/WeaponSystem/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField]
        private WeaponType _weaponType;

        [SerializeField] private string _weaponName;
        [SerializeField, Required] private Sprite _weaponIcon;

        [SerializeField]
        [TextArea(2, 4)]
        private string _description;

        [Header("Damage Settings")]
        [SerializeField]
        [Range(0, 50)]
        private int _flatDamageBonus;

        [Header("Speed Settings")]
        [SerializeField]
        [Range(-10, 10)]
        private int _speedModifier = 0;

        [Header("Special Effects")]
        [SerializeField]
        private bool _canIgnoreArmor = false;

        [SerializeField]
        [Range(0f, 1f)]
        private float _armorPenetration = 0f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _criticalChance = 0f;

        [SerializeField]
        [Range(1f, 3f)]
        private float _criticalMultiplier = 2f;

        [Header("Shield Interaction")]
        [Range(1, 5)]
        [SerializeField] private int shieldDamage;

        public WeaponType WeaponType => _weaponType;
        public string WeaponName => _weaponName;
        public string Description => _description;
        public int FlatDamageBonus => _flatDamageBonus;
        public int SpeedModifier => _speedModifier;
        public bool CanIgnoreArmor => _canIgnoreArmor;
        public float ArmorPenetration => _armorPenetration;
        public float CriticalChance => _criticalChance;
        public float CriticalMultiplier => _criticalMultiplier;
        public int ShieldDamage => shieldDamage;
        public Sprite WeaponIcon => _weaponIcon;

        public int CalculateBaseDamage(int attackerDamage, int defenderArmor)
        {
            int baseDamage = attackerDamage + _flatDamageBonus;

            int defense = _canIgnoreArmor ? 0 : Mathf.RoundToInt(defenderArmor * (1f - _armorPenetration));
            baseDamage -= defense / 2;

            return baseDamage;
        }

        public bool RollCritical()
        {
            return Random.value <= _criticalChance;
        }

        public int ApplyCritical(int damage)
        {
            return Mathf.RoundToInt(damage * _criticalMultiplier);
        }
    }
}
