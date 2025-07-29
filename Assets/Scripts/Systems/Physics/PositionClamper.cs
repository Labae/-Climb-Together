using Data.Common;
using Systems.Physics.Enums;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Systems.Physics
{
    public class PositionClamper
    {
        private readonly Transform _transform;
        private readonly CollisionHandler _collisionHandler;
        private readonly PhysicsSettings _settings;

        public PositionClamper(Transform transform, CollisionHandler collisionHandler, PhysicsSettings settings)
        {
            _transform = transform;
            _collisionHandler = collisionHandler;
            _settings = settings;
        }

        public bool ClampPosition(VelocityHandler velocityHandler)
        {
            var feetPosition = _transform.position;
            var colliderSize = _collisionHandler.GetColliderSize();
            var colliderOffset = _collisionHandler.GetColliderOffset();

            var colliderCenter = (Vector2)feetPosition + colliderOffset;
            bool wasAdjusted = false;

            var leftResult = _collisionHandler.CheckDirectionWithSurface(Vector2.left);
            var rightHit = _collisionHandler.CheckDirectionWithSurface(Vector2.right);
            var upHit = _collisionHandler.CheckDirectionWithSurface(Vector2.up);
            var downHit = _collisionHandler.CheckDirectionWithSurface(Vector2.down);

            if (leftResult is { HasCollision: true, SurfaceType: SurfaceType.Wall })
            {
                var wallX = leftResult.Hit.point.x;
                var minAllowedColliderX = wallX + colliderSize.x * 0.5f;
                var minAllowedFeetX = minAllowedColliderX - colliderOffset.x;
                if (feetPosition.x < minAllowedFeetX)
                {
                    feetPosition.x = minAllowedFeetX;
                    if (velocityHandler.GetVelocity().x < 0f)
                    {
                        velocityHandler.StopHorizontal();
                    }

                    wasAdjusted = true;
                }
            }

            if (rightHit is { HasCollision: true, SurfaceType: SurfaceType.Wall })
            {
                var wallX = rightHit.Hit.point.x;
                var maxAllowedColliderX = wallX - colliderSize.x * 0.5f;
                var maxAllowedFeetX = maxAllowedColliderX - colliderOffset.x;
                if (feetPosition.x > maxAllowedFeetX)
                {
                    feetPosition.x = maxAllowedFeetX;
                    if (velocityHandler.GetVelocity().x > 0f)
                    {
                        velocityHandler.StopHorizontal();
                    }

                    wasAdjusted = true;
                }
            }

            if (upHit.HasCollision && upHit.SurfaceType == SurfaceType.Ceiling)
            {
                var ceilingY = upHit.Hit.point.y;
                var maxAllowedColliderY = ceilingY - colliderSize.y * 0.5f;
                var maxAllowedFeetY = maxAllowedColliderY - colliderOffset.y;
                if (feetPosition.y > maxAllowedFeetY)
                {
                    feetPosition.y = maxAllowedFeetY;
                    if (velocityHandler.GetVelocity().y > 0f)
                    {
                        velocityHandler.StopVertical();
                    }

                    wasAdjusted = true;
                }
            }

            if (downHit.HasCollision && downHit.SurfaceType == SurfaceType.Ground)
            {
                var groundY = downHit.Hit.point.y;

                // Ground는 땅에 정확히
                var targetFeetY = groundY;
                var distanceToGround = feetPosition.y - targetFeetY;
                if (distanceToGround < _settings.GroundSnapDistance && velocityHandler.GetVelocity().y <= 0f)
                {
                    feetPosition.y = targetFeetY;
                    if (velocityHandler.GetVelocity().y < 0f)
                    {
                        velocityHandler.StopVertical();
                    }

                    wasAdjusted = true;
                }
            }

            _transform.position = feetPosition;
            return wasAdjusted;
        }
    }
}
