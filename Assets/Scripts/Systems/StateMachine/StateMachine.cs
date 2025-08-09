using System;
using System.Collections.Generic;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using R3;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Systems.StateMachine
{
    public class StateMachine<T> : IStateMachine<T> where T : Enum
    {
        #region Fields

        // State Storage
        private readonly Dictionary<T, IState<T>> _states = new();

        // Reactive Properties
        private readonly ReactiveProperty<T> _currentStateType;
        private readonly ReactiveProperty<IState<T>> _currentState;
        private readonly Subject<(T from, T to)> _onStateTransition = new();
        private readonly CompositeDisposable _disposables = new();

        // Performance Tracking
        private float _lastStateChangeTime;
        private readonly Dictionary<T, float> _stateTimeSpent = new();
        private readonly Dictionary<T, int> _stateTransitionCount = new();

        // Configuration
        private readonly bool _enablePerformanceTracking;
        private readonly bool _enableDetailedLogging;
        private readonly LogCategory _logCategory;

        #endregion

        #region Properties

        // Public Reactive Properties
        public ReadOnlyReactiveProperty<T> CurrentStateType { get; }
        public ReadOnlyReactiveProperty<IState<T>> CurrentState { get; }

        // Observables
        public Observable<T> OnStateChanged => _currentStateType.AsObservable();
        public Observable<(T from, T to)> OnStateTransition => _onStateTransition.AsObservable();
        public Observable<T> OnStateEnter => _onStateTransition.Select(t => t.to);
        public Observable<T> OnStateExit => _onStateTransition.Select(t => t.from);

        // Additional Properties
        public int StateCount => _states.Count;
        public bool IsInitialized => _currentState.Value != null;
        public float TimeInCurrentState => Time.time - _lastStateChangeTime;

        #endregion

        #region Constructor

        public StateMachine(T initialState = default, bool enablePerformanceTracking = false,
            bool enableDetailedLogging = false, LogCategory logCategory = LogCategory.System)
        {
            _enablePerformanceTracking = enablePerformanceTracking;
            _enableDetailedLogging = enableDetailedLogging;
            _logCategory = logCategory;

            _currentStateType = new ReactiveProperty<T>(initialState);
            _currentState = new ReactiveProperty<IState<T>>(null);

            CurrentStateType = _currentStateType.ToReadOnlyReactiveProperty();
            CurrentState = _currentState.ToReadOnlyReactiveProperty();

            _lastStateChangeTime = Time.time;

            SetupUpdateSystem();
            SetupLogging();
            SetupPerformanceTracking();
        }

        #endregion

        #region Setup

        private void SetupUpdateSystem()
        {
            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Where(_ => _currentState.Value != null)
                .Subscribe(_ => _currentState.Value?.OnUpdate())
                .AddTo(_disposables);

            Observable.EveryUpdate(UnityFrameProvider.FixedUpdate)
                .Where(_ => _currentState.Value != null)
                .Subscribe(_ => _currentState.Value?.OnFixedUpdate())
                .AddTo(_disposables);
        }

        private void SetupLogging()
        {
            if (_enableDetailedLogging)
            {
                // 상태 변경 로깅
                _currentStateType
                    .Subscribe(state => LogStateChange(state))
                    .AddTo(_disposables);

                // 상태 전환 로깅
                _onStateTransition
                    .Subscribe(transition => LogStateTransition(transition.from, transition.to))
                    .AddTo(_disposables);
            }
            else
            {
                // 기본 로깅
                _currentStateType
                    .Subscribe(state => GameLogger.Debug(ZString.Concat("State changed to: ", state), _logCategory))
                    .AddTo(_disposables);
            }
        }

        private void SetupPerformanceTracking()
        {
            if (_enablePerformanceTracking)
            {
                _onStateTransition
                    .Subscribe(transition => TrackStateTransition(transition.from, transition.to))
                    .AddTo(_disposables);
            }
        }

        #endregion

        #region State Management

        public void AddState(IState<T> state)
        {
            if (state == null)
            {
                GameLogger.Error("Cannot add null state", _logCategory);
                return;
            }

            if (_states.ContainsKey(state.StateType))
            {
                GameLogger.Warning(ZString.Concat("State ", state.StateType, " already exists. Replacing..."), _logCategory);
            }

            _states[state.StateType] = state;

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Added state: ", state.StateType), _logCategory);
            }

            _states[state.StateType].SetChangeAction(ChangeState);
        }

        public void TrySetInitialState(T stateType)
        {
            bool shouldSetAsInitial = _currentStateType.Value != null;
            if (!shouldSetAsInitial)
            {
                return;
            }

            var state = _states[stateType];
            _currentState.Value = _states[stateType];

            try
            {
                state.OnEnter();
                _onStateTransition.OnNext((default, state.StateType));

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Initial state set to: ", state.StateType), _logCategory);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error entering initial state ", state.StateType, ": ", e.Message), _logCategory);
                throw;
            }
        }

        public void RemoveState(T stateType)
        {
            if (!_states.TryGetValue(stateType, out var state))
            {
                GameLogger.Warning(ZString.Concat("Cannot remove non-existent state: ", stateType), _logCategory);
                return;
            }

            // 현재 상태를 제거하려는 경우
            if (EqualityComparer<T>.Default.Equals(_currentStateType.Value, stateType))
            {
                try
                {
                    state.OnExit();
                }
                catch (Exception e)
                {
                    GameLogger.Error(ZString.Concat("Error exiting state ", stateType, " during removal: ", e.Message), _logCategory);
                }

                _currentState.Value = null;

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Current state ", stateType, " removed"), _logCategory);
                }
            }

            _states.Remove(stateType);

            // 성능 추적 데이터 정리
            if (_enablePerformanceTracking)
            {
                _stateTimeSpent.Remove(stateType);
                _stateTransitionCount.Remove(stateType);
            }

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Removed state: ", stateType), _logCategory);
            }
        }

        public void ChangeState(T stateType)
        {
            if (!CanChangeState(stateType, out string reason))
            {
                GameLogger.Warning(ZString.Concat("Cannot change from ", _currentStateType.Value, " to ", stateType, ": ", reason), _logCategory);
                return;
            }

            ForceChangeState(stateType);
        }

        public void ForceChangeState(T stateType)
        {
            if (!_states.TryGetValue(stateType, out var newState))
            {
                GameLogger.Error(ZString.Concat("State ", stateType, " not found"), _logCategory);
                return;
            }

            var previousState = _currentStateType.Value;
            var previousStateObject = _currentState.Value;

            try
            {
                // Exit current state
                if (previousStateObject != null)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug(ZString.Concat("Exiting state: ", previousState), _logCategory);
                    }

                    previousStateObject.OnExit();
                }

                // Change state
                _currentStateType.Value = stateType;
                _currentState.Value = newState;
                _lastStateChangeTime = Time.time;

                // Enter new state
                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Entering state: ", stateType), _logCategory);
                }

                newState.OnEnter();

                // Notify transition
                _onStateTransition.OnNext((previousState, stateType));
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error during state transition from ", previousState, " to ", stateType, ": ", e.Message), _logCategory);

                // 상태 복구 시도
                if (previousStateObject != null && _states.ContainsValue(previousStateObject))
                {
                    GameLogger.Warning("Attempting to restore previous state...", _logCategory);
                    _currentStateType.Value = previousState;
                    _currentState.Value = previousStateObject;
                }

                throw;
            }
        }

        #endregion

        #region Validation

        public bool CanChangeState(T stateType)
        {
            return CanChangeState(stateType, out _);
        }

        public bool CanChangeState(T stateType, out string reason)
        {
            reason = string.Empty;

            if (!HasState(stateType))
            {
                reason = "State not found";
                return false;
            }

            if (EqualityComparer<T>.Default.Equals(_currentStateType.Value, stateType))
            {
                reason = "Already in target state";
                return false;
            }

            // 현재 상태가 null인 경우 (초기 상태 설정)
            if (_currentState.Value == null)
            {
                return true;
            }

            // 추가적인 검증 로직 (필요시 확장 가능)
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

        #endregion

        #region Logging

        private void LogStateChange(T state)
        {
            var timeInPrevious = TimeInCurrentState;

            using var sb = ZString.CreateStringBuilder();
            sb.Append("State changed to: ");
            sb.Append(state);

            if (_enablePerformanceTracking && timeInPrevious > 0)
            {
                sb.Append(" (Previous state duration: ");
                sb.Append(timeInPrevious.ToString("F3"));
                sb.Append("s)");
            }

            GameLogger.Debug(sb.ToString(), _logCategory);
        }

        private void LogStateTransition(T from, T to)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append("State transition: ");
            sb.Append(from);
            sb.Append(" -> ");
            sb.Append(to);

            if (_enablePerformanceTracking)
            {
                sb.Append(" | Transition #");
                sb.Append(GetTransitionCount(to));
            }

            GameLogger.Debug(sb.ToString(), _logCategory);
        }

        #endregion

        #region Performance Tracking

        private void TrackStateTransition(T from, T to)
        {
            var currentTime = Time.time;
            var timeSpent = currentTime - _lastStateChangeTime;

            // 이전 상태의 시간 기록
            if (!EqualityComparer<T>.Default.Equals(from, default(T)))
            {
                _stateTimeSpent.TryAdd(from, 0f);
                _stateTimeSpent[from] += timeSpent;
            }

            // 전환 횟수 기록
            _stateTransitionCount.TryAdd(to, 0);
            _stateTransitionCount[to]++;
        }

        public float GetTimeSpentInState(T stateType)
        {
            _stateTimeSpent.TryGetValue(stateType, out var time);

            // 현재 상태인 경우 현재까지의 시간 추가
            if (EqualityComparer<T>.Default.Equals(_currentStateType.Value, stateType))
            {
                time += TimeInCurrentState;
            }

            return time;
        }

        public int GetTransitionCount(T stateType)
        {
            _stateTransitionCount.TryGetValue(stateType, out var count);
            return count;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 상태 머신 정보
        /// </summary>
        public string GetStateMachineDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine(ZString.Concat("=== State Machine Debug Info ==="));
            sb.AppendLine(ZString.Concat("Current State: ", _currentStateType.Value));
            sb.AppendLine(ZString.Concat("Time in Current: ", TimeInCurrentState.ToString("F3"), "s"));
            sb.AppendLine(ZString.Concat("Total States: ", _states.Count));
            sb.AppendLine(ZString.Concat("Is Initialized: ", IsInitialized));

            if (_enablePerformanceTracking)
            {
                sb.AppendLine();
                sb.AppendLine("=== Performance Stats ===");

                foreach (var kvp in _stateTimeSpent)
                {
                    sb.AppendLine(ZString.Concat(kvp.Key, ": ", kvp.Value.ToString("F3"), "s (", GetTransitionCount(kvp.Key), " transitions)"));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 모든 상태 목록 반환
        /// </summary>
        public string GetAllStates()
        {
            using var sb = ZString.CreateStringBuilder();
            bool first = true;

            foreach (var state in _states.Keys)
            {
                if (!first) sb.Append(", ");
                sb.Append(state);
                first = false;
            }

            return sb.ToString();
        }
#endif

        #endregion

        #region Disposal

        public void Dispose()
        {
            try
            {
                // 현재 상태 종료
                if (_currentState.Value != null)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug(ZString.Concat("Exiting current state during disposal: ", _currentStateType.Value), _logCategory);
                    }

                    _currentState.Value.OnExit();
                }

                // 모든 상태 정리
                var exceptions = new List<Exception>();
                foreach (var kvp in _states)
                {
                    try
                    {
                        kvp.Value.Dispose();
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(new Exception(ZString.Concat("Error disposing state ", kvp.Key, ": ", e.Message), e));
                    }
                }

                // 정리
                _states.Clear();
                _stateTimeSpent.Clear();
                _stateTransitionCount.Clear();

                _disposables?.Dispose();
                _onStateTransition?.Dispose();
                _currentStateType?.Dispose();
                _currentState?.Dispose();

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("StateMachine disposed", _logCategory);
                }

                if (exceptions.Count > 0)
                {
                    throw new AggregateException("Errors occurred during state disposal", exceptions);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error during StateMachine disposal: ", e.Message), _logCategory);
                throw;
            }
        }

        #endregion
    }
}
