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

        [Space(10)]
        [Header("Basic Jump")]
        [Range(1f, 5f)]
        public float JumpHeight = 3.5f;
        public float TimeToJumpApex = 0.5f;

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

        public AnimationCurve DashSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public float DashEndSpeedRatio = 0.3f;
        public float DashEndVerticalSpeedRatio = 0.1f;

        [Header("Wall Slide Settings")] public float WallSlideSpeed = 3f;

        [Header("Wall Jump Settings")] public float WallJumpHeight = 3.0f;
        public float WallJumpHorizontalDistance = 2.5f;
        public float WallJumpTimeToApex = 0.4f;
        public float WallJumpInputLockTime = 0.2f;

        [Range(0.2f, 1.5f)] public float KnockbackDuration = 0.5f;

        private bool ShowAirSettings => AirMoveMultiplier < 1f;
        public float JumpPower => CalculateJumpPower();
        public float JumpGravity => CalculateJumpGravity();

        public Vector2 WallJumpVelocity => CalculateWallJumpVelocity();
        public float WallJumpGravity => CalculateWallJumpGravity();

        private float CalculateJumpPower()
        {
            return Mathf.Sqrt(2f * CalculateJumpGravity() * JumpHeight);
        }

        private float CalculateJumpGravity()
        {
            // g = 2h / t^2
            return (2f * JumpHeight) / (TimeToJumpApex * TimeToJumpApex);
        }

        private Vector2 CalculateWallJumpVelocity()
        {
            var gravity = CalculateWallJumpGravity();
            var verticalVelocity = Mathf.Sqrt(2f * gravity * WallJumpHeight);
            var horizontalVelocity = WallJumpHorizontalDistance / WallJumpTimeToApex;
            return new Vector2(horizontalVelocity, verticalVelocity);
        }

        private float CalculateWallJumpGravity()
        {
            return (2f * WallJumpHeight) / (WallJumpTimeToApex * WallJumpTimeToApex);
        }
    }
}
