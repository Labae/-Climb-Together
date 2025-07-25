using System;
using Gameplay.Common.Enums;
using R3;
using Systems.Animations;

namespace Gameplay.Common.Interfaces
{
    public interface IDirectionProvider : IDisposable
    {
        FacingDirection CurrentDirection { get; }

        Observable<FacingDirection> OnDirectionChanged { get; }

        void SetDirection(FacingDirection direction);
        void FlipDirection();
    }
}
