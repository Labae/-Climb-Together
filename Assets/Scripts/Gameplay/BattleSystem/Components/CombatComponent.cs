using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Events;
using Gameplay.BattleSystem.Interfaces;
using Systems.EventBus;
using UnityEngine;

namespace Gameplay.BattleSystem.Components
{
    public class CombatComponent : ICombatComponent
    {
        private BattleStats _stats;
        private string _unitName;
        private IEventBus _eventBus;

        public BattleStats Stats => _stats;

        public void Initialize(BattleStats stats, string unitName, IEventBus eventBus)
        {
            _stats = stats ?? throw new System.ArgumentNullException(nameof(stats));
            _unitName = unitName ?? "Unknown Unit";
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
        }

        public int CalculateDamage(BattleUnit target, WeaponType weaponType)
        {
            if (target == null)
            {
                return 0;
            }

            // 기본 데미지 계산 (공격력 - 방어력/2)
            int baseDamage = _stats.Attack - (target.Stats.Defense / 2);
            var calculatedDamage = baseDamage;

            // 약점 공격시 1.5배 데미지
            if (target.Weakness.IsWeaknessHit(weaponType))
            {
                calculatedDamage = Mathf.RoundToInt(calculatedDamage * 1.5f);
            }

            // 브레이크 상태시 추가 데미지
            if (target.Shield.IsBroken)
            {
                calculatedDamage = Mathf.RoundToInt(calculatedDamage * target.Shield.BreakDamageMultiplier);
            }

            // 최소 1데미지 보장
            int finalDamage = Mathf.Max(1, calculatedDamage);
            return finalDamage;
        }

        public void ExecuteAttack(BattleUnit attacker, BattleUnit target, WeaponType weaponType)
        {
            if (attacker == null || target == null)
            {
                GameLogger.Warning("Cannot execute attack: attacker or target is null.", LogCategory.Battle);
                return;
            }

            if (!attacker.Health.IsAlive || !target.Health.IsAlive)
            {
                GameLogger.Warning("Cannot execute attack: attacker or target is not alive.", LogCategory.Battle);
                return;
            }

            bool isWeaknessHit = target.Weakness.IsWeaknessHit(weaponType);

            // 실드 처리
            if (isWeaknessHit && target.Shield.CurrentShield > 0)
            {
                target.Shield.DamageShield(1);
                GameLogger.Debug(ZString.Format("{0}의 실드 파괴! 남은 실드: {1}",
                    target.UnitName, target.Shield.CurrentShield), LogCategory.Battle);
            }

            // 데미지 처리
            int damage = CalculateDamage(target, weaponType);
            target.Health.TakeDamage(damage);

            // 로그 출력
            string hitType = isWeaknessHit ? "약점 공격" : "일반 공격";
            string breakStatus = target.Shield.IsBroken ? "(브레이크 상태)" : "";
            GameLogger.Debug(ZString.Format("{0}이(가) {1}에게 {2}로 {3} 데미지를 입혔습니다! ({4}){5}",
                _unitName, target.UnitName, weaponType, damage, hitType, breakStatus), LogCategory.Battle);

            // 공격 이벤트 발행
            _eventBus.Publish(new UnitAttackedEvent(attacker, target));
        }

        public override string ToString()
        {
            return ZString.Format("Combat - Attack: {0}, Defense: {1}"
                , _stats?.Attack ?? 0, _stats?.Defense ?? 0);
        }
    }
}
