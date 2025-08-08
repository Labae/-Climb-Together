using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Interfaces;
using Systems.StateMachine.Interfaces;

namespace Gameplay.BattleSystem.States
{
    public class BattleStartState : StateBase<BattleState>
    {
        private readonly IBattleManager _battleManager;

        public BattleStartState(IBattleManager battleManager)
        {
            _battleManager = battleManager;
        }

        public override BattleState StateType => BattleState.BattleStart;

        public override void OnEnter()
        {
            GameLogger.Info("전투 시작!", LogCategory.Battle);

            ChangeState(BattleState.PlayerTurn);
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
