using System.Collections.Generic;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using Debugging.Enum;
using DG.Tweening;
using Gameplay.BattleSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    public class EnemyStatsUI : MonoBehaviour
    {
        [Header("Enemy Name")] [SerializeField]
        private TextMeshProUGUI _enemyNameText;

        [Header("Health Bar")] [SerializeField]
        private RectTransform _healthBarContainer;

        [SerializeField] private Image _healthBarFill;

        [Header("Health Bar Animation Settings")] [SerializeField]
        private float _healthAnimationDuration = 0.4f;

        [SerializeField] private float _healthColorDuration = 0.3f;
        [SerializeField] private float _shakeStrength = 10f;
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private Ease _healthEase = Ease.OutQuart;

        [Header("Health Bar Colors")] [SerializeField]
        private Color _healthFullColor = new Color(0.2f, 0.8f, 0.2f);

        [SerializeField] private Color _healthMediumColor = new Color(0.9f, 0.7f, 0.1f);
        [SerializeField] private Color _healthLowColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _healthWarningColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);

        [Header("Low Health Warning")] [SerializeField]
        private bool _enableLowHealthWarning = true;

        [SerializeField] private float _warningThreshold = 0.3f;
        [SerializeField] private float _warningBlinkSpeed = 1.5f;

        [Header("Shield System")] [SerializeField]
        private RectTransform _shieldContainer;

        [SerializeField] private GameObject _shieldIconPrefab;

        [Header("Shield Animation Settings")] [SerializeField]
        private float _shieldAnimationDuration = 0.5f;

        [SerializeField] private float _shieldAnimationDelay = 0.5f;
        [SerializeField] private Ease _shieldPopEase = Ease.Linear;
        [SerializeField] private Ease _shieldDestroyEase = Ease.Linear;

        private BattleUnit _targetUnit;
        private readonly List<Image> _shieldIcons = new();
        private Sequence _currentAnimation;
        private Sequence _breakAnimation;
        private Sequence _healthAnimation;
        private Sequence _lowHealthWarningAnimation;

        private bool _isInitialized = false;
        private bool _isShowingLowHealthWarning = false;

        public void Initialize(BattleUnit unit)
        {
            if (unit == null)
            {
                GameLogger.Error("EnemyStatsUI: 초기화할 유닛이 null입니다!", LogCategory.Battle);
                return;
            }

            _targetUnit = unit;
            SetupUI();
        }

        private void SetupUI()
        {
            SetupNameText();
            SetupHealthBar();
            SetupShieldIcons();

            SubscribeToUnitEvents();
            UpdateAllUI();

            _isInitialized = true;
        }

        #region UniTask Methods

        private void SetupNameText()
        {
            if (_enemyNameText != null)
            {
                _enemyNameText.text = _targetUnit.UnitName;
            }
        }

        private void SetupHealthBar()
        {
            _healthBarFill.fillAmount = 1f;
            _healthBarFill.color = _healthFullColor;
        }

        private void SetupShieldIcons()
        {
            if (_shieldContainer == null || _shieldIconPrefab == null)
            {
                GameLogger.Warning("Shield Container 또는 Icon Prefab이 설정되지 않았습니다!", LogCategory.Battle);
                return;
            }

            // 기초 아이콘 정리
            ClearShieldIcons();

            int maxShield = _targetUnit.MaxShield;
            for (int i = 0; i < maxShield; i++)
            {
                var iconObj = Instantiate(_shieldIconPrefab, _shieldContainer);
                var iconImage = iconObj.GetComponent<Image>();
                _shieldIcons.Add(iconImage);
            }
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToUnitEvents()
        {
            if (_targetUnit == null)
            {
                return;
            }

            _targetUnit.OnHealthChanged += OnHealthChanged;
            _targetUnit.OnShieldChanged += OnShieldChanged;
            _targetUnit.OnUnitBroken += OnUnitBroken;
            _targetUnit.OnUnitRecovered += OnUnitRecovered;
            _targetUnit.OnUnitDefeated += OnUnitDefeated;
            _targetUnit.OnShieldDamaged += OnShieldDamaged;
        }

        private void UnsubscribeToUnitEvents()
        {
            if (_targetUnit == null)
            {
                return;
            }

            _targetUnit.OnHealthChanged -= OnHealthChanged;
            _targetUnit.OnShieldChanged -= OnShieldChanged;
            _targetUnit.OnUnitBroken -= OnUnitBroken;
            _targetUnit.OnUnitRecovered -= OnUnitRecovered;
            _targetUnit.OnUnitDefeated -= OnUnitDefeated;
            _targetUnit.OnShieldDamaged -= OnShieldDamaged;
        }

        #endregion

        #region Event Handlers

        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            if (!_isInitialized)
            {
                return;
            }

            UpdateHealthDisplayAnimated(currentHealth, maxHealth).Forget();
        }

        private void OnShieldChanged(int previousShield, int currentShield)
        {
            if (!_isInitialized)
            {
                return;
            }

            UpdateShieldDisplayAnimated(currentShield).Forget();
        }

        private void OnUnitBroken(BattleUnit obj)
        {
        }

        private void OnUnitRecovered(BattleUnit unit)
        {
        }

        private void OnUnitDefeated(BattleUnit obj)
        {
        }

        private void OnShieldDamaged(BattleUnit unit, int amount)
        {
        }

        #endregion

        #region UI Update Methods

        private void UpdateAllUI()
        {
            if (_targetUnit == null)
            {
                return;
            }

            UpdateHealthDisplay(_targetUnit.Stats.MaxHealth, _targetUnit.Stats.MaxHealth);
        }

        private void UpdateHealthDisplay(int currentHealth, int maxHealth)
        {
            if (_healthBarFill != null)
            {
                _healthBarFill.fillAmount = (float)currentHealth / maxHealth;
                UpdateHealthBarColor(currentHealth, maxHealth);
            }
        }

        #endregion

        #region Health Bar Animation Methods

        private async UniTaskVoid UpdateHealthDisplayAnimated(int currentHealth, int maxHealth)
        {
            if (_healthBarFill == null)
            {
                return;
            }

            _healthAnimation?.Kill();

            var targetFillAmount = (float)currentHealth / maxHealth;
            var currentFillAmount = _healthBarFill.fillAmount;

            bool tookDamage = targetFillAmount < currentFillAmount;
            var sequence = DOTween.Sequence();

            // 데미지를 받았으면 흔들림 효과
            if (tookDamage)
            {
                sequence.Append(AnimateHealthBarShake());
            }

            // FillAmount 애니메이션
            sequence.Append(_healthBarFill.DOFillAmount(targetFillAmount, _healthAnimationDuration)
                .SetEase(_healthEase));

            // 색상 변화 애니메이션
            sequence.Join(AnimateHealthBarColor(currentHealth, maxHealth).SetEase(_healthEase));

            _healthAnimation = sequence;
            await sequence.ToUniTask();

            CheckLowHealthWarning(targetFillAmount);
        }

        private Tween AnimateHealthBarShake()
        {
            var originalPos = _healthBarContainer.transform.position;

            return _healthBarContainer.DOShakePosition(_shakeDuration, _shakeStrength)
                .OnComplete(() =>
                {
                    _healthBarContainer.transform.position = originalPos;
                });
        }

        private Tween AnimateHealthBarColor(int currentHealth, int maxHealth)
        {
            var targetColor = GetHealthColor((float)currentHealth / maxHealth);
            return _healthBarFill.DOColor(targetColor, _healthAnimationDuration);
        }

        private void UpdateHealthBarColor(int currentHealth, int maxHealth)
        {
            if (_healthBarFill != null)
            {
                var percentage = (float)currentHealth / maxHealth;
                _healthBarFill.color = GetHealthColor(percentage);
            }
        }

        private Color GetHealthColor(float percentage)
        {
            if (percentage > 0.6f)
            {
                return _healthFullColor;
            }
            else if (percentage > 0.3f)
            {
                return _healthMediumColor;
            }
            else
            {
                return _healthLowColor;
            }
        }

        private void CheckLowHealthWarning(float percentage)
        {
            if (!_enableLowHealthWarning)
            {
                return;
            }

            bool shouldWarning = percentage <= _warningThreshold;
            if (shouldWarning && !_isShowingLowHealthWarning)
            {
                StartLowHealthWarning();
            }
            else if (!shouldWarning && _isShowingLowHealthWarning)
            {
                StopLowHealthWarning();
            }
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

            GameLogger.Debug(ZString.Concat(_targetUnit.UnitName, " 낮은 체력 경고 시작"), LogCategory.Battle);
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

            GameLogger.Debug(ZString.Concat(_targetUnit.UnitName, " 낮은 체력 경고 중단"), LogCategory.Battle);
        }

        #endregion

        #region Shield Animation Methods

        private async UniTask UpdateShieldDisplayAnimated(int targetShieldCount)
        {
            _currentAnimation?.Kill();

            int currentCount = _shieldIcons.Count;
            if (targetShieldCount > currentCount)
            {
                await AnimateShieldCreation(targetShieldCount - currentCount);
            }
            else if (targetShieldCount < currentCount)
            {
                await AnimateShieldDestruction(currentCount - targetShieldCount);
            }
        }

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

            _currentAnimation = sequence;
            await sequence.ToUniTask();

            GameLogger.Debug(ZString.Concat(_targetUnit.UnitName, " 실드 생성 애니메이션 완료"), LogCategory.Battle);
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

            _currentAnimation = sequence;
            await sequence.ToUniTask();

            GameLogger.Debug(ZString.Concat(_targetUnit.UnitName, " 실드 파괴 애니메이션 완료"), LogCategory.Battle);
        }

        #endregion

        #region Cleanup

        private void ClearShieldIcons()
        {
            _currentAnimation?.Kill();
            foreach (var icon in _shieldIcons)
            {
                if (icon != null)
                {
                    DestroyImmediate(icon.gameObject);
                }
            }

            _shieldIcons.Clear();
        }

        private void OnDestroy()
        {
            _currentAnimation?.Kill();
            _breakAnimation?.Kill();
            _healthAnimation?.Kill();
            _lowHealthWarningAnimation?.Kill();

            UnsubscribeToUnitEvents();
            ClearShieldIcons();
        }

        #endregion
    }
}
