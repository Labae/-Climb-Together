using System.Collections.Generic;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Interfaces;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.Units;
using VContainer;
using VContainer.Unity;

namespace Gameplay.BattleSystem.DI
{
    public class BattleInitializer : IStartable
    {
        [Inject] private readonly IObjectResolver _container;
        [Inject] private readonly IBattleManager _battleManager;
        [Inject] private readonly BattleUI _battleUI;
        [Inject] private readonly PlayerUnit _playerUnit;
        [Inject] private readonly List<EnemyUnit> _enemyUnits;  // 여러 적

        public void Start()
        {
            GameLogger.Info("=== Battle System Initialization Started ===");

            InitializeBattleUI();
            SetupBattleUnits();
            InitializeBattleManager();

            GameLogger.Info("=== Battle System Initialization Completed ===");
        }

        private void InitializeBattleUI()
        {
            GameLogger.Info("Initializing Battle UI...", LogCategory.Battle);

            if (_battleUI != null)
            {
                _battleUI.Initialize();
                GameLogger.Info("Battle UI Initialized.", LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("BattleUI is Null.", LogCategory.Battle);
            }
        }

        private void SetupBattleUnits()
        {
            GameLogger.Info("Setting up Battle Units...", LogCategory.Battle);

            // 플레이어 설정
            if (_playerUnit != null)
            {
                _container.InjectGameObject(_playerUnit.gameObject);
                GameLogger.Info(ZString.Format("✅ Player Unit: {0} (HP: {1})",
                    _playerUnit.UnitName, _playerUnit.Stats.MaxHealth), LogCategory.Battle);

                // 플레이어 약점 정보
                if (_playerUnit.Weaknesses != null && _playerUnit.Weaknesses.Length > 0)
                {
                    GameLogger.Info(ZString.Format("Player weaknesses: {0}", string.Join(", ", _playerUnit.Weaknesses)), LogCategory.Battle);
                }
            }
            else
            {
                GameLogger.Error("❌ Player Unit is null!", LogCategory.Battle);
            }

            // 적들 설정
            if (_enemyUnits != null && _enemyUnits.Count > 0)
            {
                for (int i = 0; i < _enemyUnits.Count; i++)
                {
                    var enemy = _enemyUnits[i];
                    if (enemy != null)
                    {
                        _container.InjectGameObject(enemy.gameObject);
                        GameLogger.Info(ZString.Format("✅ Enemy {0}: {1} (HP: {2})",
                            i + 1, enemy.UnitName, enemy.Stats.MaxHealth), LogCategory.Battle);

                        // 적 약점 정보
                        if (enemy.Weaknesses != null && enemy.Weaknesses.Length > 0)
                        {
                            GameLogger.Info(ZString.Format("{0} weaknesses: {1}",
                                enemy.UnitName, string.Join(", ", enemy.Weaknesses)), LogCategory.Battle);
                        }
                    }
                    else
                    {
                        GameLogger.Warning(ZString.Format("❌ Enemy {0} is null!", i + 1), LogCategory.Battle);
                    }
                }
                GameLogger.Info(ZString.Format("Total {0} enemies set up", _enemyUnits.Count), LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("❌ No enemy units found!", LogCategory.Battle);
            }
        }

        private void InitializeBattleManager()
        {
            GameLogger.Info("Initializing Battle Manager...", LogCategory.Battle);

            if (_battleManager != null)
            {
                _battleManager.Initialize();
                GameLogger.Info("✅ Battle Manager Initialized.", LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("❌ BattleManager is Null.", LogCategory.Battle);
            }
        }
    }
}
