using System;
using JetBrains.Annotations;
using R3;

namespace Systems.StateMachine.Interfaces
{
    public interface ITransitionHandler<T> where T : Enum
    {
        void Setup(ITransitionContext<T> context, CompositeDisposable disposables);
        bool TryGetTransition(ITransitionContext<T> context, out T targetState);
        int Priority { get; }
        string Name { get; }
    }
}
