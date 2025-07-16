using System.ComponentModel.DataAnnotations;
using Core.Behaviours;
using Data.Player.Animations;
using Data.Player.Data.Player;
using Data.Player.Enums;
using Debugging;
using Debugging.Enum;
using Gameplay.Common;
using Gameplay.Player.States;
using R3;
using Systems.Animations;
using Systems.Animations.Interfaces;
using Systems.Input;
using Systems.Input.Interfaces;
using Systems.StateMachine;
using Systems.StateMachine.Interfaces;
using UnityEngine;
using VContainer;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : CoreBehaviour
    {
        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _spriteRenderer;

        private PlayerInputSystem _playerInputSystem;
        private PlayerLocomotion _playerLocomotion;
        private PlayerJump _playerJump;
        private GroundChecker _groundChecker;
        private PlayerPhysicsController _playerPhysicsController;
        private ISpriteAnimator _spriteAnimator;

        private IStateMachine<PlayerStateType> _stateMachine;
        private PlayerStateTransitions _playerStateTransitions;

        [SerializeField, Required] private PlayerAnimationRegistry _playerAnimationRegistry;

        [Inject] private PlayerAbilities _abilities;
        [Inject] private IGlobalInputSystem _globalInputSystem;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ValidateComponents();
            SubscribeEvents();

            SetupStateMachine();
        }

        protected override void HandleDestruction()
        {
            _playerLocomotion.Dispose();
            _playerJump.Dispose();
            _stateMachine.Dispose();
            _playerPhysicsController.Dispose();

            _playerInputSystem.Dispose();
            _playerStateTransitions.Dispose();
            base.HandleDestruction();
        }

        private void ValidateComponents()
        {
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
            _spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            _groundChecker ??= GetComponentInChildren<GroundChecker>();

            _playerInputSystem = new PlayerInputSystem(_globalInputSystem);
            _playerPhysicsController =
                new PlayerPhysicsController(_rigidbody2D, _abilities.PhysicsSettings, _groundChecker);
            _playerLocomotion = new PlayerLocomotion(_abilities.Movement,
                _playerInputSystem.MovementInput, _playerPhysicsController, _groundChecker);
            _playerJump = new PlayerJump(_abilities.Movement,
                _playerInputSystem.JumpPressed,
                _playerPhysicsController,
                _groundChecker);
            _spriteAnimator = new SpriteAnimator(_spriteRenderer);
            _stateMachine = new StateMachine<PlayerStateType>();


            GameLogger.Assert(_rigidbody2D != null, "Failed to get rigidbody2D", LogCategory.Player);

            _playerInputSystem.EnableInput();
        }

        private void SetupStateMachine()
        {
            _stateMachine.AddState(new PlayerIdleState());
            _stateMachine.AddState(new PlayerRunState());
            _stateMachine.AddState(new PlayerJumpState());
            _stateMachine.AddState(new PlayerFallState());

            _playerStateTransitions = new PlayerStateTransitions(_stateMachine, _playerLocomotion, _playerJump,
                _playerPhysicsController, _groundChecker);
        }

        private void SubscribeEvents()
        {
            var d = Disposable.CreateBuilder();
            _stateMachine.OnStateChanged.Subscribe(OnStateChanged).AddTo(ref d);
            d.RegisterTo(destroyCancellationToken);
        }

        private void FixedUpdate()
        {
            _playerPhysicsController?.FixedUpdate();
            _stateMachine?.FixedUpdate();
        }

        private void Update()
        {
            _stateMachine?.Update();
            _spriteAnimator.Update(Time.deltaTime);
        }

        private void OnStateChanged(PlayerStateType stateType)
        {
            var animationData = _playerAnimationRegistry.GetAnimation(stateType);
            if (animationData != null)
            {
                _spriteAnimator.Play(animationData);
            }
        }
    }
}
