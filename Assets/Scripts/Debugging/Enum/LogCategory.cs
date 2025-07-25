using System;

namespace Debugging.Enum
{
    [Flags]
    public enum LogCategory
    {
        Default,
        System,
        Audio,
        UI,
        Input,
        Player,
        Enemy
    }
}
