using System;
using Cysharp.Text;
using Gameplay.BattleSystem.Interfaces;
using UnityEngine;

namespace Gameplay.BattleSystem.Components
{
    public class HealthComponent : IHealthComponent
    {
        private int _currentHealth;
        private int _maxHealth;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0;

        public event Action<int, int> OnHealthChanged;
        public event Action OnUnitDefeated;

        public void Initialize(int maxHealth)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentException("MaxHealth must be greater than 0");
            }

            _maxHealth = maxHealth;
            _currentHealth = maxHealth;

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive || damage <= 0)
            {
                return;
            }

            int previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - damage);

            if (_currentHealth == previousHealth)
            {
                return;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            if (!IsAlive)
            {
                OnUnitDefeated?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            int previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

            if (_currentHealth == previousHealth)
            {
                return;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public override string ToString()
        {
            return ZString.Format("Health : {0}/{1} (Alive: {2})", _currentHealth, _maxHealth, IsAlive);
        }
    }
}
