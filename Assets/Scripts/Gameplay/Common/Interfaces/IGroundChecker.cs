using R3;
using UnityEngine;

namespace Gameplay.Common.Interfaces
{
    public interface IGroundChecker
    {
        // 기존 이벤트들
        Observable<Unit> OnGroundEntered { get; }
        Observable<Unit> OnGroundExited { get; }

        // 기존 상태 프로퍼티들
        ReadOnlyReactiveProperty<bool> IsGrounded { get; }
        bool WasGroundedLastFrame { get; }

        // 추가된 메서드들
        /// <summary>
        /// 수동으로 접지 상태를 체크합니다.
        /// </summary>
        void CheckGroundState();

        /// <summary>
        /// 특정 위치에서 접지 상태를 체크합니다.
        /// </summary>
        /// <param name="origin">체크할 위치</param>
        /// <param name="distance">체크할 거리 (기본값: -1은 설정된 거리 사용)</param>
        void CheckAtPosition(Vector2 origin, float distance = -1);

        /// <summary>
        /// 현재 접지 상태를 즉시 반환합니다 (캐시된 값)
        /// </summary>
        bool IsCurrentlyGrounded();
    }
}
