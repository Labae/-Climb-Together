using System;
using Data.Player;
using Debugging;
using Gameplay.Common.Interfaces;
using Gameplay.Player.Interfaces;
using UnityEngine;

namespace Gameplay.Player.Actions
{
    public class AirJumpAction : IPlayerAction
    {
        private readonly PlayerMovementConfig _movementConfig;
        private readonly Rigidbody2D _rigidbody2D;
        private readonly IGroundChecker _groundChecker;

        private int _airJumpRemaining;

        public AirJumpAction(PlayerMovementConfig movementConfig, Rigidbody2D rigidbody2D,
            IGroundChecker groundChecker)
        {
            _movementConfig = movementConfig;
            _rigidbody2D = rigidbody2D;
            _groundChecker = groundChecker;
            _groundChecker.OnGroundEnter += OnGroundEnter;
        }

        private void OnGroundEnter()
        {
            _airJumpRemaining = _movementConfig.MaxAirJumps;
        }

        public bool CanExecute()
        {
            return !_groundChecker.IsGrounded && _airJumpRemaining > 0;
        }

        public void Execute()
        {
            _rigidbody2D.linearVelocityY = _movementConfig.AirJumpForce;
            _airJumpRemaining--;
            GameLogger.Debug($"Player Air Jump: {_movementConfig.AirJumpForce}, Remaining: {_airJumpRemaining}");
        }

        public void Dispose()
        {
            _groundChecker.OnGroundEnter -= OnGroundEnter;
        }
    }
}