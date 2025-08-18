using Core.Behaviours;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.TargetSelection
{
    /// <summary>
    /// 타겟 선택 Indicator
    /// </summary>
    public class TargetIndicator : CoreBehaviour
    {
        [Header("Components")] [SerializeField]
        private Image _indicatorImage;

        [SerializeField] private TextMeshProUGUI _targetText;
        [SerializeField] private GameObject _weaknessIcon;

        private RectTransform _rectTransform;

        private Camera _mainCamera;
        private Sequence _pulseAnimation;

        protected override void OnInitialize()
        {
            _mainCamera = Camera.main;
            _rectTransform ??= GetComponent<RectTransform>();
            _indicatorImage ??= GetComponent<Image>();
            if (_weaknessIcon != null)
            {
                _weaknessIcon.SetActive(false);
            }

            base.OnInitialize();
        }

        public void Setup(string targetName, bool isWeakness = false)
        {
            if (_targetText != null)
            {
                _targetText.text = targetName;
            }

            if (_weaknessIcon != null)
            {
                _weaknessIcon.SetActive(isWeakness);
            }
        }

        public void SetSelected(bool selected, Color color, float pulseSpeed = 1f)
        {
            StopAnimation();

            if (_indicatorImage != null)
            {
                _indicatorImage.color = color;
            }

            if (selected)
            {
                _pulseAnimation = DOTween.Sequence();
                _pulseAnimation.Append(transform.DOScale(Vector3.one * 1.2f,
                        pulseSpeed))
                    .Append(transform.DOScale(Vector3.one, pulseSpeed))
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        public void UpdatePosition(Vector3 worldPosition, float offset = 100f)
        {
            var screenPos = _mainCamera.WorldToScreenPoint(worldPosition);
            if (_rectTransform != null)
            {
                _rectTransform.position = screenPos + Vector3.up * offset;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        public void Hide()
        {
            StopAnimation();
            transform
                .DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void StopAnimation()
        {
            _pulseAnimation?.Kill();
            transform.DOKill();
        }

        protected override void HandleDestruction()
        {
            StopAnimation();
            base.HandleDestruction();
        }
    }
}
