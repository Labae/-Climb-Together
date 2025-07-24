using System;
using Gameplay.Common.Enums;
using Gameplay.Common.WallDetection;
using R3;
using Systems.Animations;
using UnityEngine;

namespace Gameplay.Common.Interfaces
{
    public interface IWallDetector
    {
        Observable<Unit> OnWallEntered { get; }
        Observable<Unit> OnWallExited { get; }

        ReadOnlyReactiveProperty<bool> IsWallDetected { get; }
        ReadOnlyReactiveProperty<WallSideType> WallSide { get; }
        ReadOnlyReactiveProperty<Vector2> WallNormal { get; }
        bool WasWallDetectedLastFrame { get; }

        void SetDirectionProvider(IDirectionProvider directionProvider);

        void CheckWallState();

        void CheckAtPosition(Vector2 origin, FacingDirection direction, float distance = -1);

        WallDetectionResult CheckBothSides(Vector2 origin, float distance = -1);

        bool IsCurrentlyDetectingWall();
        void ForceWallState(bool detected);
    }
}
