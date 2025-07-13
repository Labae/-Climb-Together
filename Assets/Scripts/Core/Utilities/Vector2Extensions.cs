using System.Text;
using UnityEngine;

namespace Core.Utilities
{
    public static class Vector2Extensions
    {
        #region Distance Utility

        /// <summary>
        /// 두 벡터 사이의 거리 (제곱근 계산 없음 - 성능 최적화)
        /// </summary>
        public static float DistanceSqr(this Vector2 from, Vector2 to)
        {
            return (from - to).sqrMagnitude;
        }

        /// <summary>
        /// 지정된 거리보다 가까운지 확인 (제곱근 계산 없음)
        /// </summary>
        public static bool IsCloserThan(this Vector2 from, Vector2 to, float distance)
        {
            return from.DistanceSqr(to) < distance * distance;
        }

        /// <summary>
        /// 지정된 거리보다 먼지 확인 (제곱근 계산 없음)
        /// </summary>
        public static bool IsFartherThan(this Vector2 from, Vector2 to, float distance)
        {
            return from.DistanceSqr(to) > distance * distance;
        }

        #endregion

        #region Direction Utility

        /// <summary>
        /// 다른 벡터로의 방향 벡터 (정규화됨)
        /// </summary>
        public static Vector2 DirectionTo(this Vector2 from, Vector2 to)
        {
            return (to - from).normalized;
        }

        /// <summary>
        /// 각도를 Vector2로 변환 (도 단위)
        /// </summary>
        public static Vector2 AngleToVector2(float angleDegrees)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }

        /// <summary>
        /// Vector2를 각도로 변환 (도 단위)
        /// </summary>
        public static float ToAngle(this Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 벡터를 시계 방향으로 90도 회전 (최적화된 버전)
        /// </summary>
        public static Vector2 RotateClockwise90(this Vector2 vector)
        {
            float temp = vector.x;
            vector.x = vector.y;
            vector.y = -temp;
            return vector;
        }

        /// <summary>
        /// 벡터를 반시계 방향으로 90도 회전 (최적화된 버전)
        /// </summary>
        public static Vector2 RotateCounterClockwise90(this Vector2 vector)
        {
            float temp = vector.x;
            vector.x = -vector.y;
            vector.y = temp;
            return vector;
        }

        /// <summary>
        /// 벡터를 지정된 각도만큼 회전 (최적화된 버전)
        /// </summary>
        public static Vector2 Rotate(this Vector2 vector, float angleDegrees)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            float newX = vector.x * cos - vector.y * sin;
            float newY = vector.x * sin + vector.y * cos;

            vector.x = newX;
            vector.y = newY;
            return vector;
        }

        #endregion

        #region Math Utility

        /// <summary>
        /// 벡터의 각 성분을 절댓값으로 변환 (최적화된 버전)
        /// </summary>
        public static Vector2 Abs(this Vector2 vector)
        {
            vector.x = Mathf.Abs(vector.x);
            vector.y = Mathf.Abs(vector.y);
            return vector;
        }

        /// <summary>
        /// 벡터의 각 성분을 다른 벡터로 나눔 (최적화된 버전)
        /// </summary>
        public static Vector2 Divide(this Vector2 vector, Vector2 divisor)
        {
            vector.x /= divisor.x;
            vector.y /= divisor.y;
            return vector;
        }

        /// <summary>
        /// 벡터의 각 성분을 다른 벡터와 곱함 (최적화된 버전)
        /// </summary>
        public static Vector2 Multiply(this Vector2 vector, Vector2 multiplier)
        {
            vector.x *= multiplier.x;
            vector.y *= multiplier.y;
            return vector;
        }

        /// <summary>
        /// 벡터의 각 성분을 지정된 범위로 클램프 (최적화된 버전)
        /// </summary>
        public static Vector2 Clamp(this Vector2 vector, Vector2 min, Vector2 max)
        {
            vector.x = Mathf.Clamp(vector.x, min.x, max.x);
            vector.y = Mathf.Clamp(vector.y, min.y, max.y);
            return vector;
        }

