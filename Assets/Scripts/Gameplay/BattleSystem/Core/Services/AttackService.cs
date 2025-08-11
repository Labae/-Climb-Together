using Cysharp.Text;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
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

        public BattleAttackResult ExecuteAttack(BattleUnit attacker, BattleUnit target, WeaponData weapon)
        {
            if (attacker == null || target == null)
            {
                return BattleAttackResult.Failed("Invalid attacker or target");
            }

            GameLogger.Debug(ZString.Format("{0} attacking {1} with {2}", attacker.UnitName, target.UnitName, weapon),
                LogCategory.Battle);

            _battleEventService.PublishAttackAttempted(attacker, target, weapon);

            // 1. 데미지 계산
            var damageResult = attacker.Combat.CalculateDamage(target, weapon);

            // 2. 실드 처리
            int shieldDamage = damageResult.WeaponData?.ShieldDamage ?? 1;
            bool shieldBroken = false;

            if (damageResult.IsWeaknessHit && target.Shield.CurrentShield > 0)
            {
                for (int i = 0; i < shieldDamage && target.Shield.CurrentShield > 0; i++)
                {
                    target.Shield.DamageShield(1);
                    if (target.Shield.IsBroken)
                    {
                        shieldBroken = true;
                        break;
                    }
                }

                if (shieldBroken)
                {
                    GameLogger.Debug(ZString.Format("{0}의 실드 완전 파괴", target.UnitName), LogCategory.Battle);
                }
                else
                {
                    GameLogger.Debug(ZString.Format("{0}의 실드 {1} 데미지! 남은 실드: {2}",
                        target.UnitName, shieldDamage, target.Shield.CurrentShield), LogCategory.Battle);
                }
            }

            // 3. 체력 데미지
            target.Health.TakeDamage(damageResult.FinalDamage);

            // 4 결과 생성
            var result = new BattleAttackResult
            {
                Attacker = attacker,
                Target = target,
                WeaponData = weapon,
                DamageResult = damageResult,
                ShieldDamage = shieldDamage,
                WasShieldBroken = shieldBroken,
                WasTargetKilled = !target.Health.IsAlive,
                IsSuccess = true
            };

            // 5. 이벤트 발행
            _battleEventService.PublishAttackCompleted(result);

            return result;
        }
    }

    public class DamageResult
    {
        public int FinalDamage { get; set; }
        public int BaseDamage { get; set; }
        public bool IsCritical { get; set; }
        public bool IsWeaknessHit { get; set; }
        public WeaponData WeaponData { get; set; }
    }

    /// <summary>
    /// 공격 결과 정보
    /// </summary>
    public class BattleAttackResult
    {
        public BattleUnit Attacker { get; set; }
        public BattleUnit Target { get; set; }
        public WeaponData WeaponData { get; set; }
        public DamageResult DamageResult { get; set; }
        public int ShieldDamage { get; set; }
        public bool WasShieldBroken { get; set; }
        public bool WasTargetKilled { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; }

        public int FinalDamage => DamageResult?.FinalDamage ?? 0;
        public bool IsCritical => DamageResult?.IsCritical ?? false;
        public bool IsWeaknessHit => DamageResult?.IsWeaknessHit ?? false;

        public static BattleAttackResult Failed(string error)
        {
            return new BattleAttackResult { IsSuccess = false, ErrorMessage = error };
        }

        public string GetAttackDescription()
        {
            if (!IsSuccess)
            {
                return ErrorMessage;
            }

            var sb = ZString.CreateStringBuilder();
            sb.AppendFormat("{0}이(가) {1}에게 {2}로 {3} 데미지를 입혔습니다!",
                Attacker.UnitName, Target.UnitName, WeaponData, DamageResult.FinalDamage);

            if (IsCritical)
            {
                sb.Append(" (크리티컬!)");
            }
            if (IsWeaknessHit)
            {
                sb.Append(" (약점!)");
            }
            if (WasShieldBroken)
            {
                sb.Append(" (실드 파괴!)");
            }
            if (WasTargetKilled)
            {
                sb.Append(" (처치!)");
            }

            return sb.ToString();
        }
    }
}
