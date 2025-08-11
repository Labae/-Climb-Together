using System;
using System.Collections.Generic;
using Cysharp.Text;
using Data.BattleSystem.Enums;
using Gameplay.BattleSystem.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Gameplay.BattleSystem.UI.Services
{
    /// <summary>
    /// 타겟 선택 UI 서비스
    /// </summary>
    public class TargetSelectionService
    {
        private readonly Transform _selectionPanel;

        private readonly Transform _targetButtonParent;
        private readonly GameObject _targetButtonPrefab;
        private readonly List<Button> _activeButtons = new();

        private WeaponType _currentWeaponType;

        public event Action<EnemyUnit, WeaponType> OnTargetSelected;

        public TargetSelectionService(
            Transform selectionPanel,
            Transform targetButtonParent,
            GameObject targetButtonPrefab
            )
        {
            _selectionPanel = selectionPanel;
            _targetButtonParent = targetButtonParent;
            _targetButtonPrefab = targetButtonPrefab;
        }

        public void ShowTargetSelection(List<EnemyUnit> availableTargets, WeaponType weaponType)
        {
            _currentWeaponType = weaponType;
            ClearButtons();

            foreach (var target in availableTargets)
            {
                if (target != null && target.Health.IsAlive)
                {
                    CreateTargetButton(target);
                }
            }

            _selectionPanel.gameObject.SetActive(true);
        }

        public void HideTargetSelection()
        {
            _selectionPanel.gameObject.SetActive(false);
            ClearButtons();
        }

        private void CreateTargetButton(EnemyUnit target)
        {
            var buttonObj = Object.Instantiate(_targetButtonPrefab, _targetButtonParent);

            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            var isWeakness = target.Weakness.IsWeaknessHit(_currentWeaponType);
            if (buttonText != null)
            {
                buttonText.text = isWeakness ? ZString.Concat(target.UnitName, "(약점!)") : target.UnitName;
            }

            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    OnTargetSelected?.Invoke(target, _currentWeaponType);
                    HideTargetSelection();
                });

                _activeButtons.Add(button);
            }
        }

        private void ClearButtons()
        {
            foreach (var button in _activeButtons)
            {
                if (button != null)
                {
                    Object.Destroy(button.gameObject);
                }
            }

            _activeButtons.Clear();
        }
    }
}
