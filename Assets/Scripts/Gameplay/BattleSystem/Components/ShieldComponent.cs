using System;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Interfaces;
using UnityEngine;

namespace Gameplay.BattleSystem.Components
{
    public class ShieldComponent : IShieldComponent
    {
        private int _maxShield;
        private int _currentShield;
        private int _breakDuration;
        private int _breakTurnsRemaining;
        private float _breakDamageMultiplier;
        private UnitState _currentState;

        public int CurrentShield => _currentShield;
        public int MaxShield => _maxShield;
        public UnitState CurrentState => _currentState;
        public bool IsBroken => _currentState == UnitState.Broken;
        public int BreakTurnsRemaining => _breakTurnsRemaining;
        public float BreakDamageMultiplier => _breakDamageMultiplier;

        public event Action<int, int> OnShieldChanged;
        public event Action OnUnitBroken;
        public event Action OnUnitRecovered;
        public event Action<int> OnShieldDamaged;

        public void Initialize(int maxShield, int breakDuration, float breakDamageMultipler)
        {
            if (maxShield <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxShield), maxShield, "MaxShield must be greater than zero.");
            }

            if (breakDuration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(breakDuration), breakDuration, "BreakDuration must be greater than zero.");
            }

            _maxShield = maxShield;
            _currentShield = maxShield;
            _breakDuration = breakDuration;
            _breakDamageMultiplier = breakDamageMultipler;
            _currentState = UnitState.Normal;
            _breakTurnsRemaining = 0;

            if (_maxShield > 0)
            {
                OnShieldChanged?.Invoke(_currentShield, _currentShield);
            }
        }

        public void DamageShield(int amount)
        {
            if (amount <= 0 || _maxShield == 0)
            {
                return;
            }

            if (_currentShield <= 0)
            {
                return;
            }

            int previousShield = _currentShield;
            _currentShield = Mathf.Max(0, _currentShield - amount);

            if (previousShield == _currentShield)
            {
                return;
            }

            OnShieldChanged?.Invoke(previousShield, _currentShield);
            OnShieldDamaged?.Invoke(amount);

            if (_currentShield <= 0 && _currentState == UnitState.Normal)
            {
                EnterBreakState();
            }
        }

        private void EnterBreakState()
        {
            _currentState = UnitState.Broken;
            _breakTurnsRemaining = _breakDuration;

            GameLogger.Info(ZString.Format("유닛 브레이크 상태! {0}턴간 무력화", _breakTurnsRemaining), LogCategory.Battle);

            OnUnitBroken?.Invoke();
        }

        public void ProcessBreakTurn()
        {
            if (_currentState != UnitState.Broken)
            {
                return;
            }

            _breakTurnsRemaining--;
            GameLogger.Info(ZString.Format("브레이크 남은 턴: {0}", _breakTurnsRemaining), LogCategory.Battle);

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

            GameLogger.Info("브레이크 해제! 실드 복구 완료", LogCategory.Battle);

            OnUnitRecovered?.Invoke();

            if (_maxShield > 0)
            {
                OnShieldChanged?.Invoke(0, _maxShield);
            }
        }

        public void ForceRecover()
        {
            if (_currentState == UnitState.Broken)
            {
                RecoverFromBreak();
            }
        }

        public override string ToString()
        {
            return ZString.Format("Shield: {0}/{1}, State: {2}, Break Turns: {3}",
                _currentShield, _maxShield, _currentState, _breakTurnsRemaining);
        }
    }
}
