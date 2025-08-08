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
        [Inject] private readonly EnemyUnit _enemyUnit;

        public void Start()
        {
            GameLogger.Info("=== Battle System Initialization Started ===");

            InitializeBattleUI();
            SetupBattleUnits();
            InitializeBattleManager();
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

            if (_playerUnit != null)
            {
                _container.InjectGameObject(_playerUnit.gameObject);
                GameLogger.Info(ZString.Concat("Player Unit: ", _playerUnit.UnitName), LogCategory.Battle);
                GameLogger.Info(ZString.Format("Player HP: {0}", _playerUnit.Stats.MaxHealth), LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("Player Unit is null!", LogCategory.Battle);
            }

            if (_enemyUnit != null)
            {
                _container.InjectGameObject(_enemyUnit.gameObject);
                GameLogger.Info(ZString.Concat("Enemy Unit: ", _enemyUnit.UnitName), LogCategory.Battle);
                GameLogger.Info(ZString.Format("Enemy HP: {0}", _enemyUnit.Stats.MaxHealth), LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("Enemy Unit is null!", LogCategory.Battle);
            }
        }

        private void InitializeBattleManager()
        {
            GameLogger.Info("Initializing Battle Manager...", LogCategory.Battle);

            if (_battleManager != null)
            {
                _battleManager.Initialize();
                GameLogger.Info("Battle Manager Initialized.", LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("BattleManager is Null.", LogCategory.Battle);
            }
        }
    }
}
