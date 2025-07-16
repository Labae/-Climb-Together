using System.ComponentModel.DataAnnotations;
using Data.Player.Abilities.Data.Player;
using Gameplay.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField, Required]
        private Camera _mainCamera;

        [SerializeField, Required]
        private PlayerController _playerPrefab;

        [SerializeField, Required]
        private PlayerAbilities _playerAbilities;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(_mainCamera);
            builder.RegisterInstance(_playerAbilities);

            builder.RegisterComponentInNewPrefab(_playerPrefab, Lifetime.Scoped);
            builder.RegisterEntryPoint<GameInitializer>(Lifetime.Scoped);
        }
    }
}
