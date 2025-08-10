using System.Linq;
using Data.BattleSystem.Enums;
using UnityEngine;

namespace Data.BattleSystem.Configs
{
    [CreateAssetMenu(fileName = "New UnitConfig", menuName = "Gameplay/Battle System/Configs/UnitConfig")]
    public class UnitConfig : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string _unitName = "New Unit";
        [SerializeField] private Sprite _unitIcon;
        [SerializeField] [TextArea(2, 4)] private string _description;

        [Header("Stats")] [SerializeField] private BattleStats _stats;

        [Header("Weakness System")] [SerializeField]
        private WeaponType[] _weaknesses;

        [Header("Shield System")] [SerializeField]
        private int _maxShield = 3;

        [SerializeField] private int _breakDuration = 2;
        [SerializeField] [Range(1f, 5f)] private float _breakDamageMultiplier = 2f;

        public string UnitName => _unitName;
        public Sprite UnitIcon => _unitIcon;
        public string Description => _description;
        public BattleStats Stats => _stats;
        public WeaponType[]  Weaknesses => _weaknesses;
        public int MaxShield => _maxShield;
        public int BreakDuration => _breakDuration;
        public float BreakDamageMultiplier => _breakDamageMultiplier;

        public bool HasWeakness(WeaponType weaponType)
        {
            if (_weaknesses == null || _weaknesses.Length == 0)
            {
                return false;
            }

            return _weaknesses.Any(weakness => weakness == weaponType);
        }
    }
}
