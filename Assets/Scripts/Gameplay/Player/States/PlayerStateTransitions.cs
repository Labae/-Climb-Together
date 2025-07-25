using System;
using System.Collections.Generic;
using Data.Player.Enums;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Core;
using Gameplay.Player.Jump;
using Gameplay.Player.Locomotion;
using Gameplay.Player.States.Transitions;
using R3;
using Systems.StateMachine.Interfaces;

namespace Gameplay.Player.States
{
    // 메인 전환 관리자
    public class PlayerStateTransitions : IDisposable
    {
        private readonly PlayerTransitionContext _context;
        private readonly CompositeDisposable _disposables = new();
        private readonly List<ITransitionHandler<PlayerStateType>> _handlers = new();

        public PlayerStateTransitions(IStateMachine<PlayerStateType> stateMachine, PlayerLocomotionSystem playerLocomotionSystem,
            PlayerJumpSystem playerJumpSystem, IPhysicsController physicsController,
            IGroundDetector groundDetector, IWallDetector wallDetector, PlayerEventBus eventBus)
        {
            _context = new PlayerTransitionContext(stateMachine, playerLocomotionSystem,
                playerJumpSystem, physicsController, groundDetector, wallDetector, eventBus);

            SetupHandlers();
            SetupPeriodicCheck();
        }

        private void SetupHandlers()
        {
            // 핸들러들을 우선순위 순으로 추가
            _handlers.Add(new JumpTransitionHandler());
            _handlers.Add(new GroundTransitionHandler());
            _handlers.Add(new WallSlideTransitionHandler());
            _handlers.Add(new PhysicsTransitionHandler());

            // 각 핸들러 초기화
            foreach (var handler in _handlers)
            {
                handler.Setup(_context, _disposables);
            }

            // 상태 변경 로깅
            _context.StateMachine.CurrentStateType
                .Subscribe(state => GameLogger.Debug($"State: {state}", LogCategory.Player))
                .AddTo(_disposables);
        }

        private void SetupPeriodicCheck()
        {
            // 주기적으로 모든 핸들러의 전환 조건 체크
            Observable.Interval(TimeSpan.FromSeconds(0.1f))
                .Subscribe(_ => CheckAllTransitions())
                .AddTo(_disposables);
        }

        private void CheckAllTransitions()
        {
            foreach (var handler in _handlers)
            {
                if (!handler.TryGetTransition(_context, out var targetState) || targetState == _context.CurrentState)
                {
                    continue;
                }

                GameLogger.Debug($"Handler '{handler.Name}' triggered: {_context.CurrentState} -> {targetState}", LogCategory.Player);
                _context.StateMachine.ChangeState(targetState);
                return; // 첫 번째 핸들러만 실행
            }
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
