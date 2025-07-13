using System;
using Debugging;
using Systems.Input;
using UnityEngine;

namespace Systems
{
    public static class GameSystemInitializer
    {
        private static bool _isInitialized = false;

        // Systems
        private static GlobalInputSystem _globalInputSystem;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_isInitialized)
            {
                GameLogger.Info("GameSystemInitializer already initialized");
                return;
            }

            GameLogger.Info("=== Game Systems Initialization Started ===");

            try
            {
                InitializeSystems();
                RegisterCleanupCallbacks();

                _isInitialized = true;
                GameLogger.Info("=== Game Systems Initialization Complete ===");
            }
            catch (Exception e)
            {
                GameLogger.Error($"Failed to initialize game systems: {e.Message}");
            }
        }

        private static void InitializeSystems()
        {
            InitializeInputSystem();
        }

        #region System Initialization

        private static void InitializeInputSystem()
        {
            GameLogger.Info("Initializing Global Input System...");
            _globalInputSystem = new GlobalInputSystem();
            GameLogger.Info("Global Input System initialized successfully");
        }

        #endregion

        #region Lifecycle Management

        private static void RegisterCleanupCallbacks()
        {
            // Application 이벤트 등록
            Application.quitting += OnApplicationQuit;

            // 도메인 리로드 시 정리 (에디터에서)
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif
        }

        private static void OnApplicationQuit()
        {
            CleanupSystems();
        }

#if UNITY_EDITOR
        private static void OnBeforeAssemblyReload()
        {
            CleanupSystems();
        }
#endif

        #endregion

        #region Cleanup

        private static void CleanupSystems()
        {
            if (!_isInitialized) return;

            GameLogger.Info("=== Cleaning up game systems ===");

            try
            {
                // 역순으로 정리
                _globalInputSystem?.Dispose();

                // 이벤트 해제
                Application.quitting -= OnApplicationQuit;

#if UNITY_EDITOR
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
#endif

                _isInitialized = false;
                GameLogger.Info("=== Game systems cleanup complete ===");
            }
            catch (Exception e)
            {
                GameLogger.Error($"Error during cleanup: {e.Message}");
            }
        }

        #endregion
    }
}