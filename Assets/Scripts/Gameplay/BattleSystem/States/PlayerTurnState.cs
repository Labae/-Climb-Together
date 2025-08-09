using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.UI;
using Systems.StateMachine.Interfaces;

namespace Gameplay.BattleSystem.States
{
    public class PlayerTurnState : StateBase<BattleState>
    {
        private readonly BattleUI _battleUI;
        public PlayerTurnState(BattleUI battleUI)
        {
            _battleUI = battleUI;
        }

        public override BattleState StateType => BattleState.PlayerTurn;

        public override void OnEnter()
        {
            GameLogger.Info("플레이어 턴!", LogCategory.Battle);
            _battleUI.ShowActionButtons();
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
