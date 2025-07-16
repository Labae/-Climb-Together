using System;
using UnityEngine;

namespace Data.Common
{
    [Serializable]
    public class PhysicsSettings
    {
        [Header("Gravity")]
        [Range(-50f, -5f)] public float NormalGravity = -20f;
        [Range(-50f, -10f)] public float TerminalVelocity = -25f;

        [Header("Movement Thresholds")]
        public float RisingThreshold = 0.1f;
        public float FallingThreshold = -0.1f;

        [Header("Speed Limits")]
        public float MaxHorizontalSpeed = 10f;
        public float MaxVerticalSpeed = 20f;
    }
}
