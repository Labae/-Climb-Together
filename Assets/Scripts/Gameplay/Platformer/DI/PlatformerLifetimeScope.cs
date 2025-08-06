using System.ComponentModel.DataAnnotations;
using Data.Platformer.Abilities.Data.Player;
using Gameplay.Platformer.Player.Core;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Platformer.DI
{
    public class PlatformerLifetimeScope : LifetimeScope
    {
        [SerializeField, Required]
        private CinemachineCamera _mainCamera;

        [SerializeField, Required]
        private PlayerController _playerPrefab;

        [SerializeField, Required]
        private PlatformerPlayerSettings platformerPlayerSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(_mainCamera);
            builder.RegisterInstance(platformerPlayerSettings);

            builder.RegisterComponentInNewPrefab(_playerPrefab, Lifetime.Scoped);
            builder.RegisterEntryPoint<PlatformerInitializer>(Lifetime.Scoped);
        }
    }
}
