using UnityEngine;

namespace Systems.Physics.Utilities
{
    public static class PhysicsUtility
    {
        public const float VelocityThreshold = 0.01f;

        public static bool IgnoreVelocity(float velocity)
        {
            return Mathf.Abs(velocity) <= VelocityThreshold;
        }
    }
}
