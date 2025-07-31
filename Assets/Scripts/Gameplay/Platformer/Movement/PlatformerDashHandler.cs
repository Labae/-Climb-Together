using System;
using Data.Common;
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

        // Cooldown & Count 관리
        private float _dashCooldownTimer = 0f;
        private int _currentDashCount = 0;
        private bool _dashCountReset = true; // 땅에 닿으면 리셋

        // 현재 상태 추적
        private Vector2 _lastDirectionalInput = Vector2.zero;
        private Vector2 _currentLookDirection = Vector2.right;

        // 이벤트
        private readonly Subject<Vector2> _onDashStarted = new();
        private readonly Subject<Unit> _onDashEnded = new();
        private readonly CompositeDisposable _disposables = new();

        public Observable<Vector2> OnDashStarted => _onDashStarted.AsObservable();
        public Observable<Unit> OnDashEnded => _onDashEnded.AsObservable();

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

            // 수평 이동 입력으로 Look Direction 업데이트
            _platformerInput.MovementInput
                .Where(input => InputUtility.IsInputActive(input))
                .Subscribe(input => _currentLookDirection = new Vector2(Mathf.Sign(input), 0))
                .AddTo(_disposables);
        }

        private void SubscribeToPhysicsEvents()
        {
            // 착지 시 대시 카운트 리셋
            _physicsSystem.OnLanded
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
        }

        #endregion

        #region Dash Logic

        /// <summary>
        /// 대시 시도 (Celeste 스타일)
        /// </summary>
        private void TryDash()
        {
            if (!CanDash()) return;

            Vector2 dashDirection = CalculateDashDirection();
            PerformDash(dashDirection);
        }

        /// <summary>
        /// Celeste 스타일 대시 방향 계산
        /// </summary>
        private Vector2 CalculateDashDirection()
        {
            if (InputUtility.IsInputActive(_currentLookDirection))
            {
                Vector2 direction = _currentLookDirection.normalized;

                // 대각선 방향 스냅 처리 (8방향으로 제한)
                return SnapToEightDirections(direction);
            }
            else
            {
                // 바라보는 방향으로 대시 (수평만)
                return _currentLookDirection;
            }
        }

        /// <summary>
        /// 8방향으로 스냅
        /// </summary>
        private Vector2 SnapToEightDirections(Vector2 direction)
        {
            // 8방향 벡터들
            Vector2[] eightDirections = {
                Vector2.right,              // 0°
                new Vector2(1, 1).normalized,    // 45°
                Vector2.up,                 // 90°
                new Vector2(-1, 1).normalized,   // 135°
                Vector2.left,               // 180°
                new Vector2(-1, -1).normalized,  // 225°
                Vector2.down,               // 270°
                new Vector2(1, -1).normalized    // 315°
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

            // Physics에 대시 적용
            _physicsSystem.Dash(direction, _settings.DashSpeed);

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
            ResetDashCount();

            // 이벤트 발생
            _onDashEnded.OnNext(Unit.Default);
        }

        /// <summary>
        /// 대시 카운트 리셋 (착지 시)
        /// </summary>
        private void ResetDashCount()
        {
            if (_currentDashCount > 0)
            {
                _currentDashCount = 0;
            }
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
