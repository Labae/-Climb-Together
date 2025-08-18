using System;
using System.Collections.Generic;
using Core.Utilities;
using Cysharp.Text;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
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
                // TODO : Add animation
                _canvasGroup.alpha = 1;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
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
                // TODO : Add animation
                _canvasGroup.alpha = 0;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
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

        public void HighlightSelectedWeapon(WeaponData weapon)
        {
            foreach (var kvp in _activeButtons)
            {
                if (kvp.Value.Button != null)
                {
                    bool isSelected = kvp.Key == weapon;

                    var colors = kvp.Value.Button.colors;
                    colors.normalColor = isSelected ?
                        new Color(0.7f, 1f, 0.7f, 1.0f) :
                        Color.white;
                    kvp.Value.Button.colors = colors;
                }
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
            OnWeaponButtonClicked?.Invoke(weapon);
        }

        private void ClearWeaponButtons()
        {
            foreach (var weaponButton in _activeButtons.Values)
            {
                if (weaponButton.Button != null)
                {
                    weaponButton.Button.onClick.RemoveAllListeners();
                    _buttonPool.Return(weaponButton.Button);
                }
            }

            _activeButtons.Clear();
        }

        protected override void HandleDestruction()
        {
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
