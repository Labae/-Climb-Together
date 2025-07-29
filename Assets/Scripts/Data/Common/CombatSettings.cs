using UnityEngine;

namespace Data.Common
{
    [System.Serializable]
    public class CombatSettings
    {
        [Header("Health")]
        [Range(1, 20)]
        public int MaxHealth = 6;

        [Range(0.5f, 5f)]
        public float InvincibilityTime = 2f;
    }
}
