using System;
using System.Collections.Generic;
using Data.Common;
using R3;
using Systems.Physics.Debugging;
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

#if UNITY_EDITOR
        public List<BoxCastDebugInfo> DebugBoxCasts { get; private set; } = new();
        public bool ShowDebugGizmos { get; set; } = true;
#endif

        public ReadOnlyReactiveProperty<Vector3> Velocity => _velocityHandler.Velocity;
        public ReadOnlyReactiveProperty<bool> IsGrounded => _groundStateHandler.IsGrounded;
        public Observable<Unit> OnLanded => _groundStateHandler.OnLanded;
        public Observable<Unit> OnLeftGround => _groundStateHandler.OnLeftGround;

        protected PhysicsSystemBase(Transform transform, BoxCollider2D collider, PhysicsSettings settings)
        {
            _transform = transform;
            _settings = settings;

            _velocityHandler = new VelocityHandler(_settings);
            _gravityHandler = new GravityHandler(_settings);
            _collisionHandler = new CollisionHandler(_transform, collider, settings);
            _positionClamper = new PositionClamper(_transform, _collisionHandler, settings);
            _groundStateHandler = new GroundStateHandler(_transform, _collisionHandler, settings);

#if UNITY_EDITOR
            PhysicsDebugEvents.OnBoxCastPerformed += OnBoxCastPerformed;
#endif
        }

#if UNITY_EDITOR
        private void OnBoxCastPerformed(BoxCastDebugInfo info)
        {
            if (ShowDebugGizmos)
            {
                DebugBoxCasts.Add(info);
            }
        }
#endif

        public virtual void PhysicsUpdate(float deltaTime)
        {
#if UNITY_EDITOR
            DebugBoxCasts.Clear();
#endif
            _gravityHandler.ApplyGravity(_velocityHandler, _groundStateHandler.GetGroundState(), deltaTime);

            var position = _transform.position;
            ApplyMovement(ref position, deltaTime);

            _positionClamper.ClampPosition(ref position, _velocityHandler);

            _transform.position = position;

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

        protected void ApplyMovement(ref Vector3 position, float deltaTime)
        {
            var movement = _velocityHandler.GetVelocity() * deltaTime;
            position = _transform.position + movement;
        }

        public CollisionResult CheckDirectionWithSurface(Vector2 direction)
        {
            return _collisionHandler.CheckDirectionWithSurface(direction);
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
#if UNITY_EDITOR
            PhysicsDebugEvents.OnBoxCastPerformed -= OnBoxCastPerformed;
#endif
            _velocityHandler?.Dispose();
            _groundStateHandler?.Dispose();
        }
    }
}
