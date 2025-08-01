using Data.Platformer.Enums;
using Gameplay.Platformer.Movement.Interface;

namespace Gameplay.Platformer.States
{
    public class PlatformerWallJumpState : PlatformerStateBase
    {
        public PlatformerWallJumpState(IPlatformerMovementController movementController) : base(movementController)
        {
        }

        public override PlatformerStateType StateType => PlatformerStateType.WallJump;
        public override void OnEnter()
        {
        }

        public override void OnUpdate()
        {
            if (HandleSpecialActionTransition())
            {
                return;
            }

            if (_movementController.IsGrounded())
            {
                if (_movementController.IsIntendingToRun())
                {
                    ChangeState(PlatformerStateType.Run);
                }
                else
                {
                    ChangeState(PlatformerStateType.Idle);
                }
                return;
            }

            if (_movementController.IsFalling())
            {
                ChangeState(PlatformerStateType.Fall);
                return;
            }

            if (HandleWallSlidingTransition())
            {
                return;
            }
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
        }

        public override void Dispose()
        {
        }
    }
}
