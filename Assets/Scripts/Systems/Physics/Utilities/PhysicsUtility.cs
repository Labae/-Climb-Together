using UnityEngine;

namespace Systems.Physics.Utilities
{
    public static class PhysicsUtility
    {
        public const float VelocityThreshold = 0.01f;
        public const float AngularVelocityThreshold = 0.01f;
        public const float StopThreshold = 0.1f;

        /// <summary>
        /// 속도가 무시할 수 있을 정도로 작은지 확인합니다.
        /// </summary>
        /// <param name="velocity">속도 값</param>
        /// <param name="threshold">임계값 (기본값: VelocityThreshold)</param>
        /// <returns>속도를 무시할 수 있는지 여부</returns>
        public static bool IgnoreVelocity(float velocity, float threshold = VelocityThreshold)
        {
            return Mathf.Abs(velocity) <= threshold;
        }

        /// <summary>
        /// 속도가 유효한 값인지 확인합니다.
        /// </summary>
        /// <param name="velocity">속도 값</param>
        /// <param name="threshold">임계값 (기본값: VelocityThreshold)</param>
        /// <returns>속도가 유효한지 여부</returns>
        public static bool HasValidVelocity(float velocity, float threshold = VelocityThreshold)
        {
            return Mathf.Abs(velocity) > threshold;
        }

        /// <summary>
        /// Vector2 속도가 무시할 수 있을 정도로 작은지 확인합니다.
        /// </summary>
        /// <param name="velocity">속도 벡터</param>
        /// <param name="threshold">임계값</param>
        /// <returns>속도를 무시할 수 있는지 여부</returns>
        public static bool IgnoreVelocity(Vector2 velocity, float threshold = VelocityThreshold)
        {
            return velocity.magnitude <= threshold;
        }

        /// <summary>
        /// Vector2 속도가 유효한 값인지 확인합니다.
        /// </summary>
        public static bool HasValidVelocity(Vector2 velocity, float threshold = VelocityThreshold)
        {
            return velocity.magnitude > threshold;
        }

        /// <summary>
        /// 오브젝트가 거의 정지 상태인지 확인합니다.
        /// </summary>
        /// <param name="velocity">속도 벡터</param>
        /// <param name="threshold">정지 임계값</param>
        /// <returns>거의 정지 상태인지 여부</returns>
        public static bool IsNearlyStationary(Vector2 velocity, float threshold = StopThreshold)
        {
            return velocity.magnitude <= threshold;
        }

        /// <summary>
        /// 오브젝트가 움직이고 있는지 확인합니다.
        /// </summary>
        public static bool IsMoving(Vector2 velocity, float threshold = StopThreshold)
        {
            return velocity.magnitude > threshold;
        }

        /// <summary>
        /// 속도의 방향을 가져옵니다 (-1, 0, 1).
        /// </summary>
        /// <param name="velocity">속도 값</param>
        /// <param name="threshold">임계값</param>
        /// <returns>방향 (-1: 음수, 0: 정지, 1: 양수)</returns>
        public static int GetVelocityDirection(float velocity, float threshold = VelocityThreshold)
        {
            if (velocity > threshold) return 1;
            if (velocity < -threshold) return -1;
            return 0;
        }

        /// <summary>
        /// 속도를 클램프합니다.
        /// </summary>
        /// <param name="velocity">속도 벡터</param>
        /// <param name="maxSpeed">최대 속도</param>
        /// <returns>클램프된 속도</returns>
        public static Vector2 ClampVelocity(Vector2 velocity, float maxSpeed)
        {
            return Vector2.ClampMagnitude(velocity, maxSpeed);
        }

        /// <summary>
        /// 수평 속도만 클램프합니다.
        /// </summary>
        /// <param name="velocity">속도 벡터</param>
        /// <param name="maxHorizontalSpeed">최대 수평 속도</param>
        /// <returns>수평 속도가 클램프된 속도</returns>
        public static Vector2 ClampHorizontalVelocity(Vector2 velocity, float maxHorizontalSpeed)
        {
            return new Vector2(
                Mathf.Clamp(velocity.x, -maxHorizontalSpeed, maxHorizontalSpeed),
                velocity.y
            );
        }

        /// <summary>
        /// 수직 속도만 클램프합니다.
        /// </summary>
        /// <param name="velocity">속도 벡터</param>
        /// <param name="maxVerticalSpeed">최대 수직 속도</param>
        /// <returns>수직 속도가 클램프된 속도</returns>
        public static Vector2 ClampVerticalVelocity(Vector2 velocity, float maxVerticalSpeed)
        {
            return new Vector2(
                velocity.x,
                Mathf.Clamp(velocity.y, -maxVerticalSpeed, maxVerticalSpeed)
            );
        }

        /// <summary>
        /// 속도가 변화했는지 확인합니다.
        /// </summary>
        /// <param name="currentVelocity">현재 속도</param>
        /// <param name="previousVelocity">이전 속도</param>
        /// <param name="threshold">변화 감지 임계값</param>
        /// <returns>속도가 변화했는지 여부</returns>
        public static bool VelocityChanged(Vector2 currentVelocity, Vector2 previousVelocity,
            float threshold = VelocityThreshold)
        {
            return Vector2.Distance(currentVelocity, previousVelocity) > threshold;
        }

        /// <summary>
        /// 오브젝트가 떨어지고 있는지 확인합니다.
        /// </summary>
        /// <param name="velocity">속도 벡터</param>
        /// <param name="threshold">낙하 임계값</param>
        /// <returns>떨어지고 있는지 여부</returns>
        public static bool IsFalling(Vector2 velocity, float threshold = VelocityThreshold)
        {
            return velocity.y < -threshold;
        }

        /// <summary>
        /// 오브젝트가 상승하고 있는지 확인합니다.
        /// </summary>
        /// <param name="velocity">속도 벡터</param>
        /// <param name="threshold">상승 임계값</param>
        /// <returns>상승하고 있는지 여부</returns>
        public static bool IsRising(Vector2 velocity, float threshold = VelocityThreshold)
        {
            return velocity.y > threshold;
        }

        /// <summary>
        /// 각속도가 유효한지 확인합니다.
        /// </summary>
        /// <param name="angularVelocity">각속도</param>
        /// <param name="threshold">임계값</param>
        /// <returns>각속도가 유효한지 여부</returns>
        public static bool HasValidAngularVelocity(float angularVelocity, float threshold = AngularVelocityThreshold)
        {
            return Mathf.Abs(angularVelocity) > threshold;
        }

        /// <summary>
        /// 두 속도 벡터가 같은 방향인지 확인합니다.
        /// </summary>
        /// <param name="velocity1">첫 번째 속도</param>
        /// <param name="velocity2">두 번째 속도</param>
        /// <param name="threshold">방향 일치 임계값 (dot product)</param>
        /// <returns>같은 방향인지 여부</returns>
        public static bool SameDirection(Vector2 velocity1, Vector2 velocity2, float threshold = 0.5f)
        {
            if (velocity1.magnitude <= VelocityThreshold || velocity2.magnitude <= VelocityThreshold)
                return false;

            return Vector2.Dot(velocity1.normalized, velocity2.normalized) > threshold;
        }
    }
}
