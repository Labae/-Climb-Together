using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Units;

namespace Gameplay.BattleSystem.Services
{
    /// <summary>
    /// 턴 관리 전담 서비스
    /// </summary>
    public class TurnService
    {
        private readonly List<EnemyUnit> _activeEnemies;
        private int _currentEnemyIndex = 0;

        public int CurrentEnemyIndex => _currentEnemyIndex;
        public EnemyUnit CurrentEnemy
            => _activeEnemies != null &&
               _activeEnemies.Count > _currentEnemyIndex
                ? _activeEnemies[_currentEnemyIndex]
                : null;
        public int ActiveEnemyCount => _activeEnemies.Count;

        public TurnService(List<EnemyUnit> activeEnemies)
        {
            _activeEnemies = activeEnemies;
        }

        public void RefreshActiveEnemies()
        {
            var beforeCount = _activeEnemies.Count;
            _activeEnemies.RemoveAll(e => e == null || !e.Health.IsAlive);

            if (_activeEnemies.Count != beforeCount)
            {
                GameLogger.Debug(ZString.Format("Active enemies updated: {0} -> {1}",  beforeCount, _activeEnemies.Count));
            }

            if (_currentEnemyIndex >= _activeEnemies.Count)
            {
                _currentEnemyIndex = 0;
            }
        }

        public bool HasMoreEnemyTurns()
        {
            return _currentEnemyIndex < _activeEnemies.Count;
        }

        public void ResetTurnIndex()
        {
            _currentEnemyIndex = 0;
            GameLogger.Debug("적 턴 인덱스가 0으로 리셋되었습니다", LogCategory.Battle);
        }

        public void AdvanceToNextEnemy()
        {
            _currentEnemyIndex++;
            GameLogger.Debug(ZString.Format("다음 적 턴으로 진행: 인덱스 {0} / {1}",
                _currentEnemyIndex, _activeEnemies.Count), LogCategory.Battle);
        }

        public bool AreAllEnemiesDefeated()
        {
            RefreshActiveEnemies();
            return _activeEnemies.Count == 0;
        }

        public List<EnemyUnit> GetActiveEnemies()
        {
            RefreshActiveEnemies();
            return _activeEnemies;
        }
    }
}
