using System;
using Gameplay.BattleSystem.Core;

namespace Gameplay.BattleSystem.Interfaces
{
    public interface IBattleManager : IDisposable
    {
        BattleUnit Winner { get; }

        void Initialize();
        void ExecuteEnemyAction();
        string GetDebugInfo();
    }
}
