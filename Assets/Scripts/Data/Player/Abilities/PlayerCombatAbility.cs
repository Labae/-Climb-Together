using UnityEngine;

namespace Data.Player.Abilities
{
    [System.Serializable]
    public class PlayerCombatAbility
    {
        [Header("Health")]
        [Range(1, 20)]
        public int MaxHealth = 6;

        [Range(0.5f, 5f)]
        public float InvincibilityTime = 2f;
    }
}
