using System.Collections.Generic;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Units;

namespace Gameplay.BattleSystem.Events
{
    public class BattleStartedEvent
    {
        public BattleUnit Player { get; }
        public IReadOnlyList<EnemyUnit> Enemies { get; }

        public BattleStartedEvent(BattleUnit player,  IReadOnlyList<EnemyUnit> enemies)
        {
            Player = player;
            Enemies = enemies;
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
