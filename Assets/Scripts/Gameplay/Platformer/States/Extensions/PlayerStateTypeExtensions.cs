using Data.Platformer.Enums;

namespace Gameplay.Platformer.States.Extensions
{
    public static class PlayerStateExtensions
    {
        public static bool CanReceiveInput(this PlatformerStateType platformerState)
        {
            return platformerState != PlatformerStateType.Death && platformerState != PlatformerStateType.Hit;
        }

        public static bool IsAirborne(this PlatformerStateType platformerState)
        {
            return platformerState is PlatformerStateType.Jump
                or PlatformerStateType.Fall;
        }

        public static bool CanMove(this PlatformerStateType platformerState)
        {
            return platformerState is PlatformerStateType.Idle
                or PlatformerStateType.Run
                or PlatformerStateType.Jump
                or PlatformerStateType.Fall;
        }
    }
}
