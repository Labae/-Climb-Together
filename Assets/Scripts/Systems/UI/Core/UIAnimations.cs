using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Systems.UI.Core
{
    /// <summary>
    /// UI 애니메이션 유틸리티 클래스
    /// </summary>
    public static class UIAnimations
    {
        public const float DEFAULT_ANIMATION_TIME = 0.3f;
        public const Ease DEFAULT_EASE = Ease.OutQuad;

        /// <summary>
        /// 팝업 등장 애니메이션
        /// </summary>
        public static async UniTask PopupShowAsync(Transform target, float duration = DEFAULT_ANIMATION_TIME)
        {
            target.localScale = Vector3.zero;
            target.gameObject.SetActive(true);

            await target.DOScale(Vector3.one, duration)
                .SetEase(Ease.OutBack)
                .ToUniTask();
        }

        /// <summary>
        /// 팝업 사라짐 애니메이션
        /// </summary>
        public static async UniTask PopupHideAsync(Transform target, float duration = DEFAULT_ANIMATION_TIME)
        {
            await target.DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)
                .ToUniTask();

            target.gameObject.SetActive(false);
        }

        /// <summary>
        /// 페이드 인 애니메이션
        /// </summary>
        public static async UniTask FadeInAsync(CanvasGroup canvasGroup, float duration = DEFAULT_ANIMATION_TIME,
            Ease ease = DEFAULT_EASE)
        {
            canvasGroup.alpha = 0;
            canvasGroup.gameObject.SetActive(true);

            await canvasGroup.DOFade(1f, duration)
                .SetEase(ease)
                .ToUniTask();
        }

        /// <summary>
        /// 페이드 아웃 애니메이션
        /// </summary>
        public static async UniTask FadeOutAsync(CanvasGroup canvasGroup, float duration = DEFAULT_ANIMATION_TIME,
            Ease ease = DEFAULT_EASE)
        {
            await canvasGroup.DOFade(0f, duration)
                .SetEase(ease)
                .ToUniTask();

            canvasGroup.gameObject.SetActive(false);
        }

        /// <summary>
        /// 모든 트윈 제거
        /// </summary>
        public static void KillAllTweens(Transform target)
        {
            target.DOKill();
        }
    }
}
