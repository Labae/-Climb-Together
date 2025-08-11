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
    /// 공격 로직 전담 서비스
    /// </summary>
    public class AttackService
    {
        [Inject]
        private readonly BattleEventService _battleEventService;

        public AttackResult ExecuteAttack(BattleUnit attacker, BattleUnit target, WeaponType weaponType)
        {
            if (attacker == null || target == null)
            {
                return AttackResult.Failed("Invalid attacker or target");
            }

            GameLogger.Debug(ZString.Format("{0} attacking {1} with {2}", attacker.UnitName, target.UnitName, weaponType),
                LogCategory.Battle);

            _battleEventService.PublishAttackAttempted(attacker, target, weaponType);

            // 1. 약점 확인
            bool isWeaknessHit = target.Weakness.IsWeaknessHit(weaponType);

            // 2. 실드 처리
            if (isWeaknessHit && target.Shield.CurrentShield > 0)
            {
                target.Shield.DamageShield(1);
                GameLogger.Debug(ZString.Format("{0}의 실드 파괴! 남은 실드: {1}",
                    target.UnitName, target.Shield.CurrentShield), LogCategory.Battle);
            }

            // 3. 데미지 처리
            int damage = attacker.Combat.CalculateDamage(target, weaponType);
            target.Health.TakeDamage(damage);

            // 4 결과 생성
            var result = new AttackResult
            {
                Attacker = attacker,
                Target = target,
                WeaponType = weaponType,
                Damage = damage,
                IsWeaknessHit = isWeaknessHit,
                WasTargetKilled = !target.Health.IsAlive
            };

            // 5. 이벤트 발행
            _battleEventService.PublishAttackCompleted(result);

            return result;
        }
    }

    /// <summary>
    /// 공격 결과 정보
    /// </summary>
    public class AttackResult
    {
        public BattleUnit Attacker { get; set; }
        public BattleUnit Target { get; set; }
        public WeaponType WeaponType { get; set; }
        public int Damage { get; set; }
        public bool IsWeaknessHit { get; set; }
        public bool WasTargetKilled { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; }

        public static AttackResult Failed(string error)
        {
            return new AttackResult { IsSuccess = false, ErrorMessage = error };
        }
    }
}
