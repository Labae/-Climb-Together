using System;
using System.Collections.Generic;
using Core.Utilities;
using Data.WeaponSystem;
using Gameplay.BattleSystem.Player;
using Systems.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI.Services
{
    /// <summary>
    /// Action Button 관리 서비스
    /// </summary>
    public class ActionButtonService : IDisposable
    {
        private readonly Transform _buttonContainer;
        private readonly UIObjectPool<Button> _weaponButtonPool;
        private readonly List<WeaponButton> _activeWeaponButtons = new();

        private PlayerWeaponInventory _weaponInventory;

        public event Action<WeaponData> OnWeaponSelected;

        public ActionButtonService(Transform buttonContainer, Transform weaponButtonParent, Button weaponButtonPrefab, int initialPoolSize = 5)
        {
            _buttonContainer = buttonContainer;
            var buttonComponent = weaponButtonPrefab.GetComponent<Button>();
            if (buttonComponent == null)
            {
                throw new ArgumentException("Target button prefab must have a Button");
                return;
            }

            _weaponButtonPool = new UIObjectPool<Button>(buttonComponent, weaponButtonParent, initialPoolSize);
        }

        public void Initialize(PlayerWeaponInventory weaponInventory)
        {
            _weaponInventory = weaponInventory;
            if (_weaponInventory != null)
            {
                _weaponInventory.OnInventoryChanged += RefreshWeaponButtons;
            }

            RefreshWeaponButtons();
        }

        private void RefreshWeaponButtons()
        {
            ClearWeaponButtons();

            foreach (var weaponData in _weaponInventory.OwnedWeapons)
            {
                CreateWeaponButton(weaponData);
            }
        }

        private void CreateWeaponButton(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return;
            }

            var button = _weaponButtonPool.Get();
            SetupWeaponButton(button, weaponData);

            var weaponButton = new WeaponButton(button, weaponData);
            _activeWeaponButtons.Add(weaponButton);
        }

        private void SetupWeaponButton(Button button, WeaponData weaponData)
        {
            button.onClick.RemoveAllListeners();

            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = weaponData.WeaponName;
            }

            var buttonImage = CoreHelpers.FindComponentInChildren<Image>(button.transform);
            if (buttonImage != null)
            {
                buttonImage.sprite = weaponData.WeaponIcon;
            }

            button.onClick.AddListener(() =>
            {
                OnWeaponSelected?.Invoke(weaponData);
            });
        }

        private void ClearWeaponButtons()
        {
            foreach (var weaponButton in _activeWeaponButtons)
            {
                if (weaponButton.Button != null)
                {
                    weaponButton.Button.onClick.RemoveAllListeners();
                    _weaponButtonPool.Return(weaponButton.Button);
                }
            }

            _activeWeaponButtons.Clear();
        }

        public void ShowButtons() => _buttonContainer.gameObject.SetActive(true);
        public void HideButtons() => _buttonContainer.gameObject.SetActive(false);

        public void SetButtonInteractable(bool interactable)
        {
            foreach (var weaponButton in _activeWeaponButtons)
            {
                if (weaponButton.Button != null)
                {
                    weaponButton.Button.interactable = interactable;
                }
            }
        }

        public void Dispose()
        {
            if (_weaponInventory != null)
            {
                _weaponInventory.OnInventoryChanged -= RefreshWeaponButtons;
            }
        }

        private class WeaponButton
        {
            public Button Button { get; }
            public WeaponData WeaponData { get; }

            public WeaponButton(Button button, WeaponData weaponData)
            {
                Button = button;
                WeaponData = weaponData;
            }
        }
    }
}
