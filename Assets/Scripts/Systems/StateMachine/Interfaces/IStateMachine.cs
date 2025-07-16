using System;
using Cysharp.Threading.Tasks;
using R3;

namespace Systems.StateMachine.Interfaces
{
    public interface IStateMachine<T> : IDisposable where T : Enum
    {
        // Current State
        ReadOnlyReactiveProperty<T> CurrentStateType { get; }
        ReadOnlyReactiveProperty<IState<T>> CurrentState { get; }

        // State Management
        void AddState(IState<T> state);
        void RemoveState(T stateType);
        void ChangeState(T stateType);
        void ForceChangeState(T stateType);

        // Observables
        Observable<T> OnStateChanged { get; }
        Observable<(T from, T to)> OnStateTransition { get; }
        Observable<T> OnStateEnter { get; }
        Observable<T> OnStateExit { get; }

        // Update
        void Update();
        void FixedUpdate();

        // Validation
        bool CanChangeState(T stateType);
        bool HasState(T stateType);

        // State Info
        IState<T> GetState(T stateType);
    }
}
