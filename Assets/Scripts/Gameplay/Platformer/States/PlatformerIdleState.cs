using Data.Platformer.Enums;
using Gameplay.Platformer.Movement.Interface;

namespace Gameplay.Platformer.States
{
    public class PlatformerIdleState : PlatformerStateBase
    {
        public PlatformerIdleState(IPlatformerMovementController movementController) : base(movementController)
        {

        }

        public override PlatformerStateType StateType => PlatformerStateType.Idle;

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

            if (_movementController.IsIntendingToRun())
            {
                ChangeState(PlatformerStateType.Run);
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
