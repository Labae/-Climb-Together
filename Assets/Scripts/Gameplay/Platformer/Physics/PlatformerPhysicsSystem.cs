using Data.Common;
using Data.Platformer.Settings;
using Gameplay.Platformer.Movement;
using Gameplay.Platformer.Movement.Enums;
using Systems.Physics;
using UnityEngine;

namespace Gameplay.Platformer.Physics
{
    public class PlatformerPhysicsSystem : PhysicsSystemBase
    {
        private readonly PlatformerMovementSettings _movementSettings;
        private readonly PlatformerVariableGravityHandler _variableGravityHandler;

        public PlatformerPhysicsSystem(Transform transform,
            BoxCollider2D collider,
            PlatformerPhysicsSettings settings,
            PlatformerMovementSettings movementSettings
            ) : base(transform, collider, settings)
        {
            _movementSettings = movementSettings;
            _variableGravityHandler = new PlatformerVariableGravityHandler(settings, movementSettings);
        }

        public override void PhysicsUpdate(float deltaTime)
        {
#if UNITY_EDITOR
            DebugBoxCasts.Clear();
#endif
            _gravityHandler.ApplyGravity(_velocityHandler,
                _groundStateHandler.GetGroundState(),
                deltaTime,
                _variableGravityHandler.GetCurrentGravity());

            var position = _transform.position;
            ApplyMovement(ref position, deltaTime);
            _positionClamper.ClampPosition(ref position, _velocityHandler);
            _transform.position = position;

            _groundStateHandler.UpdateGroundState(_velocityHandler);
        }

        public void SetGravityState(PlatformerGravityState state)
        {
            _variableGravityHandler.SetGravityState(state);
        }

        public void Jump()
        {
            _velocityHandler.SetVerticalVelocity(_movementSettings.JumpPower);
            _groundStateHandler.SetGroundState(false);
        }

        public void Dash(Vector2 direction, float speed)
        {
            var dashVelocity = direction.normalized * speed;
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
