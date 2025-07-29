using Data.Common;
using Systems.Physics;
using UnityEngine;

namespace Gameplay.Platformer.Physics
{
    public class PlatformerPhysicsSystem : PhysicsSystemBase
    {
        private readonly PlatformerMovementSettings _movementSettings;

        public PlatformerPhysicsSystem(Transform transform,
            Collider2D collider,
            PhysicsSettings settings,
            PlatformerMovementSettings movementSettings
            ) : base(transform, collider, settings)
        {
            _movementSettings = movementSettings;
        }

        public void Jump()
        {
            _velocityHandler.SetVerticalVelocity(_movementSettings.JumpPower);
            _groundStateHandler.SetGroundState(false);
        }

        public void Dash(Vector2 direction)
        {
            var dashVelocity = direction.normalized * _movementSettings.DashSpeed;
            _velocityHandler.SetVelocity(dashVelocity);
        }

        public void Knockback(Vector2 direction, float force)
        {
            var knockbackVelocity = direction.normalized * force;
            _velocityHandler.SetVelocity(knockbackVelocity);

            if (direction.y > 0)
            {
                _groundStateHandler.SetGroundState(false);
            }
        }
    }
}
