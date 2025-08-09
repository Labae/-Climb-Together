using System;
using Data.Common;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using Gameplay.Platformer.Physics;
using R3;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerJumpHandler : IDisposable
    {
        private readonly PlatformerPhysicsSystem _physicsSystem;
        private readonly IPlatformerInput _platformerInput;
        private readonly PlatformerMovementSettings _settings;

        // Jump 관련 상태들
        private float _coyoteTimer = 0f;
        private bool _jumpBuffered = false;
        private float _jumpBufferTimer = 0f;
        private float _apexTimer = 0f;
        private bool _isJumping = false;
        private bool _enabled = true;

        // 이벤트
        private readonly Subject<Unit> _onJumpStarted = new();
        private readonly CompositeDisposable _disposables = new();

        public Observable<Unit> OnJumpStarted => _onJumpStarted.AsObservable();

        public PlatformerJumpHandler(
            PlatformerPhysicsSystem physicsSystem,
            IPlatformerInput platformerInput,
            PlatformerMovementSettings settings
        )
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
            // 점프 입력 구독
            _platformerInput.JumpPressed
                .Where(pressed => pressed)
                .Subscribe(_ => Jump())
                .AddTo(_disposables);

            // 점프 컷 입력 구독
            _platformerInput.JumpPressed
                .Where(pressed => !pressed)
                .Where(_ => CanJumpCut())
                .Subscribe(_ => JumpCut())
                .AddTo(_disposables);
        }

        private void SubscribeToPhysicsEvents()
        {
            // 착지 이벤트 구독
            _physicsSystem.OnLanded
                .Subscribe(_ => HandleLanded())
                .AddTo(_disposables);

            // 땅에서 떨어질 때 코요테 타임 시작
            _physicsSystem.IsGrounded
                .Pairwise()
                .Where(pair => pair.Previous && !pair.Current)
                .Subscribe(_ => _coyoteTimer = _settings.CoyoteTime)
                .AddTo(_disposables);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Jump Handler 활성화/비활성화 (Special Action 시 사용)
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// 점프 가능 여부 확인
        /// </summary>
        public bool CanJump()
        {
            if (!_enabled) return false;
            return _physicsSystem.IsGrounded.CurrentValue || _coyoteTimer > 0f;
        }

        /// <summary>
        /// 현재 점프 중인지 여부
        /// </summary>
        public bool IsJumping() => _isJumping;

        /// <summary>
        /// 점프 컷 가능 여부 확인
        /// </summary>
        public bool CanJumpCut()
        {
            if (!_enabled) return false;
            return _isJumping && IsRising();
        }

        /// <summary>
        /// Update 호출 (매 프레임)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_enabled)
            {
                return;
            }

            UpdateGravityState(deltaTime);
            UpdateTimers(deltaTime);
            ProcessJumpBuffer();
        }

        #endregion

        #region Jump Logic

        /// <summary>
        /// 점프 시도
        /// </summary>
        public void Jump()
        {
            if (!_enabled)
            {
                return;
            }

            if (CanJump())
            {
                PerformJump();
            }
            else
            {
                // 점프 버퍼링
                _jumpBuffered = true;
                _jumpBufferTimer = _settings.JumpBufferTime;
            }
        }

        /// <summary>
        /// 실제 점프 실행
        /// </summary>
        private void PerformJump()
        {
            _physicsSystem.Jump();
            _physicsSystem.SetGravityState(PlatformerGravityState.JumpHold);

            _coyoteTimer = 0f;
            _jumpBuffered = false;
            _isJumping = true;

            _onJumpStarted.OnNext(Unit.Default);
        }

        /// <summary>
        /// 점프 컷 실행
        /// </summary>
        private void JumpCut()
        {
            if (!_enabled)
            {
                return;
            }

            var velocity = _physicsSystem.Velocity.CurrentValue;
            _physicsSystem.SetVelocity(new Vector2(velocity.x, velocity.y * _settings.JumpCutStrength));
            _physicsSystem.SetGravityState(PlatformerGravityState.JumpCut);
            _isJumping = false;
        }

        /// <summary>
        /// 착지 처리
        /// </summary>
        private void HandleLanded()
        {
            _coyoteTimer = _settings.CoyoteTime;
            _apexTimer = 0f;
            _isJumping = false;
            _physicsSystem.SetGravityState(PlatformerGravityState.Normal);
        }

        #endregion

        #region Update Logic

        /// <summary>
        /// 중력 상태 업데이트
        /// </summary>
        private void UpdateGravityState(float deltaTime)
        {
            if (IsGrounded())
            {
                _apexTimer = 0f;
                return;
            }

            var velocity = _physicsSystem.Velocity.CurrentValue;
            bool isAtApex = Mathf.Abs(velocity.y) < _settings.ApexThreshold;

            // 1순위: Apex 처리
            if (isAtApex && !IsGrounded())
            {
                if (_apexTimer < _settings.ApexDuration)
                {
                    _physicsSystem.SetGravityState(PlatformerGravityState.Apex);
                    _apexTimer += deltaTime;
                    return;
                }
            }
            else
            {
                _apexTimer = 0f;
            }

            // 2순위: 점프 중 + 상승 중 (JumpHold로 복귀)
            if (_isJumping && IsRising())
            {
                _physicsSystem.SetGravityState(PlatformerGravityState.JumpHold);
                return;
            }

            // 3순위: 점프 중 + 하강 시작 (Falling으로 전환)
            if (_isJumping && IsFalling())
            {
                _isJumping = false;
                _physicsSystem.SetGravityState(PlatformerGravityState.Falling);
                return;
            }

            // 4순위: 일반 낙하
            if (!_isJumping && IsFalling())
            {
                _physicsSystem.SetGravityState(PlatformerGravityState.Falling);
            }
        }

        /// <summary>
        /// 타이머들 업데이트
        /// </summary>
        private void UpdateTimers(float deltaTime)
        {
            // Coyote Timer
            if (_coyoteTimer > 0f)
            {
                _coyoteTimer -= deltaTime;
            }

            // Jump Buffer Timer
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= deltaTime;
                if (_jumpBufferTimer <= 0f)
                {
                    _jumpBuffered = false;
                }
            }
        }

        /// <summary>
        /// 점프 버퍼 처리
        /// </summary>
        private void ProcessJumpBuffer()
        {
            if (_jumpBuffered && CanJump())
            {
                PerformJump();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>현재 땅에 있는지</summary>
        private bool IsGrounded() => _physicsSystem.IsGrounded.CurrentValue;

        /// <summary>현재 상승 중인지</summary>
        private bool IsRising() => PhysicsUtility.IsRising(_physicsSystem.Velocity.CurrentValue);

        /// <summary>현재 하강 중인지</summary>
        private bool IsFalling() => PhysicsUtility.IsFalling(_physicsSystem.Velocity.CurrentValue);

        #endregion

        #region Dispose

        public void Dispose()
        {
            _onJumpStarted?.Dispose();
            _disposables?.Dispose();
        }

        #endregion
    }
}
