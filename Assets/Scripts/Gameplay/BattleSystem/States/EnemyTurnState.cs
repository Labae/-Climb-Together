using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core;
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
        private readonly BattleUI  _battleUI;

        private float _minTurnDelay = 1.2f;
        private float _maxTurnDelay = 1.7f;
        private float _timer;

        public EnemyTurnState(IBattleManager battleManager, BattleUI battleUI)
        {
            _battleManager = battleManager;
            _battleUI = battleUI;
        }

        public override BattleState StateType => BattleState.EnemyTurn;

        public override void OnEnter()
        {
            GameLogger.Info("적 턴!", LogCategory.Battle);
            _battleUI.HideActionButtons();
            _timer = Random.Range(_minTurnDelay, _maxTurnDelay);
        }

        public override void OnUpdate()
        {
            _timer -= Time.deltaTime;
            if (_timer < 0)
            {
                _battleManager.ExecuteEnemyAction();
            }
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
