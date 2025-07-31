using R3;
using UnityEngine;

namespace Gameplay.Platformer.Movement.Interface
{
    public interface IPlatformerInput
    {
        Observable<float> MovementInput { get; }
        Observable<bool> JumpPressed { get; }

        Observable<Unit> DashPressed { get; }
        Observable<Vector2> DirectionalInput { get; }
    }
}
