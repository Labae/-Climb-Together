using System.Collections.Generic;
using Data.WeaponSystem;
using Debugging;
using Gameplay.BattleSystem.UI.Base;
using Gameplay.BattleSystem.Units;
using R3;

namespace Gameplay.BattleSystem.TargetSelection
{
    /// <summary>
    /// 타겟 선택 UI를 위한 Model
    /// 선택 가능한 적과 선택 상태를 관리
    /// </summary>
    public class TargetSelectionModel : BaseModel
    {
        public ReactiveProperty<List<EnemyUnit>> AvailableTargets { get; } = new(new List<EnemyUnit>());
        public ReactiveProperty<int> SelectedTargetIndex { get; } = new(0);
        public ReactiveProperty<WeaponData> CurrentWeapon { get; } = new();
        public ReactiveProperty<bool> IsSelecting { get; } = new(false);

        public ReadOnlyReactiveProperty<EnemyUnit> CurrentTarget { get; private set; }
        public ReadOnlyReactiveProperty<bool> HasTargets { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsWeaknessTarget { get; private set; }
        public ReadOnlyReactiveProperty<int> TargetCount { get; private set; }

        public Subject<TargetConfirmData> OnTargetConfirmed { get; } = new();
        public Subject<Unit> OnSelectionCancelled { get; } = new();

        public TargetSelectionModel()
        {
            Initialize();
        }

        private void Initialize()
        {
            CurrentTarget = SelectedTargetIndex.CombineLatest(AvailableTargets,
                    (index, targets) =>
                    {
                        if (targets != null && targets.Count > 0 && index >= 0 && index < targets.Count)
                        {
                            return targets[index];
                        }

                        return null;
                    })
                .ToReadOnlyReactiveProperty();

            HasTargets = AvailableTargets
                .Select(targets => targets is { Count: > 0 })
                .ToReadOnlyReactiveProperty();

            TargetCount = AvailableTargets
                .Select(targets => targets?.Count ?? 0)
                .ToReadOnlyReactiveProperty();

            IsWeaknessTarget = CurrentTarget.CombineLatest(CurrentWeapon,
                    (target, weapon) =>
                    {
                        if (target == null || weapon == null)
                        {
                            return false;
                        }

                        return target.Weakness.IsWeaknessHit(weapon.WeaponType);
                    })
                .ToReadOnlyReactiveProperty();

            SetInitialized();
            GameLogger.Info("TargetSelectionModel 초기화 완료");
        }

        public override void Dispose()
        {
            AvailableTargets?.Dispose();
            SelectedTargetIndex?.Dispose();
            CurrentWeapon?.Dispose();
            IsSelecting?.Dispose();

            CurrentTarget?.Dispose();
            HasTargets?.Dispose();
            IsWeaknessTarget?.Dispose();
            TargetCount?.Dispose();

            OnTargetConfirmed?.Dispose();
            OnSelectionCancelled?.Dispose();

            base.Dispose();
        }

        public readonly struct TargetConfirmData
        {
            public EnemyUnit Target { get; }
            public WeaponData Weapon { get; }

            public TargetConfirmData(EnemyUnit target, WeaponData weapon)
            {
                Target = target;
                Weapon = weapon;
            }
        }
    }
}