        /// <summary>
        /// 벡터의 크기를 지정된 범위로 클램프
        /// </summary>
        public static Vector2 ClampMagnitude(this Vector2 vector, float maxLength)
        {
            return Vector2.ClampMagnitude(vector, maxLength);
        }

        #endregion

        #region Interpolation & Movement Utility

        /// <summary>
        /// 목표 지점으로 부드럽게 이동 (SmoothDamp 래퍼)
        /// </summary>
        public static Vector2 SmoothMoveTo(this Vector2 current, Vector2 target, ref Vector2 velocity, float smoothTime)
        {
            return Vector2.SmoothDamp(current, target, ref velocity, smoothTime);
        }

        /// <summary>
        /// 원형 보간 (각도 기반)
        /// </summary>
        public static Vector2 SlerpTo(this Vector2 from, Vector2 to, float t)
        {
            float angle = Vector2.Angle(from, to) * Mathf.Deg2Rad;
            float sinAngle = Mathf.Sin(angle);

            if (Mathf.Abs(sinAngle) < 0.001f)
                return Vector2.Lerp(from, to, t);

            float a = Mathf.Sin((1 - t) * angle) / sinAngle;
            float b = Mathf.Sin(t * angle) / sinAngle;

            return from * a + to * b;
        }

        #endregion

        #region Random Utility

        /// <summary>
        /// 지정된 반지름 내의 랜덤 위치
        /// </summary>
        public static Vector2 RandomInRadius(this Vector2 center, float radius)
        {
            return center + Random.insideUnitCircle * radius;
        }

        /// <summary>
        /// 지정된 반지름의 원 둘레 상의 랜덤 위치
        /// </summary>
        public static Vector2 RandomOnCircle(this Vector2 center, float radius)
        {
            return center + Random.insideUnitCircle.normalized * radius;
        }

        /// <summary>
        /// 벡터에 랜덤 노이즈 추가
        /// </summary>
        public static Vector2 AddRandomNoise(this Vector2 vector, float noiseAmount)
        {
            return vector + Random.insideUnitCircle * noiseAmount;
        }

        #endregion

        #region Game Development Utility

        /// <summary>
        /// 그리드에 스냅
        /// </summary>
        public static Vector2 SnapToGrid(this Vector2 position, float gridSize)
        {
            return new Vector2(
                Mathf.Round(position.x / gridSize) * gridSize,
                Mathf.Round(position.y / gridSize) * gridSize
            );
        }

        /// <summary>
        /// 8방향 입력을 정규화된 벡터로 변환
        /// </summary>
        public static Vector2 To8Direction(this Vector2 input)
        {
            if (input.magnitude < 0.1f)
                return Vector2.zero;

            float angle = Mathf.Atan2(input.y, input.x);
            angle = Mathf.Round(angle / (Mathf.PI / 4)) * (Mathf.PI / 4);

            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// 4방향 입력을 정규화된 벡터로 변환
        /// </summary>
        public static Vector2 To4Direction(this Vector2 input)
        {
            if (input.magnitude < 0.1f)
                return Vector2.zero;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                return new Vector2(Mathf.Sign(input.x), 0);
            else
                return new Vector2(0, Mathf.Sign(input.y));
        }

        #endregion

        #region Debug Utility

        private static readonly StringBuilder StringBuilder = new(64);

        /// <summary>
        /// 벡터를 문자열로 변환 (소수점 자릿수 지정)
        /// </summary>
        public static string ToString(this Vector2 vector, int decimals)
        {
            StringBuilder.Clear();
            StringBuilder.Append($"({vector.x.ToString($"F{decimals}")}, ");
            StringBuilder.Append($"{vector.y.ToString($"F{decimals}")})");
            return StringBuilder.ToString();
        }

        /// <summary>
        /// 벡터가 거의 0인지 확인
        /// </summary>
        public static bool IsNearZero(this Vector2 vector, float threshold = 0.001f)
        {
            return vector.sqrMagnitude < threshold * threshold;
        }

        /// <summary>
        /// 두 벡터가 거의 같은지 확인
        /// </summary>
        public static bool IsNearEqual(this Vector2 a, Vector2 b, float threshold = 0.001f)
        {
            return (a - b).sqrMagnitude < threshold * threshold;
        }

        #endregion
    }
}