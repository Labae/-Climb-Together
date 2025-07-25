using System;
using System.Collections.Generic;
using Data.Common;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Events;
using Gameplay.Player.Interfaces;
using Gameplay.Player.Jump;
using Gameplay.Player.Jump.Actions;
using R3;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player.Core
{
    public class PlayerJumpSystem : IDisposable
    {
        #region Fields

        private readonly CoyoteTime _coyoteTime;
        private readonly JumpBuffer _jumpBuffer;
        private readonly JumpCooldown _jumpCooldown;
        private readonly VariableJump _variableJump;
        private readonly JumpExecutor _jumpExecutor;

        private readonly ReactiveProperty<bool> _canJump = new();
        private readonly ReactiveProperty<bool> _isJumping = new();

        private readonly CompositeDisposable _disposables = new();

        private readonly PlayerEventBus _eventBus;
        private readonly IPhysicsController _physicsController;
        private readonly PhysicsSettings _physicsSettings;

        private bool _jumpHeld;

        #endregion

        #region Constructor

        public PlayerJumpSystem(PlayerEventBus eventBus, IPhysicsController physicsController,
            IGroundDetector groundDetector, IWallDetector wallDetector, Observable<bool> jumpPressed, PlayerMovementAbility movementAbility, PhysicsSettings physicsSettings)
        {
            GameLogger.Assert(eventBus != null, "PlayerJump eventBus is null", LogCategory.Player);
            GameLogger.Assert(physicsController != null, "PlayerJump physicsController is null", LogCategory.Player);
            GameLogger.Assert(groundDetector != null, "PlayerJump groundDetector is null", LogCategory.Player);
            GameLogger.Assert(wallDetector != null, "PlayerJump wallDetector is null", LogCategory.Player);
            GameLogger.Assert(jumpPressed != null, "PlayerJump jumpPressed is null", LogCategory.Player);
            GameLogger.Assert(movementAbility != null, "PlayerJump movementAbility is null", LogCategory.Player);

            _eventBus = eventBus;
            _physicsController = physicsController;
            _physicsSettings = physicsSettings;

            _coyoteTime = new CoyoteTime(movementAbility.CoyoteTime, groundDetector);
            _jumpBuffer = new JumpBuffer(movementAbility.JumpBufferTime);
            _jumpCooldown = new JumpCooldown();
            _variableJump = new VariableJump(movementAbility, physicsController, eventBus);
            _jumpExecutor = new JumpExecutor(
                CreateJumpActions(movementAbility, physicsController, groundDetector, wallDetector),
                eventBus,
                physicsController
            );

            SetupInputHandling(jumpPressed);
            SetupJumpAvailabilityTracking();
        }

        private List<IPlayerAction> CreateJumpActions(PlayerMovementAbility movementAbility, IPhysicsController physicsController, IGroundDetector groundDetector, IWallDetector wallDetector)
        {
            return new List<IPlayerAction>
            {
                new GroundJumpAction(movementAbility, physicsController, groundDetector),
                new WallJumpAction(movementAbility, physicsController, groundDetector, wallDetector),
                new AirJumpAction(movementAbility, physicsController, groundDetector)
            };
        }

        #endregion

        #region Setup

        private void SetupInputHandling(Observable<bool> jumpPressed)
        {
            // 기본 점프 입력 처리
            jumpPressed
                .Subscribe(OnJumpInput)
                .AddTo(_disposables);
        }

        private void SetupJumpAvailabilityTracking()
        {
            _coyoteTime.IsActive
                .Subscribe(_ => EvaluateJumpAvailability())
                .AddTo(_disposables);

            _physicsController.Velocity
                .ThrottleFirst(TimeSpan.FromSeconds(PhysicsUtility.UpdateFrequency))
                .Subscribe(_ => EvaluateJumpAvailability())
                .AddTo(_disposables);

            _jumpBuffer.IsActive
                .Where(isActive => isActive)
                .Subscribe(_ => TryAutoJump())
                .AddTo(_disposables);
        }

        #endregion

        #region Input Handling

        private void OnJumpInput(bool pressed)
        {
            _jumpHeld = pressed;

            if (pressed)
            {
                _jumpBuffer.RegisterJumpInput();
                _eventBus.Publish(new JumpInputEvent(true, _jumpHeld, Time.time));

                ProcessJumpPress();
            }
            else
            {
                _eventBus.Publish(new JumpInputEvent(false, _jumpHeld, 0f));

                ProcessJumpRelease();
            }
        }


        #endregion


        #region Jump Processing

        private void ProcessJumpPress()
        {
            // 즉시 점프 시도
            if (TryExecuteJump())
            {
                _jumpBuffer.ClearBuffer();
                return;
            }

            // 즉시 점프가 안 되면 버퍼 활성화
            GameLogger.Debug("Jump buffered", LogCategory.Player);
        }

        private void ProcessJumpRelease()
        {
        }

        private bool TryExecuteJump()
        {
            // 쿨다운 체크
            if (!_jumpCooldown.IsReady)
            {
                GameLogger.Debug("Jump ignored due to cooldown", LogCategory.Player);
                return false;
            }

            // 코요테 타임 또는 일반 점프 조건 확인
            bool canJumpFromGround = _coyoteTime.IsActive.CurrentValue;
            if (_jumpExecutor.TryExecuteJump(canJumpFromGround))
            {
                _jumpCooldown.RegisterJump();
                UpdateJumpState();
                return true;
            }

            return false;
        }

        private void UpdateJumpState()
        {
            _isJumping.OnNext(true);

            // 일정 시간 후 점프 상태 해제
            Observable.Timer(TimeSpan.FromSeconds(0.1f))
                .Subscribe(_ => _isJumping.OnNext(false))
                .AddTo(_disposables);
        }

        #endregion

        #region Jump State Management

        private void EvaluateJumpAvailability()
        {
            bool canJump = CanCurrentlyJump();
            _canJump.OnNext(canJump);
        }

        private bool CanCurrentlyJump()
        {
            if (_coyoteTime.IsActive.CurrentValue)
            {
                return true;
            }

            if (!_jumpExecutor.HasAvailableJumpActions())
            {
                return false;
            }

            var velocity = _physicsController.GetVelocity();
            if (PhysicsUtility.IsFalling(velocity) &&
                Mathf.Abs(velocity.y) > Mathf.Abs(_physicsSettings.TerminalVelocity))
            {
                return false;
            }
            return true;
        }

        private void TryAutoJump()
        {
            if (!_canJump.CurrentValue)
            {
                return;
            }

            GameLogger.Debug("Auto-jump from buffer", LogCategory.Player);
            if (TryExecuteJump())
            {
                _jumpBuffer.ClearBuffer();
            }
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            _variableJump?.Dispose();
            _coyoteTime?.Dispose();
            _jumpExecutor?.Dispose();
            _jumpBuffer?.Dispose();
            _canJump?.Dispose();
            _isJumping?.Dispose();
            _disposables?.Dispose();
        }

        #endregion
    }
}
