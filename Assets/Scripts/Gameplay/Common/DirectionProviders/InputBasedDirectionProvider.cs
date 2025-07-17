using System;
using Gameplay.Common.Enums;
using Gameplay.Common.Interfaces;
using R3;
using Systems.Input.Utilities;

namespace Gameplay.Common.DirectionProviders
{
    public class InputBasedDirectionProvider : IDirectionProvider, IDisposable
    {
        private readonly DirectionProvider _baseProvider;
        private readonly float _inputThreshold;
        private readonly CompositeDisposable _disposables = new();

        public FacingDirection CurrentDirection => _baseProvider.CurrentDirection;
        public Observable<FacingDirection> OnDirectionChanged => _baseProvider.OnDirectionChanged;

        public InputBasedDirectionProvider(
            Observable<float> movementInput,
            FacingDirection initialDirection = FacingDirection.Right,
            float inputThreshold = InputUtility.InputThreshold)
        {
            _baseProvider = new DirectionProvider(initialDirection);
            _inputThreshold = inputThreshold;

            movementInput
                .Where(input => InputUtility.IsInputActive(input, _inputThreshold))
                .Select(input => input > 0 ? FacingDirection.Right : FacingDirection.Left)
                .DistinctUntilChanged()
                .Subscribe(_baseProvider.SetDirection)
                .AddTo(_disposables);
        }

        public void SetDirection(FacingDirection direction)
        {
            _baseProvider.SetDirection(direction);
        }

        public void FlipDirection()
        {
            _baseProvider.FlipDirection();
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _baseProvider.Dispose();
        }
    }
}
