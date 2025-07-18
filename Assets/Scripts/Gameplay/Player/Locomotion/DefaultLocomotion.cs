using Data.Player.Abilities;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using Systems.Input.Utilities;

namespace Gameplay.Player.Locomotion
{
    public class DefaultLocomotion : IPlayerLocomotion
    {
        private readonly PlayerMovementAbility _movementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundDetector _groundDetector;

        public DefaultLocomotion(PlayerMovementAbility movementAbility, IPhysicsController physicsController, IGroundDetector groundDetector)
        {
            _movementAbility = movementAbility;
            _physicsController = physicsController;
            _groundDetector = groundDetector;
        }

        public bool CanExecute(float horizontalInput)
        {
            return InputUtility.IsInputActive(horizontalInput);
        }

        public void Execute(float horizontalInput)
        {
            var speed = _groundDetector.IsCurrentlyGrounded()
                ? _movementAbility.RunSpeed
                : _movementAbility.RunSpeed * _movementAbility.AirMultiplier;
            _physicsController.Move(speed * horizontalInput);
        }

        public string GetName()
        {
            return nameof(DefaultLocomotion);
        }

        public void Dispose()
        {

        }
    }
}
