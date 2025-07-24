using Data.Player.Enums;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Core;
using Gameplay.Player.Jump;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.Player.States.Transitions
{
    public class PlayerTransitionContext : ITransitionContext<PlayerStateType>
    {
        public IStateMachine<PlayerStateType> StateMachine { get; }
        public PlayerStateType CurrentState => StateMachine.CurrentStateType.CurrentValue;

        // 플레이어 관련 컴포넌트들
        public PlayerLocomotion PlayerLocomotion { get; }
        public PlayerJump PlayerJump { get; }
        public IPhysicsController PhysicsController { get; }
        public IGroundDetector GroundDetector { get; }
        public IWallDetector WallDetector { get; }
        public PlayerEventBus EventBus { get; }

        public PlayerTransitionContext(
            IStateMachine<PlayerStateType> stateMachine,
            PlayerLocomotion playerLocomotion,
            PlayerJump playerJump,
            IPhysicsController physicsController,
            IGroundDetector groundDetector,
            IWallDetector wallDetector,
            PlayerEventBus eventBus)
        {
            StateMachine = stateMachine;
            PlayerLocomotion = playerLocomotion;
            PlayerJump = playerJump;
            PhysicsController = physicsController;
            GroundDetector = groundDetector;
            WallDetector = wallDetector;
            EventBus = eventBus;
        }

        // 헬퍼 프로퍼티들
        public bool IsGrounded => GroundDetector.IsGrounded.CurrentValue;
        public bool IsWallDetected => WallDetector?.IsCurrentlyDetectingWall() ?? false;
        public bool IsWallSliding => PlayerLocomotion.IsWallSliding();
        public Vector2 Velocity => PhysicsController.GetVelocity();
        public bool IsMoving => Systems.Physics.Utilities.PhysicsUtility.IsMoving(Velocity);
    }
}
