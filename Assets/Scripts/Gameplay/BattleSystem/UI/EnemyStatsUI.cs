using System;
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
        private Image _healthBarFill;

        [Header("Shield System")]
        [SerializeField] private RectTransform _shieldContainer;
        [SerializeField] private GameObject _shieldIconPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float _shieldAnimationDuration = 0.5f;
        [SerializeField] private float _shieldAnimationDelay = 0.5f;
        [SerializeField] private Ease _shieldPopEase = Ease.Linear;
        [SerializeField] private Ease _shieldDestroyEase = Ease.Linear;

        private BattleUnit _targetUnit;
        private readonly List<Image> _shieldIcons = new();
        private Sequence _currentAnimation;
        private Sequence _breakAnimation;

        private bool _isInitialized = false;

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

            UpdateHealthDisplay(currentHealth, maxHealth);
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
            }
        }

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

        #endregion

        #region Shield Animation Methods

        private async UniTask AnimateShieldCreation(int createCount)
        {
            var sequence = DOTween.Sequence();

            for (int i = 0; i < createCount; i++)
            {
                var iconObj = Instantiate(_shieldIconPrefab, _shieldContainer);
                var iconImage = iconObj.GetComponent<Image>();
                var iconTransform =  iconObj.transform;

                if (iconImage != null)
                {
                    _shieldIcons.Add(iconImage);

                    iconTransform.localScale = Vector3.zero;

                    sequence.Insert(i * _shieldAnimationDelay, iconTransform.DOScale(Vector3.one, _shieldAnimationDuration)).SetEase(_shieldPopEase);
                }
                else
                {
                    GameLogger.Warning("Shield Icon에 Image가 없습니다", LogCategory.Battle);
                    Destroy(iconObj.gameObject);
                }
            }

            _currentAnimation = sequence;
            await sequence.ToUniTask();

            GameLogger.Debug(ZString.Concat(_targetUnit.UnitName, " 실드 생성 애니메이션 완료"),  LogCategory.Battle);
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

                    sequence.Insert(i * _shieldAnimationDelay, iconToDestroy.transform.DOScale(Vector3.zero, _shieldAnimationDuration))
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

            GameLogger.Debug(ZString.Concat(_targetUnit.UnitName, " 실드 파괴 애니메이션 완료"),  LogCategory.Battle);
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

            UnsubscribeToUnitEvents();
            ClearShieldIcons();
        }

        #endregion
    }
}
