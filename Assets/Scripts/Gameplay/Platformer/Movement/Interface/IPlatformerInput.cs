using R3;

namespace Gameplay.Platformer.Movement.Interface
{
    public interface IPlatformerInput
    {
        Observable<float> MovementInput { get; }
        Observable<bool> JumpPressed { get; }
    }
}
