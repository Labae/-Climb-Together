using System;
using Data.Common;
using Gameplay.Common.Enums;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using Gameplay.Platformer.Physics;
using R3;
using Systems.Physics.Enums;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerWallHandler : IDisposable
    {
        private readonly PlatformerPhysicsSystem _physicsSystem;
        private readonly IPlatformerInput _platformerInput;
        private readonly PlatformerMovementSettings _settings;

        // 벽 상태 관리
        private bool _enabled = true;
        private bool _isWallSliding = false;
        private WallSideType _currentWallSide = WallSideType.None;
        private float _wallInputDirection = 0f;

        // 벽 점프 관련
        private float _wallJumpInputLockTimer = 0f;

        private float _instantWallJumpBufferTimer = 0f;
        private WallSideType _lastWallSide = WallSideType.None;
        private readonly float _instantWallJumpBufferTime = 0.1f;

        // 이벤트
        private readonly Subject<WallSideType> _onWallSlideStarted = new();
        private readonly Subject<Unit> _onWallSlideEnded = new();
        private readonly Subject<Vector2> _onWallJumped = new();
        private readonly CompositeDisposable _disposables = new();

        public Observable<WallSideType> OnWallSlideStarted => _onWallSlideStarted;
        public Observable<Unit> OnWallSlideEnded => _onWallSlideEnded;
        public Observable<Vector2> OnWallJumped => _onWallJumped;

        public PlatformerWallHandler(
            PlatformerPhysicsSystem physicsSystem,
            IPlatformerInput platformerInput,
            PlatformerMovementSettings settings)
        {
            _physicsSystem = physicsSystem;
            _platformerInput = platformerInput;
            _settings = settings;

            SubscribeToInputs();
            SubscribeToPhysicsEvents();
        }

        #region Input & Physics Subscription

        private void SubscribeToInputs()
        {
            // 수평 입력 추적
            _platformerInput.MovementInput
                .Subscribe(HandleMovementInput)
                .AddTo(_disposables);

            // 점프 입력 (벽 점프)
            _platformerInput.JumpPressed
                .Where(pressed => pressed)
                .Subscribe(_ => TryWallJump())
                .AddTo(_disposables);
        }

        private void SubscribeToPhysicsEvents()
        {
            _physicsSystem.OnLanded
                .Subscribe(_ => EndWallSlide())
                .AddTo(_disposables);
        }

        private void HandleMovementInput(float input)
        {
            _wallInputDirection = input;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Wall Handler 활성화/비활성화
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// 현재 벽 슬라이딩 중인지
        /// </summary>
        public bool IsWallSliding() => _isWallSliding;

        /// <summary>
        /// 현재 붙어있는 벽 방향
        /// </summary>
        public WallSideType GetCurrentWallSide() => _currentWallSide;

        /// <summary>
        /// 벽 점프 가능 여부
        /// </summary>
        public bool CanWallJump()
        {
            return _enabled &&
                   !_physicsSystem.IsGrounded.CurrentValue &&
                   (_currentWallSide != WallSideType.None || _instantWallJumpBufferTimer > 0f);
        }

        /// <summary>
        /// Update 호출
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_enabled)
            {
                return;
            }

            UpdateWallJumpInputLock(deltaTime);
            UpdateInstantWallJumpBuffer(deltaTime);
            UpdateWallDetection();
            UpdateWallSliding();
        }

        #endregion

        #region Wall Detection

        private void UpdateWallSliding()
        {
            if (!CanWallSlide())
            {
                if (_isWallSliding)
                {
                    EndWallSlide();
                }

                return;
            }

            if (_isWallSliding)
            {
                ApplyWallSlidePhysics();
            }
            else
            {
                StartWallSlide();
            }
        }

        private bool CanWallSlide()
        {
            if (!_enabled || _physicsSystem.IsGrounded.CurrentValue)
            {
                return false;
            }

            bool inputTowardWall = (_currentWallSide == WallSideType.Left && _wallInputDirection < -0.1f) ||
                                   (_currentWallSide == WallSideType.Right && _wallInputDirection > 0.1f);
            return inputTowardWall && PhysicsUtility.IsFalling(_physicsSystem.Velocity.CurrentValue);
        }

        private void UpdateWallDetection()
        {
            var previousWallSide = _currentWallSide;
            _currentWallSide = DetectWallSide();

            if (previousWallSide != _currentWallSide)
            {
                if (_currentWallSide == WallSideType.None)
                {
                    EndWallSlide();
                }
                else if (previousWallSide == WallSideType.None)
                {
                    StartInstantWallJumpBuffer(_currentWallSide);
                }
            }
        }

        private WallSideType DetectWallSide()
        {
            if (_physicsSystem.IsGrounded.CurrentValue)
            {
                return WallSideType.None;
            }

            var leftResult = _physicsSystem.CheckDirectionWithSurface(Vector2.left);
            var rightResult = _physicsSystem.CheckDirectionWithSurface(Vector2.right);

            if (leftResult is { HasCollision: true, SurfaceType: SurfaceType.Wall })
            {
                return WallSideType.Left;
            }
            if (rightResult is { HasCollision: true, SurfaceType: SurfaceType.Wall })
            {
                return WallSideType.Right;
            }

            return WallSideType.None;
        }

        #endregion

        #region Wall Sliding

        private void StartWallSlide()
        {
            if (_isWallSliding)
            {
                return;
            }

            _isWallSliding = true;

            _physicsSystem.SetGravityState(PlatformerGravityState.WallSliding);
            _onWallSlideStarted.OnNext(_currentWallSide);
        }

        private void EndWallSlide()
        {
            if (!_isWallSliding)
            {
                return;
            }

            _isWallSliding = false;

            if (!_physicsSystem.IsGrounded.CurrentValue)
            {
                _physicsSystem.SetGravityState(PlatformerGravityState.Falling);
            }
            else
            {
                _physicsSystem.SetGravityState(PlatformerGravityState.Normal);
            }

            _onWallSlideEnded.OnNext(Unit.Default);
        }

        private void ApplyWallSlidePhysics()
        {
            var currentVelocity = _physicsSystem.Velocity.CurrentValue;
            if (currentVelocity.y < -_settings.WallSlideSpeed)
            {
                _physicsSystem.SetVelocity(new Vector2(currentVelocity.x, -_settings.WallSlideSpeed));
            }
        }

        #endregion

        #region Wall Jump

        private void TryWallJump()
        {
            if (!CanWallJump())
            {
                return;
            }

            PerformWallJump();
        }

        private void PerformWallJump()
        {
            var wallSideForJump = _currentWallSide !=  WallSideType.None ? _currentWallSide : _lastWallSide;
            // 벽 반대방향으로 점프
            var jumpDirection = wallSideForJump == WallSideType.Left
                ? new Vector2(_settings.WallJumpDirection.x, _settings.WallJumpDirection.y)
                : new Vector2(-_settings.WallJumpDirection.x, _settings.WallJumpDirection.y);

            jumpDirection.Normalize();
            var jumpVelocity = jumpDirection * _settings.WallJumpForce;
            _physicsSystem.SetVelocity(jumpVelocity);
            _physicsSystem.SetGravityState(PlatformerGravityState.JumpHold);

            // 벽 슬라이딩 종료
            EndWallSlide();
            _instantWallJumpBufferTimer = 0f;
            _lastWallSide = WallSideType.None;

            // 입력 락 시작
            _wallJumpInputLockTimer = _settings.WallJumpInputLockTime;

            _onWallJumped.OnNext(jumpDirection);
        }

        private void UpdateWallJumpInputLock(float deltaTime)
        {
            if (_wallJumpInputLockTimer > 0)
            {
                _wallJumpInputLockTimer -= deltaTime;
            }
        }

        private void StartInstantWallJumpBuffer(WallSideType wallSide)
        {
            _instantWallJumpBufferTimer = _instantWallJumpBufferTime;
            _lastWallSide =  wallSide;
        }

        private void UpdateInstantWallJumpBuffer(float deltaTime)
        {
            if (_instantWallJumpBufferTimer > 0f)
            {
                _instantWallJumpBufferTimer -= deltaTime;

                if (_instantWallJumpBufferTimer <= 0f)
                {
                    _lastWallSide = WallSideType.None;
                }
            }
        }

        public bool IsHorizontalInputLocked()
        {
            return _wallJumpInputLockTimer > 0;
        }

        #endregion

        public void Dispose()
        {
            _onWallSlideStarted.Dispose();
            _onWallSlideEnded.Dispose();
            _onWallJumped.Dispose();
            _disposables.Dispose();
        }
    }
}
