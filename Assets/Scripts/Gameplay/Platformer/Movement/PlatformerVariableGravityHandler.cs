using System;
using Data.Platformer.Settings;
using Gameplay.Platformer.Movement.Enums;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerVariableGravityHandler
    {
        private readonly PlatformerPhysicsSettings _settings;
        private PlatformerGravityState _gravityState = PlatformerGravityState.Normal;

        public PlatformerVariableGravityHandler(PlatformerPhysicsSettings settings)
        {
            _settings = settings;
        }

        public void SetGravityState(PlatformerGravityState gravityState)
        {
            _gravityState = gravityState;
        }

        public float GetCurrentGravity()
        {
            return _gravityState switch
            {
                PlatformerGravityState.Normal => _settings.NormalGravity,
                PlatformerGravityState.JumpHold => _settings.JumpHoldGravity,
                PlatformerGravityState.JumpCut => _settings.JumpCutGravity,
                PlatformerGravityState.Falling => _settings.FallGravity,
                PlatformerGravityState.Apex => _settings.ApexGravity,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
