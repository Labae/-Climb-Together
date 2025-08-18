using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Data.WeaponSystem;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.PlayerAction;
using Gameplay.BattleSystem.TurnOrder;
using Gameplay.BattleSystem.UI.Services;
using Gameplay.BattleSystem.Units;
using TMPro;
using UnityEngine;

namespace Gameplay.BattleSystem.UI
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Target Selection")]
        [SerializeField]
        private Transform _targetSelectionPanel;

        [SerializeField] private Transform _targetButtonsContainer;
        [SerializeField] private GameObject _targetButtonPrefab;

        [Header("Battle Result")]
        [SerializeField]
        private Transform _battleResultContainer;

        [SerializeField] private TextMeshProUGUI _battleResultText;
        [SerializeField] private TextMeshProUGUI _battleResultSubText;

        // Services
        private TargetSelectionService _targetSelectionService;
        private BattleResultService _battleResultService;
        private EnemyStatusUIService _enemyStatusUIService;

        // Runtime
        private PlayerUnit _playerUnit;

        public event Action<EnemyUnit, WeaponData> OnTargetSelected;

        public void Initialize(PlayerUnit playerUnit)
        {
            _playerUnit = playerUnit;

            InitializeServices();
            SetupServiceEvents();
        }

        private void InitializeServices()
        {
            _targetSelectionService =
                new TargetSelectionService(_targetSelectionPanel, _targetButtonsContainer, _targetButtonPrefab);
            _battleResultService =
                new BattleResultService(_battleResultContainer, _battleResultText, _battleResultSubText);

            _targetSelectionService.HideTargetSelection();
            _battleResultService.HideBattleResult();
        }

        private void SetupServiceEvents()
        {
            _targetSelectionService.OnTargetSelected +=
                (enemy, weaponType) => OnTargetSelected?.Invoke(enemy, weaponType);
        }

        public void ShowTargetSelection(List<EnemyUnit> enemyUnits, WeaponData weapon)
            => _targetSelectionService.ShowTargetSelection(enemyUnits, weapon);

        public void ShowBattleResult(BattleUnit winner)
            => _battleResultService.ShowBattleResult(winner);
    }
}
