using Cysharp.Text;
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
        private readonly float _breakTurnDelay = 0.8f;
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

            if (currentEnemy != null)
            {
                var stateInfo = currentEnemy.IsBroken
                    ? ZString.Format(" (브레이크 {0}턴 남음)", currentEnemy.BreakTurnsRemaining)
                    : "";
                GameLogger.Info(ZString.Format("{0}턴 시작!{1}", currentEnemy.UnitName, stateInfo), LogCategory.Battle);
                _timer = currentEnemy.IsBroken ? _breakTurnDelay : Random.Range(_minTurnDelay, _maxTurnDelay);
            }
            else
            {
                GameLogger.Warning("현재 적이 Null입니다!", LogCategory.Battle);
                _timer = _minTurnDelay;
            }

            _battleUI.HideActionButtons();
            _actionExecuted = false;
            GameLogger.Debug(ZString.Format("적 턴 대기시간: {0}", _timer), LogCategory.Battle);
        }

        public override void OnUpdate()
        {
            if (_actionExecuted) return;

            _timer -= Time.deltaTime;
            if (_timer >= 0)
            {
                return;
            }

            GameLogger.Debug("적 행동 실행", LogCategory.Battle);
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
