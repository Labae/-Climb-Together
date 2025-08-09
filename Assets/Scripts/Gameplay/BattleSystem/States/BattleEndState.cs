using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Interfaces;
using Gameplay.BattleSystem.UI;
using Systems.StateMachine.Interfaces;

namespace Gameplay.BattleSystem.States
{
    public class BattleEndState : StateBase<BattleState>
    {
        private readonly IBattleManager _battleManager;
        private readonly BattleUI _battleUI;

        public BattleEndState(IBattleManager battleManager, BattleUI battleUI)
        {
            _battleManager = battleManager;
            _battleUI = battleUI;
        }

        public override BattleState StateType => BattleState.BattleEnd;

        public override void OnEnter()
        {
            GameLogger.Info("전투 종료!", LogCategory.Battle);
            _battleUI.ShowBattleResult(_battleManager.Winner);
        }

        public override void OnUpdate()
        {

        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
        }

        public override void Dispose()
        {
        }
    }
}
