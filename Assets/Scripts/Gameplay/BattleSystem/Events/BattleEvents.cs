using Gameplay.BattleSystem.Core;

namespace Gameplay.BattleSystem.Events
{
    public class BattleStartedEvent
    {
        public BattleUnit Player { get; }
        public BattleUnit Enemy { get; }

        public BattleStartedEvent(BattleUnit player, BattleUnit enemy)
        {
            Player = player;
            Enemy = enemy;
        }
    }

    public class UnitAttackedEvent
    {
        public BattleUnit Attacker { get; }
        public BattleUnit Victim { get; }

        public UnitAttackedEvent(BattleUnit attacker, BattleUnit victim)
        {
            Attacker = attacker;
            Victim = victim;
        }
    }
}
