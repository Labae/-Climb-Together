using System;
using Gameplay.Common.Enums;
using R3;
using UnityEngine;

namespace Gameplay.Common.Interfaces
{
    public interface IWallChecker
    {
        Observable<Unit> OnWallEntered { get; }
        Observable<Unit> OnWallExited { get; }

        ReadOnlyReactiveProperty<bool> IsWallDetected { get; }
        ReadOnlyReactiveProperty<Vector2> WallDirection { get; }
        ReadOnlyReactiveProperty<Vector2> WallNormal { get; }
        bool WasWallDetectedLastFrame { get; }

        void SetDirectionProvider(Func<FacingDirection> directionProvider);

        void CheckWallState();

        void CheckAtPosition(Vector2 origin, Vector2 direction, float distance = -1);
    }
}
