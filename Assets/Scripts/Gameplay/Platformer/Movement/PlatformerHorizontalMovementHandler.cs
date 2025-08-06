using System;
using Data.Common;
using Data.Platformer.Settings;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using Gameplay.Platformer.Physics;
using R3;
using Systems.Input.Utilities;
using UnityEngine;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerHorizontalMovementHandler : IDisposable
    {
        private readonly PlatformerPhysicsSystem _physicsSystem;
        private readonly IPlatformerInput _platformerInput;
        private readonly PlatformerMovementSettings _settings;
        private readonly PlatformerPhysicsSettings _physicsSettings;

        private readonly CompositeDisposable _disposables = new();

        private bool _enabled = true;
        private bool _isRunning = false;
        private float _movementInput = 0f;
        private float _currentHorizontalSpeed = 0f;
        private float _lastHorizontalSpeed = 0f;
        private float _targetHorizontalSpeed = 0f;
        private PlatformerMovementState _platformerMovementState = PlatformerMovementState.Idle;

        public PlatformerHorizontalMovementHandler(PlatformerPhysicsSystem physicsSystem,
            IPlatformerInput platformerInput, PlatformerMovementSettings settings,
            PlatformerPhysicsSettings physicsSettings)
        {
            _physicsSystem = physicsSystem;
            _platformerInput = platformerInput;
            _settings = settings;
            _physicsSettings = physicsSettings;

            SubscribeToInputs();
        }

        private void SubscribeToInputs()
        {
            _platformerInput.MovementInput
                .Subscribe(HandleMovementInput)
                .AddTo(_disposables);
        }

        private void HandleMovementInput(float input)
        {
            _movementInput = input;
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        public void Update(float deltaTime, bool inputLocked = false)
        {
            if (!_enabled)
            {
                return;
            }

            if (inputLocked)
            {
                UpdateHorizontalMovementWithMomentum(deltaTime);
            }
            else
            {
                ApplyCurrentMovementInput();
                UpdateHorizontalMovement(deltaTime);
            }
        }

        private void ApplyCurrentMovementInput()
        {
            if (!_enabled)
            {
                return;
            }

            if (InputUtility.InDeadZone(_movementInput))
            {
                StopRunning();
            }
            else
            {
                StartRunning(_movementInput);
            }
        }

        private void StartRunning(float direction)
        {
            if (!_enabled)
            {
                return;
            }

            _isRunning = true;
            var multiplier = _physicsSystem.IsGrounded.CurrentValue ? 1.0f : _settings.AirMoveMultiplier;
            _targetHorizontalSpeed = direction * _settings.RunSpeed * multiplier;
        }

        private void StopRunning()
        {
            if (!_enabled)
            {
                return;
            }

            _isRunning = false;
            _targetHorizontalSpeed = 0f;
        }

        private void UpdateHorizontalMovement(float deltaTime)
        {
            UpdateMovementState();
            var moveSpeed = GetCurrentMoveSpeed();
            _currentHorizontalSpeed =
                Mathf.MoveTowards(_currentHorizontalSpeed, _targetHorizontalSpeed, moveSpeed * deltaTime);

            var currentVelocity = _physicsSystem.Velocity.CurrentValue;
            _physicsSystem.SetVelocity(new Vector3(_currentHorizontalSpeed, currentVelocity.y));
            _lastHorizontalSpeed = _currentHorizontalSpeed;
        }

        /// <summary>
        /// 입력 락 상태에서의 이동 처리(관성 유지, 감속만)
        /// </summary>
        /// <param name="deltaTime"></param>
        private void UpdateHorizontalMovementWithMomentum(float deltaTime)
        {
            bool isGrounded = _physicsSystem.IsGrounded.CurrentValue;
            var currentVelocity = _physicsSystem.Velocity.CurrentValue;

            if (isGrounded)
            {
                var frictionForce = _physicsSettings.GroundFriction;
                var newHorizontalSpeed = currentVelocity.x * (1f -  frictionForce * deltaTime);

                if (Mathf.Abs(newHorizontalSpeed) < 0.1f)
                {
                    newHorizontalSpeed = 0f;
                }

                _currentHorizontalSpeed = newHorizontalSpeed;
                _physicsSystem.SetVelocity(new Vector3(_currentHorizontalSpeed, currentVelocity.y));
            }
            else
            {
                var airResistance = _physicsSettings.AirResistance;
                var newHorizontalSpeed = currentVelocity.x * airResistance;

                _currentHorizontalSpeed = newHorizontalSpeed;
                _physicsSystem.SetVelocity(new Vector3(_currentHorizontalSpeed, currentVelocity.y));
            }

            _targetHorizontalSpeed = _currentHorizontalSpeed;
            _lastHorizontalSpeed = _currentHorizontalSpeed;
            _isRunning = false;
        }

        private void UpdateMovementState()
        {
            bool isTurningAround = _lastHorizontalSpeed * _targetHorizontalSpeed < 0 &&
                                   _targetHorizontalSpeed != 0;
            bool isDecelerating = _targetHorizontalSpeed == 0 ||
                                  Mathf.Abs(_targetHorizontalSpeed) < Mathf.Abs(_lastHorizontalSpeed);
            bool isAccelerating = !isTurningAround &&
                                  !isDecelerating &&
                                  _targetHorizontalSpeed != 0;
            bool isRunning = Mathf.Abs(_currentHorizontalSpeed) >= _settings.RunSpeed * 0.95f &&
                             Mathf.Abs(_currentHorizontalSpeed - _targetHorizontalSpeed) < 0.1f;

            if (isTurningAround)
            {
                _platformerMovementState = PlatformerMovementState.TurningAround;
            }
            else if (isDecelerating)
            {
                _platformerMovementState = PlatformerMovementState.Decelerating;
            }
            else if (isAccelerating)
            {
                _platformerMovementState = PlatformerMovementState.Accelerating;
            }
            else if (isRunning)
            {
                _platformerMovementState = PlatformerMovementState.Running;
            }
            else
            {
                _platformerMovementState = PlatformerMovementState.Idle;
            }
        }

        private float GetCurrentMoveSpeed()
        {
            bool isGrounded = _physicsSystem.IsGrounded.CurrentValue;
            return _platformerMovementState switch
            {
                PlatformerMovementState.Idle => 0f,
                PlatformerMovementState.Accelerating => GetAccelSpeed(isGrounded),
                PlatformerMovementState.Running => float.MaxValue,
                PlatformerMovementState.Decelerating => GetDecelSpeed(isGrounded),
                PlatformerMovementState.TurningAround => GetTurnSpeed(isGrounded),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private float GetAccelSpeed(bool isGrounded)
        {
            float baseSpeed = _settings.RunSpeed / _settings.AccelTime;
            return isGrounded ? baseSpeed : baseSpeed / _settings.AirAccelMultiplier;
        }

        private float GetDecelSpeed(bool isGrounded)
        {
            float baseSpeed = _settings.RunSpeed / _settings.DecelTime;
            return isGrounded ? baseSpeed : baseSpeed / _settings.AirDecelMultiplier;
        }

        private float GetTurnSpeed(bool isGrounded)
        {
            float baseSpeed = _settings.RunSpeed / _settings.TurnAroundTime;
            float airMultiplier = (_settings.AirAccelMultiplier + _settings.AirDecelMultiplier) * 0.5f;
            return isGrounded ? baseSpeed : baseSpeed / airMultiplier;
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public bool IsIntendingToRun()
        {
            return _enabled && InputUtility.IsInputActive(_movementInput);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
