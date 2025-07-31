using Data.Common;

namespace Systems.Physics
{
    public class GravityHandler
    {
        private readonly PhysicsSettings _settings;
        public bool Enabled { get; private set; } = false;

        public GravityHandler(PhysicsSettings settings)
        {
            _settings = settings;
        }

        public void ApplyGravity(VelocityHandler velocityHandler, bool isGrounded, float deltaTime, float? customGravity = 0f)
        {
            if (!Enabled || isGrounded || velocityHandler.VerticalLocked)
            {
                return;
            }

            var gravity = customGravity ?? _settings.NormalGravity;

            var currentY = velocityHandler.GetVelocity().y;
            var newY = currentY + gravity * deltaTime;

            if (newY < _settings.TerminalVelocity)
            {
                newY = _settings.TerminalVelocity;
            }
            velocityHandler.SetVerticalVelocity(newY);
        }

        public void FastFall(VelocityHandler velocityHandler)
        {
            velocityHandler.SetVerticalVelocity(_settings.TerminalVelocity);
        }

        public void EnableGravity()
        {
            Enabled = true;
        }

        public void DisableGravity()
        {
            Enabled = false;
        }
    }
}
