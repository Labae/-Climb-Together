using System;

namespace Systems.Input
{
    public class GlobalInputSystem : IDisposable
    {
        private static GlobalInputSystem _instance;
        private readonly InputSystemActions _inputSystemActions;

        public static InputSystemActions Actions => _instance?._inputSystemActions;

        public GlobalInputSystem()
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("GlobalInputSystem already exists");
            }
        
            _instance = this;
            _inputSystemActions = new InputSystemActions();
        }

        public void Dispose()
        {
            _inputSystemActions?.Dispose();
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}