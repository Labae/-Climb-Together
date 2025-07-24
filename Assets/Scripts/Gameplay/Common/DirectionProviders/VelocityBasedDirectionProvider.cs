using System;
using Gameplay.Common.Enums;
using Gameplay.Common.Interfaces;
using R3;
using Systems.Animations;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Common.DirectionProviders
{
    public class VelocityBasedDirectionProvider : IDirectionProvider, IDisposable
    {
        private readonly DirectionProvider _baseProvider;
        private readonly float _velocityThreshold;
        private readonly float _directionLockTime;
        private readonly CompositeDisposable _disposables = new();
        private float _lastDirectionChangeTime;

        public FacingDirection CurrentDirection => _baseProvider.CurrentDirection;
        public Observable<FacingDirection> OnDirectionChanged => _baseProvider.OnDirectionChanged;

        public VelocityBasedDirectionProvider(
            Observable<float> velocityStream,
            FacingDirection initialDirection = FacingDirection.Right,
            float velocityThreshold = PhysicsUtility.VelocityThreshold,
            float directionLockTime = 0.1f)
        {
            _baseProvider = new DirectionProvider(initialDirection);
            _velocityThreshold = velocityThreshold;
            _directionLockTime = directionLockTime;

            velocityStream
                .Where(_ => Time.unscaledTime - _lastDirectionChangeTime >= _directionLockTime)
                .Where(velocity => PhysicsUtility.HasValidVelocity(velocity, _velocityThreshold))
                .Select(velocity => velocity > 0 ? FacingDirection.Right : FacingDirection.Left)
                .DistinctUntilChanged()
                .Subscribe(direction =>
                {
                    _baseProvider.SetDirection(direction);
                    _lastDirectionChangeTime = Time.unscaledTime;
                })
                .AddTo(_disposables);
        }

        public void SetDirection(FacingDirection direction)
        {
            _baseProvider.SetDirection(direction);
            _lastDirectionChangeTime = Time.unscaledTime;
        }

        public void FlipDirection()
        {
            _baseProvider.FlipDirection();
            _lastDirectionChangeTime = Time.unscaledTime;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _baseProvider.Dispose();
        }
    }
}
