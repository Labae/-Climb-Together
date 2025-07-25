using Data.Player.Enums;
using Gameplay.Physics.Interfaces;

namespace Gameplay.Player.States
{
    public class PlayerWallSlideState : PlayerStateBase
    {
        private readonly IPhysicsController _physicsController;

        public PlayerWallSlideState(IPhysicsController physicsController)
        {
            _physicsController = physicsController;
        }

        public override void OnEnter()
        {
            _physicsController.SetGravityEnabled(false);
            base.OnEnter();
        }

        public override void OnExit()
        {
            _physicsController.SetGravityEnabled(true);
            base.OnExit();
        }

        public override PlayerStateType StateType => PlayerStateType.WallSlide;
    }
}
