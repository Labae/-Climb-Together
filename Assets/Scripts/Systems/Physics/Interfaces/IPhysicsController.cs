using UnityEngine;

namespace Systems.Physics.Interfaces
{
    public interface IPhysicsController
    {
        void ProcessVelocityRequests();
        
        void RequestVelocity(VelocityRequest request);
        void Jump(float jumpSpeed);
        void Move(float horizontalSpeed);
        void Dash(Vector2 direction, float speed);
        void Knockback(Vector2 direction, float force);
    
        Vector2 GetVelocity();
        void PhysicsUpdate();
    }
}