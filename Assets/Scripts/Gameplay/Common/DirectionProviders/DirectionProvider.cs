using System;
using Gameplay.Common.Interfaces;
using R3;
using Systems.Animations;

namespace Gameplay.Common.DirectionProviders
{
    public class DirectionProvider : IDirectionProvider, IDisposable
    {
        private readonly ReactiveProperty<FacingDirection> _currentDirection;

        public FacingDirection CurrentDirection => _currentDirection.Value;
        public Observable<FacingDirection> OnDirectionChanged => _currentDirection.AsObservable();

        public DirectionProvider(FacingDirection initialDirection)
        {
            _currentDirection = new ReactiveProperty<FacingDirection>(initialDirection);
        }

        public void SetDirection(FacingDirection direction)
        {
            if (_currentDirection.Value == direction)
            {
                return;
            }

            _currentDirection.OnNext(direction);
        }

        public void FlipDirection()
        {
            var newDirection = _currentDirection.Value == FacingDirection.Left
                ? FacingDirection.Right
                : FacingDirection.Left;
            SetDirection(newDirection);
        }


        public void Dispose()
        {
            _currentDirection?.Dispose();
        }
    }
}
