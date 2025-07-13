using Systems.Physics.Enums;
using UnityEngine;

namespace Systems.Physics
{
    public readonly struct VelocityRequest
    {
        public readonly VelocityRequestType RequestType;
        public readonly Vector2 Velocity;
        public readonly ForceType Source;
        public readonly int Priority;
        public readonly float Duration;
        public readonly bool AffectsX;
        public readonly bool AffectsY;

        public VelocityRequest(VelocityRequestType requestType,
            Vector2 velocity, ForceType source,
            int priority, float duration, bool affectsX, bool affectsY)
        {
            RequestType = requestType;
            Velocity = velocity;
            Source = source;
            Priority = priority;
            Duration = duration;
            AffectsX = affectsX;
            AffectsY = affectsY;
        }

        public static VelocityRequest Set(Vector2 velocity,
            ForceType source = ForceType.None,
            int priority = VelocityPriority.Movement)
        {
            return new VelocityRequest(
                VelocityRequestType.Set,
                velocity,
                source, 
                priority,
                0f, 
                true,
                true);
        }

        public static VelocityRequest Add(Vector2 velocity,
            ForceType source = ForceType.None, float duration = 0f)
        {
            return new VelocityRequest(
                VelocityRequestType.Add,
                velocity,
                source,
                VelocityPriority.Background,
                duration,
                true, 
                true);
        }

        public static VelocityRequest Override(Vector2 velocity,
            ForceType source = ForceType.None)
        {
            return new VelocityRequest(
                VelocityRequestType.Override,
                velocity, 
                source, 
                VelocityPriority.Override,
                0f,
                true,
                true);
        }

        public static VelocityRequest SetHorizontal(float x,
            int priority = VelocityPriority.Movement)
        {
            return new VelocityRequest(
                VelocityRequestType.Set,
                new Vector2(x, 0), 
                ForceType.None,
                priority,
                0f,
                true, 
                false);
        }

        public static VelocityRequest SetVertical(float y,
            int priority = VelocityPriority.Jump)
        {
            return new VelocityRequest(
                VelocityRequestType.Set,
                new Vector2(0, y),
                ForceType.None,
                priority,
                0f,
                false,
                true);
        }
    }
}