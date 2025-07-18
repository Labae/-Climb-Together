using System;

namespace Debugging.Enum
{
    [Flags]
    public enum LogLevel
    {
        None = 0,
        Debug = 1 << 0,    // 1
        Info = 1 << 1,     // 2
        Warning = 1 << 2,  // 4
        Error = 1 << 3,    // 8
        Assert = 1 << 4    // 16
    }
}
