using System;
using Debugging;
using Debugging.Enum;
using Systems.Input.Interfaces;
using Systems.Reactive;
using UnityEngine.InputSystem;

namespace Systems.Input
{
    public class PlayerInputSystem : IInputSystem, InputSystemActions.IPlayerActions, IDisposable
    {
        public ObservableProperty<bool> JumpPressed { get; }
        public bool IsInputEnabled { get; private set; } = false;

        private InputSystemActions.PlayerActions _inputSystemActions;

        public PlayerInputSystem()
        {
            _inputSystemActions = GlobalInputSystem.Actions.Player;
            JumpPressed = new ObservableProperty<bool>(false);
            IsInputEnabled = false;

            _inputSystemActions.SetCallbacks(this);
        }

        public void EnableInput()
        {
            if (IsInputEnabled)
            {
                return;
            }

            IsInputEnabled = true;
            _inputSystemActions.Enable();
        }

        public void DisableInput()
        {
            if (!IsInputEnabled)
            {
                return;
            }

            IsInputEnabled = false;
            _inputSystemActions.Disable();
        }

        public void ResetFrameInput()
        {
            JumpPressed.Value = false;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                JumpPressed.Value = true;
                GameLogger.Debug($"JumpPressed: {JumpPressed.Value}", LogCategory.Input);
            }
        }

        public void Dispose()
        {
            _inputSystemActions.Disable();
            _inputSystemActions.RemoveCallbacks(this);
        }
    }
}