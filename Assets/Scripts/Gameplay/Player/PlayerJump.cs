using System;
using System.Collections.Generic;
using Cysharp.Text;
using Data.Common;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Actions;
using Gameplay.Player.Interfaces;
using R3;
using Systems.Input.Utilities;
using Systems.Physics;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerJump : IDisposable
    {
        #region Fields

        private readonly List<IPlayerAction> _jumpActions;
        private readonly Subject<IPlayerAction> _onJumpExecuted = new();
        private readonly Subject<IPlayerAction> _onSpecificJumpExecuted = new();

        // 점프 상태 추적
        private bool _jumpPressed;
        private bool _jumpHeld;
        private float _jumpPressTime;
        private float _lastJumpTime;
        private IPlayerAction _lastExecutedJump;

        // JumpBuffer 시스템
        private float _jumpBufferTime;
        private float _lastJumpInputTime;
        private bool _jumpBufferActive;

        // CoyoteTime 시스템
        private float _coyoteTime;
        private float _lastGroundTime;
        private bool _coyoteTimeActive;
        private bool _wasGroundedLastFrame;

        // 점프 설정
        private readonly float _jumpCooldown = 0.1f;

        // Dependencies
        private readonly PhysicsSettings _physicsSettings;
        private readonly IGroundDetector _groundDetector;
        private readonly IPhysicsController _physicsController;

        // 반응형 속성
        private readonly ReactiveProperty<bool> _canJump = new();
        private readonly ReactiveProperty<bool> _isJumping = new();
        private readonly ReactiveProperty<bool> _isInCoyoteTime = new();
        private readonly ReactiveProperty<bool> _hasJumpBuffer = new();

        private readonly CompositeDisposable _disposables = new();

        #endregion

        #region Properties

        public Observable<IPlayerAction> OnJumpExecuted => _onJumpExecuted.AsObservable();

        /// <summary>점프 가능 여부 (반응형)</summary>
        public ReadOnlyReactiveProperty<bool> CanJump => _canJump.ToReadOnlyReactiveProperty();

        /// <summary>점프 중인지 여부 (반응형)</summary>
        public ReadOnlyReactiveProperty<bool> IsJumping => _isJumping.ToReadOnlyReactiveProperty();

        /// <summary>코요테 타임 중인지 여부 (반응형)</summary>
        public ReadOnlyReactiveProperty<bool> IsInCoyoteTime => _isInCoyoteTime.ToReadOnlyReactiveProperty();

        /// <summary>점프 버퍼가 활성화되어 있는지 여부 (반응형)</summary>
        public ReadOnlyReactiveProperty<bool> HasJumpBuffer => _hasJumpBuffer.ToReadOnlyReactiveProperty();

        /// <summary>점프 버튼이 눌려있는지 여부</summary>
        public bool IsJumpHeld => _jumpHeld;

        /// <summary>마지막 점프 실행 시간</summary>
        public float LastJumpTime => _lastJumpTime;

        /// <summary>마지막에 실행된 점프 액션</summary>
        public IPlayerAction LastExecutedJump => _lastExecutedJump;

        /// <summary>남은 코요테 타임</summary>
        public float RemainingCoyoteTime => Mathf.Max(0, _coyoteTime - (Time.time - _lastGroundTime));

        /// <summary>남은 점프 버퍼 시간</summary>
        public float RemainingJumpBuffer => Mathf.Max(0, _jumpBufferTime - (Time.time - _lastJumpInputTime));

        #endregion

        #region Constructor

        public PlayerJump(
            PhysicsSettings physicsSettings,
            PlayerMovementAbility movementAbility,
            Observable<bool> jumpPressed,
            IPhysicsController physicsController,
            IGroundDetector groundDetector,
            IWallDetector wallDetector)
        {
            GameLogger.Assert(physicsSettings != null, "PhysicsSettings Null", LogCategory.Player);
            GameLogger.Assert(movementAbility != null, "Movement Ability Null", LogCategory.Player);
            GameLogger.Assert(jumpPressed != null, "jumpPressed Null", LogCategory.Player);
            GameLogger.Assert(physicsController != null, "IPhysicsController Null", LogCategory.Player);
            GameLogger.Assert(groundDetector != null, "IGroundChecker Null", LogCategory.Player);
            GameLogger.Assert(wallDetector != null, "IWallDetector Null", LogCategory.Player);

            _physicsSettings = physicsSettings;
            _groundDetector = groundDetector;
            _physicsController = physicsController;

            // 설정값 저장
            _jumpBufferTime = movementAbility.JumpBufferTime;
            _coyoteTime = movementAbility.CoyoteTime;

            _jumpActions = new List<IPlayerAction>
            {
                new GroundJumpAction(movementAbility, physicsController, groundDetector),
                new WallJumpAction(movementAbility, physicsController, groundDetector, wallDetector, true),
                new AirJumpAction(movementAbility, physicsController, groundDetector),
            };

            SetupJumpInput(jumpPressed, physicsController);
            SetupCoyoteTimeSystem();
            SetupJumpBufferSystem();
        }

        #endregion

        #region Setup

        /// <summary>
        /// 점프 입력 처리 설정
        /// </summary>
        private void SetupJumpInput(Observable<bool> jumpPressed, IPhysicsController physicsController)
        {
            // 기본 점프 입력 처리
            jumpPressed
                .Subscribe(OnJumpInput)
                .AddTo(_disposables);

            // 점프 버튼 눌림/뗌 감지
            jumpPressed
                .Pairwise()
                .Subscribe(pair => OnJumpInputStateChanged(pair.Previous, pair.Current))
                .AddTo(_disposables);

            // 점프 가능 상태 업데이트
            physicsController.Velocity
                .CombineLatest(physicsController.IsRising, physicsController.IsFalling,
                    (velocity, isRising, isFalling) => new { velocity, isRising, isFalling })
                .Subscribe(data => UpdateJumpAvailability(data.velocity, data.isRising, data.isFalling))
                .AddTo(_disposables);

            // 점프 쿠다운 체크
            Observable.Interval(TimeSpan.FromSeconds(0.1f))
                .Subscribe(_ => CheckJumpCooldown())
                .AddTo(_disposables);
        }

        /// <summary>
        /// 코요테 타임 시스템 설정
        /// </summary>
        private void SetupCoyoteTimeSystem()
        {
            // 접지 상태 변화 감지
            _groundDetector.IsGrounded
                .Subscribe(OnGroundStateChanged)
                .AddTo(_disposables);

            // 코요테 타임 상태 업데이트 (매 프레임)
            Observable.EveryUpdate()
                .Subscribe(_ => UpdateCoyoteTime())
                .AddTo(_disposables);
        }

        /// <summary>
        /// 점프 버퍼 시스템 설정
        /// </summary>
        private void SetupJumpBufferSystem()
        {
            // 점프 버퍼 상태 업데이트 (매 프레임)
            Observable.EveryUpdate()
                .Subscribe(_ => UpdateJumpBuffer())
                .AddTo(_disposables);
        }

        #endregion

        #region Input Handling

        private void OnJumpInput(bool pressed)
        {
            _jumpPressed = pressed;
            _jumpHeld = pressed;

            if (pressed)
            {
                _jumpPressTime = Time.time;
                _lastJumpInputTime = Time.time; // 점프 버퍼용
                ProcessJumpPress();
            }
            else
            {
                ProcessJumpRelease();
            }
        }

        private void OnJumpInputStateChanged(bool previous, bool current)
        {
            // 점프 버튼이 눌렸을 때
            if (InputUtility.InputActivated(current ? 1f : 0f, previous ? 1f : 0f))
            {
                GameLogger.Debug("Jump button pressed", LogCategory.Player);
            }
            // 점프 버튼이 떼어졌을 때
            else if (InputUtility.InputDeactivated(current ? 1f : 0f, previous ? 1f : 0f))
            {
                GameLogger.Debug("Jump button released", LogCategory.Player);
            }
        }

        #endregion

        #region CoyoteTime System

        private void OnGroundStateChanged(bool isGrounded)
        {
            if (isGrounded && !_wasGroundedLastFrame)
            {
                // 착지 시
                _lastGroundTime = Time.time;
                GameLogger.Debug("Landed - Coyote time reset", LogCategory.Player);
            }
            else if (!isGrounded && _wasGroundedLastFrame)
            {
                // 땅에서 떨어질 때
                _lastGroundTime = Time.time;
                GameLogger.Debug("Left ground - Coyote time started", LogCategory.Player);
            }

            _wasGroundedLastFrame = isGrounded;
        }

        private void UpdateCoyoteTime()
        {
            bool wasCoyoteActive = _coyoteTimeActive;

            // 현재 땅에 있으면 코요테 타임 활성화
            if (_groundDetector.IsGrounded.CurrentValue)
            {
                _coyoteTimeActive = true;
            }
            else
            {
                // 땅에 없으면 마지막 접지 시간으로부터 코요테 타임 계산
                float timeSinceGrounded = Time.time - _lastGroundTime;
                _coyoteTimeActive = timeSinceGrounded <= _coyoteTime;
            }

            // 상태 변화가 있으면 이벤트 발생
            if (wasCoyoteActive != _coyoteTimeActive)
            {
                _isInCoyoteTime.OnNext(_coyoteTimeActive);

                if (!_coyoteTimeActive)
                {
                    GameLogger.Debug("Coyote time expired", LogCategory.Player);
                }
            }
        }

        #endregion

        #region JumpBuffer System

        private void UpdateJumpBuffer()
        {
            bool wasBufferActive = _jumpBufferActive;

            // 마지막 점프 입력으로부터 버퍼 시간 계산
            float timeSinceInput = Time.time - _lastJumpInputTime;
            _jumpBufferActive = timeSinceInput <= _jumpBufferTime;

            // 상태 변화가 있으면 이벤트 발생
            if (wasBufferActive != _jumpBufferActive)
            {
                _hasJumpBuffer.OnNext(_jumpBufferActive);

                if (!_jumpBufferActive)
                {
                    GameLogger.Debug("Jump buffer expired", LogCategory.Player);
                }
            }
        }

        #endregion

        #region Jump Processing

        private void ProcessJumpPress()
        {
            // 즉시 점프 시도
            if (TryExecuteJump())
            {
                // 점프 성공 시 버퍼 클리어
                ClearJumpBuffer();
                return;
            }

            // 즉시 점프가 안 되면 버퍼 활성화
            GameLogger.Debug("Jump buffered", LogCategory.Player);
        }

        private void ProcessJumpRelease()
        {
            // 점프 중에 버튼을 떼면 상승 속도 감소 (가변 점프 높이)
            if (_isJumping.Value)
            {
                HandleVariableJumpHeight();
            }
        }

        private bool TryExecuteJump()
        {
            // 쿨다운 체크
            if (!IsJumpCooldownExpired())
            {
                GameLogger.Debug("Jump ignored due to cooldown", LogCategory.Player);
                return false;
            }

            // 코요테 타임 또는 일반 점프 조건 확인
            bool canJumpFromGround = _coyoteTimeActive;
            foreach (var jumpAction in _jumpActions)
            {
                // GroundJump는 코요테 타임 동안 가능
                if (jumpAction is GroundJumpAction && canJumpFromGround)
                {
                    return ExecuteJumpAction(jumpAction);
                }
                else if (jumpAction is WallJumpAction && jumpAction.CanExecute())
                {
                    return ExecuteJumpAction(jumpAction);
                }
                // AirJump는 일반 조건 확인
                else if (jumpAction is AirJumpAction && jumpAction.CanExecute())
                {
                    return ExecuteJumpAction(jumpAction);
                }
            }

            return false;
        }

        private bool ExecuteJumpAction(IPlayerAction jumpAction)
        {
            try
            {
                jumpAction.Execute();

                // 상태 업데이트
                UpdateJumpState(jumpAction);

                // 이벤트 발생
                _onJumpExecuted.OnNext(jumpAction);
                _onSpecificJumpExecuted.OnNext(jumpAction);

                GameLogger.Debug(ZString.Concat("Jump executed: ", jumpAction.GetType().Name), LogCategory.Player);
                return true;
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error executing jump: ", e.Message), LogCategory.Player);
                return false;
            }
        }

        private void UpdateJumpState(IPlayerAction executedJump)
        {
            _lastExecutedJump = executedJump;
            _lastJumpTime = Time.time;
            _isJumping.OnNext(true);

            // 일정 시간 후 점프 상태 해제
            Observable.Timer(TimeSpan.FromSeconds(0.1f))
                .Subscribe(_ => _isJumping.OnNext(false))
                .AddTo(_disposables);
        }

        #endregion

        #region Jump State Management

        /// <summary>
        /// 점프 가능 상태 업데이트
        /// </summary>
        private void UpdateJumpAvailability(Vector2 velocity, bool isRising, bool isFalling)
        {
            bool canJump = false;

            // 코요테 타임 중이거나 에어 점프 가능한 경우
            if (_coyoteTimeActive)
            {
                canJump = true;
            }
            else
            {
                // 에어 점프 체크
                foreach (var jumpAction in _jumpActions)
                {
                    if (jumpAction.CanExecute())
                    {
                        canJump = true;
                        break;
                    }
                }
            }

            // 물리 상태를 고려한 추가 검증
            if (canJump)
            {
                // 터미널 속도에 가까우면 점프 불가
                if (PhysicsUtility.IsFalling(velocity) &&
                    Mathf.Abs(velocity.y) > Mathf.Abs(_physicsSettings.TerminalVelocity))
                {
                    canJump = false;
                }
            }

            _canJump.OnNext(canJump);

            // 점프 버퍼가 활성화되어 있고 점프 가능한 상태가 되면 자동 점프
            if (canJump && _jumpBufferActive)
            {
                GameLogger.Debug("Auto-jump from buffer", LogCategory.Player);
                if (TryExecuteJump())
                {
                    ClearJumpBuffer();
                }
            }
        }

        private void CheckJumpCooldown()
        {
            // 쿨다운이 만료되면 점프 상태 재평가
            if (IsJumpCooldownExpired() && !_canJump.Value)
            {
                // 점프 가능 상태 재검사
                UpdateJumpAvailability(Vector2.zero, false, false);
            }
        }

        /// <summary>
        /// 가변 점프 높이 처리
        /// </summary>
        private void HandleVariableJumpHeight()
        {
            // 마지막 점프가 GroundJump이고 상승 중이면 속도 감소
            if (_lastExecutedJump is GroundJumpAction)
            {
                var currentVelocity = _physicsController.GetVelocity();
                if (PhysicsUtility.IsRising(currentVelocity))
                {
                    // Reduce upward velocity by a factor (e.g., 50%)
                    var reducedVelocity = new Vector2(currentVelocity.x, currentVelocity.y * 0.5f);
                    _physicsController.RequestVelocity(VelocityRequest.Set(reducedVelocity));
                }

                GameLogger.Debug("Variable jump height applied", LogCategory.Player);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 점프 쿨다운이 만료되었는지 확인
        /// </summary>
        private bool IsJumpCooldownExpired()
        {
            return Time.time - _lastJumpTime >= _jumpCooldown;
        }

        /// <summary>
        /// 점프 버퍼 클리어
        /// </summary>
        private void ClearJumpBuffer()
        {
            _lastJumpInputTime = 0f;
            _jumpBufferActive = false;
            _hasJumpBuffer.OnNext(false);
            GameLogger.Debug("Jump buffer cleared", LogCategory.Player);
        }

        /// <summary>
        /// 코요테 타임 강제 종료
        /// </summary>
        public void ForceEndCoyoteTime()
        {
            _lastGroundTime = 0f;
            _coyoteTimeActive = false;
            _isInCoyoteTime.OnNext(false);
            GameLogger.Debug("Coyote time force ended", LogCategory.Player);
        }

        /// <summary>
        /// 특정 타입의 점프가 가능한지 확인
        /// </summary>
        public bool CanExecuteJumpType<T>() where T : IPlayerAction
        {
            foreach (var jumpAction in _jumpActions)
            {
                if (jumpAction is T && jumpAction.CanExecute())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 수동으로 점프 실행
        /// </summary>
        public void ForceJump()
        {
            TryExecuteJump();
        }

        /// <summary>
        /// 점프 상태 초기화
        /// </summary>
        public void ResetJumpState()
        {
            _jumpPressed = false;
            _jumpHeld = false;
            _isJumping.OnNext(false);
            _lastExecutedJump = null;
            ClearJumpBuffer();
            ForceEndCoyoteTime();
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 점프 정보
        /// </summary>
        public string GetJumpDebugInfo()
        {
            var cooldownRemaining = Mathf.Max(0, _jumpCooldown - (Time.time - _lastJumpTime));

            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== Player Jump Debug Info ===");
            sb.AppendLine(ZString.Concat("Can Jump: ", _canJump.Value));
            sb.AppendLine(ZString.Concat("Is Jumping: ", _isJumping.Value));
            sb.AppendLine(ZString.Concat("Jump Held: ", _jumpHeld));
            sb.AppendLine(ZString.Concat("Cooldown: ", cooldownRemaining.ToString("F2"), "s"));
            sb.AppendLine();
            sb.AppendLine("=== Coyote Time ===");
            sb.AppendLine(ZString.Concat("Is In Coyote: ", _coyoteTimeActive));
            sb.AppendLine(ZString.Concat("Remaining: ", RemainingCoyoteTime.ToString("F2"), "s"));
            sb.AppendLine();
            sb.AppendLine("=== Jump Buffer ===");
            sb.AppendLine(ZString.Concat("Buffer Active: ", _jumpBufferActive));
            sb.AppendLine(ZString.Concat("Remaining: ", RemainingJumpBuffer.ToString("F2"), "s"));
            sb.AppendLine();
            sb.Append(ZString.Concat("Last Jump: ", _lastExecutedJump?.GetType().Name ?? "None"));
            return sb.ToString();
        }

        /// <summary>
        /// 사용 가능한 점프 액션들 나열
        /// </summary>
        public string GetAvailableJumpActions()
        {
            using var sb = ZString.CreateStringBuilder();
            bool first = true;

            foreach (var jumpAction in _jumpActions)
            {
                if (jumpAction.CanExecute())
                {
                    if (!first) sb.Append(", ");
                    sb.Append(jumpAction.GetType().Name);
                    first = false;
                }
            }

            if (_coyoteTimeActive)
            {
                if (!first) sb.Append(", ");
                sb.Append("CoyoteJump");
            }

            return sb.ToString();
        }
#endif

        #endregion

        #region Disposal

        public void Dispose()
        {
            foreach (var jumpAction in _jumpActions)
            {
                jumpAction?.Dispose();
            }

            _onJumpExecuted?.Dispose();
            _onSpecificJumpExecuted?.Dispose();
            _canJump?.Dispose();
            _isJumping?.Dispose();
            _isInCoyoteTime?.Dispose();
            _hasJumpBuffer?.Dispose();
            _disposables?.Dispose();
        }

        #endregion
    }
}
