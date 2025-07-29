using Systems.Physics.Enums;
using UnityEngine;

namespace Systems.Physics
{
    public readonly struct CollisionResult
    {
        public readonly RaycastHit2D Hit;
        public readonly SurfaceType SurfaceType;
        public bool HasCollision => Hit.collider != null;

        public CollisionResult(RaycastHit2D hit, SurfaceType surfaceType)
        {
            Hit = hit;
            SurfaceType = surfaceType;
        }
    }
}
