using Data.Common;
using Gameplay.Common.Interfaces;
using Gameplay.Physics;
using UnityEngine;

namespace Gameplay.Player.Core
{
    public class PlayerPhysicsController : PhysicsControllerBase
    {
        public PlayerPhysicsController(Rigidbody2D rigidbody2D, PhysicsSettings physicsSettings, IGroundDetector groundDetector) : base(rigidbody2D, physicsSettings, groundDetector)
        {
        }
    }
}
