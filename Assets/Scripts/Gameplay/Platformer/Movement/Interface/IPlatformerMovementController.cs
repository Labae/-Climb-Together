using Gameplay.Platformer.Movement.Enums;
using R3;
using UnityEngine;

namespace Gameplay.Platformer.Movement.Interface
{
    public interface IPlatformerMovementController
    {
        Observable<Unit> OnLanded { get; }
        Observable<Unit> OnJumpStarted { get; }
        Observable<SpecialActionType> OnSpecialActionStarted { get; }
        Observable<SpecialActionType> OnSpecialActionEnded { get; }

        void Knockback(Vector2 direction, float force);

        bool CanJump();
        bool IsMoving();
        bool IsGrounded();
        bool IsIntendingToRun();
        bool IsActuallyRunning();
        bool IsFalling();
        bool IsRising();
        bool IsInSpecialAction();
        SpecialActionType GetSpecialAction();
    }
}
