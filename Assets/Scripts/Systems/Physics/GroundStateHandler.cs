using System;
using Data.Common;
using R3;
using UnityEngine;

namespace Systems.Physics
{
    public class GroundStateHandler : IDisposable
    {
        private bool _isCurrentlyGrounded = false;
        private readonly Transform _transform;
        private readonly CollisionHandler _collisionHandler;
        private readonly PhysicsSettings _settings;

        private readonly ReactiveProperty<bool> _isGrounded;
        private readonly Subject<Unit> _onLanded = new();
        private readonly Subject<Unit> _onLeftGround = new();

        public ReadOnlyReactiveProperty<bool> IsGrounded => _isGrounded.ToReadOnlyReactiveProperty();
        public Observable<Unit> OnLanded => _onLanded;
        public Observable<Unit> OnLeftGround => _onLeftGround;

        public GroundStateHandler(Transform transform, CollisionHandler collisionHandler, PhysicsSettings settings)
        {
            _transform = transform;
            _collisionHandler = collisionHandler;
            _settings = settings;
            _isGrounded = new ReactiveProperty<bool>(_isCurrentlyGrounded);
        }

        public bool GetGroundState() => _isCurrentlyGrounded;

        public void SetGroundState(bool isGrounded)
        {
            bool wasGrounded = _isCurrentlyGrounded;
            _isCurrentlyGrounded = isGrounded;

            if (!wasGrounded && isGrounded)
            {
                _onLanded.OnNext(Unit.Default);
            }
            else if (wasGrounded && !isGrounded)
            {
                _onLeftGround.OnNext(Unit.Default);
            }

            _isGrounded.OnNext(isGrounded);
        }

        public void UpdateGroundState(VelocityHandler velocityHandler)
        {
            var groundResult = _collisionHandler.GetCurrentGroundResult();
            bool shouldBeGrounded = groundResult.HasCollision
                                    && groundResult.Hit.point.y >=
                                    _transform.position.y - _settings.GroundCheckDistnace;

            if (_isCurrentlyGrounded && !shouldBeGrounded)
            {
                SetGroundState(false);
            }
            else if (!_isCurrentlyGrounded && shouldBeGrounded && velocityHandler.GetVelocity().y <= 0)
            {
                SetGroundState(true);
            }
        }

        public void Dispose()
        {
            _isGrounded?.Dispose();
            _onLanded?.Dispose();
            _onLeftGround?.Dispose();
        }
    }
}
