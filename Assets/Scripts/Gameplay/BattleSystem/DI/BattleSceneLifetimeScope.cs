using System;
using System.Linq;
using Data.WeaponSystem;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.Units;
using NaughtyAttributes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.BattleSystem.DI
{
    public class BattleSceneLifetimeScope : LifetimeScope
    {
        [Header("Weapon System")]
        [SerializeField, Required]
        private WeaponDatabase _weaponDatabase;

        [Header("Battle Units")]
        [SerializeField, Required]
        private PlayerUnit _playerUnit;

        [Header("Enemy Units")]
        [SerializeField]
        private EnemyUnit[] _enemyUnits = Array.Empty<EnemyUnit>(); // 여러 적 배열

        [Header("UI")]
        [SerializeField, Required]
        private BattleUI _battleUI;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterData(builder);
            RegisterUnits(builder);
            RegisterBattleUI(builder);
            RegisterServices(builder);
            RegisterBattleManager(builder);
            RegisterInitializer(builder);
        }

        private void RegisterData(IContainerBuilder builder)
        {
            if (_weaponDatabase != null)
            {
                builder.RegisterInstance(_weaponDatabase);
            }
        }

        private void RegisterUnits(IContainerBuilder builder)
        {
            // 플레이어 등록
            if (_playerUnit != null)
            {
                builder.RegisterInstance(_playerUnit);
            }

            // 적들을 리스트로 등록
            var enemyList = _enemyUnits.Where(enemy => enemy != null).ToList();
            builder.RegisterInstance(enemyList);

            Debug.Log($"Registered {enemyList.Count} enemies in DI container");
        }

        private void RegisterBattleUI(IContainerBuilder builder)
        {
            if (_battleUI != null)
            {
                builder.RegisterInstance(_battleUI);
            }
        }

        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<BattleEventService>(Lifetime.Scoped);
            builder.Register<AttackService>(Lifetime.Scoped);
            builder.Register<TurnService>(Lifetime.Scoped);
            builder.Register<BattleConditionService>(Lifetime.Scoped);
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
