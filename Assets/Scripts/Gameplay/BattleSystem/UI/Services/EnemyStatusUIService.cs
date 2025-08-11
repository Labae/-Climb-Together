using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.BattleSystem.Units;
using UnityEngine;

namespace Gameplay.BattleSystem.UI.Services
{
    /// <summary>
    /// 적 상태 UI 관리 서비스
    /// </summary>
    public class EnemyStatusUIService
    {
        private readonly Transform _container;
        private readonly GameObject _statusUIPrefab;
        private readonly List<EnemyStatsUI> _statusUIs = new();

        public EnemyStatusUIService(Transform container, GameObject statusUIPrefab)
        {
            _container = container;
            _statusUIPrefab = statusUIPrefab;
        }

        public async UniTask SetupAsync(List<EnemyUnit> enemies)
        {
            ClearStatusUI();

            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    await CreateStatusUI(enemy);
                }
            }
        }

        private async UniTask CreateStatusUI(EnemyUnit enemy)
        {
            var uiObject = Object.Instantiate(_statusUIPrefab, _container);
            var statusUI = uiObject.GetComponent<EnemyStatsUI>();

            if (statusUI != null)
            {
                statusUI.Initialize(enemy);
                _statusUIs.Add(statusUI);
            }

            await UniTask.Yield();
        }

        private void ClearStatusUI()
        {
            foreach (var ui in _statusUIs)
            {
                if (ui != null)
                {
                    Object.Destroy(ui.gameObject);
                }
            }

            _statusUIs.Clear();
        }
    }
}
