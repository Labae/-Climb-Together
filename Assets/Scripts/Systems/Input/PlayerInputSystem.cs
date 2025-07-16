using System;
using R3;
using Systems.Input.Interfaces;
using UnityEngine.InputSystem;

namespace Systems.Input
{
    public class PlayerInputSystem : IInputSystem, InputSystemActions.IPlayerActions, IDisposable
    {
        private readonly Subject<float> _movementInput = new();
        private readonly Subject<bool> _jumpInput = new();

        public Observable<float> MovementInput => _movementInput.AsObservable();
        public Observable<bool> JumpPressed => _jumpInput.AsObservable();

        public bool IsInputEnabled { get; private set; } = false;

        private InputSystemActions.PlayerActions _inputSystemActions;

        public PlayerInputSystem(IGlobalInputSystem globalInputSystem)
        {
            _inputSystemActions = globalInputSystem.Actions.Player;
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

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            if (context.performed)
            {
                _jumpInput.OnNext(true);
            }
        }

        public void OnMovement(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            if (context.performed)
            {
                _movementInput.OnNext(context.ReadValue<float>());
            }
            else if (context.canceled)
            {
                _movementInput.OnNext(0f);
            }
        }

        public void Dispose()
        {
            _inputSystemActions.Disable();
            _inputSystemActions.RemoveCallbacks(this);
        }
    }
}
