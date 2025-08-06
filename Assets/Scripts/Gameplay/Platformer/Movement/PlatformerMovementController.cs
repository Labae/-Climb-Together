using System;
using Data.Common;
using Data.Platformer.Settings;
using Gameplay.Common.Enums;
using Gameplay.Common.Interfaces;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using Gameplay.Platformer.Physics;
using R3;
using Systems.Animations;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Platformer.Movement
{
    public class PlatformerMovementController : IPlatformerMovementController, IDisposable
    {
        private readonly PlatformerPhysicsSystem _physicsSystem;
        private readonly IPlatformerInput _platformerInput;
        private readonly IDirectionProvider _directionProvider;
        private readonly PlatformerMovementSettings _settings;
        private readonly PlatformerPhysicsSettings _physicsSettings;

        private readonly PlatformerHorizontalMovementHandler _horizontalMovementHandler;
        private readonly PlatformerJumpHandler _jumpHandler;
        private readonly PlatformerDashHandler _dashHandler;
        private readonly PlatformerWallHandler _wallHandler;

        private SpecialActionType _currentSpecialAction = SpecialActionType.None;
        private float _specialActionTimer = 0f;
        private Vector3 _knockbackVelocity = Vector3.zero;

        private readonly Subject<Unit> _onLanded = new();
        private readonly Subject<SpecialActionType> _onSpecialActionStarted = new();
        private readonly Subject<SpecialActionType> _onSpecialActionEnded = new();

        private readonly CompositeDisposable _disposables = new();

        public Observable<Unit> OnLanded => _onLanded.AsObservable();
        public Observable<Unit> OnJumpStarted => _jumpHandler.OnJumpStarted;
        public Observable<Vector2> OnDashStarted => _dashHandler.OnDashStarted;
        public Observable<Unit> OnDashEnded => _dashHandler.OnDashEnded;
        public Observable<Unit> OnDashReset => _dashHandler.OnDashReset;
        public Observable<WallSideType> OnWallSlideStarted => _wallHandler.OnWallSlideStarted;
        public Observable<Unit> OnWallSlideEnded => _wallHandler.OnWallSlideEnded;
        public Observable<Vector2> OnWallJumped => _wallHandler.OnWallJumped;
        public Observable<SpecialActionType> OnSpecialActionStarted => _onSpecialActionStarted.AsObservable();
        public Observable<SpecialActionType> OnSpecialActionEnded => _onSpecialActionEnded.AsObservable();

        public
            PlatformerMovementController(
            PlatformerPhysicsSystem physicsSystem,
            IPlatformerInput platformerInput,
            IDirectionProvider directionProvider,
            PlatformerMovementSettings settings,
            PlatformerPhysicsSettings  physicsSettings
        )
        {
            _physicsSystem = physicsSystem;
            _platformerInput = platformerInput;
            _directionProvider = directionProvider;
            _settings = settings;
            _physicsSettings = physicsSettings;

            _horizontalMovementHandler = new PlatformerHorizontalMovementHandler(
                _physicsSystem, _platformerInput, OnSpecialActionStarted, _settings, _physicsSettings);

            _jumpHandler = new PlatformerJumpHandler(
                _physicsSystem, _platformerInput, _settings);

            _dashHandler = new PlatformerDashHandler(
                _physicsSystem, _platformerInput, _settings);

            _wallHandler = new PlatformerWallHandler(
                _physicsSystem, _platformerInput, _settings);

            SubscribeToPhysicsEvents();
            SubscribeToDashEvents();
            SubscribeToWallEvents();
        }

        #region Events Subscription

        private void SubscribeToPhysicsEvents()
        {
            // 착지 이벤트 구독
            _physicsSystem.OnLanded
                .Subscribe(_ => HandleLanded())
                .AddTo(_disposables);
        }

        private void SubscribeToDashEvents()
        {
            _dashHandler.OnDashStarted
                .Subscribe(HandleDashStarted)
                .AddTo(_disposables);

            _dashHandler.OnDashEnded
                .Subscribe(_ => HandleDashEnded())
                .AddTo(_disposables);
        }

        private void SubscribeToWallEvents()
        {
            _wallHandler.OnWallSlideStarted
                .Subscribe(HandleWallSlideStarted)
                .AddTo(_disposables);

            _wallHandler.OnWallJumped
                .Subscribe(HandleWallJumped)
                .AddTo(_disposables);
        }

        private void HandleLanded()
        {
            _onLanded.OnNext(Unit.Default);

            // Special Action 종료 조건 (Knockback만, Dash는 DashHandler에서 관리)
            if (_currentSpecialAction == SpecialActionType.Knockback)
            {
                EndSpecialAction();
            }
        }

        private void HandleDashStarted(Vector2 direction)
        {
            StartSpecialAction(SpecialActionType.Dashing, _settings.DashDuration);
        }

        private void HandleDashEnded()
        {
            if (_currentSpecialAction == SpecialActionType.Dashing)
            {
                EndSpecialAction();
            }
        }

        private void HandleWallSlideStarted(WallSideType wallSide)
        {
            var facingDirection = wallSide == WallSideType.Left ? FacingDirection.Left :  FacingDirection.Right;
            _directionProvider.SetDirection(facingDirection);
        }

        private void HandleWallJumped(Vector2 direction)
        {
            StartSpecialAction(SpecialActionType.WallJump, _settings.WallJumpInputLockTime);

            var facingDirection = direction.x > 0 ? FacingDirection.Right :  FacingDirection.Left;
            _directionProvider.SetDirection(facingDirection);
        }

        #endregion

        #region Special Actions

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

            switch (specialAction)
            {
                case SpecialActionType.Dashing:
                    // 대시 중에는 수평 이동과 점프만 비활성화 (대시는 계속 활성)
                    _horizontalMovementHandler.SetEnabled(false);
                    _jumpHandler.SetEnabled(false);
                    _wallHandler.SetEnabled(false);
                    break;

                case SpecialActionType.Knockback:
                    // 넉백 중에는 모든 입력 비활성화
                    _horizontalMovementHandler.SetEnabled(false);
                    _jumpHandler.SetEnabled(false);
                    _dashHandler.SetEnabled(false);
                    _wallHandler.SetEnabled(false);
                    break;
                case SpecialActionType.WallJump:
                    break;
                case SpecialActionType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specialAction), specialAction, null);
            }

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

            _horizontalMovementHandler.SetEnabled(true);
            _jumpHandler.SetEnabled(true);
            _dashHandler.SetEnabled(true);
            _wallHandler.SetEnabled(true);

            _onSpecialActionEnded.OnNext(endedAction);
        }

        #endregion

        #region Update Logic

        public void Update(float deltaTime)
        {
            _wallHandler.Update(deltaTime);

            bool isInputLocked = _wallHandler.IsHorizontalInputLocked();
            _horizontalMovementHandler.Update(deltaTime, isInputLocked);

            _jumpHandler.Update(deltaTime);
            _dashHandler.Update(deltaTime);

            UpdateSpecialActions(deltaTime);
        }

        private void UpdateSpecialActions(float deltaTime)
        {
            if (_specialActionTimer > 0f)
            {
                _specialActionTimer -= deltaTime;

                // 대시는 DashHandler에서 자동 종료되므로 타이머로 종료하지 않음
                if (_specialActionTimer <= 0f && _currentSpecialAction != SpecialActionType.Dashing)
                {
                    EndSpecialAction();
                }
            }
        }

        #endregion

        #region State Getters (핸들러들에서 위임)

        /// <summary>점프 가능 여부</summary>
        public bool CanJump() => _jumpHandler.CanJump();

        /// <summary>현재 점프 중인지</summary>
        public bool IsJumping() => _jumpHandler.IsJumping();

        /// <summary>대시 가능 여부</summary>
        public bool CanDash() => _dashHandler.CanDash();

        /// <summary>현재 대시 중인지</summary>
        public bool IsDashing() => _dashHandler.IsDashing();

        /// <summary>현재 대시 카운트</summary>
        public int GetDashCount() => _dashHandler.GetCurrentDashCount();

        /// <summary>대시 쿨다운 남은 시간</summary>
        public float GetDashCooldown() => _dashHandler.GetDashCooldownRemaining();

        /// <summary>움직이고 있는지</summary>
        public bool IsMoving() => PhysicsUtility.IsMoving(_physicsSystem.Velocity.CurrentValue);

        /// <summary>땅에 있는지</summary>
        public bool IsGrounded() => _physicsSystem.IsGrounded.CurrentValue;

        /// <summary>달리려고 하는지</summary>
        public bool IsIntendingToRun() => _horizontalMovementHandler.IsIntendingToRun();

        /// <summary>실제로 달리고 있는지</summary>
        public bool IsActuallyRunning() => _horizontalMovementHandler.IsRunning() && IsMoving() && !IsInSpecialAction();

        /// <summary>떨어지고 있는지</summary>
        public bool IsFalling() => PhysicsUtility.IsFalling(_physicsSystem.Velocity.CurrentValue);

        /// <summary>올라가고 있는지</summary>
        public bool IsRising() => PhysicsUtility.IsRising(_physicsSystem.Velocity.CurrentValue);

        /// <summary>특수 액션 중인지</summary>
        public bool IsInSpecialAction() => _currentSpecialAction != SpecialActionType.None;

        public bool IsWallSliding()
        {
            return _wallHandler.IsWallSliding();
        }

        /// <summary>현재 특수 액션 타입</summary>
        public SpecialActionType GetSpecialAction() => _currentSpecialAction;

        #endregion

        #region Dispose

        public void Dispose()
        {
            _horizontalMovementHandler?.Dispose();
            _jumpHandler?.Dispose();
            _dashHandler?.Dispose();
            _wallHandler?.Dispose();

            // 이벤트들 정리
            _onLanded?.Dispose();
            _onSpecialActionStarted?.Dispose();
            _onSpecialActionEnded?.Dispose();
            _disposables?.Dispose();
        }

        #endregion
    }
}
