using Cysharp.Text;
using Data.BattleSystem.Configs;
using Data.BattleSystem.Enums;
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

        public BattleStats Stats => _stats;

        public void Initialize(BattleStats stats)
        {
            _stats = stats ?? throw new System.ArgumentNullException(nameof(stats));
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

        public override string ToString()
        {
            return ZString.Format("Combat - Attack: {0}, Defense: {1}"
                , _stats?.Attack ?? 0, _stats?.Defense ?? 0);
        }
    }
}
