using System;
using Gameplay.BattleSystem.Enum;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface IShieldComponent
    {
        int CurrentShield { get; }
        int MaxShield { get; }
        UnitState CurrentState { get; }
        bool IsBroken { get; }
        int BreakTurnsRemaining { get; }
        float BreakDamageMultiplier { get; }

        event Action<int, int> OnShieldChanged;
        event Action OnUnitBroken;
        event Action OnUnitRecovered;
        event Action<int> OnShieldDamaged;

        void Initialize(int maxShield, int breakDuration, float breakDamageMultipler);
        void DamageShield(int amount);
        void ProcessBreakTurn();
        void ForceRecover();
    }
}
