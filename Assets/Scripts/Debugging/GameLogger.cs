using System;
using System.Collections.Generic;
using System.Text;
using Debugging.Enum;

namespace Debugging
{
    public static class GameLogger
    {
        private static readonly Queue<StringBuilder> StringBuilderPool = new();
        private static string _logFilePath;

        static GameLogger()
        {
        }

        public static void Debug(string message, LogCategory category = LogCategory.System)
        {
            Log(message, LogLevel.Debug, category);
        }

        public static void Info(string message, LogCategory category = LogCategory.System)
        {
            Log(message, LogLevel.Info, category);
        }

        public static void Warning(string message, LogCategory category = LogCategory.System)
        {
            Log(message, LogLevel.Warning, category);
        }

        public static void Error(string message, LogCategory category = LogCategory.System)
        {
            Log(message, LogLevel.Error, category);
        }

        public static void Assert(bool condition, string message, LogCategory category = LogCategory.System)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (condition)
            {
                return;
            }

            Log($"ASSERTION FAILED: {message}", LogLevel.Assert, category);
            UnityEngine.Debug.Break();
#endif
        }

        private static void Log(string message, LogLevel level, LogCategory category)
        {
            // 메시지 포맷팅
            var sb = GetStringBuilder();
            sb.Append($"[{DateTime.Now:HH:mm:ss.fff}]");
            sb.Append($"[{level}]");
            sb.Append($"[{category}] ");
            sb.Append(message);

            var formattedMessage = sb.ToString();
            ReturnStringBuilder(sb);

            // 콘솔 출력
#if UNITY_EDITOR || DEVELOPMENT_BUILD

            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Assert:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
#endif
        }

        private static StringBuilder GetStringBuilder()
        {
            lock (StringBuilderPool)
            {
                if (StringBuilderPool.Count <= 0)
                {
                    return new StringBuilder(256);
                }

                var sb = StringBuilderPool.Dequeue();
                sb.Clear();
                return sb;
            }
        }

        private static void ReturnStringBuilder(StringBuilder sb)
        {
            lock (StringBuilderPool)
            {
                StringBuilderPool.Enqueue(sb);
            }
        }
    }
}