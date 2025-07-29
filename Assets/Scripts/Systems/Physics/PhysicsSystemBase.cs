using System;
using Data.Common;
using R3;
using UnityEngine;

namespace Systems.Physics
{
    public abstract class PhysicsSystemBase : IDisposable
    {
        protected readonly Transform _transform;
        protected readonly PhysicsSettings _settings;

        protected readonly VelocityHandler _velocityHandler;
        protected readonly GravityHandler _gravityHandler;
        protected readonly CollisionHandler _collisionHandler;
        protected readonly PositionClamper _positionClamper;
        protected readonly GroundStateHandler _groundStateHandler;

        public ReadOnlyReactiveProperty<Vector3> Velocity => _velocityHandler.Velocity;
        public ReadOnlyReactiveProperty<bool> IsGrounded => _groundStateHandler.IsGrounded;
        public Observable<Unit> OnLanded => _groundStateHandler.OnLanded;
        public Observable<Unit> OnLeftGround => _groundStateHandler.OnLeftGround;

        protected PhysicsSystemBase(Transform transform, Collider2D collider, PhysicsSettings settings)
        {
            _transform = transform;
            _settings = settings;

            _velocityHandler = new VelocityHandler(_settings);
            _gravityHandler = new GravityHandler(_settings);
            _collisionHandler = new CollisionHandler(_transform, collider, settings);
            _positionClamper = new PositionClamper(_transform, _collisionHandler, settings);
            _groundStateHandler = new GroundStateHandler(_transform, _collisionHandler, settings);
        }

        public void PhysicsUpdate(float deltaTime)
        {
            _gravityHandler.ApplyGravity(_velocityHandler, _groundStateHandler.GetGroundState(), deltaTime);

            ApplyMovement(deltaTime);

            _positionClamper.ClampPosition(_velocityHandler);

            _groundStateHandler.UpdateGroundState(_velocityHandler);
        }

        public void Stop()
        {
            _velocityHandler.Stop();
        }

        public void StopHorizontal()
        {
            _velocityHandler.StopHorizontal();
        }

        public void StopVertical()
        {
            _velocityHandler.StopVertical();
        }

        public void SetVelocity(Vector3 velocity)
        {
            _velocityHandler.SetVelocity(velocity);
        }

        public void AddVelocity(Vector3 velocity)
        {
            _velocityHandler.AddVelocity(velocity);
        }

        private void ApplyMovement(float deltaTime)
        {
            var movement = _velocityHandler.GetVelocity() * deltaTime;
            _transform.position += movement;
        }

        #region Controls

        public void SetGravityEnabled(bool enabled)
        {
            if (enabled)
            {
                _gravityHandler.EnableGravity();
            }
            else
            {
                _gravityHandler.DisableGravity();
            }
        }

        public void LockHorizontalMovement(bool lockState)
        {
            _velocityHandler.LockHorizontal(lockState);
        }

        public void LockVerticalMovement(bool lockState)
        {
            _velocityHandler.LockVertical(lockState);
        }

        #endregion

        public virtual void Dispose()
        {
            _velocityHandler?.Dispose();
            _groundStateHandler?.Dispose();
        }
    }
}
