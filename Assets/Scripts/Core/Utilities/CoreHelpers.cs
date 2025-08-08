using UnityEngine;

namespace Core.Utilities
{
    public static class CoreHelpers
    {
        public static T FindComponentInChildren<T>(Transform parent, string name = null) where T : Component
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                // 이름 조건 확인 (이름이 지정된 경우)
                if (!string.IsNullOrEmpty(name) && child.name != name)
                {
                    // 재귀적으로 더 깊이 찾기
                    T result = FindComponentInChildren<T>(child, name);
                    if (result != null) return result;
                    continue;
                }

                // 컴포넌트 확인
                T component = child.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }

                // 재귀적으로 더 깊이 찾기
                T deepResult = FindComponentInChildren<T>(child, name);
                if (deepResult != null) return deepResult;
            }

            return null;
        }
    }
}
