using System;
using Cysharp.Text;
using Gameplay.Common.Enums;

namespace Gameplay.Common.WallDetection
{
    [Serializable]
    public readonly struct WallDetectionResult
    {
        public readonly bool LeftWall;
        public readonly bool RightWall;

        public WallDetectionResult(bool leftWall, bool rightWall)
        {
            LeftWall = leftWall;
            RightWall = rightWall;
        }

        public bool HasAnyWall => LeftWall || RightWall;
        public bool HasBothWalls => LeftWall && RightWall;
        public bool HasOnlyLeftWall => LeftWall && !RightWall;
        public bool HasOnlyRightWall => !LeftWall && RightWall;

        public bool HasWallOn(WallSideType sideType)
        {
            return sideType == WallSideType.Left ? LeftWall : RightWall;
        }

        public override string ToString()
        {
            return ZString.Format("WallDetection(Left: {0}, Right: {1})", LeftWall, RightWall);
        }
    }
}
