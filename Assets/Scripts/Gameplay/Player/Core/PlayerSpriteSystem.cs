using System;
using Cysharp.Text;
using Data.Platformer.Enums;
using Data.Player.Animations;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Platformer.Movement.Enums;
using Gameplay.Platformer.Movement.Interface;
using R3;
using Systems.Animations;
using Systems.StateMachine.Interfaces;
using Systems.Visuals.Animation;
using Systems.Visuals.Orientation;

namespace Gameplay.Player.Core
{
    public class PlayerSpriteSystem : IDisposable
    {
        private readonly IStateMachine<PlatformerStateType> _stateMachine;
        private readonly IDirectionProvider _directionProvider;
        private readonly ISpriteOrientation _spriteOrientation;
        private readonly ISpriteAnimator _spriteAnimator;
        private readonly IPlatformerMovementController _movementController;
        private readonly PlayerAnimationRegistry _animationRegistry;
        private readonly CompositeDisposable _disposables = new();

        private PlatformerStateType _lastAnimationState;

        public PlayerSpriteSystem(
            IStateMachine<PlatformerStateType> stateMachine,
            IDirectionProvider  directionProvider,
            ISpriteOrientation spriteOrientation,
            ISpriteAnimator spriteAnimator,
            IPlatformerMovementController movementController,
            PlayerAnimationRegistry animationRegistry
        )
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _directionProvider = directionProvider  ?? throw new ArgumentNullException(nameof(directionProvider));
            _spriteOrientation = spriteOrientation ?? throw new ArgumentNullException(nameof(spriteOrientation));
            _spriteAnimator = spriteAnimator ?? throw new ArgumentNullException(nameof(spriteAnimator));
            _movementController = movementController ?? throw new ArgumentNullException(nameof(movementController));
            _animationRegistry = animationRegistry ?? throw new ArgumentNullException(nameof(animationRegistry));

            SubscribeToEvents();
            GameLogger.Debug("[PlayerAnimationSystem] initialized.", LogCategory.Player);
        }

        private void SubscribeToEvents()
        {
            try
            {
                _stateMachine.OnStateEnter
                    .Subscribe(HandleAnimationChange)
                    .AddTo(_disposables);

                _directionProvider.OnDirectionChanged
                    .Subscribe(HandleDirectionChange)
                    .AddTo(_disposables);

                _movementController.OnSpecialActionStarted
                    .Subscribe(HandleSpecialActionAnimation)
                    .AddTo(_disposables);

                GameLogger.Debug("[PlayerAnimationSystem] event subscribed.", LogCategory.Player);
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("[PlayerAnimationSystem] Failed to subscribe to events: ", e.Message), LogCategory.Player);
                throw;
            }
        }

        private void HandleAnimationChange(PlatformerStateType stateType)
        {
            try
            {
                if (_lastAnimationState == stateType)
                {
                    return;
                }

                var animationData = _animationRegistry.GetAnimation(stateType);
                if (animationData == null)
                {
                    GameLogger.Warning(
                        ZString.Format("[PlayerAnimationSystem] No animation found for state: {0}", stateType),
                        LogCategory.Player);
                    return;
                }

                _spriteAnimator.Play(animationData);
                _lastAnimationState = stateType;
                GameLogger.Debug(
                    ZString.Format("[PlayerAnimationSystem] Animation changed to: {0} ({1})", stateType,
                        animationData.AnimationName), LogCategory.Player);
            }
            catch (Exception e)
            {
                GameLogger.Error(
                    ZString.Concat("[PlayerAnimationSystem] Error handling animation change to {0}: {1}", stateType,
                        e.Message), LogCategory.Player);
            }
        }

        private void HandleDirectionChange(FacingDirection direction)
        {
            try
            {
                _spriteOrientation.SetDirection(direction);
                GameLogger.Debug(ZString.Format("[PlayerAnimationSystem] Direction changed to: {0}", direction),
                    LogCategory.Player);
            }
            catch (Exception e)
            {
                GameLogger.Error(
                    ZString.Concat("[PlayerAnimationSystem] Error handling direction change to {0}: {1}", direction,
                        e.Message), LogCategory.Player);
            }
        }

        private void HandleSpecialActionAnimation(SpecialActionType actionType)
        {
            var stateType = actionType switch
            {
                SpecialActionType.WallJump => PlatformerStateType.WallJump,
                SpecialActionType.Dashing => PlatformerStateType.Dash,
                _ => (PlatformerStateType?)null
            };

            if (stateType.HasValue)
            {
                ForcePlayAnimation(stateType.Value);
            }
        }

        private void ForcePlayAnimation(PlatformerStateType stateType)
        {
            var animationData = _animationRegistry.GetAnimation(stateType);
            if (animationData == null)
            {
                GameLogger.Warning(
                    ZString.Format("[PlayerAnimationSystem] No animation found for state: {0}", stateType),
                    LogCategory.Player);
                return;
            }

            _spriteAnimator.Play(animationData);
            _lastAnimationState = stateType;
        }

        public void Dispose()
        {
            try
            {
                _spriteAnimator?.Dispose();
                _disposables.Dispose();
                GameLogger.Debug("[PlayerAnimationSystem] disposed", LogCategory.Player);
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("[PlayerAnimationSystem] Error disposing: ", e.Message),
                    LogCategory.Player);
            }
        }
    }
}
