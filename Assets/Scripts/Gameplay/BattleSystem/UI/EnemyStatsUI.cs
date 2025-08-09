using System;
using System.Collections.Generic;
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

            UpdateShieldDisplay(currentShield);
        }

        private void OnUnitBroken(BattleUnit obj)
        {
        }

        private void OnUnitRecovered(BattleUnit unit)
        {
            UpdateShieldDisplay(unit.MaxShield);
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
            UpdateShieldDisplay(_targetUnit.CurrentShield);
        }

        private void UpdateHealthDisplay(int currentHealth, int maxHealth)
        {
            if (_healthBarFill != null)
            {
                _healthBarFill.fillAmount = (float)currentHealth / maxHealth;
            }
        }

        private void UpdateShieldDisplay(int currentShield)
        {
            // remove
            while (_shieldIcons.Count > currentShield)
            {
                int lastIndex = _shieldIcons.Count - 1;
                if (_shieldIcons[lastIndex] != null)
                {
                    DestroyImmediate(_shieldIcons[lastIndex].gameObject);
                }
                _shieldIcons.RemoveAt(lastIndex);
            }

            // add
            while (_shieldIcons.Count < currentShield)
            {
                var iconObj = Instantiate(_shieldIconPrefab, _shieldContainer);
                var iconImage = iconObj.GetComponent<Image>();
                _shieldIcons.Add(iconImage);
            }
        }

        #endregion

        #region Cleanup

        private void ClearShieldIcons()
        {
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
            UnsubscribeToUnitEvents();
            ClearShieldIcons();
        }

        #endregion
    }
}
