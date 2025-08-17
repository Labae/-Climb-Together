using System;
using Cysharp.Text;
using UnityEngine;

namespace Data.BattleSystem.Combat
{
    /// <summary>
    /// 실드 관련 데이터 구조체
    /// </summary>
    [Serializable]
    public readonly struct ShieldData : IEquatable<ShieldData>
    {
        // Values
        public readonly int Current;
        public readonly int Max;

        // Properties
        public float Percentage => Max > 0 ? Mathf.Clamp01(Current / (float)Max) : 0f;
        public bool HasShield => Current > 0;
        public bool IsFull => Current >= Max;
        public bool IsEmpty => Current <= 0;
        public int Missing => Max - Current;

        public ShieldData(int current, int max)
        {
            Current = current;
            Max = max;
        }

        public ShieldData TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return this;
            }

            return new ShieldData(Current - damage, Max);
        }

        public ShieldData Restore()
        {
            return new ShieldData(Max, Max);
        }

        public ShieldData SetMaxShield(int newMax)
        {
            if (newMax < 0)
            {
                return this;
            }

            if (newMax == 0)
            {
                return new ShieldData(0, 0);
            }

            var ratio = Percentage;
            return new ShieldData(Mathf.RoundToInt(newMax * ratio), newMax);
        }

        public static bool operator==(ShieldData a, ShieldData b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ShieldData a, ShieldData b)
        {
            return !a.Equals(b);
        }

        public bool Equals(ShieldData other)
        {
            return Current == other.Current && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            return obj is ShieldData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Current, Max);
        }

        public override string ToString()
        {
            return ZString.Format("Shield {0}/{1} ({2:P1})", Current, Max, Percentage);
        }
    }
}
