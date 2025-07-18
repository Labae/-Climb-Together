using System;
using System.Collections.Generic;
using Cysharp.Text;
using Debugging.Enum;

namespace Debugging
{
    public static class GameLogger
    {
        private static readonly Queue<Utf16ValueStringBuilder> StringBuilderPool = new();
        private static string _logFilePath;

        // Flags 기반 카테고리 필터
        private static LogCategory _enabledCategories = LogCategory.Default;
        private static LogLevel _enabledLogLevels = LogLevel.Debug | LogLevel.Info | LogLevel.Warning | LogLevel.Error | LogLevel.Assert;

        // 전역 로깅 활성화 여부
        private static bool _isLoggingEnabled = true;

        static GameLogger()
        {
            // 기본적으로 모든 카테고리 활성화
            EnableAllCategories();
        }

        #region Configuration Methods

        /// <summary>
        /// 전체 로깅 시스템 활성화/비활성화
        /// </summary>
        public static void SetLoggingEnabled(bool enabled)
        {
            _isLoggingEnabled = enabled;
        }

        /// <summary>
        /// 카테고리 활성화 (기존 설정에 추가)
        /// </summary>
        public static void EnableCategory(LogCategory category)
        {
            _enabledCategories |= category;
        }

        /// <summary>
        /// 카테고리 비활성화 (기존 설정에서 제거)
        /// </summary>
        public static void DisableCategory(LogCategory category)
        {
            _enabledCategories &= ~category;
        }

        /// <summary>
        /// 여러 카테고리 일괄 활성화
        /// </summary>
        public static void EnableCategories(LogCategory categories)
        {
            _enabledCategories |= categories;
        }

        /// <summary>
        /// 여러 카테고리 일괄 비활성화
        /// </summary>
        public static void DisableCategories(LogCategory categories)
        {
            _enabledCategories &= ~categories;
        }

        /// <summary>
        /// 특정 카테고리들만 활성화 (다른 모든 카테고리 비활성화)
        /// </summary>
        public static void SetEnabledCategories(LogCategory categories)
        {
            _enabledCategories = categories;
        }

        /// <summary>
        /// 모든 카테고리 활성화
        /// </summary>
        public static void EnableAllCategories()
        {
            _enabledCategories = (LogCategory)~0; // 모든 비트 설정
        }

        /// <summary>
        /// 모든 카테고리 비활성화
        /// </summary>
        public static void DisableAllCategories()
        {
            _enabledCategories = 0;
        }

        /// <summary>
        /// 로그 레벨 활성화
        /// </summary>
        public static void EnableLogLevel(LogLevel level)
        {
            _enabledLogLevels |= level;
        }

        /// <summary>
        /// 로그 레벨 비활성화
        /// </summary>
        public static void DisableLogLevel(LogLevel level)
        {
            _enabledLogLevels &= ~level;
        }

        /// <summary>
        /// 특정 로그 레벨들만 활성화
        /// </summary>
        public static void SetEnabledLogLevels(LogLevel levels)
        {
            _enabledLogLevels = levels;
        }

        /// <summary>
        /// 모든 로그 레벨 활성화
        /// </summary>
        public static void EnableAllLogLevels()
        {
            _enabledLogLevels = LogLevel.Debug | LogLevel.Info | LogLevel.Warning | LogLevel.Error | LogLevel.Assert;
        }

        /// <summary>
        /// 프로덕션 모드 설정 (Warning, Error, Assert만)
        /// </summary>
        public static void SetProductionMode()
        {
            _enabledLogLevels = LogLevel.Warning | LogLevel.Error | LogLevel.Assert;
        }

        /// <summary>
        /// 개발 모드 설정 (모든 레벨 활성화)
        /// </summary>
        public static void SetDevelopmentMode()
        {
            EnableAllLogLevels();
        }

        #endregion

        #region Preset Configurations

        /// <summary>
        /// 플레이어 관련 로그만 활성화
        /// </summary>
        public static void EnablePlayerLogsOnly()
        {
            SetEnabledCategories(LogCategory.Player | LogCategory.Input);
        }

        /// <summary>
        /// UI 관련 로그만 활성화
        /// </summary>
        public static void EnableUILogsOnly()
        {
            SetEnabledCategories(LogCategory.UI | LogCategory.Audio);
        }

        /// <summary>
        /// 시스템 로그만 활성화
        /// </summary>
        public static void EnableSystemLogsOnly()
        {
            SetEnabledCategories(LogCategory.System | LogCategory.Default);
        }

