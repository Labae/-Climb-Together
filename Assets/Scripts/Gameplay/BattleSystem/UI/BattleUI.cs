using System;
using System.Collections.Generic;
using Core.Utilities;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Data.BattleSystem.Enums;
using Debugging;
using Debugging.Enum;
using DG.Tweening;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.UI.Services;
using Gameplay.BattleSystem.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Services")] private ActionButtonService _actionButtonService;
        private TargetSelectionService _targetSelectionService;
        private BattleResultService _battleResultService;
        private EnemyStatusUIService _enemyStatusUIService;

        [Header("Action Buttons")] [SerializeField]
        private Transform _actionButtonContainer;

        [SerializeField] private Button[] _weaponButtons;

        [Header("Target Selection")] [SerializeField]
        private Transform _targetSelectionPanel;

        [SerializeField] private Transform _targetButtonsContainer;
        [SerializeField] private GameObject _targetButtonPrefab;

        [Header("Battle Result")] [SerializeField]
        private Transform _battleResultContainer;

        [SerializeField] private TextMeshProUGUI _battleResultText;
        [SerializeField] private TextMeshProUGUI _battleResultSubText;

        [Header("Enemy Status UI")] [SerializeField]
        private Transform _enemyStatsContainer;

        [SerializeField] private GameObject _enemyStatsUIPrefab;

        public event Action<WeaponType> OnAttackButtonClicked;
        public event Action<EnemyUnit, WeaponType> OnTargetSelected;

        public void Initialize()
        {
            InitializeServices();
            SetupServiceEvents();
        }

        private void InitializeServices()
        {
            _actionButtonService = new ActionButtonService(_actionButtonContainer, _weaponButtons);
            _targetSelectionService =
                new TargetSelectionService(_targetSelectionPanel, _targetButtonsContainer, _targetButtonPrefab);
            _battleResultService =
                new BattleResultService(_battleResultContainer, _battleResultText, _battleResultSubText);
            _enemyStatusUIService = new EnemyStatusUIService(_enemyStatsContainer, _enemyStatsUIPrefab);

            _actionButtonService.HideButtons();
            _targetSelectionService.HideTargetSelection();
            _battleResultService.HideBattleResult();
        }

        private void SetupServiceEvents()
        {
            _actionButtonService.OnWeaponSelected += (weaponType) => OnAttackButtonClicked?.Invoke(weaponType);
            _targetSelectionService.OnTargetSelected +=
                (enemy, weaponType) => OnTargetSelected?.Invoke(enemy, weaponType);
        }

        public void ShowActionButtons() => _actionButtonService.ShowButtons();
        public void HideActionButtons() => _actionButtonService.HideButtons();

        public void ShowTargetSelection(List<EnemyUnit> enemyUnits, WeaponType weaponType)
            => _targetSelectionService.ShowTargetSelection(enemyUnits, weaponType);

        public void ShowBattleResult(BattleUnit winner)
            => _battleResultService.ShowBattleResult(winner);

        public async UniTask SetupEnemyStats(List<EnemyUnit> enemyUnits)
            => await _enemyStatusUIService.SetupAsync(enemyUnits);
    }
}
