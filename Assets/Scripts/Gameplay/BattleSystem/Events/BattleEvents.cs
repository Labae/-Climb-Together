using System;
using System.Collections.Generic;
using Data.BattleSystem.Enums;
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
        public WeaponType WeaponType { get; }

        public AttackAttemptedEvent(BattleUnit attacker, BattleUnit victim, WeaponType weaponType)
        {
            Attacker = attacker;
            Victim = victim;
            WeaponType = weaponType;
        }
    }

    public class AttackCompletedEvent : BattleEventBase
    {
        public override string EventName => nameof(AttackCompletedEvent);
        public BattleUnit Attacker { get; }
        public BattleUnit Victim { get; }
        public WeaponType WeaponType { get; }
        public int Damage { get; }
        public bool IsWeaknessHit { get; }
        public bool WasTargetKilled { get; }

        public AttackCompletedEvent(BattleUnit attacker, BattleUnit victim, WeaponType weaponType, int damage, bool isWeaknessHit, bool wasTargetKilled)
        {
            Attacker = attacker;
            Victim = victim;
            WeaponType = weaponType;
            Damage = damage;
            IsWeaknessHit = isWeaknessHit;
            WasTargetKilled = wasTargetKilled;
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
}
