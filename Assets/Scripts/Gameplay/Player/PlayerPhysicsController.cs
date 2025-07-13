using UnityEngine;
using Systems.Physics;
using Data.Player;

namespace Gameplay.Player
{
    public class PlayerPhysicsController : PhysicsControllerBase
    {
        private readonly PlayerMovementConfig _config;

        // Player 특화 설정들
        private float _maxHorizontalSpeed;
        private float _maxVerticalSpeed = float.MaxValue;

        public PlayerPhysicsController(Rigidbody2D rigidbody2D, PlayerMovementConfig config)
            : base(rigidbody2D)
        {
            _config = config;
            _maxHorizontalSpeed = config.MaxSpeed;
        }

        #region Player Specific Overrides

        protected override void ApplyConstraints(ref Vector2 velocity)
        {
            // 기본 제약 조건 적용
            base.ApplyConstraints(ref velocity);

            // Player 전용 속도 제한
            velocity.x = Mathf.Clamp(velocity.x, -_maxHorizontalSpeed, _maxHorizontalSpeed);
            velocity.y = Mathf.Clamp(velocity.y, -_maxVerticalSpeed, _maxVerticalSpeed);
        }

        #endregion

        #region Player Specific Methods

        public void SetMaxHorizontalSpeed(float maxSpeed)
        {
            _maxHorizontalSpeed = maxSpeed;
        }

        public void SetMaxVerticalSpeed(float maxSpeed)
        {
            _maxVerticalSpeed = maxSpeed;
        }

        #endregion
    }
}