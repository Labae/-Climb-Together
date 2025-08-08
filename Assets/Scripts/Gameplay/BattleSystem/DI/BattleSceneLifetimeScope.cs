using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.Units;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.BattleSystem.DI
{
    public class BattleSceneLifetimeScope : LifetimeScope
    {
        [Header("Battle Units")] [SerializeField]
        private PlayerUnit _playerUnit;
        [SerializeField] private EnemyUnit _enemyUnit;
        [SerializeField] private BattleUI _battleUI;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterUnits(builder);
            RegisterBattleUI(builder);
            RegisterBattleManager(builder);
            RegisterInitializer(builder);
        }

        private void RegisterUnits(IContainerBuilder builder)
        {
            if (_playerUnit != null)
            {
                builder.RegisterInstance(_playerUnit);
            }

            if (_enemyUnit != null)
            {
                builder.RegisterInstance(_enemyUnit);
            }
        }

        private void RegisterBattleUI(IContainerBuilder builder)
        {
            if (_battleUI != null)
            {
                builder.RegisterInstance(_battleUI);
            }
        }

        private void RegisterBattleManager(IContainerBuilder builder)
        {
            builder.Register<BattleManager>(Lifetime.Scoped).AsImplementedInterfaces();
        }

        private void RegisterInitializer(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BattleInitializer>(Lifetime.Scoped);
        }
    }
}
