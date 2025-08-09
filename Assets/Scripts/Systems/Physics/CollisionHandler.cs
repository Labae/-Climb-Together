using Data.Common;
using Systems.Physics.Debugging;
using Systems.Physics.Enums;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Systems.Physics
{
    public class CollisionHandler
    {
        private readonly Transform _transform;
        private readonly BoxCollider2D _collider;
        private readonly PhysicsSettings _settings;

        public CollisionHandler(Transform transform, BoxCollider2D collider, PhysicsSettings settings)
        {
            _transform = transform;
            _collider = collider;
            _settings = settings;
        }

        public RaycastHit2D CheckDirection(Vector2 direction, int layerMask)
        {
            var center = GetColliderCenter();
            var size = GetColliderSize();

            Vector2 boxSize;
            const float thinkness = 0.1f;
            const float sizeReduction = 0.7f;

            // left / right
            if (direction.x != 0)
            {
                boxSize = new Vector2(thinkness, size.y * sizeReduction);
            }
            else
            {
                boxSize = new Vector2(size.x * sizeReduction, thinkness);
            }

            var hit = Physics2D.BoxCast(center, boxSize, 0, direction, _settings.RaycastDistance, layerMask);

#if UNITY_EDITOR
            var debugInfo = new BoxCastDebugInfo
            {
                Center = center,
                Size = boxSize,
                Direction = direction,
                Distance = _settings.RaycastDistance,
                Hit = hit,
                HasHit = hit.collider != null,
                Color = GetColorForDirection(direction)
            };

            PhysicsDebugEvents.NotifyBoxCast(debugInfo);
#endif

            return hit;
        }

#if UNITY_EDITOR
        private Color GetColorForDirection(Vector2 direction)
        {
            if (direction == Vector2.down)
            {
                return Color.red;
            }

            if (direction == Vector2.up)
            {
                return Color.blue;
            }

            if (direction == Vector2.left || direction == Vector2.right)
            {
                return Color.yellow;
            }

            return Color.white;
        }
#endif

        public CollisionResult CheckDirectionWithSurface(Vector2 direction, int layerMask = PhysicsLayers.Ground)
        {
            var hit = CheckDirection(direction, layerMask);
            var surface = GetSurfaceType(hit);
            return new CollisionResult(hit, surface);
        }

        private SurfaceType GetSurfaceType(RaycastHit2D hit, float slopeThreshold = 0.7f)
        {
            if (hit.collider == null)
            {
                return SurfaceType.None;
            }

            var normal = hit.normal;

            if (normal.y > slopeThreshold)
            {
                return SurfaceType.Ground;
            }
            else if (normal.y < -slopeThreshold)
            {
                return SurfaceType.Ceiling;
            }
            else
            {
                return SurfaceType.Wall;
            }
        }

        public CollisionResult CheckForSurfaceType(Vector2 direction, SurfaceType targetType,
            int layerMask = PhysicsLayers.Ground)
        {
            var result = CheckDirectionWithSurface(direction, layerMask);
            return result.SurfaceType != targetType ? new CollisionResult(new RaycastHit2D(), SurfaceType.None) : result;
        }

        public bool CheckGround(float checkDistance = -1f)
        {
            var distance = checkDistance > 0 ? checkDistance : _settings.GroundCheckDistnace;
            var result = CheckForSurfaceType(Vector2.down, SurfaceType.Ground);

            return result.HasCollision && result.Hit.point.y >= _transform.position.y - distance;
        }

        private Vector2 GetColliderCenter()
        {
            return (Vector2)_transform.position + GetColliderOffset();
        }

        public Vector2 GetColliderOffset()
        {
            return _collider.offset;
        }

        public Vector2 GetColliderSize()
        {
            return _collider.size;
        }

        public CollisionResult GetCurrentGroundResult()
        {
            return CheckForSurfaceType(Vector2.down, SurfaceType.Ground);
        }
    }
}
