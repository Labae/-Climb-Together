using Data.Configs;
using NaughtyAttributes;
using Systems.EventBus;
using Systems.Input;
using Systems.UI.Core;
using Systems.UI.Interfaces;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.DI
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField, Required]
        private ProjectConfig _projectConfig;

        [SerializeField, Required]
        private UIManager _uiManager;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(_projectConfig);
            builder.Register<GlobalInputSystem>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ProjectInitializer>();

            RegisterUISystem(builder);
        }

        private void RegisterUISystem(IContainerBuilder builder)
        {
            builder.RegisterInstance<IUIManager>(_uiManager);
        }
    }
}
