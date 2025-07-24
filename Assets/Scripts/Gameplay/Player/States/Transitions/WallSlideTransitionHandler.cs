using Data.Player.Enums;
using R3;
using Systems.StateMachine.Interfaces;

namespace Gameplay.Player.States.Transitions
{
 public class WallSlideTransitionHandler : ITransitionHandler<PlayerStateType>
    {
        public int Priority => 60;
        public string Name => "WallSlide";

        public void Setup(ITransitionContext<PlayerStateType> context, CompositeDisposable disposables)
        {
            if (context is PlayerTransitionContext playerContext && playerContext.WallDetector != null)
            {
                playerContext.WallDetector.OnWallExited
                    .Subscribe(_ => HandleWallExited(playerContext))
                    .AddTo(disposables);
            }
        }

        public bool TryGetTransition(ITransitionContext<PlayerStateType> context, out PlayerStateType targetState)
        {
            targetState = default;

            if (context is PlayerTransitionContext playerContext)
            {
                var currentState = playerContext.CurrentState;

                // WallSlide 진입
                if (currentState != PlayerStateType.WallSlide && CanEnterWallSlide(playerContext))
                {
                    targetState = PlayerStateType.WallSlide;
                    return true;
                }

                // WallSlide 탈출
                if (currentState == PlayerStateType.WallSlide && !CanContinueWallSlide(playerContext))
                {
                    targetState = PlayerStateType.Fall;
                    return true;
                }
            }

            return false;
        }

        private void HandleWallExited(PlayerTransitionContext context)
        {
            if (context.CurrentState == PlayerStateType.WallSlide)
            {
                context.PhysicsController.SetGravityEnabled(true);
                context.StateMachine.ChangeState(PlayerStateType.Fall);
            }
        }

        private bool CanEnterWallSlide(PlayerTransitionContext context)
        {
            return !context.IsGrounded &&
                   context.IsWallDetected &&
                   context.IsWallSliding &&
                   context.Velocity.y <= 0.1f;
        }

        private bool CanContinueWallSlide(PlayerTransitionContext context)
        {
            return !context.IsGrounded &&
                   context.IsWallDetected &&
                   context.IsWallSliding &&
                   context.Velocity.y <= 2.0f;
        }
    }
}
