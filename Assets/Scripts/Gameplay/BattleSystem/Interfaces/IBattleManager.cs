using System;
using System.Collections.Generic;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Units;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface IBattleManager : IDisposable
    {
        // 기본 속성들
        BattleUnit Winner { get; }
        bool IsInitialized { get; }
        bool IsDisposed { get; }

// 유닛 접근
        PlayerUnit PlayerUnit { get; }
        IReadOnlyList<EnemyUnit> EnemyUnits { get; }
        EnemyUnit CurrentEnemy { get; }

        // 이벤트
        event Action<PlayerUnit, IReadOnlyList<EnemyUnit>> OnBattleStarted;
        event Action<BattleUnit> OnBattleEnded;

        void Initialize();
        void ExecuteEnemyAction();
        bool HasMoreEnemyTurns();
        void ResetEnemyTurnIndex();
        string GetDebugInfo();
    }
}
