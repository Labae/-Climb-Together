using System;
using System.Collections.Generic;
using Cysharp.Text;
using Debugging;
using R3;
using Systems.StateMachine.Interfaces;

namespace Systems.StateMachine
{
    public class StateMachine<T> : IStateMachine<T> where T : Enum
    {
        // State Storage
        private readonly Dictionary<T, IState<T>> _states = new();

        // Reactive Properties
        private readonly ReactiveProperty<T> _currentStateType;
        private readonly ReactiveProperty<IState<T>> _currentState;
        private readonly Subject<(T from, T to)> _onStateTransition = new();
        private readonly CompositeDisposable _disposables = new();

        // Public Reactive Properties
        public ReadOnlyReactiveProperty<T> CurrentStateType { get; }
        public ReadOnlyReactiveProperty<IState<T>> CurrentState { get; }

        // Observables
        public Observable<T> OnStateChanged => _currentStateType.AsObservable();
        public Observable<(T from, T to)> OnStateTransition => _onStateTransition.AsObservable();
        public Observable<T> OnStateEnter => _onStateTransition.Select(t => t.to);
        public Observable<T> OnStateExit => _onStateTransition.Select(t => t.from);

        // Constructor
        public StateMachine(T initialState = default)
        {
            _currentStateType = new ReactiveProperty<T>(initialState);
            _currentState = new ReactiveProperty<IState<T>>(null);

            CurrentStateType = _currentStateType.ToReadOnlyReactiveProperty();
            CurrentState = _currentState.ToReadOnlyReactiveProperty();

            // Subscribe to state changes for logging
            _currentStateType.Subscribe(state => GameLogger.Debug(ZString.Format("State changed to: {0}", state)))
                .AddTo(_disposables);
        }

        // State Management
        public void AddState(IState<T> state)
        {
            if (state == null)
            {
                GameLogger.Error("Cannot add null state");
                return;
            }

            _states[state.StateType] = state;

            // If this is the first state or matches initial state, set as current
            if (_currentState.Value == null &&
                (EqualityComparer<T>.Default.Equals(_currentStateType.Value, state.StateType) ||
                 _states.Count == 1))
            {
                _currentState.Value = state;
                state.OnEnter();
                _onStateTransition.OnNext((default(T), state.StateType));
            }
        }

        public void RemoveState(T stateType)
        {
            if (_states.TryGetValue(stateType, out var state))
            {
                if (EqualityComparer<T>.Default.Equals(_currentStateType.Value, stateType))
                {
                    state.OnExit();
                    _currentState.Value = null;
                }

                _states.Remove(stateType);
            }
        }

        public void ChangeState(T stateType)
        {
            if (!CanChangeState(stateType))
            {
                GameLogger.Warning(ZString.Format("Cannot change from {0} to {1}", _currentStateType.Value, stateType));
                return;
            }

            ForceChangeState(stateType);
        }

        public void ForceChangeState(T stateType)
        {
            if (!_states.TryGetValue(stateType, out var newState))
            {
                GameLogger.Error(ZString.Format("State {0} not found", stateType));
                return;
            }

            var previousState = _currentStateType.Value;
            var previousStateObject = _currentState.Value;

            // Exit current state
            previousStateObject?.OnExit();

            // Change state
            _currentStateType.Value = stateType;
            _currentState.Value = newState;

            // Enter new state
            newState.OnEnter();

            // Notify transition
            _onStateTransition.OnNext((previousState, stateType));
        }

        // Update Methods
        public void Update()
        {
            _currentState.Value?.OnUpdate();
        }

        public void FixedUpdate()
        {
            _currentState.Value?.OnFixedUpdate();
        }

        // Validation
        public bool CanChangeState(T stateType)
        {
            if (!HasState(stateType))
                return false;

            if (EqualityComparer<T>.Default.Equals(_currentStateType.Value, stateType))
                return false;

            return true;
        }

        public bool HasState(T stateType)
        {
            return _states.ContainsKey(stateType);
        }

        public IState<T> GetState(T stateType)
        {
            _states.TryGetValue(stateType, out var state);
            return state;
        }

        // Disposal
        public void Dispose()
        {
            _currentState.Value?.OnExit();

            var exceptions = new List<Exception>();
            foreach (var state in _states.Values)
            {
                try
                {
                    state.Dispose();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            _states.Clear();
            _disposables.Dispose();
            _onStateTransition.Dispose();
            _currentStateType.Dispose();
            _currentState.Dispose();

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Error occurred during state disposal", exceptions);
            }
        }
    }
}
