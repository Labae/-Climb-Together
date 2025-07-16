using Data.Player.Enums;

namespace Gameplay.Player.States.Extensions
{
    public static class PlayerStateExtensions
    {
        public static bool CanReceiveInput(this PlayerStateType playerState)
        {
            return playerState != PlayerStateType.Death && playerState != PlayerStateType.Hit;
        }

        public static bool IsAirborne(this PlayerStateType playerState)
        {
            return playerState is PlayerStateType.Jump
                or PlayerStateType.Fall;
        }

        public static bool CanMove(this PlayerStateType playerState)
        {
            return playerState is PlayerStateType.Idle
                or PlayerStateType.Run
                or PlayerStateType.Jump
                or PlayerStateType.Fall;
        }
    }
}
