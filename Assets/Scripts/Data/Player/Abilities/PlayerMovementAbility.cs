using NaughtyAttributes;
using UnityEngine;

namespace Data.Player.Abilities
{
    [System.Serializable]
    public class PlayerMovementAbility
    {
        [Header("Basic Movement")]
        [Range(1f, 20f)]
        public float RunSpeed = 8f;

        [Range(0.1f, 2f)]
        public float AirMultiplier = 1.0f;

        [Header("Basie Jump")]
        [Range(5f, 30f)]
        public float JumpPower = 15f;

        [Range(0.1f, 2f)]
        public float JumpBufferTime = 0.2f;

        [Range(0.1f, 2f)]
        public float CoyoteTime = 0.15f;

        [Header("Advanced Movement")]
        public bool HasDoubleJump = false;

        [ShowIf("HasDoubleJump")]
        [Range(0.5f, 1.5f)]
        public float DoubleJumpMultiplier = 0.8f;

        public bool HasWallJump = false;

        [ShowIf("HasWallJump")]
        [Range(5f, 25f)]
        public float WallJumpForce = 12f;

        [ShowIf("HasWallJump")]
        [Range(0.1f, 1f)]
        public float WallJumpDuration = 0.3f;

    }
}
