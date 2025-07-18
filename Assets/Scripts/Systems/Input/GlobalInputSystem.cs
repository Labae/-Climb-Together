using System;
using Data.Configs;
using Systems.Input.Interfaces;
using VContainer;

namespace Systems.Input
{
    public class GlobalInputSystem : IGlobalInputSystem, IDisposable
    {
        [Inject]
        private ProjectConfig _projectConfig;

        private readonly InputSystemActions _inputSystemActions = new();

        public InputSystemActions Actions => _inputSystemActions;

        public void Dispose()
        {
            _inputSystemActions?.Dispose();
        }
    }
}
