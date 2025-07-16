using Data.Common;
using Gameplay.Common.Interfaces;
using Gameplay.Physics;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerPhysicsController : PhysicsControllerBase
    {
        public PlayerPhysicsController(Rigidbody2D rigidbody2D, PhysicsSettings physicsSettings, IGroundChecker groundChecker) : base(rigidbody2D, physicsSettings, groundChecker)
        {
        }
    }
}
