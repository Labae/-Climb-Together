using Data.Player.Enums;
using R3;
using Systems.StateMachine.Interfaces;

namespace Gameplay.Player.States.Transitions
{
public class GroundTransitionHandler : ITransitionHandler<PlayerStateType>
    {
        public int Priority => 80;
        public string Name => "Ground";

        public void Setup(ITransitionContext<PlayerStateType> context, CompositeDisposable disposables)
        {
            if (context is PlayerTransitionContext playerContext)
            {
                // 땅 상태 변경 즉시 반응
                playerContext.GroundDetector.OnGroundEntered
                    .Subscribe(_ => HandleGroundEntered(playerContext))
                    .AddTo(disposables);

                playerContext.GroundDetector.OnGroundExited
                    .Subscribe(_ => HandleGroundExited(playerContext))
                    .AddTo(disposables);
            }
        }

        public bool TryGetTransition(ITransitionContext<PlayerStateType> context, out PlayerStateType targetState)
        {
            targetState = default;

            if (context is PlayerTransitionContext playerContext)
            {
                if (!playerContext.IsGrounded) return false;

                var currentState = playerContext.CurrentState;

                // 땅에 있을 때 이동 상태 체크
                if (playerContext.IsMoving && currentState != PlayerStateType.Run)
                {
                    targetState = PlayerStateType.Run;
                    return true;
                }

                if (!playerContext.IsMoving && currentState != PlayerStateType.Idle)
                {
                    targetState = PlayerStateType.Idle;
                    return true;
                }
            }

            return false;
        }

        private void HandleGroundEntered(PlayerTransitionContext context)
        {
            if (CanLandFrom(context.CurrentState))
            {
                var targetState = context.IsMoving ? PlayerStateType.Run : PlayerStateType.Idle;
                context.StateMachine.ChangeState(targetState);
            }
        }

        private void HandleGroundExited(PlayerTransitionContext context)
        {
            if (context.CurrentState is PlayerStateType.Idle or PlayerStateType.Run)
            {
                context.StateMachine.ChangeState(PlayerStateType.Fall);
            }
        }

        private bool CanLandFrom(PlayerStateType state) =>
            state is PlayerStateType.Jump or PlayerStateType.DoubleJump
                or PlayerStateType.Fall or PlayerStateType.WallSlide;
    }

}
