using Data.Player.Enums;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using R3;

namespace Gameplay.Player.States
{
    public class PlayerIdleState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Idle;
    }
}
