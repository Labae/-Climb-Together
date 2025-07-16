using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using Systems.Input.Utilities;

namespace Gameplay.Player.Locomotion
{
    public class NoneLocomotion : IPlayerLocomotion
    {
        private readonly IPhysicsController _physicsController;

        public NoneLocomotion(IPhysicsController physicsController)
        {
            _physicsController = physicsController;
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }

        public bool CanExecute(float horizontalInput)
        {
            return InputUtility.InDeadZone(horizontalInput);
        }

        public void Execute(float horizontalInput)
        {
            _physicsController.Move(0f);
        }

        public string GetName()
        {
            return nameof(NoneLocomotion);
        }
    }
}
