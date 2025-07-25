using Gameplay.Player.Interfaces;

namespace Gameplay.Player.Events
{
    public readonly struct MovementInputEvent : IPlayerEvent
    {
        public float Input { get; }
        public float Timestamp { get; }

        public MovementInputEvent(float input, float timestamp)
        {
            Input = input;
            Timestamp = timestamp;
        }
    }

    public readonly struct LocomotionRequestEvent : IPlayerEvent
    {
        public float Input { get; }
        public bool ForceExecution { get; }

        public LocomotionRequestEvent(float input, bool forceExecution = false)
        {
            Input = input;
            ForceExecution = forceExecution;
        }
    }

    public readonly struct LocomotionExecutedEvent : IPlayerEvent
    {
        public IPlayerLocomotion Locomotion { get; }
        public float Input { get; }

        public LocomotionExecutedEvent(IPlayerLocomotion locomotion, float input)
        {
            Locomotion = locomotion;
            Input = input;
        }
    }

    public readonly struct LocomotionChangedEvent : IPlayerEvent
    {
        public IPlayerLocomotion PreviousLocomotion { get; }
        public IPlayerLocomotion CurrentLocomotion { get; }
        public float Input { get; }

        public LocomotionChangedEvent(IPlayerLocomotion previousLocomotion, IPlayerLocomotion currentLocomotion, float input)
        {
            PreviousLocomotion = previousLocomotion;
            CurrentLocomotion = currentLocomotion;
            Input = input;
        }
    }
}
