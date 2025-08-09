using Data.Common;
using Systems.Physics.Enums;
using UnityEngine;

namespace Systems.Physics
{
    public class PositionClamper
    {
        private readonly Transform _transform;
        private readonly CollisionHandler _collisionHandler;
        private readonly PhysicsSettings _settings;

        private readonly float _wallMargin = 0.05f;
        private readonly float _groundMargin = 0.02f;
        private readonly float _ceilingMargin = 0.05f;

        public PositionClamper(Transform transform, CollisionHandler collisionHandler, PhysicsSettings settings)
        {
            _transform = transform;
            _collisionHandler = collisionHandler;
            _settings = settings;
        }

        public bool ClampPosition(ref Vector3 position, VelocityHandler velocityHandler)
        {
            var originalPosition = position;
            var newPosition = originalPosition;
            bool wasAdjusted = false;

            var colliderSize = _collisionHandler.GetColliderSize();
            var colliderOffset = _collisionHandler.GetColliderOffset();
            var colliderHalfSize = colliderSize * 0.5f;

            wasAdjusted |= TryClampHorizontal(ref newPosition, velocityHandler, colliderHalfSize, colliderOffset);
            wasAdjusted |= TryClampVertical(ref newPosition, velocityHandler, colliderHalfSize, colliderOffset);

            position = newPosition;
            return wasAdjusted;
        }

        private bool TryClampHorizontal(ref Vector3 position, VelocityHandler velocityHandler,
            Vector2 colliderHalfSize, Vector2 colliderOffset)
        {
            bool adjusted = false;

            var leftResult = _collisionHandler.CheckDirectionWithSurface(Vector2.left);
            if (leftResult is { HasCollision: true, SurfaceType: SurfaceType.Wall })
            {
                var minX = leftResult.Hit.point.x + colliderHalfSize.x + _wallMargin - colliderOffset.x;
                if (position.x < minX)
                {
                    position.x = minX;

                    if (velocityHandler.GetVelocity().x < 0f)
                    {
                        velocityHandler.StopHorizontal();
                    }

                    adjusted = true;
                }
            }

            var rightResult = _collisionHandler.CheckDirectionWithSurface(Vector2.right);
            if (rightResult is { HasCollision: true, SurfaceType: SurfaceType.Wall })
            {
                var maxX = rightResult.Hit.point.x - colliderHalfSize.x - _wallMargin - colliderOffset.x;
                if (position.x > maxX)
                {
                    position.x = maxX;

                    if (velocityHandler.GetVelocity().x > 0f)
                    {
                        velocityHandler.StopHorizontal();
                    }

                    adjusted = true;
                }
            }

            return adjusted;
        }

        private bool TryClampVertical(ref Vector3 position, VelocityHandler velocityHandler,
            Vector2 colliderHalfSize, Vector2 colliderOffset)
        {
            bool adjusted = false;

            var upResult = _collisionHandler.CheckDirectionWithSurface(Vector2.up);
            if (upResult is { HasCollision: true, SurfaceType: SurfaceType.Ceiling })
            {
                var maxY = upResult.Hit.point.y - colliderHalfSize.y - _ceilingMargin - colliderOffset.y;
                if (position.y > maxY)
                {
                    position.y = maxY;

                    if (velocityHandler.GetVelocity().y > 0f)
                    {
                        velocityHandler.StopVertical();
                    }

                    adjusted = true;
                }
            }

            var downResult = _collisionHandler.CheckDirectionWithSurface(Vector2.down);
            if (downResult is { HasCollision: true, SurfaceType: SurfaceType.Ground })
            {
                var targetY = downResult.Hit.point.y + colliderHalfSize.y - _groundMargin - colliderOffset.y;
                var distanceToGround = position.y - targetY;
                if (distanceToGround < _settings.GroundCheckDistnace && velocityHandler.GetVelocity().y <= 0f)
                {
                    position.y = targetY;

                    if (velocityHandler.GetVelocity().y < 0f)
                    {
                        velocityHandler.StopVertical();
                    }

                    adjusted = true;
                }
            }

            return adjusted;
        }
    }
}
