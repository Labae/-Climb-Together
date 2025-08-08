using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Interfaces;
using Gameplay.BattleSystem.UI;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.BattleSystem.States
{
    public class EnemyTurnState : StateBase<BattleState>
    {
        private readonly IBattleManager _battleManager;
        private readonly BattleUI _battleUI;

        private readonly float _minTurnDelay = 1.0f;
        private readonly float _maxTurnDelay = 1.5f;
        private float _timer;
        private bool _actionExecuted = false;

        public EnemyTurnState(IBattleManager battleManager, BattleUI battleUI)
        {
            _battleManager = battleManager;
            _battleUI = battleUI;
        }

        public override BattleState StateType => BattleState.EnemyTurn;

        public override void OnEnter()
        {
            // BattleManager에서 현재 적 정보 가져오기
            var currentEnemy = _battleManager.CurrentEnemy;

            string enemyName = currentEnemy?.UnitName ?? "적";
            GameLogger.Info($"🔥 {enemyName} 턴!", LogCategory.Battle);

            _battleUI.HideActionButtons();
            _timer = Random.Range(_minTurnDelay, _maxTurnDelay);
            _actionExecuted = false;
        }

        public override void OnUpdate()
        {
            if (_actionExecuted) return;

            _timer -= Time.deltaTime;
            if (_timer >= 0)
            {
                return;
            }

            _battleManager.ExecuteEnemyAction();
            _actionExecuted = true;
            _timer = 0f;
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
            _actionExecuted = false;  // 다음 턴을 위해 리셋
        }

        public override void Dispose()
        {
        }
    }
}
