using System;
using System.Collections.Generic;
using Data.WeaponSystem;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Units;

namespace Gameplay.BattleSystem.Events
{
    /// <summary>
    /// 모든 전투 이벤트의 기본 클래스
    /// </summary>
    public abstract class BattleEventBase
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public abstract string EventName { get; }
    }

    public class BattleStartedEvent : BattleEventBase
    {
        public override string EventName => nameof(BattleStartedEvent);
        public BattleUnit Player { get; }
        public BattleUnit[] Enemies { get; }

        public BattleStartedEvent(BattleUnit player, BattleUnit[] enemies)
        {
            Player = player;
            Enemies = enemies;
        }
    }

    public class BattleEndedEvent : BattleEventBase
    {
        public override string EventName => nameof(BattleEndedEvent);
        public BattleUnit Winner { get; }
        public string EndReason { get; }

        public BattleEndedEvent(BattleUnit winner, string endReason)
        {
            Winner = winner;
            EndReason = endReason;
        }
    }

    public class AttackAttemptedEvent : BattleEventBase
    {
        public override string EventName => nameof(AttackAttemptedEvent);
        public BattleUnit Attacker { get; }
        public BattleUnit Victim { get; }
        public WeaponData WeaponData { get; }

        public AttackAttemptedEvent(BattleUnit attacker, BattleUnit victim, WeaponData weaponData)
        {
            Attacker = attacker;
            Victim = victim;
            WeaponData = weaponData;
        }
    }

    public class AttackCompletedEvent : BattleEventBase
    {
        public override string EventName => nameof(AttackCompletedEvent);
        public BattleUnit Attacker { get; }
        public BattleUnit Victim { get; }
        public WeaponData WeaponData { get; }
        public int Damage { get; }
        public bool IsWeaknessHit { get; }
        public bool WasTargetKilled { get; }
        public bool IsCritical { get; }
        public bool WasShieldBroken { get; }

        public AttackCompletedEvent(
            BattleUnit attacker,
            BattleUnit victim,
            WeaponData weaponData,
            int damage,
            bool isWeaknessHit,
            bool wasTargetKilled,
            bool isCritical,
            bool wasShieldBroken)
        {
            Attacker = attacker;
            Victim = victim;
            WeaponData = weaponData;
            Damage = damage;
            IsWeaknessHit = isWeaknessHit;
            WasTargetKilled = wasTargetKilled;
            IsCritical = isCritical;
            WasShieldBroken = wasShieldBroken;
        }
    }

    public class TurnChangedEvent : BattleEventBase
    {
        public override string EventName => nameof(TurnChangedEvent);

        public BattleState PreviousState { get; }
        public BattleState NewState { get; }
        public BattleUnit ActiveUnit { get; }

        public TurnChangedEvent(BattleState previousState, BattleState newState, BattleUnit activeUnit)
        {
            PreviousState = previousState;
            NewState = newState;
            ActiveUnit = activeUnit;
        }
    }

    public class WeaponSelectedEvent : BattleEventBase
    {
        public override string EventName => nameof(WeaponSelectedEvent);

        public WeaponData SelectedWeapon { get; }
        public BattleUnit Attacker { get; }

        public WeaponSelectedEvent(WeaponData weapon, BattleUnit attacker)
        {
            SelectedWeapon = weapon;
            Attacker = attacker;
        }
    }

    public class ActionCompletedEvent : BattleEventBase
    {
        public override string EventName => nameof(ActionCompletedEvent);
    }

    public class ActionCancelledEvent : BattleEventBase
    {
        public override string EventName => nameof(ActionCancelledEvent);
    }

    public class StartTargetSelectionEvent : BattleEventBase
    {
        public override string EventName => nameof(StartTargetSelectionEvent);

        public List<EnemyUnit> AvailableTargets { get; }
        public WeaponData SelectedWeapon { get; }

        public StartTargetSelectionEvent(List<EnemyUnit> targets, WeaponData weapon)
        {
            AvailableTargets = targets;
            SelectedWeapon = weapon;
        }
    }

    public class TargetSelectedEvent : BattleEventBase
    {
        public override string EventName => nameof(TargetSelectedEvent);

        public EnemyUnit SelectedTarget { get; }
        public WeaponData SelectedWeapon { get; }

        public TargetSelectedEvent(EnemyUnit unit, WeaponData weapon)
        {
            SelectedTarget = unit;
            SelectedWeapon = weapon;
        }
    }
}
