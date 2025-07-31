using NaughtyAttributes;
using UnityEngine;

namespace Data.Common
{
    [System.Serializable]
    public class PlatformerMovementSettings
    {
        [Header("Basic Movement")] [Range(1f, 20f)]
        public float RunSpeed = 8f;

        [Range(0.1f, 1f)] public float AirMoveMultiplier = 0.7f;

        [Space(5)]
        [Header("Movement Timing")]
        [Range(0.05f, 0.5f)]
        [Tooltip("Time to reach maximum speed from standstill")]
        public float AccelTime = 0.1f;

        [Range(0.05f, 0.8f)] [Tooltip("Time to come to a complete stop from maximum speed")]
        public float DecelTime = 0.15f;

        [Range(0.02f, 0.3f)] [Tooltip("Time to change direction (turnaround time)")]
        public float TurnAroundTime = 0.08f;

        [Space(5)] [ShowIf("ShowAirSettings")] [Range(0.05f, 2f)] [Tooltip("Acceleration time multiplier when in air")]
        public float AirAccelMultiplier = 1.5f;

        [ShowIf("ShowAirSettings")] [Range(0.1f, 3f)] [Tooltip("Deceleration time multiplier when in air")]
        public float AirDecelMultiplier = 2f;

        [Space(5)] [Range(0.01f, 0.5f)] [Tooltip("Minimum speed threshold to be considered 'moving'")]
        public float MovingThreshold = 0.1f;

        [Space(10)] [Header("Basic Jump")] [Range(5f, 30f)]
        public float JumpPower = 15f;

        [Range(0.1f, 2f)] public float JumpBufferTime = 0.2f;

        [Range(0.1f, 2f)] public float CoyoteTime = 0.15f;

        [Header("Jump Cut")]
        [Range(0.1f, 0.8f)]
        [Tooltip("Jump cut strength (0.5 = half speed)")]
        public float JumpCutStrength = 0.3f;

        [Header("Apex Handling")]
        [Range(0.5f, 5f)]
        [Tooltip("Velocity threshold to detect apex")]
        public float ApexThreshold = 2f;

        [Range(0.05f, 0.3f)]
        [Tooltip("Minimum time to stay in apex state")]
        public float ApexDuration = 0.1f;

        [Space(10)] [Header("Special Actions")]

        [Header("Dash System")]
        [Range(8f, 25f)]
        public float DashSpeed = 15f;

        [Range(0.1f, 0.5f)]
        public float DashDuration = 0.2f;

        [Range(0.3f, 2f)]
        public float DashCooldown = 1f;

        [Range(1, 3)]
        public int MaxDashCount = 1;  // 연속 대시 횟수

        [Header("Dash Directions")]
        public bool AllowDiagonalDash = true;
        public bool AllowVerticalDash = true;

        [Range(0.2f, 1.5f)] public float KnockbackDuration = 0.5f;

        // NaughtyAttributes condition
        private bool ShowAirSettings => AirMoveMultiplier < 1f;
    }
}
