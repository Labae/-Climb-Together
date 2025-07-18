using Gameplay.Common.Enums;
using R3;
using Systems.Input.Utilities;
using Systems.Physics.Utilities;

namespace Gameplay.Common.DirectionProviders
{
    public static class DirectionProviderFactory
    {
        public static DirectionProvider Create(FacingDirection initialDirection = FacingDirection.Right)
        {
            return new DirectionProvider(initialDirection);
        }

        public static InputBasedDirectionProvider CreateInputBased(Observable<float> movementInput,
            FacingDirection initialDirection = FacingDirection.Right, float threshold = InputUtility.InputThreshold)
        {
            return new InputBasedDirectionProvider(movementInput, initialDirection, threshold);
        }

        public static VelocityBasedDirectionProvider CreateVelocityBased(Observable<float> velocityStream,
            FacingDirection initialDirection = FacingDirection.Right, float threshold = PhysicsUtility.VelocityThreshold, float lockTime = 0.1f)
        {
            return new VelocityBasedDirectionProvider(velocityStream, initialDirection, threshold, lockTime);
        }
    }
}
