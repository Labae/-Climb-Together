using Data.Player.Enums;
using Gameplay.Player.Actions;
using R3;
using Systems.StateMachine.Interfaces;

namespace Gameplay.Player.States.Transitions
{
    public class JumpTransitionHandler : ITransitionHandler<PlayerStateType>
    {
        public int Priority => 100;
        public string Name => "Jump";

        public void Setup(ITransitionContext<PlayerStateType> context, CompositeDisposable disposables)
        {
            if (context is PlayerTransitionContext playerContext)
            {
                // 점프 즉시 반응
                playerContext.PlayerJump.OnJumpExecuted
                    .Subscribe(action => HandleJumpAction(playerContext, action))
                    .AddTo(disposables);
            }
        }

        public bool TryGetTransition(ITransitionContext<PlayerStateType> context, out PlayerStateType targetState)
        {
            targetState = default;
            // 주기적 체크에서는 false 반환 (이벤트에서만 처리)
            return false;
        }

        private void HandleJumpAction(PlayerTransitionContext context, object action)
        {
            switch (action)
            {
                case GroundJumpAction when CanJumpFrom(context.CurrentState):
                    context.StateMachine.ChangeState(PlayerStateType.Jump);
                    break;
                case AirJumpAction when CanDoubleJumpFrom(context.CurrentState):
                    context.StateMachine.ChangeState(PlayerStateType.DoubleJump);
                    break;
            }
        }

        private bool CanJumpFrom(PlayerStateType state) =>
            state is PlayerStateType.Idle or PlayerStateType.Run or PlayerStateType.WallSlide;

        private bool CanDoubleJumpFrom(PlayerStateType state) =>
            state is PlayerStateType.Jump or PlayerStateType.Fall or PlayerStateType.WallSlide;
    }
}
