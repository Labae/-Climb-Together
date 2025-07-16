using Cysharp.Text;
using Data.Player;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using UnityEngine;

namespace Gameplay.Player.Actions
{
    public class GroundJumpAction : IPlayerAction
    {
        private readonly PlayerMovementAbility _movementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundChecker _groundChecker;

        public GroundJumpAction(PlayerMovementAbility movementAbility, IPhysicsController physicsController,
            IGroundChecker groundChecker)
        {
            _movementAbility = movementAbility;
            _physicsController = physicsController;
            _groundChecker = groundChecker;
        }

        public bool CanExecute()
        {
            return _groundChecker.IsGrounded.CurrentValue;
        }

        public void Execute()
        {
            _physicsController.Jump(_movementAbility.JumpPower);
        }

        public void Dispose()
        {

        }
    }
}
