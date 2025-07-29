using Data.Platformer.Enums;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using Systems.StateMachine.Interfaces;

namespace Gameplay.Platformer.States
{
    public abstract class PlatformerStateBase : StateBase<PlatformerStateType>
    {
        protected readonly IPlatformerMovementController _movementController;

        protected PlatformerStateBase(IPlatformerMovementController movementController)
        {
            _movementController = movementController;
        }

        protected bool HandleSpecialActionTransition()
        {
            if (!_movementController.IsInSpecialAction())
            {
                return false;
            }

            var specialAction = _movementController.GetSpecialAction();
            switch (specialAction)
            {
                case SpecialActionType.Dashing:
                    ChangeState(PlatformerStateType.Dash);
                    return true;
                case SpecialActionType.Knockback:
                    ChangeState(PlatformerStateType.Hit);
                    return true;
                case SpecialActionType.None:
                default:
                    break;
            }

            return false;
        }
    }
}
