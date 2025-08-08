using System;
using System.Linq;
using Cysharp.Text;
using Debugging;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Events;
using NaughtyAttributes;
using Systems.EventBus;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.Core
{
    public abstract class BattleUnit : MonoBehaviour
    {
        [Header("Unit Data")] [SerializeField] private string _unitName = "Unit_";
        [SerializeField] private BattleStats _stats;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Weakness System")] [SerializeField]
        private WeaponType[] _weaknesses;

        [SerializeField, ReadOnly] private int _currentHealth;

        [Inject] protected IEventBus _eventBus;

        public event Action<int, int> OnHealthChanged;
        public event Action<BattleUnit> OnUnitDefeated;

        public string UnitName => _unitName;
        public bool IsAlive => _currentHealth > 0;
        public BattleStats Stats => _stats;
        public WeaponType[] Weaknesses => _weaknesses;

        private void Awake()
        {
            InitializeUnit();
        }

        private void InitializeUnit()
        {
            _currentHealth = _stats.MaxHealth;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void AttackTarget(BattleUnit targetUnit, WeaponType weaponType)
        {
            if (!IsAlive || targetUnit == null || !targetUnit.IsAlive)
            {
                return;
            }

            int damage = CalculateDamage(targetUnit, weaponType);
            bool isWeaknessHit = targetUnit.IsWeaknessHit(weaponType);
            targetUnit.TakeDamage(damage);

            // 공격 이벤트 발행
            GameLogger.Debug(ZString.Format("{0}이(가) {1}에게 {2}로 {3} 데미지를 입혔습니다! ({4})",
                _unitName, targetUnit.UnitName, weaponType, damage, isWeaknessHit ? "약점 공격" : "일반 공격"));

            _eventBus.Publish(new UnitAttackedEvent(this, targetUnit));
        }

        private int CalculateDamage(BattleUnit targetUnit, WeaponType weaponType)
        {
            int baseDamage = _stats.Attack - (targetUnit.Stats.Defense / 2);
            var bonus = 0;
            if (targetUnit.IsWeaknessHit(weaponType))
            {
                var weaknessBonus = Mathf.RoundToInt(baseDamage * 1.5f);
                bonus += weaknessBonus;
            }

            int finalDamage = Mathf.Max(1, baseDamage + bonus);
            return finalDamage;
        }

        public bool IsWeaknessHit(WeaponType weaponType)
        {
            return _weaknesses.Any(weakness => weakness == weaponType);
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive)
            {
                return;
            }

            int actualDamage = Mathf.Max(1, damage);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);

            OnHealthChanged?.Invoke(_currentHealth, _stats.MaxHealth);

            if (!IsAlive)
            {
                OnUnitDefeated?.Invoke(this);
            }
        }
    }
}