        /// <summary>
        /// 게임플레이 관련 로그만 활성화
        /// </summary>
        public static void EnableGameplayLogsOnly()
        {
            SetEnabledCategories(LogCategory.Player | LogCategory.Enemy | LogCategory.Input);
        }

        /// <summary>
        /// 디버깅용 프리셋
        /// </summary>
        public static void EnableDebuggingPreset()
        {
            SetEnabledCategories(LogCategory.Player | LogCategory.Enemy | LogCategory.System);
        }

        #endregion

        #region Status Methods

        /// <summary>
        /// 특정 카테고리가 활성화되어 있는지 확인
        /// </summary>
        public static bool IsCategoryEnabled(LogCategory category)
        {
            return (_enabledCategories & category) == category;
        }

        /// <summary>
        /// 특정 로그 레벨이 활성화되어 있는지 확인
        /// </summary>
        public static bool IsLogLevelEnabled(LogLevel level)
        {
            return (_enabledLogLevels & level) == level;
        }

        /// <summary>
        /// 현재 활성화된 카테고리와 레벨 정보 반환
        /// </summary>
        public static string GetLoggerStatus()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== GameLogger Status ===");
            sb.AppendFormat("Logging Enabled: {0}\n", _isLoggingEnabled);
            sb.AppendFormat("Enabled Categories: {0}\n", _enabledCategories);
            sb.AppendFormat("Enabled Log Levels: {0}\n", _enabledLogLevels);

            return sb.ToString();
        }

        /// <summary>
        /// 현재 활성화된 카테고리 목록을 문자열로 반환
        /// </summary>
        public static string GetEnabledCategoriesString()
        {
            if (_enabledCategories == 0)
                return "None";

            using var sb = ZString.CreateStringBuilder();
            bool first = true;

            foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
            {
                if (IsCategoryEnabled(category))
                {
                    if (!first) sb.Append(", ");
                    sb.Append(category);
                    first = false;
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Logging Methods

        public static void Debug(string message, LogCategory category = LogCategory.Default)
        {
            Log(message, LogLevel.Debug, category);
        }

        public static void Info(string message, LogCategory category = LogCategory.Default)
        {
            Log(message, LogLevel.Info, category);
        }

        public static void Warning(string message, LogCategory category = LogCategory.Default)
        {
            Log(message, LogLevel.Warning, category);
        }

        public static void Error(string message, LogCategory category = LogCategory.Default)
        {
            Log(message, LogLevel.Error, category);
        }

        public static void Assert(bool condition, string message, LogCategory category = LogCategory.Default)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (condition)
            {
                return;
            }

            Log(ZString.Concat("ASSERTION FAILED: ", message), LogLevel.Assert, category);
            UnityEngine.Debug.Break();
#endif
        }

        /// <summary>
        /// 조건부 로깅 메서드들
        /// </summary>
        public static void DebugIf(bool condition, string message, LogCategory category = LogCategory.Default)
        {
            if (condition) Debug(message, category);
        }

        public static void InfoIf(bool condition, string message, LogCategory category = LogCategory.Default)
        {
            if (condition) Info(message, category);
        }

        public static void WarningIf(bool condition, string message, LogCategory category = LogCategory.Default)
        {
            if (condition) Warning(message, category);
        }

        public static void ErrorIf(bool condition, string message, LogCategory category = LogCategory.Default)
        {
            if (condition) Error(message, category);
        }

        #endregion

        private static void Log(string message, LogLevel level, LogCategory category)
        {
            // 전역 로깅 비활성화 체크
            if (!_isLoggingEnabled)
                return;

            // 로그 레벨 필터링
            if (!IsLogLevelEnabled(level))
                return;

            // 카테고리 필터링
            if (!IsCategoryEnabled(category))
                return;

            // 메시지 포맷팅
            using var sb = GetStringBuilder();
            sb.AppendFormat("[{0:HH:mm:ss.fff}]", DateTime.Now);
            sb.AppendFormat("[{0}]", level);
            sb.AppendFormat("[{0}] ", category);
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

        private static Utf16ValueStringBuilder GetStringBuilder()
        {
            lock (StringBuilderPool)
            {
                if (StringBuilderPool.Count <= 0)
                {
                    return ZString.CreateStringBuilder();
                }

                var sb = StringBuilderPool.Dequeue();
                sb.Clear();
                return sb;
            }
        }

        private static void ReturnStringBuilder(Utf16ValueStringBuilder sb)
        {
            lock (StringBuilderPool)
            {
                StringBuilderPool.Enqueue(sb);
            }
        }
    }
}
