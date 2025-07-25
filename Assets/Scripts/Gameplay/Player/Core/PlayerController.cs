using System;
using System.ComponentModel.DataAnnotations;
using Core.Behaviours;
using Cysharp.Text;
using Data.Player.Abilities.Data.Player;
using Data.Player.Animations;
using Data.Player.Enums;
using Debugging;
using Debugging.Enum;
using Gameplay.Common;
using Gameplay.Common.DirectionProviders;
using Gameplay.Common.Interfaces;
using Gameplay.Common.WallDetection;
using Gameplay.Player.Core;
using Gameplay.Player.Events;
using Gameplay.Player.Jump;
using Gameplay.Player.Locomotion;
using Gameplay.Player.States;
using R3;
using Systems.Animations;
using Systems.Input.Interfaces;
using Systems.StateMachine;
using Systems.StateMachine.Interfaces;
using Systems.Visuals.Animation;
using Systems.Visuals.Orientation;
using UnityEngine;
using VContainer;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : CoreBehaviour
    {
        #region Core Components

        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _spriteRenderer;

        #endregion

        #region Player Systems

        private PlayerInputSystem _playerInputSystem;
        private IDirectionProvider _directionProvider;
        private PlayerLocomotionSystem _playerLocomotionSystem;
        private PlayerJumpSystem _playerJumpSystem;
        private GroundDetector _groundDetector;
        private WallDetector _wallDetector;
        private PlayerPhysicsController _playerPhysicsController;
        private ISpriteOrientation _spriteOrientation;
        private ISpriteAnimator _spriteAnimator;

        #endregion

        #region State Machine

        private IStateMachine<PlayerStateType> _stateMachine;
        private PlayerStateTransitions _playerStateTransitions;

        #endregion

        #region Event System

        private PlayerEventBus _eventBus;
        private PlayerAnimationSystem _animationSystem;

        #endregion

        #region Configuration

        [Header("Required References")]
        [SerializeField, Required]
        private PlayerAnimationRegistry _playerAnimationRegistry;

        [Header("Debug Options")]
        [SerializeField]
        private bool _enablePerformanceTracking = false;

        [SerializeField]
        private bool _enableDetailedLogging = false;

        [SerializeField]
        private bool _showDebugInfo = false;

        #endregion

        #region Dependencies

        [Inject] private PlayerAbilities _abilities;
        [Inject] private IGlobalInputSystem _globalInputSystem;

        #endregion

        #region State Tracking

        private PlayerStateType _lastAnimationState;
        private float _initializationStartTime;

        #endregion

        #region Properties

        /// <summary>현재 플레이어 상태</summary>
        public PlayerStateType CurrentState => _stateMachine?.CurrentStateType.CurrentValue ?? PlayerStateType.Idle;


        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            base.OnInitialize();

            try
            {
                _initializationStartTime = Time.time;

                ValidateConfiguration();
                ValidateComponents();
                SetupSystems();
                SetupStateMachine();

                SetupAnimationSystem();

                SubscribeEvents();

                LogInitializationComplete();
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to initialize PlayerController: ", e.Message), LogCategory.Player);
                throw;
            }
        }

        private void ValidateConfiguration()
        {
            if (_playerAnimationRegistry == null)
            {
                throw new InvalidOperationException("PlayerAnimationRegistry is required but not assigned");
            }

            if (_abilities == null)
            {
                throw new InvalidOperationException("PlayerAbilities dependency not injected");
            }

            if (_globalInputSystem == null)
            {
                throw new InvalidOperationException("IGlobalInputSystem dependency not injected");
            }
        }

        private void ValidateComponents()
        {
            // Unity 컴포넌트 검증
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
            GameLogger.Assert(_rigidbody2D != null, "Failed to get Rigidbody2D component", LogCategory.Player);

            _spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            GameLogger.Assert(_spriteRenderer != null, "Failed to get SpriteRenderer component", LogCategory.Player);

            _groundDetector ??= GetComponentInChildren<GroundDetector>();
            GameLogger.Assert(_groundDetector != null, "Failed to get GroundDetector component", LogCategory.Player);

            _wallDetector ??= GetComponentInChildren<WallDetector>();
            GameLogger.Assert(_wallDetector != null, "Failed to get WallDetector component", LogCategory.Player);

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("All required components validated successfully", LogCategory.Player);
            }
        }

        private void SetupSystems()
        {
            try
            {
                // 이벤트 버스 설정
                _eventBus = new PlayerEventBus();

                // 입력 시스템 설정
                _playerInputSystem = new PlayerInputSystem(_globalInputSystem);

                // 방향 제공자 설정
                _directionProvider = DirectionProviderFactory.CreateInputBased(
                    _playerInputSystem.MovementInput
                );

                // Wall에 방향 제공자 설정
                _wallDetector.SetDirectionProvider(_directionProvider);

                // 물리 컨트롤러 설정
                _playerPhysicsController = new PlayerPhysicsController(
                    _rigidbody2D,
                    _abilities.PhysicsSettings,
                    _groundDetector
                );

                // 로코모션 시스템 설정
                _playerLocomotionSystem = new PlayerLocomotionSystem(
                    _eventBus,
                    _abilities.Movement,
                    _playerInputSystem.MovementInput,
                    _playerPhysicsController,
                    _groundDetector,
                    _wallDetector,
                    true
                );

                // 점프 시스템 설정
                _playerJumpSystem = new PlayerJumpSystem(
                    _eventBus,
                    _playerPhysicsController,
                    _groundDetector,
                    _wallDetector,
                    _playerInputSystem.JumpPressed,
                    _abilities.Movement,
                    _abilities.PhysicsSettings
                );

                // 스프라이트 방향 시스템 설정
                _spriteOrientation = new SpriteOrientation(_spriteRenderer, FacingDirection.Right);

                // 애니메이션 시스템 설정
                _spriteAnimator = new SpriteAnimator(_spriteRenderer);

                // 입력 활성화
                _playerInputSystem.EnableInput();

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("All player systems initialized successfully", LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to setup player systems: ", e.Message), LogCategory.Player);
                throw;
            }
        }

        private void SetupStateMachine()
        {
            try
            {
                // 상태 머신 생성
                _stateMachine = new StateMachine<PlayerStateType>(enablePerformanceTracking: _enablePerformanceTracking,
                    enableDetailedLogging: _enableDetailedLogging,
                    logCategory: LogCategory.Player
                );

                // 상태들 추가
                _stateMachine.AddState(new PlayerIdleState());
                _stateMachine.AddState(new PlayerRunState());
                _stateMachine.AddState(new PlayerJumpState());
                _stateMachine.AddState(new PlayerDoubleJumpState());
                _stateMachine.AddState(new PlayerFallState());
                _stateMachine.AddState(new PlayerWallSlideState(_playerPhysicsController));

                // 상태 전환 시스템 설정
                _playerStateTransitions = new PlayerStateTransitions(
                    _stateMachine,
                    _playerLocomotionSystem,
                    _playerJumpSystem,
                    _playerPhysicsController,
                    _groundDetector,
                    _wallDetector,
                    _eventBus
                );

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("State machine initialized with ", _stateMachine.StateCount, " states"), LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to setup state machine: ", e.Message), LogCategory.Player);
                throw;
            }
        }

        private void SetupAnimationSystem()
        {
            try
            {
                _animationSystem = new PlayerAnimationSystem(_eventBus, _spriteOrientation, _spriteAnimator,
                    _playerAnimationRegistry);

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("Animation system initialized successfully", LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to setup animation system: ", e.Message), LogCategory.Player);
                throw;
            }
        }

        #endregion

        #region Event Subscription

        private void SubscribeEvents()
        {
            try
            {
                var d = Disposable.CreateBuilder();

                // 상태 변경 이벤트
                _stateMachine.OnStateChanged
                    .Subscribe(stateType =>
                    {
                        _eventBus.Publish(new StateChangedEvent(stateType));
                    })
                    .AddTo(ref d);

                // 방향 변경 이벤트 (스프라이트 플립)
                _directionProvider.OnDirectionChanged
                    .Subscribe(direction =>
                    {
                        _eventBus.Publish(new DirectionChangedEvent(direction));
                    })
                    .AddTo(ref d);

                d.RegisterTo(destroyCancellationToken);

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("Event subscriptions completed", LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to subscribe events: ", e.Message), LogCategory.Player);
                throw;
            }
        }

        #endregion

        #region Update Methods

        private void Update()
        {
            if (!IsInitialized) return;

            try
            {
                _spriteAnimator?.Update(Time.deltaTime);
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in Update: ", e.Message), LogCategory.Player);
            }
        }

        private void FixedUpdate()
        {
            if (!IsInitialized) return;

            try
            {
                _playerPhysicsController?.FixedUpdate();
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in FixedUpdate: ", e.Message), LogCategory.Player);
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 플레이어 정보
        /// </summary>
        public string GetPlayerDebugInfo()
        {
            if (!IsInitialized)
                return "PlayerController not initialized";

            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== Player Controller Debug Info ===");
            sb.AppendLine(ZString.Concat("Initialized: ", IsInitialized));
            sb.AppendLine(ZString.Concat("Current State: ", CurrentState));
            sb.AppendLine(ZString.Concat("Direction: ", _directionProvider?.CurrentDirection ?? FacingDirection.Right));
            sb.AppendLine(ZString.Concat("Grounded: ", _groundDetector?.IsGrounded.CurrentValue ?? false));
            sb.AppendLine(ZString.Concat("Moving: ", _playerPhysicsController?.IsCurrentlyMoving() ?? false));
            sb.AppendLine(ZString.Concat("Input Enabled: ", _playerInputSystem?.IsInputEnabled ?? false));

            if (_enablePerformanceTracking && _stateMachine != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== Performance Data ===");
                sb.Append(_stateMachine.GetStateMachineDebugInfo());
            }

            return sb.ToString();
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugInfo || !IsInitialized) return;

            // 플레이어 상태 정보를 Scene 뷰에 표시
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                GetPlayerDebugInfo(),
                new GUIStyle() { normal = { textColor = Color.white } }
            );
        }
#endif

        #endregion

        #region Destruction

        protected override void HandleDestruction()
        {
            try
            {
                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("Starting PlayerController destruction", LogCategory.Player);
                }

                // 시스템들 정리 (순서 중요)
                _animationSystem?.Dispose();
                _eventBus?.Dispose();
                _playerStateTransitions?.Dispose();
                _playerLocomotionSystem?.Dispose();
                _playerJumpSystem?.Dispose();
                _stateMachine?.Dispose();
                _playerPhysicsController?.Dispose();
                _playerInputSystem?.Dispose();
                _directionProvider?.Dispose();


                if (_enableDetailedLogging)
                {
                    var destructionTime = Time.time - InitializationTime;
                    GameLogger.Debug(ZString.Concat("PlayerController destroyed after ", destructionTime.ToString("F2"), " seconds"), LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error during PlayerController destruction: ", e.Message), LogCategory.Player);
            }
            finally
            {
                base.HandleDestruction();
            }
        }

        #endregion

        #region Logging

        private void LogInitializationComplete()
        {
            var initTime = Time.time - _initializationStartTime;

            using var sb = ZString.CreateStringBuilder();
            sb.Append("PlayerController initialized successfully in ");
            sb.Append((initTime * 1000f).ToString("F1"));
            sb.Append("ms");

            if (_enablePerformanceTracking)
            {
                sb.Append(" (Performance tracking enabled)");
            }

            GameLogger.Debug(sb.ToString(), LogCategory.Player);
        }

        #endregion
    }
}
