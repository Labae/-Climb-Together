using Data.Player.Enums;
using Gameplay.Player.Interfaces;
using Systems.Animations;

namespace Gameplay.Player.Events
{
    public readonly struct StateChangedEvent : IPlayerEvent
    {
        public PlayerStateType NewState { get; }

        public StateChangedEvent(PlayerStateType newState)
        {
            NewState = newState;
        }
    }

    public readonly struct DirectionChangedEvent : IPlayerEvent
    {
        public FacingDirection Direction { get; }

        public DirectionChangedEvent(FacingDirection direction)
        {
            Direction = direction;
        }
    }
}
