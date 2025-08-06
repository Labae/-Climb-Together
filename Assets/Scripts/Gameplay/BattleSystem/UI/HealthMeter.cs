using System;
using Gameplay.BattleSystem.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    public class HealthMeter : MonoBehaviour
    {
        [SerializeField] private Image _healthBar;

        [SerializeField]
        private BattleUnit _battleUnit;

        private void Start()
        {
            _battleUnit ??= GetComponentInParent<BattleUnit>();
            _battleUnit.OnHealthChanged += UpdateMeter;
        }

        private void OnDestroy()
        {
            if (_battleUnit != null)
            {
              _battleUnit.OnHealthChanged -= UpdateMeter;
            }
        }

        private void UpdateMeter(int health, int maxHealth)
        {
            _healthBar.fillAmount = (float)health / maxHealth;;
        }
    }
}
