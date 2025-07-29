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

        [Header("Collision")] public float RaycastDistance = 100f;
        public float GroundSnapDistance = 0.5f;
        public float GroundCheckDistnace = 1f;

        [Header("Speed Limits")]
        public float MaxHorizontalSpeed = 10f;
        public float MaxVerticalSpeed = 20f;
    }
}
