using System;
using Data.Player.Enums;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using Gameplay.Player.Locomotion;
using Gameplay.Player.States.Extensions;
using R3;
using Systems.Physics.Utilities;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.Player.States
{
    public class PlayerStateTransitions : IDisposable
    {
        private readonly IStateMachine<PlayerStateType> _stateMachine;
        private readonly PlayerLocomotion _playerLocomotion;
        private readonly PlayerJump _playerJump;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundChecker _groundChecker;

        private readonly CompositeDisposable _disposables = new();

        public PlayerStateTransitions(IStateMachine<PlayerStateType> stateMachine, PlayerLocomotion playerLocomotion,
            PlayerJump playerJump, IPhysicsController physicsController, IGroundChecker groundChecker)
        {
            _stateMachine = stateMachine;
            _playerLocomotion = playerLocomotion;
            _playerJump = playerJump;
            _physicsController = physicsController;
            _groundChecker = groundChecker;

            SetupAllTransitions();
        }

        private void SetupAllTransitions()
        {
            SetupLocomotionTransitions();
            SetupJumpTransitions();
            SetupGroundTransitions();
            SetupPhysicsTransitions();
        }

        #region Locomotion Transitions

        private void SetupLocomotionTransitions()
        {
            _playerLocomotion.OnLocomotionExecuted.Subscribe(
                HandleLocomotionTransition).AddTo(_disposables);
        }

        private void HandleLocomotionTransition(IPlayerLocomotion locomotion)
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            switch (locomotion)
            {
                case DefaultLocomotion when currentState != PlayerStateType.Run && _groundChecker.IsGrounded.CurrentValue:
                    _stateMachine.ChangeState(PlayerStateType.Run);
                    break;
                case NoneLocomotion when currentState != PlayerStateType.Idle && _groundChecker.IsGrounded.CurrentValue:
                    _stateMachine.ChangeState(PlayerStateType.Idle);
                    break;
                default:
                    break;
            }
        }

        private bool IsMoving()
        {
            return Mathf.Abs(_physicsController.GetVelocity().x) > PhysicsUtility.VelocityThreshold;
        }

        #endregion

        #region Jump Transitions

        private void SetupJumpTransitions()
        {
            _playerJump.OnJumpExecuted.Subscribe(
                HandleJumpTransition).AddTo(_disposables);
        }

        private void HandleJumpTransition(Unit unit)
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            if (!CanJumpFrom(currentState))
            {
                return;
            }

            _stateMachine.ChangeState(PlayerStateType.Jump);
        }

        private bool CanJumpFrom(PlayerStateType state)
        {
            return state is PlayerStateType.Idle
                or PlayerStateType.Run
                or PlayerStateType.Fall;
        }

        #endregion

        #region Physics Transitions

        private void SetupPhysicsTransitions()
        {
            _physicsController.IsRising.CombineLatest(_physicsController.IsFalling,
                    (rising, falling) => !rising && falling)
                .Where(shouldFall => shouldFall)
                .Subscribe(_ => HandleFallTransition())
                .AddTo(_disposables);

            _physicsController.IsFalling.CombineLatest(_groundChecker.IsGrounded,
                    (falling, ground) => falling && !ground)
                .Where(shouldFall => shouldFall)
                .Subscribe(_ => HandleDirectFallTransition())
                .AddTo(_disposables);
        }

        private void HandleFallTransition()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            if (CanFallFrom(currentState))
            {
                _stateMachine.ChangeState(PlayerStateType.Fall);
            }
        }

        private void HandleDirectFallTransition()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;
            if (CanDirectFallFrom(currentState))
            {
                _stateMachine.ChangeState(PlayerStateType.Fall);
            }
        }

        private bool CanFallFrom(PlayerStateType currentState)
        {
            return currentState != PlayerStateType.Fall
                   && currentState.CanReceiveInput()
                   && !_groundChecker.IsGrounded.CurrentValue;
        }

        private bool CanDirectFallFrom(PlayerStateType currentState)
        {
            return currentState is PlayerStateType.Idle or PlayerStateType.Run
                   && currentState.CanReceiveInput()
                   && !_groundChecker.IsGrounded.CurrentValue;
        }

        #endregion

        #region Ground Transitions

        private void SetupGroundTransitions()
        {
            _groundChecker.OnGroundEntered
                .Subscribe(_ => HandleLandTransition())
                .AddTo(_disposables);
        }

        private void HandleLandTransition()
        {
            var currentState = _stateMachine.CurrentStateType.CurrentValue;

            if (!CanLandFrom(currentState))
            {
                return;
            }

            if (IsMoving())
            {
                _stateMachine.ChangeState(PlayerStateType.Run);
            }
            else
            {
                _stateMachine.ChangeState(PlayerStateType.Idle);
            }
        }

        private bool CanLandFrom(PlayerStateType state)
        {
            return state is PlayerStateType.Jump
                or PlayerStateType.Fall;
        }

        #endregion

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
