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

        private void OnValidate()
        {
            // 물리 설정 적용
            if (!Application.isPlaying)
            {
                return;
            }

            ApplyPhysicsSettings();
            ApplyPerformanceSettings();
        }

        [Button("Apply All Settings")]
        private void ApplyAllSettings()
        {
            ApplyPerformanceSettings();
            ApplyPhysicsSettings();
            ApplyAudioSettings();
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
        }

        public bool IsDebugMode()
        {
            return ShowDebugInfo;
        }
    }
}
