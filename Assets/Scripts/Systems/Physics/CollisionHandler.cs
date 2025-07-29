using Data.Common;
using Systems.Physics.Enums;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Systems.Physics
{
    public class CollisionHandler
    {
        private readonly Transform _transform;
        private readonly Collider2D _collider;
        private readonly PhysicsSettings _settings;

        public CollisionHandler(Transform transform, Collider2D collider, PhysicsSettings settings)
        {
            _transform = transform;
            _collider = collider;
            _settings = settings;
        }

        public RaycastHit2D CheckDirection(Vector2 direction, int layerMask)
        {
            var center = GetColliderCenter();
            var size = GetColliderSize();

            var boxSize = direction.x != 0 ?
                new Vector2(0.1f, size.y) :
                new Vector2(size.x, 0.1f);

            return Physics2D.BoxCast(center, boxSize, 0, direction, _settings.RaycastDistance, layerMask);
        }

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
            if (_collider == null)
            {
                return _transform.position;
            }

            return _collider switch
            {
                BoxCollider2D box => box.offset,
                CapsuleCollider2D capsule => capsule.offset,
                CircleCollider2D circle => circle.offset,
                _ => Vector2.zero
            };
        }

        public Vector2 GetColliderSize()
        {
            if (_collider == null)
            {
                return Vector2.one;
            }

            return _collider switch
            {
                BoxCollider2D box => box.size,
                CapsuleCollider2D capsule => capsule.size,
                CircleCollider2D circle => circle.radius * Vector2.one,
                _ => Vector2.one
            };
        }

        public CollisionResult GetCurrentGroundResult()
        {
            return CheckForSurfaceType(Vector2.down, SurfaceType.Ground);
        }
    }
}
