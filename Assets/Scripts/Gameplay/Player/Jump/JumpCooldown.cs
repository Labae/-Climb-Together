using UnityEngine;

namespace Gameplay.Player.Jump
{
    public class JumpCooldown
    {
        private readonly float _cooldownTime;
        private float _lastJumpInputTime;

        public bool IsReady => Time.time - _lastJumpInputTime > _cooldownTime;
        public float RemainingTime => Mathf.Max(0f, _cooldownTime - (Time.time - _lastJumpInputTime));

        public JumpCooldown(float cooldownTime = 0.1f)
        {
            _cooldownTime = cooldownTime;
        }

        public void RegisterJump()
        {
            _lastJumpInputTime = Time.time;
        }

        public void Reset()
        {
            _lastJumpInputTime = 0f;
        }
    }
}
