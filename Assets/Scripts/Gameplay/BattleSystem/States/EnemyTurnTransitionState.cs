using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.UI;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.BattleSystem.States
{
    public class EnemyTurnTransitionState : StateBase<BattleState>
    {
        private readonly TurnService _turnService;
        private readonly float _delay = 0.1f;
        private float _timer;

        public override BattleState StateType => BattleState.EnemyTurnTransition;

        public EnemyTurnTransitionState(TurnService turnService)
        {
            _turnService = turnService;
        }

        public override void OnEnter()
        {
            GameLogger.Debug("EnemyTurnTransition: 다음 적 턴 확인 중...", LogCategory.Battle);
            _timer = _delay;
        }

        public override void OnUpdate()
        {
            _timer -= Time.deltaTime;
            if (_timer >= 0)
            {
                return;
            }

            if (_turnService.HasMoreEnemyTurns())
            {
                GameLogger.Debug("EnemyTurnTransition: 다음 적 턴으로 이동", LogCategory.Battle);
                ChangeState(BattleState.EnemyTurn);
            }
            else
            {
                GameLogger.Warning("EnemyTurnTransition: No more enemy turns but state was entered", LogCategory.Battle);
            }
            _timer = 0f;
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
            GameLogger.Debug("EnemyTurnTransition: 전환 상태 종료", LogCategory.Battle);
        }

        public override void Dispose()
        {
        }
    }
}
