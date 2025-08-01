using System;
using Data.Common;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Physics;
using Gameplay.Platformer.Movement.Interface;
using R3;
using Systems.Input.Utilities;
using UnityEngine;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerDashHandler : IDisposable
    {
        private readonly PlatformerPhysicsSystem _physicsSystem;
        private readonly IPlatformerInput _platformerInput;
        private readonly PlatformerMovementSettings _settings;

        // Dash 상태 관리
        private bool _enabled = true;
        private bool _isDashing = false;
        private float _dashTimer = 0f;
        private Vector2 _dashDirection = Vector2.right;
        private bool _wasGroundedWhenDashStarted = false;
        private Vector2 _initialDashVelocity = Vector2.zero;

        // Cooldown & Count 관리
        private float _dashCooldownTimer = 0f;
        private int _currentDashCount = 0;

        // 현재 상태 추적
        private Vector2 _lastDirectionalInput = Vector2.zero;
        private Vector2 _currentLookDirection = Vector2.right;
        private float _currentHorizontalLookDirection = 0f;

        // 이벤트
        private readonly Subject<Vector2> _onDashStarted = new();
        private readonly Subject<Unit> _onDashEnded = new();
        private readonly Subject<Unit> _onDashReset = new();
        private readonly CompositeDisposable _disposables = new();

        public Observable<Vector2> OnDashStarted => _onDashStarted.AsObservable();
        public Observable<Unit> OnDashEnded => _onDashEnded.AsObservable();
        public Observable<Unit> OnDashReset => _onDashReset.AsObservable();

        public PlatformerDashHandler(
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
            // Dash 입력 구독
            _platformerInput.DashPressed
                .Subscribe(_ => TryDash())
                .AddTo(_disposables);

            // 방향 입력 추적 (Look Direction 업데이트용)
            _platformerInput.DirectionalInput
                .Subscribe(UpdateDirectionalInput)
                .AddTo(_disposables);
        }

        private void SubscribeToPhysicsEvents()
        {
            // 착지 시 대시 카운트 리셋
            _physicsSystem.OnLanded
                .Where(_ => !_isDashing)
                .Subscribe(_ => ResetDashCount())
                .AddTo(_disposables);
        }

        private void UpdateDirectionalInput(Vector2 input)
        {
            _lastDirectionalInput = input;

            // 방향 입력이 있으면 Look Direction 업데이트
            if (InputUtility.IsInputActive(input))
            {
                _currentLookDirection = input.normalized;

                if (InputUtility.IsInputActive(input.x))
                {
                    _currentHorizontalLookDirection = input.x;
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Dash Handler 활성화/비활성화
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// 현재 대시 중인지
        /// </summary>
        public bool IsDashing() => _isDashing;

        /// <summary>
        /// 대시 가능 여부
        /// </summary>
        public bool CanDash()
        {
            if (!_enabled) return false;
            if (_isDashing) return false;
            if (_dashCooldownTimer > 0f) return false;
            if (_currentDashCount >= _settings.MaxDashCount) return false;

            return true;
        }

        /// <summary>
        /// 현재 대시 카운트 반환
        /// </summary>
        public int GetCurrentDashCount() => _currentDashCount;

        /// <summary>
        /// 대시 쿨다운 남은 시간
        /// </summary>
        public float GetDashCooldownRemaining() => Mathf.Max(0f, _dashCooldownTimer);

        /// <summary>
        /// Update 호출
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_enabled) return;

            UpdateDashTimer(deltaTime);
            UpdateCooldownTimer(deltaTime);

            if (_isDashing)
            {
                ApplyDashCurve();
            }
        }

        #endregion

        #region Dash Logic

        /// <summary>
        /// 대시 시도
        /// </summary>
        private void TryDash()
        {
            if (!CanDash()) return;

            Vector2 dashDirection = CalculateDashDirection();
            PerformDash(dashDirection);
        }

        /// <summary>
        /// 대시 방향 계산
        /// </summary>
        private Vector2 CalculateDashDirection()
        {
            if (InputUtility.IsInputActive(_lastDirectionalInput))
            {
                // 대각선 방향 스냅 처리 (8방향으로 제한)
                return SnapToEightDirections(_lastDirectionalInput.normalized);
            }
            else
            {
                // 바라보는 방향으로 대시 (수평만)
                return new Vector2(_currentHorizontalLookDirection, 0f).normalized;
            }
        }

        /// <summary>
        /// 8방향으로 스냅
        /// </summary>
        private Vector2 SnapToEightDirections(Vector2 direction)
        {
            // 8방향 벡터들
            Vector2[] eightDirections =
            {
                Vector2.right, // 0°
                new Vector2(1, 1).normalized, // 45°
                Vector2.up, // 90°
                new Vector2(-1, 1).normalized, // 135°
                Vector2.left, // 180°
                new Vector2(-1, -1).normalized, // 225°
                Vector2.down, // 270°
                new Vector2(1, -1).normalized // 315°
            };

            Vector2 closestDirection = Vector2.right;
            float closestDot = -1f;

            foreach (var dir in eightDirections)
            {
                float dot = Vector2.Dot(direction, dir);
                if (dot > closestDot)
                {
                    closestDot = dot;
                    closestDirection = dir;
                }
            }

            return closestDirection;
        }

        /// <summary>
        /// 실제 대시 실행
        /// </summary>
        private void PerformDash(Vector2 direction)
        {
            _isDashing = true;
            _dashTimer = _settings.DashDuration;
            _dashDirection = direction;
            _currentDashCount++;

            _wasGroundedWhenDashStarted = _physicsSystem.IsGrounded.CurrentValue;

            // Physics에 대시 적용
            _initialDashVelocity = _settings.DashSpeed * _dashDirection.normalized;
            _physicsSystem.SetVelocity(_initialDashVelocity);

            // 대시 중 중력 상태 설정(0f)
            _physicsSystem.SetGravityState(PlatformerGravityState.Dashing);

            // 이벤트 발생
            _onDashStarted.OnNext(direction);
        }

        /// <summary>
        /// 대시 종료 처리
        /// </summary>
        private void EndDash()
        {
            if (!_isDashing) return;

            _isDashing = false;
            _dashTimer = 0f;
            _dashCooldownTimer = _settings.DashCooldown;
            _initialDashVelocity = Vector2.zero;

            // 대쉬 종료 시 속도 처리
            HandleDashEndVelocity();

            // 중력 상태를 복원
            RestoreGravityState();

            // 땅에서 시작한 대쉬라면 일정 시간 후 카운트 리셋
            if (_wasGroundedWhenDashStarted)
            {
                ResetDashCountWithDelay();
            }

            // 이벤트 발생
            _onDashEnded.OnNext(Unit.Default);
        }

        private void HandleDashEndVelocity()
        {
            var currentVelocity = _physicsSystem.Velocity
                .CurrentValue;

            var endSpeedRatio = _settings.DashEndSpeedRatio;
            var endVelocity = currentVelocity * endSpeedRatio;

            if (Mathf.Abs(_dashDirection.y) > 0.1f)
            {
                endVelocity.y *= _settings.DashEndVerticalSpeedRatio;
            }

            _physicsSystem.SetVelocity(endVelocity);
        }

        private void RestoreGravityState()
        {
            if (_physicsSystem.IsGrounded.CurrentValue)
            {
                _physicsSystem.SetGravityState(PlatformerGravityState.Normal);
            }
            else
            {
                _physicsSystem.SetGravityState(PlatformerGravityState.Falling);
            }
        }

        private void ResetDashCountWithDelay()
        {
            Observable.Timer(TimeSpan.FromSeconds(_settings.DashCooldown))
                .Subscribe(_ => ResetDashCount())
                .AddTo(_disposables);
        }

        private void ApplyDashCurve()
        {
            var normalizedTime = 1f - (_dashTimer / _settings.DashDuration);
            var speedMultiplier = _settings.DashSpeedCurve.Evaluate(normalizedTime);
            var currentVelocity = _initialDashVelocity * speedMultiplier;

            _physicsSystem.SetVelocity(currentVelocity);
        }

        /// <summary>
        /// 대시 카운트 리셋 (착지 시)
        /// </summary>
        private void ResetDashCount()
        {
            _currentDashCount = 0;
            _onDashReset.OnNext(Unit.Default);
        }

        #endregion

        #region Update Logic

        /// <summary>
        /// 대시 타이머 업데이트
        /// </summary>
        private void UpdateDashTimer(float deltaTime)
        {
            if (_isDashing)
            {
                _dashTimer -= deltaTime;

                if (_dashTimer <= 0f)
                {
                    EndDash();
                }
            }
        }

        /// <summary>
        /// 쿨다운 타이머 업데이트
        /// </summary>
        private void UpdateCooldownTimer(float deltaTime)
        {
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= deltaTime;
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _onDashStarted?.Dispose();
            _onDashEnded?.Dispose();
            _disposables?.Dispose();
        }

        #endregion
    }
}
