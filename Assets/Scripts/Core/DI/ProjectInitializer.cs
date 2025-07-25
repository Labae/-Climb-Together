using Cysharp.Text;
using Data.Configs;
using Debugging;
using Debugging.Enum;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.DI
{
    public class ProjectInitializer : IStartable
    {
        [Inject] private ProjectConfig gameConfig;

        public void Start()
        {
            // 먼저 로깅 설정을 적용
            ApplyLoggingSettings();

            GameLogger.Info("=== Game Initialization Started ===", LogCategory.System);

            ApplyGameSettings();

            GameLogger.Info("=== Game Initialization Completed ===", LogCategory.System);
        }

        private void ApplyLoggingSettings()
        {
            // 로깅 설정을 가장 먼저 적용
            gameConfig.ApplyLoggingSettings();

            // 초기 로그 (로깅 설정 적용 후)
            GameLogger.Info("Logging system initialized", LogCategory.System);
            GameLogger.Debug($"Enabled Categories: {gameConfig.GetEnabledLogCategories()}", LogCategory.System);
            GameLogger.Debug($"Enabled Log Levels: {gameConfig.GetEnabledLogLevels()}", LogCategory.System);
        }

        private void ApplyGameSettings()
        {
            GameLogger.Info("Applying Project Settings..", LogCategory.System);

            // 성능 설정 적용
            ApplyPerformanceSettings();

            // 물리 설정 적용
            ApplyPhysicsSettings();

            // 오디오 설정 적용
            ApplyAudioSettings();

            GameLogger.Info("All Project Settings Applied Successfully", LogCategory.System);
        }

        private void ApplyPerformanceSettings()
        {
            GameLogger.Debug("Applying Performance Settings..", LogCategory.System);

            // 프레임레이트 설정
            Application.targetFrameRate = gameConfig.TargetFrameRate;
            GameLogger.Debug(ZString.Format("Target Frame Rate: {0}", gameConfig.TargetFrameRate), LogCategory.System);

            // VSync 설정
            QualitySettings.vSyncCount = gameConfig.VSyncCount;
            GameLogger.Debug(ZString.Format("VSync Count: {0}", gameConfig.VSyncCount), LogCategory.System);

            // 백그라운드 실행 설정
            Application.runInBackground = gameConfig.RunInBackground;
            GameLogger.Debug(ZString.Format("Run In Background: {0}", gameConfig.RunInBackground), LogCategory.System);

            GameLogger.Info("Performance Settings Applied", LogCategory.System);
        }

        private void ApplyPhysicsSettings()
        {
            GameLogger.Debug("Applying Physics Settings..", LogCategory.System);

            // 물리 설정 적용
            gameConfig.ApplyPhysicsSettings();

            GameLogger.Debug(ZString.Format("Gravity 2D: {0}", gameConfig.Gravity2D), LogCategory.System);
            GameLogger.Debug(ZString.Format("Fixed Time Step: {0}", gameConfig.FixedTimeStep), LogCategory.System);
            GameLogger.Debug(ZString.Format("Maximum Allowed Timestep: {0}", gameConfig.MaximumAllowedTimestep), LogCategory.System);

            GameLogger.Info("Physics Settings Applied", LogCategory.System);
        }

        private void ApplyAudioSettings()
        {
            GameLogger.Debug("Applying Audio Settings..", LogCategory.Audio);

            // 오디오 설정 적용
            gameConfig.ApplyAudioSettings();

            GameLogger.Debug(ZString.Format("Master Volume: {0}", gameConfig.MasterVolume), LogCategory.Audio);
            GameLogger.Debug(ZString.Format("Music Volume: {0}", gameConfig.MusicVolume), LogCategory.Audio);
            GameLogger.Debug(ZString.Format("SFX Volume: {0}", gameConfig.SfxVolume), LogCategory.Audio);

            GameLogger.Info("Audio Settings Applied", LogCategory.Audio);
        }
    }
}
