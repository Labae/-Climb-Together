using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Systems.StateMachine.Interfaces;

namespace Gameplay.BattleSystem.States
{
    public class PlayerTurnState : StateBase<BattleState>
    {
        private readonly BattleManager _battleManager;

        public PlayerTurnState(BattleManager battleManager)
        {
            _battleManager = battleManager;
        }

        public override BattleState StateType => BattleState.PlayerTurn;

        public override void OnEnter()
        {
            GameLogger.Info("플레이어 턴!", LogCategory.Battle);
            _battleManager.ShowPlayerActions();
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
