using System;
using Cysharp.Text;
using Data.Player.Animations;
using Data.Player.Enums;
using Debugging;
using Debugging.Enum;
using Gameplay.Player.Events;
using R3;
using Systems.Animations;
using Systems.Visuals.Animation;
using Systems.Visuals.Orientation;

namespace Gameplay.Player.Core
{
    public class PlayerAnimationSystem : IDisposable
    {
        private readonly PlayerEventBus _eventBus;
        private readonly ISpriteOrientation _spriteOrientation;
        private readonly ISpriteAnimator _spriteAnimator;
        private readonly PlayerAnimationRegistry _animationRegistry;
        private readonly CompositeDisposable _disposables = new();

        private PlayerStateType _lastAnimationState;

        public PlayerAnimationSystem(
            PlayerEventBus eventBus,
            ISpriteOrientation spriteOrientation,
            ISpriteAnimator spriteAnimator,
            PlayerAnimationRegistry animationRegistry
        )
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _spriteOrientation = spriteOrientation ?? throw new ArgumentNullException(nameof(spriteOrientation));
            _spriteAnimator = spriteAnimator ?? throw new ArgumentNullException(nameof(spriteAnimator));
            _animationRegistry = animationRegistry ?? throw new ArgumentNullException(nameof(animationRegistry));

            SubscribeToEvents();
            GameLogger.Debug("[PlayerAnimationSystem] initialized.", LogCategory.Player);
        }

        private void SubscribeToEvents()
        {
            try
            {
                _eventBus.Subscribe<StateChangedEvent>()
                    .Subscribe(e => HandleAnimationChange(e.NewState))
                    .AddTo(_disposables);

                _eventBus.Subscribe<DirectionChangedEvent>()
                    .Subscribe(e => HandleDirectionChange(e.Direction))
                    .AddTo(_disposables);

                GameLogger.Debug("[PlayerAnimationSystem] event subscribed.", LogCategory.Player);
            }
            catch (Exception e)
            {
                GameLogger.Error("[PlayerAnimationSystem] Failed to subscribe to events.", LogCategory.Player);
                throw;
            }
        }

        private void HandleAnimationChange(PlayerStateType stateType)
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

        public void Dispose()
        {
            try
            {
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
