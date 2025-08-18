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
        [Header("Battle Result")]
        [SerializeField]
        private Transform _battleResultContainer;

        [SerializeField] private TextMeshProUGUI _battleResultText;
        [SerializeField] private TextMeshProUGUI _battleResultSubText;

        // Services
        private BattleResultService _battleResultService;

        // Runtime
        private PlayerUnit _playerUnit;


        public void Initialize(PlayerUnit playerUnit)
        {
            _playerUnit = playerUnit;

            InitializeServices();
        }

        private void InitializeServices()
        {
            _battleResultService =
                new BattleResultService(_battleResultContainer, _battleResultText, _battleResultSubText);

            _battleResultService.HideBattleResult();
        }

        public void ShowBattleResult(BattleUnit winner)
            => _battleResultService.ShowBattleResult(winner);
    }
}
