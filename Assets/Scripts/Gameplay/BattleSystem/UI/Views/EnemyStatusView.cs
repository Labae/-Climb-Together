using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data.BattleSystem.Combat;
using Debugging;
using Debugging.Enum;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI.Views
{
    public class EnemyStatusView : MonoBehaviour, IDisposable
    {
        [Header("Enemy Name")] [SerializeField]
        private TextMeshProUGUI _enemyNameText;

        [Header("Health Bar")] [SerializeField]
        private RectTransform _healthBarContainer;
        [SerializeField]
        private Image _healthBarFill;

        [Header("Health Bar Animation Settings")]
        [SerializeField]
        private float _healthAnimationDuration = 0.4f;

        [SerializeField] private float _healthColorDuration = 0.3f;
        [SerializeField] private float _shakeStrength = 10f;
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private Ease _healthEase = Ease.OutQuart;

        [Header("Health Bar Colors")]
        [SerializeField]
        private Color _healthFullColor = new Color(0.2f, 0.8f, 0.2f);

        [SerializeField] private Color _healthMediumColor = new Color(0.9f, 0.7f, 0.1f);
        [SerializeField] private Color _healthLowColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _healthWarningColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);

        [Header("Low Health Warning")]
        [SerializeField]
        private bool _enableLowHealthWarning = true;

        [SerializeField] private float _warningThreshold = 0.3f;
        [SerializeField] private float _warningBlinkSpeed = 1.5f;

        [Header("Shield System")]
        [SerializeField]
        private RectTransform _shieldContainer;

        [SerializeField] private GameObject _shieldIconPrefab;

        [Header("Shield Animation Settings")]
        [SerializeField]
        private float _shieldAnimationDuration = 0.5f;

        [SerializeField] private float _shieldAnimationDelay = 0.5f;
        [SerializeField] private Ease _shieldPopEase = Ease.Linear;
        [SerializeField] private Ease _shieldDestroyEase = Ease.Linear;

        private readonly List<Image> _shieldIcons = new();
        private Sequence _healthAnimation;
        private Sequence _shieldAnimation;
        private Sequence _lowHealthWarningAnimation;
        private bool _isShowingLowHealthWarning = false;

        /// <summary>
        /// 유닛 이름 설정
        /// </summary>
        public void SetUnitName(string unitName)
        {
            if (_enemyNameText != null)
            {
                _enemyNameText.text = unitName;
            }
        }

        public async UniTask UpdateHealthAnimate(HealthData healthData)
        {
            if (_healthBarFill == null || healthData.Max <= 0)
            {
                return;
            }

            _healthAnimation?.Kill();

            var targetFillAmount = healthData.Percentage;
            var currentFillAmount = _healthBarFill.fillAmount;
            bool tookDamage = targetFillAmount < currentFillAmount;

            var sequence = DOTween.Sequence();
            if (tookDamage)
            {
                sequence.Append(AnimateHealthBarShake());
            }

            sequence.Append(_healthBarFill.DOFillAmount(targetFillAmount, _healthAnimationDuration))
                .SetEase(_healthEase);

            sequence.Join(AnimateHealthBarColor(healthData.Percentage));

            _healthAnimation = sequence;
            await sequence.ToUniTask();
        }

        public async UniTask UpdateShieldAnimate(ShieldData shieldData)
        {
            _shieldAnimation?.Kill();

            int currentCount = _shieldIcons.Count;
            int targetCount = shieldData.Current;

            if (targetCount > currentCount)
            {
                await AnimateShieldCreation(targetCount - currentCount);
            }
            else if (targetCount < currentCount)
            {
                await AnimateShieldDestruction(currentCount - targetCount);
            }
        }

        public void SetLowHealthWarning(bool enable)
        {
            if (!_enableLowHealthWarning)
            {
                return;
            }

            if (enable && !_isShowingLowHealthWarning)
            {
                StartLowHealthWarning();
            }
            else if (!enable && _isShowingLowHealthWarning)
            {
                StopLowHealthWarning();
            }
        }

        public void TriggerDamageEffect()
        {
            if (_healthBarContainer != null)
            {
                _healthBarContainer.DOShakePosition(_shakeDuration, _shakeStrength);
            }
        }

        public void ShowCriticalHitEffect()
        {
            if (_healthBarFill != null)
            {
                var originalColor = _healthBarFill.color;
                _healthBarFill.DOColor(Color.red, 0.1f)
                    .SetLoops(2,  LoopType.Yoyo)
                    .OnComplete(() => _healthBarFill.color = originalColor);
            }
        }

        public void ShowHealEffect()
        {
            if (_healthBarFill != null)
            {
                var originalColor = _healthBarFill.color;
                _healthBarFill.DOColor(Color.gray, 0.2f)
                    .SetLoops(2,  LoopType.Yoyo)
                    .OnComplete(() => _healthBarFill.color = originalColor);
            }
        }

        #region HealthBar Animation Methods

        private Tween AnimateHealthBarShake()
        {
            var originalPos = _healthBarContainer.transform.position;

            return _healthBarContainer.DOShakePosition(_shakeDuration, _shakeStrength)
                .OnComplete(() =>
                {
                    _healthBarContainer.transform.position = originalPos;
                });
        }

        private Tween AnimateHealthBarColor(float percentage)
        {
            var targetColor = GetHealthColor(percentage);
            return _healthBarFill.DOColor(targetColor, _healthAnimationDuration);
        }

        private Color GetHealthColor(float percentage)
        {
            return percentage switch
            {
                > 0.6f => _healthFullColor,
                > 0.3f => _healthMediumColor,
                _ => _healthLowColor
            };
        }

        private void StartLowHealthWarning()
        {
            if (_healthBarFill == null)
            {
                return;
            }

            _isShowingLowHealthWarning = true;
            _lowHealthWarningAnimation?.Kill();

            var originalColor = _healthBarFill.color;

            _lowHealthWarningAnimation = DOTween.Sequence()
                .Append(_healthBarFill.DOFade(_healthWarningColor.a, 1f / _warningBlinkSpeed))
                .Append(_healthBarFill.DOFade(originalColor.a, 1f / _warningBlinkSpeed))
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void StopLowHealthWarning()
        {
            _isShowingLowHealthWarning = false;
            _lowHealthWarningAnimation?.Kill();

            if (_healthBarFill != null)
            {
                _healthBarFill.DOKill();
                var originalColor = GetHealthColor(_healthBarFill.fillAmount);
                _healthBarFill.color = originalColor;
            }
        }

        #endregion

        #region Shield Animation Methods

         private async UniTask AnimateShieldCreation(int createCount)
        {
            var sequence = DOTween.Sequence();

            for (int i = 0; i < createCount; i++)
            {
                var iconObj = Instantiate(_shieldIconPrefab, _shieldContainer);
                var iconImage = iconObj.GetComponent<Image>();
                var iconTransform = iconObj.transform;

                if (iconImage != null)
                {
                    _shieldIcons.Add(iconImage);

                    iconTransform.localScale = Vector3.zero;

                    sequence.Insert(i * _shieldAnimationDelay,
                        iconTransform.DOScale(Vector3.one, _shieldAnimationDuration)).SetEase(_shieldPopEase);
                }
                else
                {
                    GameLogger.Warning("Shield Icon에 Image가 없습니다", LogCategory.Battle);
                    Destroy(iconObj.gameObject);
                }
            }

            _shieldAnimation = sequence;
            await sequence.ToUniTask();
        }

        private async UniTask AnimateShieldDestruction(int destroyCount)
        {
            if (destroyCount <= 0 || _shieldIcons.Count == 0)
            {
                return;
            }

            var sequence = DOTween.Sequence();

            for (int i = 0; i < destroyCount && _shieldIcons.Count > 0; i++)
            {
                var lastIndex = _shieldIcons.Count - 1;
                var iconToDestroy = _shieldIcons[lastIndex];

                if (iconToDestroy != null)
                {
                    _shieldIcons.RemoveAt(lastIndex);

                    sequence.Insert(i * _shieldAnimationDelay,
                            iconToDestroy.transform.DOScale(Vector3.zero, _shieldAnimationDuration))
                        .SetEase(_shieldDestroyEase)
                        .OnComplete(() =>
                        {
                            if (iconToDestroy != null)
                            {
                                DestroyImmediate(iconToDestroy.gameObject);
                            }
                        });
                }
            }

            _shieldAnimation = sequence;
            await sequence.ToUniTask();
        }

        #endregion

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            _shieldAnimation?.Kill();
            _healthAnimation?.Kill();
            _lowHealthWarningAnimation?.Kill();

            foreach (var icon in _shieldIcons)
            {
                if (icon != null)
                {
                    DestroyImmediate(icon.gameObject);
                }
            }
            _shieldIcons.Clear();
        }
    }
}
