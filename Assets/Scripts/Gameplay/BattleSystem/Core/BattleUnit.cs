using System;
using System.ComponentModel.DataAnnotations;
using Cysharp.Text;
using Data.BattleSystem.Configs;
using Gameplay.BattleSystem.Components;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Interfaces;
using Systems.EventBus;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.Core
{
    public abstract class BattleUnit : MonoBehaviour
    {
        [Header("Unit Configuration")]
        [SerializeField, Required] private UnitConfig _unitConfig;

        // Components
        public IHealthComponent Health { get; private set; }
        public IWeaknessComponent Weakness { get; private set; }
        public IShieldComponent Shield { get; private set; }
        public ICombatComponent Combat { get; private set; }
        public ITurnComponent Turn { get; private set; }

        public string UnitName => _unitConfig?.UnitName ?? "Unknown Unit";
        public BattleStats Stats => _unitConfig?.Stats;
        public UnitConfig UnitConfig => _unitConfig;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Health = new HealthComponent();
            Weakness = new WeaknessComponent();
            Shield = new ShieldComponent();
            Combat = new CombatComponent();
            Turn = new TurnComponent();

            Health.Initialize(_unitConfig.Stats.MaxHealth);
            Weakness.Initialize(_unitConfig.Weaknesses);
            Shield.Initialize(_unitConfig.MaxShield, _unitConfig.BreakDuration, _unitConfig.BreakDamageMultiplier);
            Combat.Initialize(_unitConfig.Stats);
            Turn.Initialize(_unitConfig.UnitName, Shield);
        }

        #region Debugging

        public string GetDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine(ZString.Format("=== {0} Debug Info ===", UnitName));
            sb.AppendLine(Health.ToString());
            sb.AppendLine(Shield.ToString());
            sb.AppendLine(Weakness.ToString());
            sb.AppendLine(Combat.ToString());
            sb.AppendLine(Turn.ToString());
            return sb.ToString();
        }

        #endregion
    }
}
