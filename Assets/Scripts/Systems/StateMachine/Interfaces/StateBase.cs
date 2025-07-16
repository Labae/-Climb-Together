using System;

namespace Systems.StateMachine.Interfaces
{
    public abstract class StateBase<T> : IState<T> where T : Enum
    {
        private Action<T> _changeStateAction;

        public abstract T StateType { get; }

        public void Initialize(Action<T> changeAction)
        {
            _changeStateAction = changeAction;
        }

        protected void ChangeState(T state)
        {
            _changeStateAction?.Invoke(state);
        }

        public abstract void OnEnter();
        public abstract void OnUpdate();
        public abstract void OnFixedUpdate();
        public abstract void OnExit();

        public abstract void Dispose();
    }
}
