using System;
using Cysharp.Text;

namespace Data.BattleSystem.Combat
{
    public enum DamageType
    {
        Normal,     // 일반 데미지
        Health,     // 체력 데미지
        Shield,     // 실드 데미지
        True        // 방어 무시 데미지
    }

    /// <summary>
    /// 데미지 관련 데이터 구조체
    /// </summary>
    [Serializable]
    public readonly struct DamageData
    {
        public readonly int Amount;
        public readonly DamageType Type;
        public readonly bool IsCritical;
        public readonly bool IsWeaknessHit;

        public bool HasSpecialEffect => IsCritical || IsWeaknessHit;
        public float DamageMultiplier => (IsCritical ? 1.5f : 1f) * (IsWeaknessHit ? 1.5f : 1f);

        public DamageData(int amount, DamageType type, bool isCritical = false, bool isWeaknessHit = false)
        {
            Amount = amount;
            Type = type;
            IsCritical = isCritical;
            IsWeaknessHit = isWeaknessHit;
        }

        public override string ToString()
        {
            var sb = ZString.CreateStringBuilder();
            sb.AppendFormat("{0}, {1} damage", Type, Amount);
            if (IsCritical)
            {
                sb.Append("[CRITICAL]");
            }

            if (IsWeaknessHit)
            {
                sb.Append("[WEAKNESS]");
            }

            return sb.ToString();
        }
    }
}
