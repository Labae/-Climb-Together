using System;
using R3;
using Systems.Physics;
using Systems.Physics.Enums;
using UnityEngine;

namespace Gameplay.Physics.Interfaces
{
    public interface IPhysicsController : IDisposable
    {
        #region Core Update

        /// <summary>
        /// 물리 업데이트 - FixedUpdate에서 호출되어야 함
        /// </summary>
        void FixedUpdate();

        #endregion

        #region Velocity System

        /// <summary>
        /// 속도 요청 추가 - 다음 프레임에 처리됨
        /// </summary>
        /// <param name="request">속도 요청</param>
        void RequestVelocity(VelocityRequest request);

        /// <summary>
        /// 지속적인 힘 설정 - 매 프레임마다 적용됨
        /// </summary>
        /// <param name="type">힘의 타입</param>
        /// <param name="force">적용할 힘</param>
        void SetForce(ForceType type, Vector2 force);

        /// <summary>
        /// 지속적인 힘 제거
        /// </summary>
        /// <param name="type">제거할 힘의 타입</param>
        void RemoveForce(ForceType type);

        #endregion

        #region Quick Actions

        /// <summary>
        /// 점프 - 수직 속도를 설정
        /// </summary>
        /// <param name="jumpSpeed">점프 속도</param>
        void Jump(float jumpSpeed);

        /// <summary>
        /// 이동 - 수평 속도를 지속적으로 적용
        /// </summary>
        /// <param name="horizontalSpeed">수평 이동 속도</param>
        void Move(float horizontalSpeed);

        /// <summary>
        /// 대시 - 방향과 속도로 Override
        /// </summary>
        /// <param name="direction">대시 방향</param>
        /// <param name="speed">대시 속도</param>
        void Dash(Vector2 direction, float speed);

        /// <summary>
        /// 넉백 - 방향과 힘으로 Override
        /// </summary>
        /// <param name="direction">넉백 방향</param>
        /// <param name="force">넉백 힘</param>
        void Knockback(Vector2 direction, float force);

        /// <summary>
        /// 완전 정지 - 모든 속도를 0으로 설정
        /// </summary>
        void Stop();

        /// <summary>
        /// 점진적 정지 - 현재 속도를 감소시킴
        /// </summary>
        /// <param name="deceleration">감속도</param>
        /// <param name="deltaTime">델타 타임</param>
        void SlowDown(float deceleration, float deltaTime);

        #endregion

        #region Getters

        /// <summary>현재 속도 벡터 반환</summary>
        Vector2 GetVelocity();

        /// <summary>현재 수평 속도 반환</summary>
        float GetHorizontalSpeed();

        /// <summary>현재 수직 속도 반환</summary>
        float GetVerticalSpeed();

        /// <summary>움직이고 있는지 여부 - PhysicsUtility 사용</summary>
        bool IsCurrentlyMoving();

        /// <summary>거의 정지 상태인지 여부 - PhysicsUtility 사용</summary>
        bool IsCurrentlyNearlyStationary();

        /// <summary>떨어지고 있는지 여부 - PhysicsUtility 사용</summary>
        bool IsCurrentlyFalling();

        /// <summary>상승하고 있는지 여부 - PhysicsUtility 사용</summary>
        bool IsCurrentlyRising();

        /// <summary>속도 방향 반환 - PhysicsUtility 사용</summary>
        int GetHorizontalDirection();

        /// <summary>이전 프레임과 속도가 변화했는지 확인</summary>
        bool HasVelocityChanged();

        /// <summary>현재 속도가 같은 방향인지 확인</summary>
        /// <param name="otherVelocity">비교할 속도</param>
        bool IsSameDirection(Vector2 otherVelocity);

        #endregion

        #region Reactive Properties

        /// <summary>현재 속도 벡터</summary>
        ReadOnlyReactiveProperty<Vector2> Velocity { get; }

        /// <summary>현재 수평 속도</summary>
        ReadOnlyReactiveProperty<float> HorizontalVelocity { get; }

        /// <summary>현재 수직 속도</summary>
        ReadOnlyReactiveProperty<float> VerticalVelocity { get; }

        /// <summary>상승 중인지 여부</summary>
        ReadOnlyReactiveProperty<bool> IsRising { get; }

        /// <summary>하강 중인지 여부</summary>
        ReadOnlyReactiveProperty<bool> IsFalling { get; }

        /// <summary>움직이고 있는지 여부</summary>
        ReadOnlyReactiveProperty<bool> IsMoving { get; }

        /// <summary>거의 정지 상태인지 여부</summary>
        ReadOnlyReactiveProperty<bool> IsNearlyStationary { get; }

        #endregion

        #region Gravity System

        /// <summary>중력 활성화/비활성화</summary>
        /// <param name="enabled">중력 활성화 여부</param>
        void SetGravityEnabled(bool enabled);

        /// <summary>현재 중력값 반환</summary>
        float GetCurrentGravity();

        /// <summary>빠른 낙하 - 즉시 터미널 속도로 설정</summary>
        void FastFall();

        #endregion

        #region Constraints

        /// <summary>
        /// 수평 이동 잠금/해제
        /// </summary>
        /// <param name="locked">잠금 여부</param>
        void LockHorizontalMovement(bool locked);

        /// <summary>
        /// 수직 이동 잠금/해제
        /// </summary>
        /// <param name="locked">잠금 여부</param>
        void LockVerticalMovement(bool locked);

        #endregion
    }
}
