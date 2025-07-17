using System;
using Cysharp.Text;
using Data.Player.Enums;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using Gameplay.Player.Locomotion;
using Gameplay.Player.States.Extensions;
using R3;
using Systems.Physics.Utilities;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.Player.States
{
    public class PlayerStateTransitions : IDisposable
    {
        private readonly IStateMachine<PlayerStateType> _stateMachine;
        private readonly PlayerLocomotion _playerLocomotion;
        private readonly PlayerJump _playerJump;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundChecker _groundChecker;

        private readonly CompositeDisposable _disposables = new();

        // 상태 전환 캐시 (성능 최적화)
        private PlayerStateType _lastState;
        private bool _wasGrounded;
        private bool _wasMoving;

        public PlayerStateTransitions(IStateMachine<PlayerStateType> stateMachine, PlayerLocomotion playerLocomotion,
            PlayerJump playerJump, IPhysicsController physicsController, IGroundChecker groundChecker)
        {
            _stateMachine = stateMachine;
            _playerLocomotion = playerLocomotion;
            _playerJump = playerJump;
            _physicsController = physicsController;
            _groundChecker = groundChecker;

            // 초기 상태 캐싱
            _lastState = _stateMachine.CurrentStateType.CurrentValue;
            _wasGrounded = _groundChecker.IsGrounded.CurrentValue;
            _wasMoving = IsCurrentlyMoving();

            SetupAllTransitions();
        }

        private void SetupAllTransitions()
        {
            SetupLocomotionTransitions();
            SetupJumpTransitions();
            SetupGroundTransitions();
            SetupPhysicsTransitions();
            SetupStateChangeTracking();
        }

        #region State Change Tracking

        /// <summary>
        /// 상태 변경 추적 설정 (캐싱을 위해)
        /// </summary>
        private void SetupStateChangeTracking()
        {
            _stateMachine.CurrentStateType
                .Subscribe(newState => _lastState = newState)
                .AddTo(_disposables);
        }

        #endregion

        #region Locomotion Transitions

        private void SetupLocomotionTransitions()
        {
            _playerLocomotion.OnLocomotionExecuted
                .Subscribe(HandleLocomotionTransition)
                .AddTo(_disposables);

            // 움직임 상태 변화에 따른 전환 (최적화된 버전)
            _physicsController.IsMoving
                .DistinctUntilChanged()
                .Where(_ => _groundChecker.IsGrounded.CurrentValue)
                .Subscribe(HandleMovementStateChange)
                .AddTo(_disposables);
        }

        private void HandleLocomotionTransition(IPlayerLocomotion locomotion)
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            bool isGrounded = _groundChecker.IsGrounded.CurrentValue;

            switch (locomotion)
            {
                case DefaultLocomotion when ShouldTransitionToRun(currentState, isGrounded):
                    _stateMachine.ChangeState(PlayerStateType.Run);
                    break;
                case NoneLocomotion when ShouldTransitionToIdle(currentState, isGrounded):
                    _stateMachine.ChangeState(PlayerStateType.Idle);
                    break;
            }
        }

        private void HandleMovementStateChange(bool isMoving)
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;

            // 상태가 변경되었고 땅에 있을 때만 처리
            if (_wasMoving != isMoving && _groundChecker.IsGrounded.CurrentValue)
            {
                if (isMoving && currentState == PlayerStateType.Idle)
                {
                    _stateMachine.ChangeState(PlayerStateType.Run);
                }
                else if (!isMoving && currentState == PlayerStateType.Run)
                {
                    _stateMachine.ChangeState(PlayerStateType.Idle);
                }
            }

            _wasMoving = isMoving;
        }

        private bool ShouldTransitionToRun(PlayerStateType currentState, bool isGrounded)
        {
            return currentState != PlayerStateType.Run &&
                   isGrounded &&
                   IsCurrentlyMoving();
        }

        private bool ShouldTransitionToIdle(PlayerStateType currentState, bool isGrounded)
        {
            return currentState != PlayerStateType.Idle &&
                   isGrounded &&
                   !IsCurrentlyMoving();
        }

        /// <summary>
        /// PhysicsUtility를 사용한 움직임 상태 체크
        /// </summary>
        private bool IsCurrentlyMoving()
        {
            return PhysicsUtility.IsMoving(_physicsController.GetVelocity());
        }

        /// <summary>
        /// 거의 정지 상태인지 체크
        /// </summary>
        private bool IsNearlyStationary()
        {
            return PhysicsUtility.IsNearlyStationary(_physicsController.GetVelocity());
        }

        #endregion

        #region Jump Transitions

        private void SetupJumpTransitions()
        {
            _playerJump.OnJumpExecuted
                .Subscribe(HandleJumpTransition)
                .AddTo(_disposables);
        }

        private void HandleJumpTransition(Unit unit)
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            if (CanJumpFrom(currentState))
            {
                _stateMachine.ChangeState(PlayerStateType.Jump);
            }
        }

        private bool CanJumpFrom(PlayerStateType state)
        {
            return state is PlayerStateType.Idle
                or PlayerStateType.Run
                or PlayerStateType.Fall;
        }

        #endregion

        #region Physics Transitions

        private void SetupPhysicsTransitions()
        {
            // 상승에서 하강으로 전환 (더 명확한 조건)
            SetupRisingToFallingTransition();

            // 직접 낙하 전환 (땅에서 떨어질 때)
            SetupDirectFallTransition();

            // 터미널 속도 도달 시 전환
            SetupTerminalVelocityTransition();
        }

        private void SetupRisingToFallingTransition()
        {
            _physicsController.IsRising
                .CombineLatest(_physicsController.IsFalling, (rising, falling) => !rising && falling)
                .Where(shouldFall => shouldFall)
                .Where(_ => !_groundChecker.IsGrounded.CurrentValue)
                .Subscribe(_ => HandleFallTransition())
                .AddTo(_disposables);
        }

        private void SetupDirectFallTransition()
        {
            _groundChecker.OnGroundExited
                .Where(_ => !PhysicsUtility.IsRising(_physicsController.GetVelocity()))
                .Subscribe(_ => HandleDirectFallTransition())
                .AddTo(_disposables);
        }

        private void SetupTerminalVelocityTransition()
        {
            _physicsController.VerticalVelocity
                .Where(velocity => PhysicsUtility.IsFalling(Vector2.down * velocity))
                .Where(velocity => Mathf.Abs(velocity) > PhysicsUtility.StopThreshold * 2) // 충분히 빠른 낙하
                .Subscribe(_ => HandleFastFallTransition())
                .AddTo(_disposables);
        }

        private void HandleFallTransition()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            if (CanFallFrom(currentState))
            {
                _stateMachine.ChangeState(PlayerStateType.Fall);
            }
        }

        private void HandleDirectFallTransition()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            if (CanDirectFallFrom(currentState))
            {
                _stateMachine.ChangeState(PlayerStateType.Fall);
            }
        }

        private void HandleFastFallTransition()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            // 빠른 낙하 시에만 상태 전환 (선택적)
            if (currentState == PlayerStateType.Jump && PhysicsUtility.IsFalling(_physicsController.GetVelocity()))
            {
                _stateMachine.ChangeState(PlayerStateType.Fall);
            }
        }

        private bool CanFallFrom(PlayerStateType currentState)
        {
            return currentState != PlayerStateType.Fall
                   && currentState.CanReceiveInput()
                   && !_groundChecker.IsGrounded.CurrentValue
                   && PhysicsUtility.IsFalling(_physicsController.GetVelocity());
        }

        private bool CanDirectFallFrom(PlayerStateType currentState)
        {
            return currentState is PlayerStateType.Idle or PlayerStateType.Run
                   && currentState.CanReceiveInput()
                   && !_groundChecker.IsGrounded.CurrentValue;
        }

        #endregion

        #region Ground Transitions

        private void SetupGroundTransitions()
        {
            _groundChecker.OnGroundEntered
                .Subscribe(_ => HandleLandTransition())
                .AddTo(_disposables);

            // 착지 시 속도에 따른 상태 결정 (최적화)
            _groundChecker.OnGroundEntered
                .DelayFrame(1) // 물리 업데이트 후 체크
                .Subscribe(_ => HandleLandStateDecision())
                .AddTo(_disposables);
        }

        private void HandleLandTransition()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;

            if (!CanLandFrom(currentState))
            {
                return;
            }

            // 착지 시 즉시 상태 결정
            DecideLandingState();
        }

        private void HandleLandStateDecision()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;

            // 착지 후 1프레임 지연하여 정확한 상태 결정
            if (currentState.CanReceiveInput() && _groundChecker.IsGrounded.CurrentValue)
            {
                DecideLandingState();
            }
        }

        private void DecideLandingState()
        {
            // PhysicsUtility를 사용하여 더 정확한 상태 판정
            if (IsCurrentlyMoving())
            {
                _stateMachine.ChangeState(PlayerStateType.Run);
            }
            else if (IsNearlyStationary())
            {
                _stateMachine.ChangeState(PlayerStateType.Idle);
            }
            // 중간 속도인 경우 현재 속도 방향에 따라 결정
            else
            {
                int horizontalDirection = PhysicsUtility.GetVelocityDirection(_physicsController.GetHorizontalSpeed());
                if (horizontalDirection != 0)
                {
                    _stateMachine.ChangeState(PlayerStateType.Run);
                }
                else
                {
                    _stateMachine.ChangeState(PlayerStateType.Idle);
                }
            }
        }

        private bool CanLandFrom(PlayerStateType state)
        {
            return state is PlayerStateType.Jump
                or PlayerStateType.Fall;
        }

        #endregion

        #region Advanced Transition Conditions

        /// <summary>
        /// 복합 조건 체크 - 점프에서 낙하로 전환 조건
        /// </summary>
        private bool ShouldTransitionFromJumpToFall()
        {
            var velocity = _physicsController.GetVelocity();
            return PhysicsUtility.IsFalling(velocity) &&
                   !_groundChecker.IsGrounded.CurrentValue &&
                   !PhysicsUtility.IsRising(velocity);
        }

        /// <summary>
        /// 복합 조건 체크 - 실행 중에서 아이들로 전환 조건
        /// </summary>
        private bool ShouldTransitionFromRunToIdle()
        {
            return _groundChecker.IsGrounded.CurrentValue &&
                   IsNearlyStationary() &&
                   !_playerLocomotion.IsExecuting;
        }

        /// <summary>
        /// 복합 조건 체크 - 아이들에서 실행으로 전환 조건
        /// </summary>
        private bool ShouldTransitionFromIdleToRun()
        {
            return _groundChecker.IsGrounded.CurrentValue &&
                   IsCurrentlyMoving() &&
                   _playerLocomotion.IsExecuting;
        }

        #endregion

        #region Debug Utilities

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 상태 전환 정보
        /// </summary>
        public string GetTransitionDebugInfo()
        {
            var velocity = _physicsController.GetVelocity();

            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine(ZString.Concat("State: ", _stateMachine.CurrentStateType.CurrentValue));
            sb.AppendLine(ZString.Concat("Grounded: ", _groundChecker.IsGrounded.CurrentValue));
            sb.AppendLine(ZString.Concat("Moving: ", IsCurrentlyMoving()));
            sb.AppendLine(ZString.Concat("Stationary: ", IsNearlyStationary()));
            sb.AppendLine(ZString.Concat("Rising: ", PhysicsUtility.IsRising(velocity)));
            sb.AppendLine(ZString.Concat("Falling: ", PhysicsUtility.IsFalling(velocity)));
            sb.Append(ZString.Concat("Velocity: ", velocity.x.ToString("F2"), ", ", velocity.y.ToString("F2")));
            return sb.ToString();
        }
#endif

        #endregion

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
