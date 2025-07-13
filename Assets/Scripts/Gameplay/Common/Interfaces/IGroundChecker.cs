using System;

namespace Gameplay.Common.Interfaces
{
    public interface IGroundChecker
    {
        event Action OnGroundEnter;
        event Action OnGroundExit;

        bool IsGrounded { get; }
        bool WasGroundedLastFrame { get; }
    }
}