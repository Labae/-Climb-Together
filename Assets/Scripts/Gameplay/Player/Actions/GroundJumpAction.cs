using Data.Player;
using Debugging;
using Gameplay.Common.Interfaces;
using Gameplay.Player.Interfaces;
using UnityEngine;

namespace Gameplay.Player.Actions
{
    public class GroundJumpAction : IPlayerAction
    {
        private readonly PlayerMovementConfig _movementConfig;
        private readonly Rigidbody2D _rigidbody2D;
        private readonly IGroundChecker _groundChecker;

        public GroundJumpAction(PlayerMovementConfig movementConfig, Rigidbody2D rigidbody2D,
            IGroundChecker groundChecker)
        {
            _movementConfig = movementConfig;
            _rigidbody2D = rigidbody2D;
            _groundChecker = groundChecker;
        }
        
        public bool CanExecute()
        {
            return _groundChecker.IsGrounded;
        }

        public void Execute()
        {
            _rigidbody2D.linearVelocityY += _movementConfig.GroundJumpForce;
            GameLogger.Debug($"Player Ground Jump: {_movementConfig.GroundJumpForce}");
        }

        public void Dispose()
        {
            
        }
    }
}