using Core.Behaviours;
using Data.Player;
using Debugging;
using Debugging.Enum;
using Gameplay.Common;
using Systems.Input;
using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : CoreBehaviour
    {
        [SerializeField]
        private PlayerMovementConfig _movementConfig;
        
        private Rigidbody2D _rigidbody2D;
        private PlayerInputSystem _playerInputSystem;
        private PlayerJump _playerJump;
        private GroundChecker _groundChecker;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ValidateComponents();
        }

        protected override void HandleDestruction()
        {
            _playerJump.Dispose();
            _playerInputSystem.Dispose();
            base.HandleDestruction();
        }

        private void ValidateComponents()
        {
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
            _groundChecker ??= GetComponentInChildren<GroundChecker>();

            _playerInputSystem = new PlayerInputSystem();
            _playerJump = new PlayerJump(_movementConfig,
                _playerInputSystem.JumpPressed,
                _rigidbody2D,
                _groundChecker);
            
            GameLogger.Assert(_rigidbody2D != null, "Failed to get rigidbody2D", LogCategory.Player);
            
            _playerInputSystem.EnableInput();
        }

        private void Update()
        {
            _playerInputSystem.ResetFrameInput();
        }
    }
}