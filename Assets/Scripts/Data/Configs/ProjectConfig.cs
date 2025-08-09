using Debugging;
using Debugging.Enum;
using NaughtyAttributes;
using UnityEngine;

namespace Data.Configs
{
    [CreateAssetMenu(fileName = nameof(ProjectConfig), menuName = "Project/Configs/" + nameof(ProjectConfig))]
    public class ProjectConfig : ScriptableObject
    {
        [Header("Performance Settings")]
        [Range(30, 300)]
        public int TargetFrameRate = 60;

        [Range(1, 4)]
        public int VSyncCount = 1;

        public bool RunInBackground = true;

        [Header("Physics Settings")]
        [Range(-50f, -5f)]
        public float Gravity2D = -9.81f;

        [Range(0.01f, 1f)]
        public float FixedTimeStep = 0.02f;

        [Range(1, 10)]
        public int MaximumAllowedTimestep = 3;

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float MasterVolume = 1f;

        [Range(0f, 1f)]
        public float MusicVolume = 0.8f;

        [Range(0f, 1f)]
        public float SfxVolume = 1f;

        [Header("Debug Settings")]
        public bool ShowDebugInfo = false;

        [ShowIf("ShowDebugInfo")]
        public bool ShowFPS = true;

        [Header("Logging Settings")]
        [Tooltip("Enable/Disable all logging")]
        public bool EnableLogging = true;

        [ShowIf("EnableLogging")]
        [Tooltip("Select which log categories to enable")]
        public LogCategory EnabledLogCategories = (LogCategory)~0; // 모든 카테고리 기본 활성화

        [ShowIf("EnableLogging")]
        [Tooltip("Select which log levels to enable")]
        public LogLevel EnabledLogLevels = LogLevel.Debug | LogLevel.Info | LogLevel.Warning | LogLevel.Error | LogLevel.Assert;

        [ShowIf("EnableLogging")]
        [Tooltip("Use production logging (Warning, Error, Assert only)")]
        public bool UseProductionLogging = false;

        [ShowIf("EnableLogging")]
        [Button("Enable All Logs")]
        private void EnableAllLogs()
        {
            EnabledLogCategories = (LogCategory)~0;
            EnabledLogLevels = LogLevel.Debug | LogLevel.Info | LogLevel.Warning | LogLevel.Error | LogLevel.Assert;
            ApplyLoggingSettings();
        }

        [ShowIf("EnableLogging")]
        [Button("Player Logs Only")]
        private void EnablePlayerLogsOnly()
        {
            EnabledLogCategories = LogCategory.Player | LogCategory.Input;
            ApplyLoggingSettings();
        }

        [ShowIf("EnableLogging")]
        [Button("System Logs Only")]
        private void EnableSystemLogsOnly()
        {
            EnabledLogCategories = LogCategory.System | LogCategory.Default;
            ApplyLoggingSettings();
        }

        [ShowIf("EnableLogging")]
        [Button("UI Logs Only")]
        private void EnableUILogsOnly()
        {
            EnabledLogCategories = LogCategory.UI | LogCategory.Audio;
            ApplyLoggingSettings();
        }

        [ShowIf("EnableLogging")]
        [Button("Gameplay Logs Only")]
        private void EnableGameplayLogsOnly()
        {
            EnabledLogCategories = LogCategory.Player | LogCategory.Enemy | LogCategory.Input;
            ApplyLoggingSettings();
        }

        private void OnValidate()
        {
            // 물리 설정 적용
            if (!Application.isPlaying)
            {
                return;
            }

            ApplyPhysicsSettings();
            ApplyPerformanceSettings();
            ApplyLoggingSettings();
        }

        [Button("Apply All Settings")]
        private void ApplyAllSettings()
        {
            ApplyPerformanceSettings();
            ApplyPhysicsSettings();
            ApplyAudioSettings();
            ApplyLoggingSettings();
            Debug.Log("All game settings applied!");
        }

        public void ApplyPerformanceSettings()
        {
            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.vSyncCount = VSyncCount;
            Application.runInBackground = RunInBackground;
        }

        public void ApplyPhysicsSettings()
        {
            Physics2D.gravity = new Vector2(0f, Gravity2D);
            Time.fixedDeltaTime = FixedTimeStep;
            Time.maximumDeltaTime = MaximumAllowedTimestep * FixedTimeStep;
        }

        public void ApplyAudioSettings()
        {
            AudioListener.volume = MasterVolume;
            // 실제 AudioManager에서 개별 볼륨 설정
        }

        public void ApplyLoggingSettings()
        {
            GameLogger.SetLoggingEnabled(EnableLogging);

            if (EnableLogging)
            {
                if (UseProductionLogging)
                {
                    GameLogger.SetProductionMode();
                }
                else
                {
                    GameLogger.SetEnabledLogLevels(EnabledLogLevels);
                }

                GameLogger.SetEnabledCategories(EnabledLogCategories);

                // 로깅 설정 정보 출력
                GameLogger.Info($"Logging settings applied. Categories: {EnabledLogCategories}, Levels: {EnabledLogLevels}", LogCategory.System);
            }
        }

        [Button("Reset to Default")]
        private void ResetToDefault()
        {
            TargetFrameRate = 60;
            VSyncCount = 1;
            RunInBackground = true;

            Gravity2D = -9.81f;
            FixedTimeStep = 0.02f;
            MaximumAllowedTimestep = 3;

            MasterVolume = 1f;
            MusicVolume = 0.8f;
            SfxVolume = 1f;

            ShowDebugInfo = false;

            // 로깅 설정 기본값
            EnableLogging = true;
            EnabledLogCategories = (LogCategory)~0;
            EnabledLogLevels = LogLevel.Debug | LogLevel.Info | LogLevel.Warning | LogLevel.Error | LogLevel.Assert;
            UseProductionLogging = false;
        }

        [ShowIf("EnableLogging")]
        [Button("Show Current Logging Status")]
        private void ShowLoggingStatus()
        {
            Debug.Log(GameLogger.GetLoggerStatus());
        }

        public bool IsDebugMode()
        {
            return ShowDebugInfo;
        }

        public bool IsLoggingEnabled()
        {
            return EnableLogging;
        }

        public LogCategory GetEnabledLogCategories()
        {
            return EnabledLogCategories;
        }

        public LogLevel GetEnabledLogLevels()
        {
            return EnabledLogLevels;
        }
    }
}
