using System;
using System.Linq;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Events;
using NaughtyAttributes;
using Systems.EventBus;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.Core
{
    public abstract class BattleUnit : MonoBehaviour
    {
        [Header("Unit Data")][SerializeField] private string _unitName = "Unit_";
        [SerializeField] private BattleStats _stats;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Weakness System")]
        [SerializeField]
        private WeaponType[] _weaknesses;

        [Header("Shield System")]
        [SerializeField]
        private int _maxShield = 3;

        [SerializeField] private int _breakDuration = 2;
        [SerializeField] private float _breakDamageMultiplier = 2f;


        [SerializeField, ReadOnly] private int _currentHealth;
        [SerializeField, ReadOnly] private int _currentShield;
        [SerializeField, ReadOnly] private UnitState _currentState = UnitState.Normal;
        [SerializeField, ReadOnly] private int _breakTurnsRemaining;

        [Inject] protected IEventBus _eventBus;

        public event Action<int, int> OnHealthChanged;
        public event Action<BattleUnit> OnUnitDefeated;

        public event Action<int, int> OnShieldChanged;
        public event Action<BattleUnit> OnUnitBroken;
        public event Action<BattleUnit> OnUnitRecovered;
        public event Action<BattleUnit, int> OnShieldDamaged;

        public string UnitName => _unitName;
        public bool IsAlive => _currentHealth > 0;
        public BattleStats Stats => _stats;

        // 약점
        public WeaponType[] Weaknesses => _weaknesses;

        // Shield & Break
        public int MaxShield => _maxShield;
        public int CurrentShield => _currentShield;
        public int BreakDuration => _breakDuration;
        public UnitState CurrentState => _currentState;
        public bool IsBroken => _currentState == UnitState.Broken;
        public int BreakTurnsRemaining => _breakTurnsRemaining;

        public float BreakDamageMultiplier => _breakDamageMultiplier;

        private void Awake()
        {
            InitializeUnit();
        }

        private void InitializeUnit()
        {
            _currentHealth = _stats.MaxHealth;
            _currentShield = _maxShield;
            _currentState = UnitState.Normal;
            _breakTurnsRemaining = 0;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            GameLogger.Debug(ZString.Format("{0} 초기화: HP {1}/{2}, Shield {3}/{4}",
                _unitName, _currentHealth, _stats.MaxHealth, _currentShield, _maxShield), LogCategory.Battle);
        }

        public void AttackTarget(BattleUnit targetUnit, WeaponType weaponType)
        {
            if (!IsAlive || targetUnit == null || !targetUnit.IsAlive)
            {
                return;
            }

            int damage = CalculateDamage(targetUnit, weaponType);
            bool isWeaknessHit = targetUnit.IsWeaknessHit(weaponType);

            // 실드 처리
            if (isWeaknessHit && targetUnit.CurrentShield > 0)
            {
                targetUnit.DamageShield(1);
                GameLogger.Debug(ZString.Format("{0}의 실드 파괴! 남은 실드: {1}",
                    targetUnit.UnitName, targetUnit.CurrentShield), LogCategory.Battle);

            }

            // 데미지 처리
            targetUnit.TakeDamage(damage);

            string hitType = isWeaknessHit ? "약점 공격" : "일반 공격";
            string breakStatus = targetUnit.IsBroken ? "(브레이크 상태)" : "";
            GameLogger.Debug(ZString.Format("{0}이(가) {1}에게 {2}로 {3} 데미지를 입혔습니다! ({4}){5}",
                _unitName, targetUnit.UnitName, weaponType, damage, hitType, breakStatus), LogCategory.Battle);

            // 공격 이벤트 발행
            _eventBus.Publish(new UnitAttackedEvent(this, targetUnit));
        }

        private int CalculateDamage(BattleUnit targetUnit, WeaponType weaponType)
        {
            int baseDamage = _stats.Attack - (targetUnit.Stats.Defense / 2);
            var calculatedDamage = baseDamage;
            if (targetUnit.IsWeaknessHit(weaponType))
            {
                calculatedDamage = Mathf.RoundToInt(calculatedDamage * 1.5f);
            }

            if (targetUnit.IsBroken)
            {
                calculatedDamage = Mathf.RoundToInt(calculatedDamage * targetUnit.BreakDamageMultiplier);
            }

            int finalDamage = Mathf.Max(1, calculatedDamage);
            return finalDamage;
        }

        public bool IsWeaknessHit(WeaponType weaponType)
        {
            if (_weaknesses == null || _weaknesses.Length == 0)
            {
                return false;
            }

            return _weaknesses.Any(weakness => weakness == weaponType);
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive)
            {
                return;
            }

            int actualDamage = Mathf.Max(1, damage);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);

            OnHealthChanged?.Invoke(_currentHealth, _stats.MaxHealth);

            if (!IsAlive)
            {
                OnUnitDefeated?.Invoke(this);
            }
        }

        #region Shield & Break System

        public void DamageShield(int amount)
        {
            if (_currentShield <= 0)
            {
                return;
            }

            int previousShield = _currentShield;
            _currentShield = Mathf.Max(0, _currentShield - amount);

            OnShieldChanged?.Invoke(previousShield, _currentShield);
            OnShieldDamaged?.Invoke(this, amount);

            GameLogger.Debug(ZString.Format("{0} 실드 데미지: {1} -> {2}", _unitName, previousShield, _currentShield), LogCategory.Battle);

            if (_currentShield <= 0 && _currentState == UnitState.Normal)
            {
                EnterBreakState();
            }
        }

        private void EnterBreakState()
        {
            _currentState = UnitState.Broken;
            _breakTurnsRemaining = _breakDuration;

            GameLogger.Info(ZString.Format("{0} 브레이크! {1}턴간 무력화", _unitName, _breakTurnsRemaining), LogCategory.Battle);

            OnUnitBroken?.Invoke(this);
        }

        private void ProcessBreakTurn()
        {
            if (_currentState != UnitState.Broken)
            {
                return;
            }

            _breakTurnsRemaining--;
            GameLogger.Info(ZString.Format("{0} 브레이크 남은 턴: {1}", _unitName, _breakTurnsRemaining), LogCategory.Battle);

            if (_breakTurnsRemaining <= 0)
            {
                RecoverFromBreak();
            }
        }

        private void RecoverFromBreak()
        {
            _currentState = UnitState.Normal;
            _currentShield = _maxShield;
            _breakTurnsRemaining = 0;

            GameLogger.Info(ZString.Format("{0} 브레이크 해제! 실드 복구 완료", _unitName), LogCategory.Battle);

            OnUnitRecovered?.Invoke(this);
            OnShieldChanged?.Invoke(_currentShield, _maxShield);
        }

        #endregion

        #region Turn Management

        public void OnTurnStart()
        {
            GameLogger.Debug(ZString.Format("{0} 턴 시작 - 상태: {1}, 실드: {2}/{3}"
                , _unitName, _currentState, _currentShield, _maxShield), LogCategory.Battle);
        }

        public void OnTurnEnd()
        {
            if (_currentState == UnitState.Broken)
            {
                ProcessBreakTurn();
            }

            GameLogger.Debug(ZString.Format("{0} 턴 종료", _unitName), LogCategory.Battle);
        }

        #endregion
    }
}
