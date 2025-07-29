using System;
using Data.Common;
using Debugging;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using Gameplay.Platformer.Physics;
using R3;
using Systems.Input.Utilities;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerMovementController : IPlatformerMovementController, IDisposable
    {
        private readonly PlatformerPhysicsSystem _physicsSystem;
        private readonly IPlatformerInput _platformerInput;
        private readonly PlatformerMovementSettings _settings;

        // Default Gameplay State
        private float _coyoteTimer = 0f;
        private bool _jumpBuffered = false;
        private float _jumpBufferTimer = 0f;
        private bool _isRunning = false;
        private float _movementInput = 0f;

        // Special Action State
        private SpecialActionType _currentSpecialAction = SpecialActionType.None;
        private float _specialActionTimer = 0f;
        private Vector3 _knockbackVelocity = Vector3.zero;

        private readonly Subject<Unit> _onLanded = new();
        private readonly Subject<Unit> _onJumpStarted = new();
        private readonly Subject<SpecialActionType> _onSpecialActionStarted = new();
        private readonly Subject<SpecialActionType> _onSpecialActionEnded = new();

        private readonly CompositeDisposable _disposables = new();

        public Observable<Unit> OnLanded => _onLanded.AsObservable();
        public Observable<Unit> OnJumpStarted => _onJumpStarted.AsObservable();
        public Observable<SpecialActionType> OnSpecialActionStarted => _onSpecialActionStarted.AsObservable();
        public Observable<SpecialActionType> OnSpecialActionEnded => _onSpecialActionEnded.AsObservable();

        public PlatformerMovementController(
            PlatformerPhysicsSystem physicsSystem,
            IPlatformerInput platformerInput,
            PlatformerMovementSettings settings
        )
        {
            _physicsSystem = physicsSystem;
            _platformerInput = platformerInput;
            _settings = settings;

            SubscribeToInputs();
            SubscribeToPhysicsEvents();
        }

        private void SubscribeToInputs()
        {
            _platformerInput.MovementInput
                .Subscribe(HandleMovementInput)
                .AddTo(_disposables);

            _platformerInput.JumpPressed
                .Where(pressed => pressed)
                .Subscribe(_ => Jump())
                .AddTo(_disposables);
        }

        private void SubscribeToPhysicsEvents()
        {
            _physicsSystem.OnLanded
                .Subscribe(_ =>
                {
                    _coyoteTimer = _settings.CoyoteTime;
                    _onLanded.OnNext(Unit.Default);

                    if (_currentSpecialAction == SpecialActionType.Knockback)
                    {
                        EndSpecialAction();
                    }
                })
                .AddTo(_disposables);

            _physicsSystem.IsGrounded
                .Pairwise()
                .Where(pair => pair.Previous && !pair.Current)
                .Subscribe(_ => _coyoteTimer = _settings.CoyoteTime)
                .AddTo(_disposables);
        }

        private void HandleMovementInput(float input)
        {
            _movementInput = input;
        }

        public void StartRunning(float direction)
        {
            if (IsInSpecialAction() && _currentSpecialAction != SpecialActionType.Dashing)
            {
                return;
            }

            _isRunning = true;
            var multiplier = _physicsSystem.IsGrounded.CurrentValue ? 1.0f : _settings.AirMoveMultiplier;
            var targetSpeed = direction * _settings.RunSpeed * multiplier;
            var currentVelocity = _physicsSystem.Velocity.CurrentValue;
            _physicsSystem.SetVelocity(new Vector3(targetSpeed, currentVelocity.y, currentVelocity.z));
        }

        public void StopRunning()
        {
            if (IsInSpecialAction() && _currentSpecialAction != SpecialActionType.Dashing)
            {
                return;
            }

            _isRunning = false;

            if (_settings.UseGradualStop)
            {
                // TODO: Lerp나 Friction
                var currentVelocity = _physicsSystem.Velocity.CurrentValue;
                _physicsSystem.SetVelocity(new Vector3(0f, currentVelocity.y, currentVelocity.z));
            }
            else
            {
                _physicsSystem.StopHorizontal();
            }
        }

        public void Jump()
        {
            if (IsInSpecialAction())
            {
                return;
            }

            if (CanJump())
            {
                PerformJump();
            }
            else
            {
                _jumpBuffered = true;
                _jumpBufferTimer = _settings.JumpBufferTime;
            }
        }

        public void Dash(Vector2 direction)
        {
            if (_currentSpecialAction != SpecialActionType.None)
            {
                return;
            }

            StartSpecialAction(SpecialActionType.Dashing, _settings.DashDuration);
            _physicsSystem.Dash(direction);
        }

        public void Knockback(Vector2 direction, float force)
        {
            StartSpecialAction(SpecialActionType.Knockback, _settings.KnockbackDuration);
            _knockbackVelocity = direction.normalized * force;
            _physicsSystem.Knockback(direction, force);
        }

        private void StartSpecialAction(SpecialActionType specialAction, float duration)
        {
            if (_currentSpecialAction != SpecialActionType.None)
            {
                EndSpecialAction();
            }

            _currentSpecialAction = specialAction;
            _specialActionTimer = duration;
            _onSpecialActionStarted.OnNext(specialAction);
        }

        private void EndSpecialAction()
        {
            if (_currentSpecialAction == SpecialActionType.None)
            {
                return;
            }

            var endedAction = _currentSpecialAction;
            _currentSpecialAction = SpecialActionType.None;
            _specialActionTimer = 0f;
            _knockbackVelocity = Vector3.zero;

            _onSpecialActionEnded.OnNext(endedAction);
        }

        private void PerformJump()
        {
            _physicsSystem.Jump();
            _coyoteTimer = 0f;
            _jumpBuffered = false;
            _onJumpStarted.OnNext(Unit.Default);
        }

        public bool CanJump()
        {
            if (IsInSpecialAction())
            {
                return false;
            }

            return _physicsSystem.IsGrounded.CurrentValue || _coyoteTimer > 0f;
        }

        public bool IsMoving()
        {
            return PhysicsUtility.IsMoving(_physicsSystem.Velocity.CurrentValue);
        }

        public bool IsGrounded()
        {
            return _physicsSystem.IsGrounded.CurrentValue;
        }

        public bool IsRunning()
        {
            return _isRunning && IsMoving() && !IsInSpecialAction();
        }

        public bool IsFalling()
        {
            return PhysicsUtility.IsFalling(_physicsSystem.Velocity.CurrentValue);
        }

        public bool IsRising()
        {
            return PhysicsUtility.IsRising(_physicsSystem.Velocity.CurrentValue);
        }

        public bool IsInSpecialAction()
        {
            return _currentSpecialAction != SpecialActionType.None;
        }

        public SpecialActionType GetSpecialAction()
        {
            return _currentSpecialAction;
        }

        public void Update(float deltaTime)
        {
            ApplyCurrentMovementInput();
            UpdateTimers(deltaTime);
            UpdateSpecialActions(deltaTime);
            ProcessJumpBuffer();
        }

        private void ApplyCurrentMovementInput()
        {
            if (IsInSpecialAction() && _currentSpecialAction != SpecialActionType.Dashing)
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

        private void UpdateTimers(float deltaTime)
        {
            if (_coyoteTimer > 0f)
            {
                _coyoteTimer -= deltaTime;
            }

            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= deltaTime;
                if (_jumpBufferTimer <= 0f)
                {
                    _jumpBuffered = false;
                }
            }
        }

        private void UpdateSpecialActions(float deltaTime)
        {
            if (_specialActionTimer > 0f)
            {
                _specialActionTimer -= deltaTime;

                if (_specialActionTimer <= 0f)
                {
                    EndSpecialAction();
                }
            }
        }

        private void ProcessJumpBuffer()
        {
            if (_jumpBuffered && CanJump())
            {
                PerformJump();
            }
        }

        public void Dispose()
        {
            _onLanded?.Dispose();
            _onJumpStarted?.Dispose();
            _onSpecialActionStarted?.Dispose();
            _onSpecialActionEnded?.Dispose();
            _disposables?.Dispose();
        }
    }
}
