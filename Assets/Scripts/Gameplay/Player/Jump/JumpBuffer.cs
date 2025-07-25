using System;
using Debugging;
using Debugging.Enum;
using R3;
using UnityEngine;

namespace Gameplay.Player.Jump
{
    public class JumpBuffer : IDisposable
    {
        private readonly float _jumpBufferTime;
        private readonly ReactiveProperty<bool> _isActive = new();
        private readonly CompositeDisposable _disposables = new();

        private float _lastJumpInputTime;

        public ReadOnlyReactiveProperty<bool> IsActive => _isActive.ToReadOnlyReactiveProperty();
        public float RemainingTime => Mathf.Max(0f, _jumpBufferTime - (Time.time - _lastJumpInputTime));

        public JumpBuffer(float bufferTime)
        {
            _jumpBufferTime = bufferTime;
            SetupBufferTracking();
        }

        private void SetupBufferTracking()
        {
            // 점프 버퍼 상태 업데이트 (매 프레임)
            Observable.EveryUpdate()
                .Subscribe(_ => UpdateJumpBuffer())
                .AddTo(_disposables);
        }

        private void UpdateJumpBuffer()
        {
            bool wasBufferActive = _isActive.Value;

            // 마지막 점프 입력으로부터 버퍼 시간 계산
            float timeSinceInput = Time.time - _lastJumpInputTime;
            bool newActive = timeSinceInput <= _jumpBufferTime;

            // 상태 변화가 있으면 이벤트 발생
            if (wasBufferActive != newActive)
            {
                _isActive.OnNext(newActive);

                if (!newActive)
                {
                    GameLogger.Debug("Jump buffer expired", LogCategory.Player);
                }
            }
        }

        public void RegisterJumpInput()
        {
            _lastJumpInputTime = Time.time;
        }

        public void ClearBuffer()
        {
            _lastJumpInputTime = 0f;
            _isActive.OnNext(false);
        }

        public void Dispose()
        {
            _isActive.Dispose();
            _disposables.Dispose();
        }
    }
}
