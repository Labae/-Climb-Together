using NaughtyAttributes;
using UnityEngine;

namespace Data.Common
{
    [System.Serializable]
    public class PlatformerMovementSettings
    {
        [Header("Basic Movement")] [Range(1f, 20f)]
        public float RunSpeed = 8f;

        public float AirMoveMultiplier = 0.7f;

        public bool UseGradualStop = false;

        [Header("Basie Jump")] [Range(5f, 30f)]
        public float JumpPower = 15f;

        [Range(0.1f, 2f)] public float JumpBufferTime = 0.2f;
        [Range(0.1f, 2f)] public float CoyoteTime = 0.15f;

        [Header("Special Actions")] public float DashSpeed = 12f;
        public float DashDuration = 0.2f;

        public float KnockbackDuration = 0.5f;
    }
}
