using System;
using Cysharp.Text;
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
        [Header("Unit Data")] [SerializeField] private string _unitName = "Unit_";
        [SerializeField] private BattleStats _stats;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Weakness System")] [SerializeField]
        private WeaponType[] _weaknesses;

        [Header("Shield System")] [SerializeField]
        private int _maxShield = 3;

        [SerializeField] private int _breakDuration = 2;
        [SerializeField] private float _breakDamageMultiplier = 2f;

        [Inject] protected IEventBus _eventBus;

        // Components
        public IHealthComponent Health { get; private set; }
        public IWeaknessComponent Weakness { get; private set; }
        public IShieldComponent Shield { get; private set; }
        public ICombatComponent Combat { get; private set; }
        public ITurnComponent Turn { get; private set; }

        public string UnitName => _unitName;
        public BattleStats Stats => _stats;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            Health.Initialize(_stats.MaxHealth);
            Weakness.Initialize(_weaknesses);
            Shield.Initialize(_maxShield, _breakDuration, _breakDamageMultiplier);
            Combat.Initialize(_stats, _unitName, _eventBus);
            Turn.Initialize(_unitName, Shield);
        }

        private void InitializeComponents()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            Health = new HealthComponent();
            Weakness = new WeaknessComponent();
            Shield = new ShieldComponent();
            Combat = new CombatComponent();
            Turn = new TurnComponent();
        }

        #region Debugging

        public string GetDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine(ZString.Format("=== {0} Debug Info ===", _unitName));
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
