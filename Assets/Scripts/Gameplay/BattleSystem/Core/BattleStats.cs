using System;
using UnityEngine;

namespace Gameplay.BattleSystem.Core
{
    [Serializable]
    public class BattleStats
    {
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _attack;
        [SerializeField] private int _defense;
        [SerializeField] private int _speed;

        public int MaxHealth => _maxHealth;
        public int Attack => _attack;
        public int Defense => _defense;
        public int Speed => _speed;
    }
}
