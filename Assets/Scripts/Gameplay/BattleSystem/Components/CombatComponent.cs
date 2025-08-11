using Cysharp.Text;
using Data.BattleSystem.Configs;
using Data.BattleSystem.Configs.Core;
using Data.WeaponSystem;
using Data.WeaponSystem.Enums;
using Debugging;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.Interfaces;
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

        public DamageResult CalculateDamage(BattleUnit target, WeaponData weaponData)
        {
            if (target == null)
            {
                return new DamageResult
                {
                    FinalDamage = 0,
                    IsCritical = false,
                    WeaponData =null,
                };
            }

            if (weaponData == null)
            {
                return CalculateFallbackDamage(target, weaponData.WeaponType);
            }

            // 1. 기본 데미지 계산 (무기별 특성 반영)
            int baseDamage = weaponData.CalculateBaseDamage(_stats.Attack, target.Stats.Defense);

            // 2. 크리티컬 판정
            bool isCritical = weaponData.RollCritical();
            if (isCritical)
            {
                baseDamage = weaponData.ApplyCritical(baseDamage);
                GameLogger.Debug(ZString.Format("크리티컬 히트! {0}배 데미지", weaponData.CriticalMultiplier));
            }

            var calculatedDamage = baseDamage;

            // 약점 공격시 1.5배 데미지
            bool isWeaknessHit = target.Weakness.IsWeaknessHit(weaponData.WeaponType);
            if (isWeaknessHit)
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

            return new DamageResult
            {
                FinalDamage = finalDamage,
                BaseDamage = baseDamage,
                IsCritical = isCritical,
                IsWeaknessHit = isWeaknessHit,
                WeaponData = weaponData
            };
        }

        private DamageResult CalculateFallbackDamage(BattleUnit target, WeaponType weaponType)
        {
            // 기본 데미지 계산 (공격력 - 방어력/2)
            int baseDamage = _stats.Attack - (target.Stats.Defense / 2);
            var calculatedDamage = baseDamage;

            // 약점 공격시 1.5배 데미지
            bool isWeaknessHit = target.Weakness.IsWeaknessHit(weaponType);
            if (isWeaknessHit)
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
            return new DamageResult
            {
                FinalDamage = finalDamage,
                BaseDamage = baseDamage,
                IsCritical = false,
                IsWeaknessHit = isWeaknessHit,
                WeaponData = null
            };
        }

        public override string ToString()
        {
            return ZString.Format("Combat - Attack: {0}, Defense: {1}"
                , _stats?.Attack ?? 0, _stats?.Defense ?? 0);
        }
    }
}
