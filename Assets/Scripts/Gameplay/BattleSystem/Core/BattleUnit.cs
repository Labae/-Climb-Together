using System;
using Cysharp.Text;
using Debugging;
using NaughtyAttributes;
using UnityEngine;

namespace Gameplay.BattleSystem.Core
{
    public class BattleUnit : MonoBehaviour
    {
        [Header("Unit Data")] [SerializeField] private string _unitName = "Unit_";
        [SerializeField] private BattleStats _stats;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [SerializeField, ReadOnly] private int _currentHealth;

        public event Action<int, int> OnHealthChanged;
        public event Action<BattleUnit> OnUnitDefeated;

        public string UnitName => _unitName;
        public bool IsAlive => _currentHealth > 0;
        public BattleStats Stats => _stats;

        private void Awake()
        {
            InitializeUnit();
        }

        private void InitializeUnit()
        {
            _currentHealth = _stats.MaxHealth;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void AttackTarget(BattleUnit targetUnit)
        {
            if (!IsAlive || targetUnit == null || !targetUnit.IsAlive)
            {
                return;
            }

            int damage = CalculateDamage(targetUnit);
            targetUnit.TakeDamage(damage);

            GameLogger.Debug(ZString.Format("{0}이(가) {1}에게 {2} 데미지를 입혔습니다!", _unitName, targetUnit.UnitName, damage));
        }

        private int CalculateDamage(BattleUnit targetUnit)
        {
            int baseDamage = _stats.Attack - (targetUnit.Stats.Defense / 2);
            int randomBonus = UnityEngine.Random.Range(1, 6);
            int finalDamage = Mathf.Max(1, baseDamage + randomBonus);

            return finalDamage;
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
