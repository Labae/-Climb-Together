using System;
using Gameplay.Common.Enums;
using R3;

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
