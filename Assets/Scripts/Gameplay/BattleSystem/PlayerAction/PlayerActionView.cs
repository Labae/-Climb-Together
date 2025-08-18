using System;
using System.Collections.Generic;
using Core.Utilities;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using DG.Tweening;
using Gameplay.BattleSystem.UI.Base;
using Systems.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.PlayerAction
{
    public class PlayerActionView : BaseView
    {
        [Header("Container")] [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Button Prefab")] [SerializeField]
        private Transform _buttonContainer;

        [SerializeField] private Button _buttonPrefab;

        [Header("Animation Settings")] [SerializeField]
        private float _fadeInDuration = 0.3f;

        [SerializeField] private float _fadeOutDuration = 0.2f;

        [SerializeField] private float _buttonAnimDelay = 0.05f;

        [Header("Pool Settings")] [SerializeField]
        private int _initialPoolSize = 5;

        // Components
        private UIObjectPool<Button> _buttonPool;
        private readonly Dictionary<WeaponData, WeaponButton> _activeButtons = new();
        private Sequence _currentAnimation;

        // Events
        public event Action<WeaponData> OnWeaponButtonClicked;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _buttonPool = new UIObjectPool<Button>(_buttonPrefab, _buttonContainer, _initialPoolSize);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        protected override void ValidateComponents()
        {
            if (_canvasGroup == null)
            {
                GameLogger.Error(ZString.Format("[{0}] CanvasGroup is not set!", name), LogCategory.UI);
            }
            if (_buttonContainer == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Button Container is not set!", name), LogCategory.UI);
            }
            if (_buttonPrefab == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Button Prefab is not set!", name), LogCategory.UI);
            }
        }

        #region Public Methods

        public void UpdateWeaponButtons(List<WeaponData> weapons)
        {
            ClearWeaponButtons();

            if (weapons is not { Count: > 0 })
            {
                GameLogger.Warning("표시할 무기가 없습니다", LogCategory.UI);
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                CreateWeaponButton(weapons[i], i);
            }
        }

        public void ShowButtons(bool animated = false)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            if (animated)
            {
                ShowButtonsAnimated().Forget();
            }
            else
            {
                _canvasGroup.alpha = 1;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        public void HideButtons(bool animated = false)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            if (animated)
            {
                HideButtonsAnimated().Forget();
            }
            else
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void SetButtonsInteractable(bool interactable)
        {
            foreach (var weaponButton in _activeButtons.Values)
            {
                if (weaponButton.Button != null)
                {
                    weaponButton.Button.interactable = interactable;
                }
            }
        }

        #endregion

        #region Animation Methods

        private async UniTask ShowButtonsAnimated()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _currentAnimation?.Kill();

            var sequence = DOTween.Sequence();

            _canvasGroup.alpha = 0;
            sequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration));

            int index = 0;
            foreach (var weaponButton in _activeButtons.Values)
            {
                var button = weaponButton.Button;
                var rectTransform = button.GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.zero;
                    sequence.Insert(index * _buttonAnimDelay,
                            rectTransform.DOScale(Vector3.one, _fadeInDuration))
                        .SetEase(Ease.OutBack);
                }

                index++;
            }

            sequence.OnStart(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            });

            _currentAnimation = sequence;
            await sequence.ToUniTask();
        }

        private async UniTask HideButtonsAnimated()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _currentAnimation?.Kill();

            var sequence = DOTween.Sequence();

            int index = 0;
            foreach (var weaponButton in _activeButtons.Values)
            {
                var button = weaponButton.Button;
                var rectTransform = button.GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one;
                    sequence.Insert(index * _buttonAnimDelay * 0.5f,
                            rectTransform.DOScale(Vector3.zero, _fadeOutDuration))
                        .SetEase(Ease.InBack);
                }

                index++;
            }

            sequence.Append(_canvasGroup.DOFade(0f, _fadeOutDuration));

            sequence.OnStart(() =>
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            });

            _currentAnimation = sequence;
            await sequence.ToUniTask();
        }

        public void HighlightSelectedWeapon(WeaponData weapon)
        {
            foreach (var kvp in _activeButtons)
            {
                var button = kvp.Value.Button;
                if (button == null)
                {
                    continue;
                }

                bool isSelected = kvp.Key == weapon;

                var colors = kvp.Value.Button.colors;
                colors.normalColor = isSelected ?
                    new Color(0.7f, 1f, 0.7f, 1.0f) :
                    Color.white;
                kvp.Value.Button.colors = colors;

                if (!isSelected)
                {
                    continue;
                }

                var rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    continue;
                }

                var sequence = DOTween.Sequence();
                sequence.Append(rectTransform.DOScale(1.1f, 0.2f));
                sequence.Append(rectTransform.DOScale(1f, 0.2f));
                sequence.Join(button.GetComponent<Image>().DOColor(new Color(1f, 1f, 0.8f), 0.2f)
                    .SetLoops(2, LoopType.Yoyo));
            }
        }

        #endregion

        #region Private Methods

        private void CreateWeaponButton(WeaponData weapon, int index)
        {
            var button = _buttonPool.Get();

            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = weapon.WeaponName;
            }

            var iconImage = CoreHelpers.FindComponentInChildren<Image>(button.transform);
            if (iconImage != null && weapon.WeaponIcon != null)
            {
                iconImage.sprite = weapon.WeaponIcon;
                iconImage.enabled = true;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked(weapon));
            _activeButtons[weapon] = new WeaponButton(button, weapon, index);
        }

        private void OnButtonClicked(WeaponData weapon)
        {
            AnimateButtonClickAndNotify(weapon).Forget();
        }

        private async UniTaskVoid AnimateButtonClickAndNotify(WeaponData weapon)
        {
            if (!_activeButtons.TryGetValue(weapon, out var weaponButton))
            {
                return;
            }

            var rectTransform = weaponButton.Button.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.DOKill();
            await rectTransform.DOPunchScale(Vector3.one * -0.1f, 0.2f, 2)
                .SetEase(Ease.OutQuad);

            OnWeaponButtonClicked?.Invoke(weapon);
        }


        private void ClearWeaponButtons()
        {
            foreach (var weaponButton in _activeButtons.Values)
            {
                if (weaponButton.Button == null)
                {
                    continue;
                }

                weaponButton.Button.onClick.RemoveAllListeners();
                _buttonPool.Return(weaponButton.Button);
            }

            _activeButtons.Clear();
        }

        protected override void HandleDestruction()
        {
            _currentAnimation?.Kill();

            foreach (var weaponButton in _activeButtons.Values)
            {
                if (weaponButton.Button == null)
                {
                    continue;
                }

                var rectTransform = weaponButton.Button.GetComponent<RectTransform>();
                rectTransform?.DOKill();
            }

            ClearWeaponButtons();
            base.HandleDestruction();
        }

        #endregion

        private class WeaponButton
        {
            public Button Button { get; }
            public WeaponData WeaponData { get; }
            public int Index { get; }

            public WeaponButton(Button button, WeaponData weaponData, int index)
            {
                Button = button;
                WeaponData = weaponData;
                Index = index;
            }
        }
    }
}
