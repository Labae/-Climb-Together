using System;
using Gameplay.Platformer.Movement.Interface;
using R3;
using Systems.Input.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Player.Core
{
    public class PlayerInputSystem : IInputSystem, IPlatformerInput, InputSystemActions.IPlatformerPlayerActions, IDisposable
    {
        private readonly Subject<float> _movementInput = new();
        private readonly Subject<bool> _jumpInput = new();
        private readonly Subject<Unit> _dashInput = new();
        private readonly Subject<Vector2> _directionalInput = new();

        public Observable<float> MovementInput => _movementInput.AsObservable();
        public Observable<bool> JumpPressed => _jumpInput.AsObservable();
        public Observable<Unit> DashPressed => _dashInput.AsObservable();
        public Observable<Vector2> DirectionalInput =>  _directionalInput.AsObservable();

        public bool IsInputEnabled { get; private set; } = false;

        private InputSystemActions.PlatformerPlayerActions _inputSystemActions;

        public PlayerInputSystem(IGlobalInputSystem globalInputSystem)
        {
            _inputSystemActions = globalInputSystem.Actions.PlatformerPlayer;
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

            if (context.canceled)
            {
                _jumpInput.OnNext(false);
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

        public void OnDash(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            if (context.performed)
            {
                _dashInput.OnNext(Unit.Default);
            }
        }

        public void OnDirectionalInput(InputAction.CallbackContext context)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            _directionalInput.OnNext(context.ReadValue<Vector2>());
        }

        public void Dispose()
        {
            _movementInput?.Dispose();
            _jumpInput?.Dispose();
            _dashInput?.Dispose();
            _directionalInput?.Dispose();

            _inputSystemActions.Disable();
            _inputSystemActions.RemoveCallbacks(this);
        }
    }
}
