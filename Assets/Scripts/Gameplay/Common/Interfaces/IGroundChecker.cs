using R3;

namespace Gameplay.Common.Interfaces
{
    public interface IGroundChecker
    {
        Observable<Unit> OnGroundEntered { get; }
        Observable<Unit> OnGroundExited { get; }

        ReadOnlyReactiveProperty<bool> IsGrounded { get; }
        bool WasGroundedLastFrame { get; }
    }
}
