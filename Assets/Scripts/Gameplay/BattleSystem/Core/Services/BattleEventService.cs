using Cysharp.Text;
using Data.BattleSystem.Enums;
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

        public void PublishAttackAttempted(BattleUnit attacker, BattleUnit target, WeaponType weaponType)
        {
            var evt = new AttackAttemptedEvent(attacker, target, weaponType);
            _eventBus.Publish(evt);
        }

        public void PublishAttackCompleted(AttackResult result)
        {
            var evt = new AttackCompletedEvent(result.Attacker, result.Target,
                result.WeaponType, result.Damage, result.IsWeaknessHit, result.WasTargetKilled);
            // 공격 이벤트 발행
            _eventBus.Publish(evt);

            // 로그 출력
            string hitType = result.IsWeaknessHit ? "약점 공격" : "일반 공격";
            string breakStatus = result.Target.Shield.IsBroken ? "(브레이크 상태)" : "";
            string killStatus = result.WasTargetKilled ? " [처치!]" : "";
            GameLogger.Debug(ZString.Format("{0}이(가) {1}에게 {2}로 {3} 데미지를 입혔습니다! ({4}){5}{6}",
                result.Attacker.UnitName, result.Target.UnitName, result.WeaponType, result.Damage, hitType, breakStatus, killStatus), LogCategory.Battle);
        }
    }
}
