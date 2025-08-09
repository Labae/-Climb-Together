using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Utilities
{
    public static class ImageExtensions
    {
        public static Tween DOFillAmount(this Image image, float fillAmount, float duration)
        {
            return DOTween.To(() => image.fillAmount, x => image.fillAmount = x, fillAmount, duration);
        }

        public static Tween DOColor(this Image image, Color targetColor, float duration)
        {
            return DOTween.To(() => image.color, x => image.color = x, targetColor, duration);
        }

        public static Tween DOFade(this Image image, float targetAlpha, float duration)
        {
            return DOTween.To(() => image.color.a,
                x => image.color = new Color(image.color.r, image.color.g, image.color.b, x),
                targetAlpha, duration);
        }
    }
}
