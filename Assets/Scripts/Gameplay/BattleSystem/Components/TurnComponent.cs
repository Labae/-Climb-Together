using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Interfaces;

namespace Gameplay.BattleSystem.Components
{
    public class TurnComponent : ITurnComponent
    {
        private string _unitName;
        private IShieldComponent _shieldComponent;

        public void Initialize(string unitName, IShieldComponent shieldComponent)
        {
            _unitName = unitName ?? "Unknown Unit";
            _shieldComponent = shieldComponent;
        }

        public void OnTurnStart()
        {
            GameLogger.Debug(ZString.Format("{0} 턴 시작 - 상태: {1}, 실드: {2}/{3}"
                , _unitName, _shieldComponent?.CurrentState.ToString(),
                _shieldComponent?.CurrentShield ?? 0,
                _shieldComponent?.MaxShield ?? 0),
                LogCategory.Battle);
        }

        public void OnTurnEnd()
        {
            if (_shieldComponent is { IsBroken: true })
            {
                _shieldComponent.ProcessBreakTurn();
            }
            GameLogger.Debug(ZString.Format("{0} 턴 종료", _unitName), LogCategory.Battle);
        }
    }
}
