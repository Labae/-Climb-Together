using System;
using Gameplay.BattleSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] private GameObject _actionPanel;
        [SerializeField] private Button _attackButton;

        [SerializeField] private GameObject _battleResult;

        public event Action OnAttackButtonClicked;

        public void Initialize()
        {
            _battleResult.SetActive(false);
            HideActionButtons();
            SetupButtons();
        }

        private void OnDestroy()
        {
            if (_attackButton != null)
            {
                _attackButton.onClick.RemoveAllListeners();
            }
        }

        private void SetupButtons()
        {
            if (_attackButton != null)
            {
                _attackButton.onClick.AddListener(() => OnAttackButtonClicked?.Invoke());
            }
        }

        public void ShowActionButtons()
        {
            if (_actionPanel != null)
            {
                _actionPanel.SetActive(true);
            }
        }

        public void HideActionButtons()
        {
            if (_actionPanel != null)
            {
                _actionPanel.SetActive(false);
            }
        }

        public void ShowBattleResult(BattleUnit winner)
        {
            _battleResult.GetComponentInChildren<TextMeshProUGUI>().text = winner.UnitName;
            _battleResult.SetActive(true);
        }
    }
}
