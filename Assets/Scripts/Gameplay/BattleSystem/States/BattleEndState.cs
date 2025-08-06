using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Systems.StateMachine.Interfaces;

namespace Gameplay.BattleSystem.States
{
    public class BattleEndState : StateBase<BattleState>
    {
        private readonly BattleManager _battleManager;


        public BattleEndState(BattleManager battleManager)
        {
            _battleManager = battleManager;
        }

        public override BattleState StateType => BattleState.BattleEnd;

        public override void OnEnter()
        {
            GameLogger.Info("전투 종료!", LogCategory.Battle);
            _battleManager.ShowBattleResult();
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
