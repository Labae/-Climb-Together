using UnityEngine;

namespace Data.Player
{
    [CreateAssetMenu(fileName = nameof(PlayerMovementConfig),
        menuName = "Gameplay/Player/"+nameof(PlayerMovementConfig))]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Movement Configuration")] [Range(6.0f, 24.0f)]
        public float MaxSpeed = 20f;
        
        [Header("Jump Configuration")]
        [Range(6.0f, 24.0f)]
        public float GroundJumpForce = 12.0f;

        [Range(1, 3)] public int MaxAirJumps = 1;
        [Range(6.0f, 24.0f)]
        public float AirJumpForce = 8.0f;
    }
}
