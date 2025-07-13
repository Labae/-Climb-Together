using System;
using System.Collections.Generic;
using Data.Player;
using Gameplay.Common.Interfaces;
using Gameplay.Player.Actions;
using Gameplay.Player.Interfaces;
using Systems.Reactive;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerJump : IDisposable
    {
        private readonly List<IPlayerAction> _jumpActions;
        private readonly ObservableProperty<bool> _jumpPressed;

        public PlayerJump(PlayerMovementConfig config, 
            ObservableProperty<bool> jumpPressed, 
            Rigidbody2D rigidbody2D,
            IGroundChecker groundChecker)
        {
            _jumpPressed = jumpPressed;
            _jumpActions = new List<IPlayerAction>
            {
                new GroundJumpAction(config, rigidbody2D, groundChecker),
                new AirJumpAction(config, rigidbody2D, groundChecker)
            };
            _jumpPressed.OnValueChanged += OnJumpPressed;
        }

        public void Dispose()
        {
            foreach (var jumpAction in _jumpActions)
            {
                jumpAction.Dispose();
            }
            _jumpPressed.OnValueChanged -= OnJumpPressed;
        }
        
        private void OnJumpPressed(bool pressed)
        {
            if (!pressed)
            {
                return;
            }

            foreach (var jumpAction in _jumpActions)
            {
                if (!jumpAction.CanExecute())
                {
                    continue;
                }
                
                jumpAction.Execute();
                break;
            }
        }
    }
}