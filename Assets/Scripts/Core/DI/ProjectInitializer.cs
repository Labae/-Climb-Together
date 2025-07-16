using Cysharp.Text;
using Data.Configs;
using Debugging;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.DI
{
    public class ProjectInitializer : IStartable
    {
        [Inject] private ProjectConfig _gameConfig;

        public void Start()
        {
            GameLogger.Info("=== Game Initialization Started ===");

            ApplyGameSettings();

            GameLogger.Info("=== Game Initialization Completed ===");
        }

        private void ApplyGameSettings()
        {
            GameLogger.Info("Applying Project Settings..");

            // 프레임레이트 설정
            Application.targetFrameRate = _gameConfig.TargetFrameRate;
            GameLogger.Info(ZString.Format("Target Frame Rate: {0}", _gameConfig.TargetFrameRate));

            // VSync 설정
            QualitySettings.vSyncCount = _gameConfig.VSyncCount;
            GameLogger.Info(ZString.Format("VSync Count: {0}", _gameConfig.VSyncCount));

            // 백그라운드 실행 설정
            Application.runInBackground = _gameConfig.RunInBackground;
            GameLogger.Info(ZString.Format("Run In Background: {0}", _gameConfig.RunInBackground));

            // 물리 설정 적용
            _gameConfig.ApplyPhysicsSettings();
            GameLogger.Info("Physics Settings Application Completed");
        }
    }
}
