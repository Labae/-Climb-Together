using Gameplay.BattleSystem.Core;
using UnityEngine;

namespace Gameplay.BattleSystem.EnemyStatus
{
    public class EnemyStatusSetup : MonoBehaviour
    {
        [SerializeField] private BattleUnit _enemyUnit;
        [SerializeField] private EnemyStatusView _enemyStatusView;

        private EnemyStatusController _controller;

        private void Start()
        {
            _enemyUnit.Initialize();

            var model = EnemyStatusModelFactory.Create(_enemyUnit);

            _controller = new EnemyStatusController(model, _enemyStatusView);
            _controller.Initialize();
        }

        private void OnDestroy()
        {
            _controller?.Dispose();
        }
    }
}
