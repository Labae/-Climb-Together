using Data.Player.Abilities.Data.Player;
using Debugging;
using Debugging.Enum;
using Gameplay.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.DI
{
    public class GameInitializer : IStartable
    {
        [Inject] private Camera _mainCamera;
        [Inject] private PlayerController _playerController;
        [Inject] private PlayerAbilities _playerAbilities;

        public void Start()
        {
            GameLogger.Info("=== Game Scene Initialization Started ===");

            // 1. 씬 설정
            SetupScene();

            // 2. 플레이어 설정
            SetupPlayer();

            // 3. 카메라 설정
            SetupCamera();

            // 4. 게임 시작
            StartGame();

            GameLogger.Info("=== Game Scene Initialization Completed ===");
        }

        private void SetupScene()
        {
            GameLogger.Info("게임 씬 설정 중...");

            // 게임 타임 스케일 설정 (일시정지 등에서 사용)
            Time.timeScale = 1f;
            GameLogger.Info("Time Scale: 1.0");
        }

        private void SetupPlayer()
        {
            GameLogger.Info("플레이어 설정 중...", LogCategory.Player);

            if (_playerController != null)
            {
                GameLogger.Info($"✅ PlayerController: {_playerController.name}", LogCategory.Player);
                GameLogger.Info($"Player Position: {_playerController.transform.position}", LogCategory.Player);

                // 플레이어 능력치 로그
                var movement = _playerAbilities.Movement;
                GameLogger.Info($"Move Speed: {movement.RunSpeed}", LogCategory.Player);
                GameLogger.Info($"Jump Power: {movement.JumpPower}", LogCategory.Player);
                GameLogger.Info($"Has Double Jump: {movement.HasDoubleJump}", LogCategory.Player);
                GameLogger.Info($"Has Wall Jump: {movement.HasWallJump}", LogCategory.Player);
            }
            else
            {
                GameLogger.Error("❌ PlayerController가 null입니다!", LogCategory.Player);
            }
        }

        private void SetupCamera()
        {
            GameLogger.Info("카메라 설정 중...");

            if (_mainCamera != null)
            {
                GameLogger.Info($"✅ Main Camera: {_mainCamera.name}");
                GameLogger.Info($"Camera Position: {_mainCamera.transform.position}");

                // TODO: 카메라가 플레이어를 따라가도록 설정
                // var cameraFollow = _mainCamera.GetComponent<CameraFollow>();
                // cameraFollow.SetTarget(_playerController.transform);
            }
            else
            {
                GameLogger.Error("❌ Main Camera가 null입니다!");
            }
        }

        private void StartGame()
        {
            GameLogger.Info("게임 시작!");

            // 게임 상태를 Playing으로 변경
            // TODO: GameStateManager 구현 후 사용
            // _gameStateManager.ChangeState(GameState.Playing);

            GameLogger.Info("🎮 게임 플레이 시작!");
        }
    }
}
