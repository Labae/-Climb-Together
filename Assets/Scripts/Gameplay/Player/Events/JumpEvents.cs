using Gameplay.Common.Enums;
using Gameplay.Player.Interfaces;
using UnityEngine;

namespace Gameplay.Player.Events
{
    public readonly struct JumpExecutedEvent : IPlayerEvent
    {
        public JumpType JumpType { get; }
        public Vector2 JumpVelocity { get; }

        public JumpExecutedEvent(JumpType jumpType, Vector2 jumpVelocity)
        {
            JumpType = jumpType;
            JumpVelocity = jumpVelocity;
        }
    }

    public readonly struct JumpInputEvent : IPlayerEvent
    {
        public bool IsPressed { get; }
        public bool IsHeld { get; }
        public float PressTime { get; }

        public JumpInputEvent(bool isPressed, bool isHeld, float pressTime)
        {
            IsPressed = isPressed;
            IsHeld = isHeld;
            PressTime = pressTime;
        }
    }

    public readonly struct JumpBufferEvent : IPlayerEvent
    {
        public bool IsActive { get; }
        public float RemainingTime { get; }

        public JumpBufferEvent(bool isActive, float remainingTime)
        {
            IsActive = isActive;
            RemainingTime = remainingTime;
        }
    }
}
