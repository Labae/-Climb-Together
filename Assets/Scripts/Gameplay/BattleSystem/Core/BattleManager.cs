using System;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.States;
using Gameplay.BattleSystem.UI;
using Systems.StateMachine;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.BattleSystem.Core
{
    public class BattleManager : MonoBehaviour
    {
        [SerializeField] private BattleUnit _playerUnit;
        [SerializeField] private BattleUnit _enemyUnit;

        [SerializeField] private BattleUI _battleUI;

        private IStateMachine<BattleState> _stateMachine;

        private BattleUnit _winner;

        private void Awake()
        {
            InitializeBattle();
        }

        private void OnDestroy()
        {
            CleanupBattle();
        }

        private void InitializeBattle()
        {
            if (_battleUI != null)
            {
                _battleUI.Initialize();
                _battleUI.OnAttackButtonClicked += OnPlayerAttack;
            }

            _stateMachine = new StateMachine<BattleState>();

            _stateMachine.AddState(new BattleStartState(this));
            _stateMachine.AddState(new PlayerTurnState(this));
            _stateMachine.AddState(new EnemyTurnState(this));
            _stateMachine.AddState(new BattleEndState(this));

            _stateMachine.TrySetInitialState(BattleState.BattleStart);

            _playerUnit.OnUnitDefeated += OnPlayerDefeated;
            _enemyUnit.OnUnitDefeated += OnEnemyDefeated;
        }

        private void CleanupBattle()
        {
            if (_playerUnit != null)
            {
                _playerUnit.OnUnitDefeated -= OnPlayerDefeated;
            }

            if (_enemyUnit != null)
            {
                _enemyUnit.OnUnitDefeated -= OnEnemyDefeated;
            }

            _stateMachine?.Dispose();
        }

        public void ShowPlayerActions()
        {
            if (_battleUI != null)
            {
                _battleUI.ShowActionButtons();
            }
        }

        public void HidePlayerActions()
        {
            if (_battleUI != null)
            {
                _battleUI.HideActionButtons();
            }
        }

        private void OnPlayerAttack()
        {
            if (_stateMachine.CurrentStateType.CurrentValue != BattleState.PlayerTurn)
            {
                return;
            }

            if (!_playerUnit.IsAlive || !_enemyUnit.IsAlive)
            {
                return;
            }

            _playerUnit.AttackTarget(_enemyUnit);

            if (_enemyUnit.IsAlive)
            {
                _stateMachine.ChangeState(BattleState.EnemyTurn);
            }
        }

        public void ExecuteEnemyAction()
        {
            if (_stateMachine.CurrentStateType.CurrentValue != BattleState.EnemyTurn)
            {
                return;
            }

            if (!_playerUnit.IsAlive || !_enemyUnit.IsAlive)
            {
                return;
            }

            _enemyUnit.AttackTarget(_playerUnit);

            if (_playerUnit.IsAlive)
            {
                _stateMachine.ChangeState(BattleState.PlayerTurn);
            }
        }

        public void ShowBattleResult()
        {
            _battleUI.ShowBattleResult(_winner);
        }

        private void OnEnemyDefeated(BattleUnit unit)
        {
            _winner = _playerUnit;
            _stateMachine.ChangeState(BattleState.BattleEnd);
        }

        private void OnPlayerDefeated(BattleUnit unit)
        {
            _winner = _enemyUnit;
            _stateMachine.ChangeState(BattleState.BattleEnd);
        }
    }
}
