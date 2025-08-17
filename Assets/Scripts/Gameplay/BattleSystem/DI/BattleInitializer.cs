using System.Collections.Generic;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.Interfaces;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.Units;
using VContainer;
using VContainer.Unity;

namespace Gameplay.BattleSystem.DI
{
    public class BattleInitializer : IInitializable
    {
        [Inject] private readonly IObjectResolver _container;
        [Inject] private readonly IBattleManager _battleManager;
        [Inject] private readonly BattleUI _battleUI;
        [Inject] private readonly PlayerUnit _playerUnit;
        [Inject] private readonly List<EnemyUnit> _enemyUnits;  // 여러 적

        public void Initialize()
        {
            GameLogger.Info("=== Battle System Initialization Started ===");

            SetupBattleUnits();
            InitializeBattleUI();
            InitializeBattleManager();

            GameLogger.Info("=== Battle System Initialization Completed ===");
        }

        private void InitializeBattleUI()
        {
            GameLogger.Info("Initializing Battle UI...", LogCategory.Battle);

            if (_battleUI != null)
            {
                _battleUI.Initialize(_playerUnit, _container.Resolve<TurnOrderService>());
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
                _playerUnit.Initialize();
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
                        enemy.Initialize();
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
