using System;

namespace Systems.StateMachine.Interfaces
{
    public interface ITransitionContext<T> where T : Enum
    {
        IStateMachine<T> StateMachine { get; }
        T CurrentState { get; }
    }
}
