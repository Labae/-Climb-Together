using System;
using Cysharp.Text;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Enums;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Core;
using Gameplay.Player.Events;
using R3;
using Systems.Physics;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player.Jump
{
    public class VariableJump : IDisposable
    {
        private readonly PlayerMovementAbility _playerMovementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly PlayerEventBus _playerEventBus;
        private readonly CompositeDisposable _disposables = new();

        private bool _isActive;
        private float _jumpStartTime;
        private JumpType _currentJumpType;

        public bool IsActive => _isActive;
        public float RemainingTime => _isActive ? Mathf.Max(0f, _playerMovementAbility.VariableJumpTimeWindow - (Time.time - _jumpStartTime)) : 0f;

        public VariableJump(PlayerMovementAbility playerMovementAbility, IPhysicsController physicsController,
            PlayerEventBus playerEventBus)
        {
            _playerMovementAbility = playerMovementAbility ?? throw new ArgumentNullException(nameof(playerMovementAbility));
            _physicsController = physicsController ?? throw new ArgumentNullException(nameof(physicsController));
            _playerEventBus = playerEventBus ?? throw new ArgumentNullException(nameof(playerEventBus));

            SetupEventSubscriptions();
        }

        private void SetupEventSubscriptions()
        {
            _playerEventBus.Subscribe<JumpExecutedEvent>()
                .Subscribe(OnJumpExecuted)
                .AddTo(_disposables);

            _playerEventBus.Subscribe<JumpInputEvent>()
                .Where(e => !e.IsHeld && _isActive)
                .Subscribe(e => TryApplyVariableJump())
                .AddTo(_disposables);

            Observable.EveryUpdate()
                .Where(_ => _isActive)
                .Subscribe(_ => CheckTimeLimit())
                .AddTo(_disposables);
        }

        private void OnJumpExecuted(JumpExecutedEvent e)
        {
            _currentJumpType = e.JumpType;

            if (_currentJumpType == JumpType.Ground && _playerMovementAbility.HasVariableJump)
            {
                ActivateVariableJump();
            }
            else
            {
                DeactivateVariableJump();
            }
        }

        private void ActivateVariableJump()
        {
            _isActive = true;
            _jumpStartTime = Time.time;
        }

        private void DeactivateVariableJump()
        {
            _isActive = false;
        }

        private void TryApplyVariableJump()
        {
            if (!CanApplyVariableJump())
            {
                return;
            }

            ApplyVariableJump();
        }

        private bool CanApplyVariableJump()
        {
            if (!_playerMovementAbility.HasVariableJump)
            {
                return false;
            }

            if (!_isActive)
            {
                return false;
            }

            if (_currentJumpType != JumpType.Ground)
            {
                return false;
            }

            if (Time.time - _jumpStartTime > _playerMovementAbility.VariableJumpTimeWindow)
            {
                return false;
            }

            var currentVelocity = _physicsController.GetVelocity();
            return PhysicsUtility.IsRising(currentVelocity);
        }

        private void ApplyVariableJump()
        {
            try
            {
                var currentVelocity = _physicsController.GetVelocity();
                var reducedVelocity = new Vector2(currentVelocity.x, currentVelocity.y * _playerMovementAbility.VariableJumpFactor);
                _physicsController.RequestVelocity(VelocityRequest.Set(reducedVelocity));

                DeactivateVariableJump();
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error applying variable jump: ", e.Message), LogCategory.Player);
            }
        }

        private void CheckTimeLimit()
        {
            if (Time.time - _jumpStartTime > _playerMovementAbility.VariableJumpTimeWindow)
            {
                DeactivateVariableJump();
                GameLogger.Debug("Variable jump time limit reached", LogCategory.Player);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
