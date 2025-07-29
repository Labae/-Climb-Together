using Data.Platformer.Enums;
using Gameplay.Platformer.Movement.Interface;

namespace Gameplay.Platformer.States
{
    public class PlatformerJumpState : PlatformerStateBase
    {
        public PlatformerJumpState(IPlatformerMovementController movementController) : base(movementController)
        {
        }

        public override PlatformerStateType StateType => PlatformerStateType.Jump;
        public override void OnEnter()
        {
        }

        public override void OnUpdate()
        {
            if (HandleSpecialActionTransition())
            {
                return;
            }

            if (_movementController.IsFalling())
            {
                ChangeState(PlatformerStateType.Fall);
                return;
            }

            if (_movementController.IsGrounded() && !_movementController.IsRising())
            {
                if (_movementController.IsMoving())
                {
                    ChangeState(PlatformerStateType.Run);
                }
                else
                {
                    ChangeState(PlatformerStateType.Idle);
                }

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
