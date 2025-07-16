using System;
using R3;
using Systems.Physics;
using UnityEngine;

namespace Gameplay.Physics.Interfaces
{
    public interface IPhysicsController : IDisposable
    {
        void FixedUpdate();
        void RequestVelocity(VelocityRequest request);
        void Jump(float jumpSpeed);
        void Move(float horizontalSpeed);
        void Dash(Vector2 direction, float speed);
        void Knockback(Vector2 direction, float force);
        void Stop();
        Vector2 GetVelocity();

        ReadOnlyReactiveProperty<Vector2> Velocity { get; }
        ReadOnlyReactiveProperty<float> HorizontalVelocity { get; }
        ReadOnlyReactiveProperty<float> VerticalVelocity { get; }
        ReadOnlyReactiveProperty<bool> IsRising { get; }
        ReadOnlyReactiveProperty<bool> IsFalling { get; }

        void SetGravityEnabled(bool enabled);
        float GetCurrentGravity();
        void FastFall();
    }
}
