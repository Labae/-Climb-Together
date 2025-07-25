using Data.Player.Enums;
using Gameplay.Common.Enums;
using Gameplay.Player.Events;
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
                playerContext.EventBus.Subscribe<JumpExecutedEvent>()
                    .Subscribe(jumpEvent => HandleJumpEvent(playerContext, jumpEvent))
                    .AddTo(disposables);
            }
        }

        public bool TryGetTransition(ITransitionContext<PlayerStateType> context, out PlayerStateType targetState)
        {
            targetState = default;
            // 주기적 체크에서는 false 반환 (이벤트에서만 처리)
            return false;
        }

        private void HandleJumpEvent(PlayerTransitionContext context, JumpExecutedEvent jumpEvent)
        {
            var targetState = DetermineTargetState(jumpEvent.JumpType, context.CurrentState);
            if (targetState.HasValue)
            {
                context.StateMachine.ChangeState(targetState.Value);
            }
        }

        private PlayerStateType? DetermineTargetState(JumpType jumpType, PlayerStateType currentState)
        {
            return jumpType switch
            {
                JumpType.Ground when CanJumpFrom(currentState) => PlayerStateType.Jump,
                JumpType.Air when CanDoubleJumpFrom(currentState) => PlayerStateType.DoubleJump,
                JumpType.Wall when CanWallJump(currentState) => PlayerStateType.WallSlide,
                _ => null
            };
        }

        private bool CanJumpFrom(PlayerStateType state) =>
            state is PlayerStateType.Idle or PlayerStateType.Run or PlayerStateType.WallSlide;

        private bool CanDoubleJumpFrom(PlayerStateType state) =>
            state is PlayerStateType.Jump or PlayerStateType.Fall or PlayerStateType.WallSlide;

        private bool CanWallJump(PlayerStateType state) =>
            state is PlayerStateType.WallSlide or PlayerStateType.Fall;
    }
}
