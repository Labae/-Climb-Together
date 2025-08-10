using System;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface IHealthComponent
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsAlive { get; }

        event Action<int, int> OnHealthChanged;
        event Action OnUnitDefeated;

        void Initialize(int maxHealth);
        void TakeDamage(int damage);
        void Heal(int amount);
    }
}
