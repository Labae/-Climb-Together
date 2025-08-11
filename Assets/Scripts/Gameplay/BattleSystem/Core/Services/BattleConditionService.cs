using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Units;

namespace Gameplay.BattleSystem.Core.Services
{
    // 전투 조건 확인 전담 서비스
    public class BattleConditionService
    {
        public BattleEndCondition CheckBattleEndCondition(PlayerUnit player, TurnService turnService)
        {
            if (!player.Health.IsAlive)
            {
                var winner = turnService.ActiveEnemyCount > 0 ? turnService.CurrentEnemy : null;
                return BattleEndCondition.PlayerDefeated(winner);
            }

            if (turnService.AreAllEnemiesDefeated())
            {
                return BattleEndCondition.AllEnemiesDefeated(player);
            }

            return BattleEndCondition.ContinueBattle();
        }

        public bool CanExecuteAction(PlayerUnit player, TurnService turnService, BattleState currentState, BattleState requiredState)
        {
            if (currentState != requiredState)
            {
                GameLogger.Warning(ZString.Format("Cannot execute action: Wrong state. Current: {0} Required: {1}", currentState, requiredState), LogCategory.Battle);
                return false;
            }

            if (!player.Health.IsAlive)
            {
                GameLogger.Warning("Cannot execute action: Player is dead", LogCategory.Battle);
                return false;
            }

            if (turnService.ActiveEnemyCount == 0)
            {
                GameLogger.Warning("Cannot execute action: No active enemies", LogCategory.Battle);
                return false;
            }

            return true;
        }
    }

    public class BattleEndCondition
    {
        public bool ShouldEndBattle { get; private set; }
        public BattleUnit Winner { get; private set; }
        public string Reason { get; private set; }

        public BattleEndCondition(bool shouldEndBattle, BattleUnit winner, string reason)
        {
            ShouldEndBattle = shouldEndBattle;
            Winner = winner;
            Reason = reason;
        }

        public static BattleEndCondition ContinueBattle()
            => new BattleEndCondition(false, null, "Battle continues");

        public static BattleEndCondition PlayerDefeated(BattleUnit winner)
            => new BattleEndCondition(true, winner, "Player defeated");

        public static BattleEndCondition AllEnemiesDefeated(BattleUnit winner)
            => new BattleEndCondition(true, winner, "All enemies defeated");
    }
}
