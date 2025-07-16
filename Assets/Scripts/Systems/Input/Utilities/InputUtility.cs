using UnityEngine;

namespace Systems.Input.Utilities
{
    public static class InputUtility
    {
        public const float DeadZoneThreshold = 0.1f;

        public static bool IsInputActive(float input)
        {
            return Mathf.Abs(input) > DeadZoneThreshold;
        }

        public static bool InDeadZone(float input)
        {
            return Mathf.Abs(input) <= DeadZoneThreshold;
        }
    }
}
