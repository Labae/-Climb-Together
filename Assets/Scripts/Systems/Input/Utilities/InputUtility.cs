using UnityEngine;

namespace Systems.Input.Utilities
{
    public static class InputUtility
    {
        public const float InputThreshold = 0.1f;
        public const float StickDeadZone = 0.2f;
        public const float TriggerThreshold = 0.5f;

        /// <summary>
        /// 입력이 임계값을 넘어서 활성화된 상태인지 확인합니다.
        /// </summary>
        /// <param name="input">입력 값</param>
        /// <param name="threshold">임계값 (기본값: InputThreshold)</param>
        /// <returns>입력이 활성화되었는지 여부</returns>
        public static bool IsInputActive(float input, float threshold = InputThreshold)
        {
            return Mathf.Abs(input) > threshold;
        }

        /// <summary>
        /// 입력이 데드존 내에 있는지 확인합니다.
        /// </summary>
        /// <param name="input">입력 값</param>
        /// <param name="threshold">데드존 임계값 (기본값: InputThreshold)</param>
        /// <returns>입력이 데드존 내에 있는지 여부</returns>
        public static bool InDeadZone(float input, float threshold = InputThreshold)
        {
            return Mathf.Abs(input) <= threshold;
        }

        /// <summary>
        /// Vector2 입력이 활성화된 상태인지 확인합니다.
        /// </summary>
        /// <param name="input">Vector2 입력</param>
        /// <param name="threshold">임계값</param>
        /// <returns>입력이 활성화되었는지 여부</returns>
        public static bool IsInputActive(Vector2 input, float threshold = InputThreshold)
        {
            return input.magnitude > threshold;
        }

        /// <summary>
        /// Vector2 입력이 데드존 내에 있는지 확인합니다.
        /// </summary>
        /// <param name="input">Vector2 입력</param>
        /// <param name="threshold">데드존 임계값</param>
        /// <returns>입력이 데드존 내에 있는지 여부</returns>
        public static bool InDeadZone(Vector2 input, float threshold = InputThreshold)
        {
            return input.magnitude <= threshold;
        }

        /// <summary>
        /// 입력을 정규화하고 데드존을 적용합니다.
        /// </summary>
        /// <param name="input">원본 입력</param>
        /// <param name="deadZone">데드존 크기</param>
        /// <returns>정규화된 입력</returns>
        public static Vector2 ApplyDeadZone(Vector2 input, float deadZone = StickDeadZone)
        {
            float magnitude = input.magnitude;

            if (magnitude <= deadZone)
                return Vector2.zero;

            // 데드존을 제거하고 나머지 범위를 0-1로 재매핑
            float normalizedMagnitude = (magnitude - deadZone) / (1f - deadZone);
            return input.normalized * Mathf.Clamp01(normalizedMagnitude);
        }

        /// <summary>
        /// 입력 방향을 가져옵니다 (-1, 0, 1).
        /// </summary>
        /// <param name="input">입력 값</param>
        /// <param name="threshold">임계값</param>
        /// <returns>방향 (-1: 음수, 0: 중립, 1: 양수)</returns>
        public static int GetInputDirection(float input, float threshold = InputThreshold)
        {
            if (input > threshold) return 1;
            if (input < -threshold) return -1;
            return 0;
        }

        /// <summary>
        /// 입력을 스무딩합니다 (관성 적용).
        /// </summary>
        /// <param name="currentInput">현재 입력</param>
        /// <param name="targetInput">목표 입력</param>
        /// <param name="smoothTime">스무딩 시간</param>
        /// <param name="deltaTime">델타 타임</param>
        /// <returns>스무딩된 입력</returns>
        public static float SmoothInput(float currentInput, float targetInput, float smoothTime, float deltaTime)
        {
            return Mathf.MoveTowards(currentInput, targetInput, smoothTime * deltaTime);
        }

        /// <summary>
        /// Vector2 입력을 스무딩합니다.
        /// </summary>
        public static Vector2 SmoothInput(Vector2 currentInput, Vector2 targetInput, float smoothTime, float deltaTime)
        {
            return Vector2.MoveTowards(currentInput, targetInput, smoothTime * deltaTime);
        }

        /// <summary>
        /// 입력이 임계값을 넘었는지 확인 (이전 프레임과 비교)
        /// </summary>
        /// <param name="currentInput">현재 입력</param>
        /// <param name="previousInput">이전 입력</param>
        /// <param name="threshold">임계값</param>
        /// <returns>입력이 새로 활성화되었는지 여부</returns>
        public static bool InputActivated(float currentInput, float previousInput, float threshold = InputThreshold)
        {
            return !IsInputActive(previousInput, threshold) && IsInputActive(currentInput, threshold);
        }

        /// <summary>
        /// 입력이 비활성화되었는지 확인 (이전 프레임과 비교)
        /// </summary>
        public static bool InputDeactivated(float currentInput, float previousInput, float threshold = InputThreshold)
        {
            return IsInputActive(previousInput, threshold) && !IsInputActive(currentInput, threshold);
        }
    }
}
