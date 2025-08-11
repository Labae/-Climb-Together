using System;
using Data.BattleSystem.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI.Services
{
    /// <summary>
    /// Action Button 관리 서비스
    /// </summary>
    public class ActionButtonService
    {
        private readonly Transform _buttonContainer;
        private readonly Button[] _weaponButtons;

        public event Action<WeaponType> OnWeaponSelected;


        public ActionButtonService(Transform buttonContainer, Button[] weaponButtons)
        {
            _buttonContainer = buttonContainer;
            _weaponButtons = weaponButtons;

            SetupButtons();
        }

        private void SetupButtons()
        {
            // 무기별 버튼 설정
            if (_weaponButtons?.Length >= 3)
            {
                _weaponButtons[0].onClick.AddListener(() => OnWeaponSelected?.Invoke(WeaponType.Sword));
                _weaponButtons[1].onClick.AddListener(() => OnWeaponSelected?.Invoke(WeaponType.Bow));
                _weaponButtons[2].onClick.AddListener(() => OnWeaponSelected?.Invoke(WeaponType.Fire));
            }
        }

        public void ShowButtons() => _buttonContainer.gameObject.SetActive(true);
        public void HideButtons() => _buttonContainer.gameObject.SetActive(false);

        public void SetButtonInteractable(WeaponType weaponType, bool interactable)
        {
            var buttonIndex = (int)weaponType;
            if (buttonIndex < _weaponButtons.Length)
            {
                _weaponButtons[buttonIndex].interactable = interactable;
            }
        }
    }
}
