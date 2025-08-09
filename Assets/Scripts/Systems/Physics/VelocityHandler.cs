using System;
using Data.Common;
using R3;
using UnityEngine;

namespace Systems.Physics
{
    public class VelocityHandler : IDisposable
    {
        private Vector3 _currentVelocity = Vector3.zero;
        private readonly PhysicsSettings _settings;

        public bool HorizontalLocked { get; private set; } = false;
        public bool VerticalLocked { get; private set; } = false;

        private readonly ReactiveProperty<Vector3> _velocity;
        public ReadOnlyReactiveProperty<Vector3> Velocity => _velocity;

        public VelocityHandler(PhysicsSettings settings)
        {
            _settings = settings;
            _velocity = new ReactiveProperty<Vector3>(_currentVelocity);
        }

        public Vector3 GetVelocity() => _currentVelocity;

        public void SetVelocity(Vector3 velocity)
        {
            _currentVelocity = velocity;
            ApplyConstraints();
            _velocity.OnNext(_currentVelocity);
        }

        public void AddVelocity(Vector3 velocity)
        {
            _currentVelocity += velocity;
            ApplyConstraints();
            _velocity.OnNext(_currentVelocity);
        }

        public void SetHorizontalVelocity(float x)
        {
            if (HorizontalLocked)
            {
                return;
            }

            _currentVelocity.x = x;
            ApplyConstraints();
            _velocity.OnNext(_currentVelocity);
        }

        public void SetVerticalVelocity(float y)
        {
            if (VerticalLocked)
            {
                return;
            }

            _currentVelocity.y = y;
            ApplyConstraints();
            _velocity.OnNext(_currentVelocity);
        }

        public void Stop()
        {
            SetVelocity(Vector3.zero);
        }

        public void StopHorizontal()
        {
            SetHorizontalVelocity(0f);
        }

        public void StopVertical()
        {
            SetVerticalVelocity(0f);
        }

        public void LockHorizontal(bool locked)
        {
            HorizontalLocked = locked;
        }

        public void LockVertical(bool locked)
        {
            VerticalLocked = locked;
        }

        private void ApplyConstraints()
        {
            if (HorizontalLocked)
            {
                _currentVelocity.x = 0f;
            }

            if (VerticalLocked)
            {
                _currentVelocity.y = 0f;
            }

            _currentVelocity.x = Mathf.Clamp(_currentVelocity.x, -_settings.MaxHorizontalSpeed, _settings.MaxHorizontalSpeed);
            _currentVelocity.y = Mathf.Clamp(_currentVelocity.y, -_settings.MaxVerticalSpeed, _settings.MaxVerticalSpeed);
        }

        public void Dispose()
        {
            _velocity?.Dispose();
        }
    }
}
