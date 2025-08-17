using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data.WeaponSystem;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.UI.Services;
using Gameplay.BattleSystem.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField]
        private Transform _actionButtonContainer;
        [SerializeField]
        private Transform _weaponButtonParent;
        [SerializeField] private Button _weaponButtonPrefab;

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

        [Header("Enemy Status UI")]
        [SerializeField]
        private Transform _enemyStatsContainer;

        [SerializeField] private GameObject _enemyStatsUIPrefab;

        [Header("Turn Order UI")]
        [SerializeField] private CanvasGroup _turnOrderCanvasGroup;
        [SerializeField] private TextMeshProUGUI _turnOrderHeaderText;
        [SerializeField] private Image _currentTurnBackground;
        [SerializeField] private Image _currentTurnIcon;
        [SerializeField] private TextMeshProUGUI _currentTurnName;
        [SerializeField] private Transform _upcomingContainer;
        [SerializeField] private TurnSlot _turnSlotPrefab;

        // Services
        private ActionButtonService _actionButtonService;
        private TargetSelectionService _targetSelectionService;
        private BattleResultService _battleResultService;
        private EnemyStatusUIService _enemyStatusUIService;
        private TurnOrderUIService _turnOrderUIService;

        // Runtime
        private PlayerUnit _playerUnit;
        private TurnOrderService _turnOrderService;

        public event Action<WeaponData> OnAttackButtonClicked;
        public event Action<EnemyUnit, WeaponData> OnTargetSelected;

        public void Initialize(PlayerUnit playerUnit, TurnOrderService turnOrderService)
        {
            _playerUnit = playerUnit;
            _turnOrderService = turnOrderService;
            InitializeServices();
            SetupServiceEvents();

        }

        private void OnDestroy()
        {
            _actionButtonService?.Dispose();
            _turnOrderUIService?.Dispose();
        }

        private void InitializeServices()
        {
            _actionButtonService = new ActionButtonService(_actionButtonContainer, _weaponButtonParent, _weaponButtonPrefab);
            _targetSelectionService =
                new TargetSelectionService(_targetSelectionPanel, _targetButtonsContainer, _targetButtonPrefab);
            _battleResultService =
                new BattleResultService(_battleResultContainer, _battleResultText, _battleResultSubText);
            _enemyStatusUIService = new EnemyStatusUIService(_enemyStatsContainer, _enemyStatsUIPrefab);

            _turnOrderUIService = new TurnOrderUIService(
                _turnOrderCanvasGroup,
                _turnOrderHeaderText,
                _currentTurnBackground,
                _currentTurnIcon,
                _currentTurnName,
                _upcomingContainer,
                _turnSlotPrefab,
                _turnOrderService
            );

            _targetSelectionService.HideTargetSelection();
            _battleResultService.HideBattleResult();
            _actionButtonService.HideButtons();

            _actionButtonService.Initialize(_playerUnit.WeaponInventory);
            _turnOrderUIService.Initialize();
        }

        private void SetupServiceEvents()
        {
            _actionButtonService.OnWeaponSelected += (weaponType) => OnAttackButtonClicked?.Invoke(weaponType);
            _targetSelectionService.OnTargetSelected +=
                (enemy, weaponType) => OnTargetSelected?.Invoke(enemy, weaponType);
            _turnOrderService.OnTurnChanged += (_) => OnTurnChanged().Forget();
        }

        public void ShowActionButtons() => _actionButtonService.ShowButtons();
        public void HideActionButtons() => _actionButtonService.HideButtons();

        public void ShowTargetSelection(List<EnemyUnit> enemyUnits, WeaponData weapon)
            => _targetSelectionService.ShowTargetSelection(enemyUnits, weapon);

        public void ShowBattleResult(BattleUnit winner)
            => _battleResultService.ShowBattleResult(winner);

        public async UniTask SetupEnemyStats(List<EnemyUnit> enemyUnits)
            => await _enemyStatusUIService.SetupAsync(enemyUnits);

        private async UniTask OnTurnChanged()
        {
            if (_turnOrderUIService != null)
            {
                await _turnOrderUIService.AnimateTurnTransition();
            }
        }
    }
}
