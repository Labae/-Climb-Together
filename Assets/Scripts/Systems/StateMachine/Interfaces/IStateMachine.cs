using System;
using R3;

namespace Systems.StateMachine.Interfaces
{
    public interface IStateMachine<T> : IDisposable where T : Enum
    {
        #region Current State Properties

        /// <summary>현재 상태 타입</summary>
        ReadOnlyReactiveProperty<T> CurrentStateType { get; }

        /// <summary>현재 상태 객체</summary>
        ReadOnlyReactiveProperty<IState<T>> CurrentState { get; }

        #endregion

        #region State Management

        /// <summary>
        /// 상태를 추가합니다
        /// </summary>
        /// <param name="state">추가할 상태</param>
        void AddState(IState<T> state);

        /// <summary>
        /// 상태를 제거합니다
        /// </summary>
        /// <param name="stateType">제거할 상태 타입</param>
        void RemoveState(T stateType);

        /// <summary>
        /// 상태를 변경합니다 (검증 후)
        /// </summary>
        /// <param name="stateType">변경할 상태 타입</param>
        void ChangeState(T stateType);

        /// <summary>
        /// 상태를 강제로 변경합니다 (검증 없이)
        /// </summary>
        /// <param name="stateType">변경할 상태 타입</param>
        void ForceChangeState(T stateType);

        void TrySetInitialState(T stateType);

        #endregion

        #region Observables

        /// <summary>상태 변경 이벤트</summary>
        Observable<T> OnStateChanged { get; }

        /// <summary>상태 전환 이벤트 (from, to)</summary>
        Observable<(T from, T to)> OnStateTransition { get; }

        /// <summary>상태 진입 이벤트</summary>
        Observable<T> OnStateEnter { get; }

        /// <summary>상태 종료 이벤트</summary>
        Observable<T> OnStateExit { get; }

        #endregion

        #region Validation & Queries

        /// <summary>
        /// 상태 변경이 가능한지 확인
        /// </summary>
        /// <param name="stateType">확인할 상태 타입</param>
        /// <returns>변경 가능 여부</returns>
        bool CanChangeState(T stateType);

        /// <summary>
        /// 상태 변경이 가능한지 확인 (실패 이유 포함)
        /// </summary>
        /// <param name="stateType">확인할 상태 타입</param>
        /// <param name="reason">실패 이유</param>
        /// <returns>변경 가능 여부</returns>
        bool CanChangeState(T stateType, out string reason);

        /// <summary>
        /// 특정 상태가 존재하는지 확인
        /// </summary>
        /// <param name="stateType">확인할 상태 타입</param>
        /// <returns>존재 여부</returns>
        bool HasState(T stateType);

        #endregion

        #region State Information

        /// <summary>
        /// 특정 상태 객체를 가져옵니다
        /// </summary>
        /// <param name="stateType">가져올 상태 타입</param>
        /// <returns>상태 객체 (없으면 null)</returns>
        IState<T> GetState(T stateType);

        /// <summary>등록된 상태의 개수</summary>
        int StateCount { get; }

        /// <summary>상태 머신이 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>현재 상태에서 머무른 시간</summary>
        float TimeInCurrentState { get; }

        #endregion

        #region Performance Tracking (Optional)

        /// <summary>
        /// 특정 상태에서 보낸 총 시간 반환
        /// </summary>
        /// <param name="stateType">확인할 상태 타입</param>
        /// <returns>총 시간 (초)</returns>
        float GetTimeSpentInState(T stateType);

        /// <summary>
        /// 특정 상태로의 전환 횟수 반환
        /// </summary>
        /// <param name="stateType">확인할 상태 타입</param>
        /// <returns>전환 횟수</returns>
        int GetTransitionCount(T stateType);

        #endregion

        #region Debug Support

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 상태 머신 정보 반환
        /// </summary>
        /// <returns>디버그 정보 문자열</returns>
        string GetStateMachineDebugInfo();

        /// <summary>
        /// 모든 등록된 상태 목록 반환
        /// </summary>
        /// <returns>상태 목록 문자열</returns>
        string GetAllStates();
#endif

        #endregion
    }
}
