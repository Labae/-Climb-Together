using Data.Platformer.Enums;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using R3;

namespace Gameplay.Platformer.States
{
    public class PlatformerDashState : PlatformerStateBase
    {
        private readonly CompositeDisposable _disposables = new();

        public PlatformerDashState(IPlatformerMovementController movementController) : base(movementController)
        {
        }

        public override PlatformerStateType StateType => PlatformerStateType.Dash;

        public override void OnEnter()
        {
            _movementController.OnSpecialActionEnded
                .Where(specialActionEnded => specialActionEnded == SpecialActionType.Dashing)
                .Subscribe(_ => HandleDashEnded())
                .AddTo(_disposables);
        }

        public override void OnUpdate()
        {
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
            // 구독 해제
            _disposables.Clear();
        }

        private void HandleDashEnded()
        {
            PlatformerStateType nextState = DetermineNextState();
            ChangeState(nextState);
        }

        private PlatformerStateType DetermineNextState()
        {
            if (_movementController.IsGrounded() && _movementController.IsIntendingToRun())
            {
                return PlatformerStateType.Run;
            }

            if (_movementController.IsGrounded())
            {
                return PlatformerStateType.Idle;
            }

            if (_movementController.IsRising())
            {
                return PlatformerStateType.Jump;
            }

            return PlatformerStateType.Fall;
        }

        public override void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
