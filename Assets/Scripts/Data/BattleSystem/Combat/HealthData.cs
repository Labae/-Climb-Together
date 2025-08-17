using System;
using Cysharp.Text;
using UnityEngine;

namespace Data.BattleSystem.Combat
{
    /// <summary>
    /// 체력 관련 데이터 구조체
    /// </summary>
    [Serializable]
    public readonly struct HealthData : IEquatable<HealthData>
    {
        // Values
        public readonly int Current;
        public readonly int Max;

        // Properties
        public float Percentage => Max > 0 ? Mathf.Clamp01(Current / (float)Max) : 0f;
        public bool IsAlive => Current > 0;
        public bool IsLowHealth => Percentage <= 0.3f;
        public bool IsCriticalHealth => Percentage <= 0.15f;
        public bool IsFull => Current >= Max;
        public int Missing => Max - Current;

        public HealthData(int current, int max)
        {
            Current = Mathf.Max(0, current);
            Max = Mathf.Max(1, max);
        }

        public HealthData TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return this;
            }

            return new HealthData(Current - damage, Max);
        }

        public HealthData Heal(int amount)
        {
            if (amount <= 0)
            {
                return this;
            }

            return new HealthData(Current + amount, Max);
        }

        public HealthData SetMaxHealth(int newMax)
        {
            if (newMax <= 0)
            {
                return this;
            }

            var ratio = Percentage;
            return new HealthData(Mathf.RoundToInt(newMax * ratio), newMax);
        }

        public static bool operator==(HealthData a, HealthData b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(HealthData a, HealthData b)
        {
            return !a.Equals(b);
        }

        public bool Equals(HealthData other)
        {
            return Current == other.Current && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            return obj is HealthData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Current, Max);
        }

        public override string ToString()
        {
            return ZString.Format("Health {0}/{1} ({2:P1})", Current, Max, Percentage);
        }
    }
}
