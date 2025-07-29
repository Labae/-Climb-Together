using System;
using System.ComponentModel.DataAnnotations;
using Core.Behaviours;
using Cysharp.Text;
using Data.Platformer.Abilities.Data.Player;
using Data.Platformer.Enums;
using Data.Player.Animations;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.DirectionProviders;
using Gameplay.Common.Interfaces;
using Gameplay.Platformer.Movement;
using Gameplay.Platformer.Physics;
using Gameplay.Platformer.States;
using R3;
using Systems.Animations;
using Systems.Input.Interfaces;
using Systems.StateMachine;
using Systems.StateMachine.Interfaces;
using Systems.Visuals.Animation;
using Systems.Visuals.Orientation;
using UnityEngine;
using VContainer;

namespace Gameplay.Player.Core
{
    public class PlayerController : CoreBehaviour
    {
        #region Core Components

        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _circleCollider;

        #endregion

        #region Player Systems

        private PlayerInputSystem _playerInputSystem;
        private IDirectionProvider _directionProvider;
        private PlayerSpriteSystem _spriteSystem;
        private PlatformerPhysicsSystem _platformerPhysicsSystem;
        private PlatformerMovementController _platformerMovementController;

        #endregion

        #region State Machine

        private IStateMachine<PlatformerStateType> _stateMachine;

        #endregion

        #region Configuration

        [Header("Required References")] [SerializeField, Required]
        private PlayerAnimationRegistry _playerAnimationRegistry;

        [Header("Debug Options")] [SerializeField]
        private bool _enablePerformanceTracking = false;

        [SerializeField] private bool _enableDetailedLogging = false;

        [SerializeField] private bool _showDebugInfo = false;

        #endregion

        #region Dependencies

        [Inject] private PlatformerPlayerSettings _settings;
        [Inject] private IGlobalInputSystem _globalInputSystem;

        #endregion

        #region State Tracking

        private PlatformerStateType _lastAnimationState;
        private float _initializationStartTime;

        #endregion

        #region Properties

        /// <summary>현재 플레이어 상태</summary>
        public PlatformerStateType CurrentState =>
            _stateMachine?.CurrentStateType.CurrentValue ?? PlatformerStateType.Idle;

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
                GameLogger.Error(ZString.Concat("Failed to initialize PlayerController: ", e.Message),
                    LogCategory.Player);
                throw;
            }
        }

        private void ValidateConfiguration()
        {
            if (_playerAnimationRegistry == null)
            {
                throw new InvalidOperationException("PlayerAnimationRegistry is required but not assigned");
            }

            if (_settings == null)
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
            _circleCollider ??= GetComponent<CircleCollider2D>();
            GameLogger.Assert(_circleCollider != null, "Failed to get CircleCollider component", LogCategory.Player);

            _spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            GameLogger.Assert(_spriteRenderer != null, "Failed to get SpriteRenderer component", LogCategory.Player);

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("All required components validated successfully", LogCategory.Player);
            }
        }

        private void SetupSystems()
        {
            try
            {
                // 입력 시스템 설정
                _playerInputSystem = new PlayerInputSystem(_globalInputSystem);

                // 방향 제공자 설정
                _directionProvider = DirectionProviderFactory.CreateInputBased(
                    _playerInputSystem.MovementInput
                );

                _platformerPhysicsSystem = new PlatformerPhysicsSystem(
                    transform,
                    _circleCollider,
                    _settings.PhysicsSettings,
                    _settings.PlatformerMovement
                );

                _platformerMovementController = new PlatformerMovementController(
                    _platformerPhysicsSystem,
                    _playerInputSystem,
                    _settings.PlatformerMovement
                );


                // 입력 활성화
                _playerInputSystem.EnableInput();
                _platformerPhysicsSystem.SetGravityEnabled(true);

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
                _stateMachine = new StateMachine<PlatformerStateType>(
                    PlatformerStateType.Fall,
                    enablePerformanceTracking: _enablePerformanceTracking,
                    enableDetailedLogging: _enableDetailedLogging,
                    logCategory: LogCategory.Player
                );

                // 상태들 추가
                _stateMachine.AddState(new PlatformerIdleState(_platformerMovementController));
                _stateMachine.AddState(new PlatformerRunState(_platformerMovementController));
                _stateMachine.AddState(new PlatformerJumpState(_platformerMovementController));
                _stateMachine.AddState(new PlatformerFallState(_platformerMovementController));

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(
                        ZString.Concat("State machine initialized with ", _stateMachine.StateCount, " states"),
                        LogCategory.Player);
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
                // 스프라이트 방향 시스템 설정
                var spriteOrientation = new SpriteOrientation(_spriteRenderer, FacingDirection.Right);

                // 애니메이션 시스템 설정
                var spriteAnimator = new SpriteAnimator(_spriteRenderer);

                _spriteSystem = new PlayerSpriteSystem(
                    _stateMachine,
                    _directionProvider,
                    spriteOrientation,
                    spriteAnimator,
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
                _platformerMovementController.Update(Time.deltaTime);
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in Update: ", e.Message), LogCategory.Player);
            }
        }

        #endregion

        #region Input Processing

        private void FixedUpdate()
        {
            if (!IsInitialized) return;

            try
            {
                _platformerPhysicsSystem.PhysicsUpdate(Time.fixedDeltaTime);
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

                _platformerMovementController?.Dispose();
                _platformerPhysicsSystem?.Dispose();
                _spriteSystem?.Dispose();
                _stateMachine?.Dispose();
                _playerInputSystem?.Dispose();
                _directionProvider?.Dispose();

                if (_enableDetailedLogging)
                {
                    var destructionTime = Time.time - InitializationTime;
                    GameLogger.Debug(
                        ZString.Concat("PlayerController destroyed after ", destructionTime.ToString("F2"), " seconds"),
                        LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error during PlayerController destruction: ", e.Message),
                    LogCategory.Player);
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
