using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Interfaces;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.BattleSystem.States
{
    public class EnemyTurnTransitionState : StateBase<BattleState>
    {
        private readonly IBattleManager _battleManager;
        private readonly float _delay = 0.1f;
        private float _timer;

        public override BattleState StateType => BattleState.EnemyTurnTransition;

        public EnemyTurnTransitionState(IBattleManager battleManager)
        {
            _battleManager = battleManager;
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

            if (_battleManager != null)
            {
                if (_battleManager.HasMoreEnemyTurns())
                {
                    GameLogger.Debug("EnemyTurnTransition: 다음 적 턴으로 이동", LogCategory.Battle);
                    ChangeState(BattleState.EnemyTurn);
                }
                else
                {
                    GameLogger.Debug("EnemyTurnTransition: 모든 적 턴 완료, 플레이어 턴으로 이동", LogCategory.Battle);
                    _battleManager.ResetEnemyTurnIndex();  // 인덱스 리셋
                    ChangeState(BattleState.PlayerTurn);
                }
            }
            else
            {
                GameLogger.Error("EnemyTurnTransition: BattleManager를 찾을 수 없음", LogCategory.Battle);
                ChangeState(BattleState.PlayerTurn);  // 안전을 위해 플레이어 턴으로
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
