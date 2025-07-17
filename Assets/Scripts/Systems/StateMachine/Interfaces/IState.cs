using System;

namespace Systems.StateMachine.Interfaces
{
    public interface IState : IDisposable
    {
        void OnEnter();

        void OnExit();
    }

    public interface IState<T> : IState where T : Enum
    {
        T StateType { get; }
    }
}
