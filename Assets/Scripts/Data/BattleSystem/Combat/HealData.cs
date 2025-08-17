using System;
using Cysharp.Text;
using UnityEngine;

namespace Data.BattleSystem.Combat
{
    public enum HealType
    {
        Normal,         // 일반 회복
        Regeneration,   // 시간에 따른 회복
        Item,           // 아이템 회복
        Magic           // 마법 회복
    }

    /// <summary>
    /// 회복 관련 데이터 구조체
    /// </summary>
    [Serializable]
    public readonly struct HealData
    {
        public readonly int Amount;
        public readonly HealType Type;

        public bool IsInstantHeal => Type == HealType.Item || Type == HealType.Magic;
        public bool IsOverTime => Type == HealType.Regeneration;

        public HealData(int amount, HealType type = HealType.Normal)
        {
            Amount = Mathf.Max(0, amount);
            Type = type;
        }

        public override string ToString()
        {
            return ZString.Format("+{0} {1} heal", Amount, Type);
        }
    }
}
