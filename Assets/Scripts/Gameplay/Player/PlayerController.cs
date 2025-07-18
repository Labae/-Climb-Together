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
using Gameplay.Common.Enums;
using Gameplay.Common.Interfaces;
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
        #region Core Components

        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _spriteRenderer;

        #endregion

        #region Player Systems

        private PlayerInputSystem _playerInputSystem;
        private IDirectionProvider _directionProvider;
        private PlayerLocomotion _playerLocomotion;
        private PlayerJump _playerJump;
        private GroundChecker _groundChecker;
        private PlayerPhysicsController _playerPhysicsController;
        private ISpriteAnimator _spriteAnimator;

        #endregion

        #region State Machine

        private IStateMachine<PlayerStateType> _stateMachine;
        private PlayerStateTransitions _playerStateTransitions;

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

        /// <summary>플레이어 방향 제공자</summary>
        public IDirectionProvider DirectionProvider => _directionProvider;

        /// <summary>물리 컨트롤러</summary>
        public PlayerPhysicsController PhysicsController => _playerPhysicsController;

        /// <summary>상태 머신</summary>
        public IStateMachine<PlayerStateType> StateMachine => _stateMachine;

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

            _groundChecker ??= GetComponentInChildren<GroundChecker>();
            GameLogger.Assert(_groundChecker != null, "Failed to get GroundChecker component", LogCategory.Player);

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
                    _playerInputSystem.MovementInput,
                    FacingDirection.Right
                );

                // 물리 컨트롤러 설정
                _playerPhysicsController = new PlayerPhysicsController(
                    _rigidbody2D,
                    _abilities.PhysicsSettings,
                    _groundChecker
                );

                // 로코모션 시스템 설정
                _playerLocomotion = new PlayerLocomotion(
                    _abilities.Movement,
                    _playerInputSystem.MovementInput,
                    _playerPhysicsController,
                    _groundChecker
                );

                // 점프 시스템 설정
                _playerJump = new PlayerJump(
                    _abilities.PhysicsSettings,
                    _abilities.Movement,
                    _playerInputSystem.JumpPressed,
                    _playerPhysicsController,
                    _groundChecker
                );

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
                _stateMachine = new StateMachine<PlayerStateType>(
                    PlayerStateType.Idle,
                    enablePerformanceTracking: _enablePerformanceTracking,
                    enableDetailedLogging: _enableDetailedLogging,
                    logCategory: LogCategory.Player
                );

                // 상태들 추가
                _stateMachine.AddState(new PlayerIdleState());
                _stateMachine.AddState(new PlayerRunState());
                _stateMachine.AddState(new PlayerJumpState());
                _stateMachine.AddState(new PlayerFallState());

                // 상태 전환 시스템 설정
                _playerStateTransitions = new PlayerStateTransitions(
                    _stateMachine,
                    _playerLocomotion,
                    _playerJump,
                    _playerPhysicsController,
                    _groundChecker
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

        #endregion

        #region Event Subscription

        private void SubscribeEvents()
        {
            try
            {
                var d = Disposable.CreateBuilder();

                // 상태 변경 이벤트
                _stateMachine.OnStateChanged
                    .Subscribe(OnStateChanged)
                    .AddTo(ref d);

                // 방향 변경 이벤트 (스프라이트 플립)
                _directionProvider.OnDirectionChanged
                    .Subscribe(OnDirectionChanged)
                    .AddTo(ref d);

                // 애니메이션 완료 이벤트 (필요시)
                if (_spriteAnimator is SpriteAnimator spriteAnimator)
                {
                    // 애니메이션 관련 이벤트 구독 가능
                }

                // 성능 추적 이벤트 (디버그용)
                if (_enablePerformanceTracking)
                {
                    SetupPerformanceTracking(ref d);
                }

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

        private void SetupPerformanceTracking(ref DisposableBuilder disposableBuilder)
        {
            // 물리 상태 변화 추적
            _playerPhysicsController.IsMoving
                .DistinctUntilChanged()
                .Subscribe(isMoving => {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug(ZString.Concat("Physics - IsMoving changed to: ", isMoving), LogCategory.Player);
                    }
                })
                .AddTo(ref disposableBuilder);

            // 접지 상태 변화 추적
            _groundChecker.IsGrounded
                .DistinctUntilChanged()
                .Subscribe(isGrounded => {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug(ZString.Concat("Ground - IsGrounded changed to: ", isGrounded), LogCategory.Player);
                    }
                })
                .AddTo(ref disposableBuilder);
        }

        #endregion

        #region Event Handlers

        private void OnStateChanged(PlayerStateType stateType)
        {
            try
            {
                // 애니메이션 처리
                HandleAnimationChange(stateType);

                // 상태별 특수 처리
                HandleStateSpecificLogic(stateType);

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("State changed handled: ", stateType), LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error handling state change to ", stateType, ": ", e.Message), LogCategory.Player);
            }
        }

        private void HandleAnimationChange(PlayerStateType stateType)
        {
            // 같은 애니메이션이면 스킵 (성능 최적화)
            if (_lastAnimationState == stateType)
                return;

            var animationData = _playerAnimationRegistry.GetAnimation(stateType);
            if (animationData != null)
            {
                _spriteAnimator.Play(animationData);
                _lastAnimationState = stateType;

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Animation played: ", animationData.name), LogCategory.Player);
                }
            }
            else
            {
                GameLogger.Warning(ZString.Concat("No animation found for state: ", stateType), LogCategory.Player);
            }
        }

        private void HandleStateSpecificLogic(PlayerStateType stateType)
        {
            switch (stateType)
            {
                case PlayerStateType.Jump:
                    // 점프 시작 시 특수 로직 (예: 파티클 효과, 사운드 등)
                    break;

                case PlayerStateType.Fall:
                    // 낙하 시작 시 특수 로직
                    break;

                case PlayerStateType.Idle:
                    // 아이들 상태 진입 시 특수 로직
                    break;

                case PlayerStateType.Run:
                    // 달리기 상태 진입 시 특수 로직
                    break;
            }
        }

        private void OnDirectionChanged(FacingDirection direction)
        {
            try
            {
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.flipX = direction == FacingDirection.Left;

                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug(ZString.Concat("Direction changed to: ", direction), LogCategory.Player);
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error handling direction change: ", e.Message), LogCategory.Player);
            }
        }

        #endregion

        #region Update Methods

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

        #endregion

        #region Public API

        /// <summary>
        /// 플레이어를 특정 위치로 텔레포트
        /// </summary>
        /// <param name="position">목표 위치</param>
        public void TeleportTo(Vector2 position)
        {
            EnsureInitialized();

            transform.position = position;
            _groundChecker?.CheckGroundState();

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Player teleported to: ", position.x.ToString("F2"), ", ", position.y.ToString("F2")), LogCategory.Player);
            }
        }

        /// <summary>
        /// 플레이어 입력 활성화/비활성화
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        public void SetInputEnabled(bool enabled)
        {
            if (!IsInitialized) return;

            if (enabled)
            {
                _playerInputSystem?.EnableInput();
            }
            else
            {
                _playerInputSystem?.DisableInput();
            }

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Player input ", enabled ? "enabled" : "disabled"), LogCategory.Player);
            }
        }

        /// <summary>
        /// 플레이어 강제 정지
        /// </summary>
        public void ForceStop()
        {
            if (!IsInitialized) return;

            _playerPhysicsController?.Stop();
            _playerLocomotion?.StopLocomotion();

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("Player force stopped", LogCategory.Player);
            }
        }

        /// <summary>
        /// 상태 강제 변경
        /// </summary>
        /// <param name="stateType">목표 상태</param>
        public void ForceChangeState(PlayerStateType stateType)
        {
            if (!IsInitialized) return;

            _stateMachine?.ForceChangeState(stateType);

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("State force changed to: ", stateType), LogCategory.Player);
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
            sb.AppendLine(ZString.Concat("Grounded: ", _groundChecker?.IsGrounded.CurrentValue ?? false));
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
                _playerStateTransitions?.Dispose();
                _playerLocomotion?.Dispose();
                _playerJump?.Dispose();
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
