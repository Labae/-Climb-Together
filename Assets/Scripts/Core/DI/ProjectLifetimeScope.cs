using Data.Configs;
using NaughtyAttributes;
using Systems.Input;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.DI
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField, Required]
        private ProjectConfig _projectConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInstance(_projectConfig);
            builder.Register<GlobalInputSystem>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterEntryPoint<ProjectInitializer>();
        }
    }
}
