using System;
using System.Collections.Generic;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using DG.Tweening;
using Gameplay.BattleSystem.UI.Base;
using Gameplay.BattleSystem.Units;
using Systems.UI.Core;
using TMPro;
using UnityEngine;

namespace Gameplay.BattleSystem.TargetSelection
{
    public class TargetSelectionView : BaseView
    {
        [Header("Container")] [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Help Panel")] [SerializeField]
        private RectTransform _helpPanel;

        [SerializeField] private TextMeshProUGUI _helpText;
        [SerializeField] private TextMeshProUGUI _targetInfoText;

        [Header("Indicator Prefab")] [SerializeField]
        private TargetIndicator _indicatorPrefab;

        [SerializeField] private RectTransform _indicatorContainer;
        [SerializeField] private int _initialPoolSize = 5;

        [Header("Visual Settings")] [SerializeField]
        private Color _normalIndicatorColor = new Color(1f, 0.6f, 0.2f);

        [SerializeField] private Color _weaknessIndicatorColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private float _indicatorOffset = 100f;

        [Header("Animation Settings")] [SerializeField]
        private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [SerializeField] private float _indicatorPulseSpeed = 1f;
        [SerializeField] private float _indicatorShowDelay = 0.05f;

        private UIObjectPool<TargetIndicator> _indicatorPool;
        private readonly Dictionary<EnemyUnit, TargetIndicator> _activeIndicators = new();
        private TargetIndicator _currentIndicator;
        private Sequence _currentAnimation;

        public event Action<int> OnNavigationInput;
        public event Action OnConfirmInput;
        public event Action OnCancelInput;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_helpPanel != null)
            {
                _helpPanel.gameObject.SetActive(false);
            }

            _indicatorPool = new UIObjectPool<TargetIndicator>(_indicatorPrefab, _indicatorContainer, _initialPoolSize);
        }

        protected override void ValidateComponents()
        {
            if (_canvasGroup == null)
            {
                GameLogger.Error(ZString.Format("[{0}] CanvasGroup is not set!", name), LogCategory.UI);
            }

            if (_helpPanel == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Help Panel is not set!", name), LogCategory.UI);
            }

            if (_helpText == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Help Text is not set!", name), LogCategory.UI);
            }

            if (_targetInfoText == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Target Info Text is not set!", name), LogCategory.UI);
            }

            if (_indicatorPrefab == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Indicator Prefab is not set!", name), LogCategory.UI);
            }

            if (_indicatorContainer == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Indicator Container is not set!", name), LogCategory.UI);
            }
        }

        #region Public Mehtods

        public void ShowTargetSelection(List<EnemyUnit> targets, WeaponData weapon)
        {
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            ClearIndicators();

            foreach (var enemy in targets)
            {
                CreateIndicator(enemy);
            }

            if (_helpPanel != null)
            {
                _helpPanel.gameObject.SetActive(true);
                UpdateHelpText(weapon);
            }

            ShowUI().Forget();
        }

        public void HideTargetSelection()
        {
            HideUI().Forget();
            ClearIndicatorsDelayed().Forget();

            if (_helpPanel != null)
            {
                _helpPanel.gameObject.SetActive(false);
            }
        }

        public void UpdateSelectedTarget(EnemyUnit target, bool isWeakness)
        {
            if (_currentIndicator != null)
            {
                _currentIndicator.SetSelected(false, _normalIndicatorColor);
            }

            if (target != null && _activeIndicators.TryGetValue(target, out var indicator))
            {
                _currentIndicator = indicator;
                indicator.Setup(target.UnitName, isWeakness);
                indicator.SetSelected(true, isWeakness ? _weaknessIndicatorColor : _normalIndicatorColor);
                indicator.Show();

                UpdateTargetInfo(target,  isWeakness);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = enabled;
            }
        }

        #endregion

        #region Private Methods

        private void CreateIndicator(EnemyUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            var indicator = _indicatorPool.Get();

            indicator.Setup(unit.UnitName);
            indicator.UpdatePosition(unit.transform.position + Vector3.up * 2f, _indicatorOffset);
            indicator.Hide();

            _activeIndicators[unit] = indicator;
        }

        private void ClearIndicators()
        {
            foreach (var indicator in _activeIndicators.Values)
            {
                if (indicator != null)
                {
                    indicator.Hide();
                    _indicatorPool.Return(indicator);
                }
            }

            _activeIndicators.Clear();
            _currentIndicator = null;
        }

        private async UniTaskVoid ClearIndicatorsDelayed()
        {
            await UniTask.WaitForSeconds(_fadeInDuration + 0.1f);
            ClearIndicators();
        }

        private void UpdateHelpText(WeaponData weapon)
        {
            if (_helpText != null)
            {
                _helpText.text = ZString.Format("<- -> 타겟 변경 | Enter 공격 | ESC 취소\n무기: {0}",
                    weapon?.WeaponName ?? "Unknown");
            }
        }

        private void UpdateTargetInfo(EnemyUnit target, bool isWeakness)
        {
            if (_targetInfoText == null)
            {
                return;
            }

            var weaknessText = isWeakness ? "<color=#ff4444>[약점!]</color>" : "";
            _targetInfoText.text = ZString.Format("타겟: {0}{1}", target.UnitName, weaknessText);
        }

        #endregion

        #region Animation Methods

        private async UniTask ShowUI()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _currentAnimation?.Kill();

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            await _canvasGroup.DOFade(1f, _fadeInDuration);

            foreach (var kvp in _activeIndicators)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Show();
                    await UniTask.WaitForSeconds(_indicatorShowDelay);
                }
            }
        }

        private async UniTask HideUI()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _currentAnimation?.Kill();

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            foreach (var kvp in _activeIndicators)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Hide();
                }
            }
            await _canvasGroup.DOFade(0f, _fadeOutDuration);
        }

        #endregion

        protected override void HandleDestruction()
        {
            _currentAnimation?.Kill();
            ClearIndicators();
            base.HandleDestruction();
        }

        #region Input Handling

        private void Update()
        {
            if (_canvasGroup == null || !_canvasGroup.interactable)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnNavigationInput?.Invoke(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnNavigationInput?.Invoke(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                OnConfirmInput?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelInput?.Invoke();
            }
        }

        #endregion
    }
}
