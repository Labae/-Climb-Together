using System;
using Data.Common;
using UnityEngine;

namespace Data.Platformer.Settings
{
    [Serializable]
    public class PlatformerPhysicsSettings : PhysicsSettings
    {
        [Header("Variable Gravity")]
        [Range(-50f, -15f)]
        [Tooltip("Gravity after jump cut (stronger for quick fall)")]
        public float JumpCutGravity = -35f;

        [Range(-40f, -10f)]
        [Tooltip("Gravity when falling normally")]
        public float FallGravity = -25f;

        [Header("Apex Handling")]
        [Range(-15f, -3f)]
        [Tooltip("Gravity at jump apex (weaker for floating feel)")]
        public float ApexGravity = -8f;

        [Header("Wall Slide Handling")] public float WallSlideGravity = -5f;

        [Header("Air Resistance & Momentum")]
        [Range(0.9f, 1.0f)]
        [Tooltip("공중에서 관성 유지율 (1 = 완전 유지, 0.9 = 10% 감소)")]
        public float AirResistance = 0.995f;

        [Range(0.1f, 0.9f)]
        [Tooltip("땅에서 마찰 계수 (낮을수록 미끄러움)")]
        public float GroundFriction = 0.85f;

        [Range(0.9f, 1.0f)]
        [Tooltip("벽 점프 후 관성 유지율")]
        public float WallJumpMomentumKeep = 0.98f;

        [Range(0f, 1.0f)]
        [Tooltip("공중에서 방향 제어 강도 (0 = 제어 불가, 1 = 완전 제어)")]
        public float AirControlStrength = 0.3f;
    }
}
