using System;
using Data.Common;
using Data.Platformer.Settings;
using Gameplay.Platformer.Movement.Enums;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerVariableGravityHandler
    {
        private readonly PlatformerPhysicsSettings _physicsSettings;
        private readonly PlatformerMovementSettings _movementSettings;
        private PlatformerGravityState _gravityState = PlatformerGravityState.Normal;

        public PlatformerVariableGravityHandler(PlatformerPhysicsSettings physicsSettings, PlatformerMovementSettings movementSettings)
        {
            _physicsSettings = physicsSettings;
            _movementSettings = movementSettings;
        }

        public void SetGravityState(PlatformerGravityState gravityState)
        {
            _gravityState = gravityState;
        }

        public float GetCurrentGravity()
        {
            return _gravityState switch
            {
                PlatformerGravityState.Normal => _physicsSettings.NormalGravity,
                PlatformerGravityState.JumpHold => -_movementSettings.JumpGravity,
                PlatformerGravityState.JumpCut => _physicsSettings.JumpCutGravity,
                PlatformerGravityState.Falling => _physicsSettings.FallGravity,
                PlatformerGravityState.Apex => _physicsSettings.ApexGravity,
                PlatformerGravityState.Dashing => 0f,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
