using Data.Platformer.Enums;
using Gameplay.Platformer.Movement.Interface;

namespace Gameplay.Platformer.States
{
    public class PlatformerRunState : PlatformerStateBase
    {
        public PlatformerRunState(IPlatformerMovementController movementController) : base(movementController)
        {
        }

        public override PlatformerStateType StateType => PlatformerStateType.Run;
        public override void OnEnter()
        {

        }

        public override void OnUpdate()
        {
            if (HandleSpecialActionTransition())
            {
                return;
            }

            if (!_movementController.IsGrounded())
            {
                if (_movementController.IsRising())
                {
                    ChangeState(PlatformerStateType.Jump);
                }
                if (_movementController.IsFalling())
                {
                    ChangeState(PlatformerStateType.Fall);
                }
                return;
            }

            if (!_movementController.IsIntendingToRun())
            {
                ChangeState(PlatformerStateType.Idle);
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
