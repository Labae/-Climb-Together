using System;
using System.Collections.Generic;
using Cysharp.Text;
using Data.BattleSystem.Enums;
using Gameplay.BattleSystem.Units;
using Systems.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI.Services
{
    /// <summary>
    /// 타겟 선택 UI 서비스
    /// </summary>
    public class TargetSelectionService
    {
        private readonly Transform _selectionPanel;

        private readonly UIObjectPool<Button> _targetButtonPool;
        private readonly List<Button> _activeButtons = new();

        private WeaponType _currentWeaponType;

        public event Action<EnemyUnit, WeaponType> OnTargetSelected;

        public TargetSelectionService(
            Transform selectionPanel,
            Transform targetButtonParent,
            GameObject targetButtonPrefab,
            int initialPoolSize = 5
            )
        {
            _selectionPanel = selectionPanel;

            var buttonComponent = targetButtonPrefab.GetComponent<Button>();
            if (buttonComponent == null)
            {
                throw new ArgumentException("Target button prefab must have a Button");
                return;
            }

            _targetButtonPool = new UIObjectPool<Button>(buttonComponent, targetButtonParent, initialPoolSize);
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
            var button = _targetButtonPool.Get();

            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            var isWeakness = target.Weakness.IsWeaknessHit(_currentWeaponType);
            if (buttonText != null)
            {
                buttonText.text = isWeakness ? ZString.Concat(target.UnitName, "(약점!)") : target.UnitName;
            }

            button.onClick.AddListener(() =>
            {
                OnTargetSelected?.Invoke(target, _currentWeaponType);
                HideTargetSelection();
            });

            _activeButtons.Add(button);
        }

        private void ClearButtons()
        {
            foreach (var button in _activeButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    _targetButtonPool.Return(button);
                }
            }

            _activeButtons.Clear();
        }
    }
}
