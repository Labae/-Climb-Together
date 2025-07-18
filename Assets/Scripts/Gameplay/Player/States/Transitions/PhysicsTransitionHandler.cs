using Data.Player.Enums;
using R3;
using Systems.StateMachine.Interfaces;

namespace Gameplay.Player.States.Transitions
{
    public class PhysicsTransitionHandler : ITransitionHandler<PlayerStateType>
    {
        public int Priority => 40;
        public string Name => "Physics";

        public void Setup(ITransitionContext<PlayerStateType> context, CompositeDisposable disposables)
        {
            // 물리는 주기적 체크로만 처리
        }

        public bool TryGetTransition(ITransitionContext<PlayerStateType> context, out PlayerStateType targetState)
        {
            targetState = default;

            if (context is PlayerTransitionContext playerContext)
            {
                var currentState = playerContext.CurrentState;
                var velocity = playerContext.Velocity;

                // Jump/DoubleJump에서 Fall로 전환
                if (currentState is PlayerStateType.Jump or PlayerStateType.DoubleJump)
                {
                    if (velocity.y < -0.1f && !playerContext.IsGrounded)
                    {
                        targetState = PlayerStateType.Fall;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
