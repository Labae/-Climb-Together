using System;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using R3;
using UnityEngine;

namespace Gameplay.Player.Jump
{
    public class CoyoteTime : IDisposable
    {
        private readonly float _coyoteTime;
        private readonly IGroundDetector _groundDetector;
        private readonly ReactiveProperty<bool> _isActive = new();
        private readonly CompositeDisposable _disposables = new();

        private float _lastGroundTime;
        private bool _wasGroundedLastFrame;

        public ReadOnlyReactiveProperty<bool> IsActive => _isActive.ToReadOnlyReactiveProperty();
        public float RemainingTime => Mathf.Max(0, _coyoteTime - (Time.time - _lastGroundTime));

        public CoyoteTime(float coyoteTime, IGroundDetector groundDetector)
        {
            _coyoteTime = coyoteTime;
            _groundDetector = groundDetector ?? throw new ArgumentNullException(nameof(groundDetector));
            SetupCoyoteTimeTracking();
        }

        private void SetupCoyoteTimeTracking()
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
            bool wasCoyoteActive = _isActive.Value;
            bool newActive;

            // 현재 땅에 있으면 코요테 타임 활성화
            if (_groundDetector.IsGrounded.CurrentValue)
            {
                newActive = true;
            }
            else
            {
                // 땅에 없으면 마지막 접지 시간으로부터 코요테 타임 계산
                float timeSinceGrounded = Time.time - _lastGroundTime;
                newActive = timeSinceGrounded <= _coyoteTime;
            }

            // 상태 변화가 있으면 이벤트 발생
            if (wasCoyoteActive != newActive)
            {
                _isActive.OnNext(newActive);

                if (!newActive)
                {
                    GameLogger.Debug("Coyote time expired", LogCategory.Player);
                }
            }
        }

        public void ForceEnd()
        {
            _lastGroundTime = 0f;
            _isActive.OnNext(false);
        }

        public void Dispose()
        {
            _isActive.Dispose();
            _disposables.Dispose();
        }
    }
}
