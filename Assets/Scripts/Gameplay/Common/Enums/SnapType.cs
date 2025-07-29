using System;

namespace Gameplay.Common.Enums
{
    [Flags]
    public enum SnapType
    {
        None = 0,
        Ground = 1 << 0,
        Wall = 1 << 1,
    }
}
