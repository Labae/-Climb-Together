using Cysharp.Text;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Events;
using Systems.EventBus;
using VContainer;

namespace Gameplay.BattleSystem.Core.Services
{
    /// <summary>
    /// 이벤트 발행을 담당하는 서비스
    /// 모든 전투 이벤트는 이 서비스를 통해 발행된다
    /// </summary>
    public class BattleEventService
    {
        [Inject] private readonly IEventBus _eventBus;

        public void PublishBattleStarted(BattleUnit player, BattleUnit[] enemies)
        {
            var evt = new BattleStartedEvent(player, enemies);
            _eventBus.Publish(evt);

            GameLogger.Info(ZString.Format("전투 시작: {0} vs {1} enemies", player.UnitName, enemies.Length),
                LogCategory.Battle);
        }

        public void PublishBattleEnded(BattleUnit winner, string endReason)
        {
            var evt = new BattleEndedEvent(winner, endReason);
            _eventBus.Publish(evt);

            GameLogger.Info(ZString.Format("전투 종료: {0} ({1})", winner?.UnitName ?? "무승부", endReason),
                LogCategory.Battle);
        }

        public void PublishAttackAttempted(BattleUnit attacker, BattleUnit target, WeaponData weaponData)
        {
            var evt = new AttackAttemptedEvent(attacker, target, weaponData);
            _eventBus.Publish(evt);
        }

        public void PublishAttackCompleted(BattleAttackResult result)
        {
            var evt = new AttackCompletedEvent(
                result.Attacker,
                result.Target,
                result.WeaponData,
                result.FinalDamage,
                result.IsWeaknessHit,
                result.WasTargetKilled,
                result.IsCritical,
                result.WasShieldBroken
                );
            // 공격 이벤트 발행
            _eventBus.Publish(evt);

            GameLogger.Debug(result.GetAttackDescription(), LogCategory.Battle);
        }
    }
}
