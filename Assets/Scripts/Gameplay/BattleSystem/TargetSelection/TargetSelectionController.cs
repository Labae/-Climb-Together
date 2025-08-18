using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.UI.Base;
using Gameplay.BattleSystem.Units;
using R3;

namespace Gameplay.BattleSystem.TargetSelection
{
    public class TargetSelectionController : BaseController<TargetSelectionModel, TargetSelectionView>
    {
        public TargetSelectionController(TargetSelectionModel model, TargetSelectionView view) : base(model, view)
        {
        }

        protected override void SetupBindings()
        {
            Model.IsSelecting
                .Subscribe(isSelecting =>
                {
                    if (isSelecting)
                    {
                        var targets = Model.AvailableTargets.CurrentValue;
                        var weapon = Model.CurrentWeapon.CurrentValue;
                        View.ShowTargetSelection(targets, weapon);

                        UpdateCurrentTarget();
                    }
                    else
                    {
                        View.HideTargetSelection();
                    }
                })
                .AddTo(_disposables);

            Model.SelectedTargetIndex
                .Subscribe(_ => UpdateCurrentTarget())
                .AddTo(_disposables);

            Model.IsWeaknessTarget
                .Subscribe(isWeakness =>
                {
                    if (Model.CurrentTarget.CurrentValue != null)
                    {
                        View.UpdateSelectedTarget(Model.CurrentTarget.CurrentValue, isWeakness);
                    }
                })
                .AddTo(_disposables);

            View.OnNavigationInput += OnNavigationInput;
            View.OnConfirmInput += OnConfirmInput;
            View.OnCancelInput += OnCancelInput;
        }

        private void UpdateCurrentTarget()
        {
            var target = Model.CurrentTarget.CurrentValue;
            var isWeakness = Model.IsWeaknessTarget.CurrentValue;

            if (target != null)
            {
                View.UpdateSelectedTarget(target, isWeakness);
                GameLogger.Debug(ZString.Format("타겟 변경: {0} (약점: {1})", target.UnitName, isWeakness), LogCategory.UI);
            }
        }

        #region Public Methods

        public void StartSelection(List<EnemyUnit> enemies, WeaponData weapon)
        {
            if (enemies == null || enemies.Count == 0)
            {
                GameLogger.Warning("선택 가능한 적이 없습니다", LogCategory.UI);
                return;
            }

            var aliveEnemies = enemies.Where(e => e != null && e.Health.IsAlive).ToList();
            if (aliveEnemies.Count == 0)
            {
                GameLogger.Warning("살아 있는 적이 없습니다", LogCategory.UI);
                return;
            }

            Model.AvailableTargets.Value = aliveEnemies;
            Model.CurrentWeapon.Value = weapon;
            Model.SelectedTargetIndex.Value = 0;
            Model.IsSelecting.Value = true;
        }

        public void EndSelection()
        {
            Model.IsSelecting.Value = false;
            Model.SelectedTargetIndex.Value = 0;
        }

        #endregion

        #region Input Handling

        private void OnNavigationInput(int direction)
        {
            if (!Model.IsSelecting.CurrentValue)
            {
                return;
            }

            var targets = Model.AvailableTargets.CurrentValue;
            if (targets == null || targets.Count <= 1)
            {
                return;
            }

            var currentIndex = Model.SelectedTargetIndex.CurrentValue;
            var newIndex = currentIndex + direction;

            if (newIndex < 0)
            {
                newIndex = targets.Count - 1;
            }
            else if (newIndex >= targets.Count)
            {
                newIndex = 0;
            }

            Model.SelectedTargetIndex.Value = newIndex;
        }

        private void OnConfirmInput()
        {
            if (!Model.IsSelecting.CurrentValue)
            {
                return;
            }

            var target =  Model.CurrentTarget.CurrentValue;
            var weapon = Model.CurrentWeapon.CurrentValue;

            if (target == null || weapon == null)
            {
                GameLogger.Warning("타겟이나 무기가 null입니다", LogCategory.UI);
                return;
            }

            Model.OnTargetConfirmed.OnNext(new TargetSelectionModel.TargetConfirmData(target, weapon));
            EndSelection();
        }

        private void OnCancelInput()
        {
            if (!Model.IsSelecting.CurrentValue)
            {
                return;
            }

            Model.OnSelectionCancelled.OnNext(Unit.Default);
            EndSelection();
        }

        #endregion

        public override void Dispose()
        {
            if (View != null)
            {
                View.OnNavigationInput -= OnNavigationInput;
                View.OnConfirmInput -= OnConfirmInput;
                View.OnCancelInput -= OnCancelInput;
            }
            base.Dispose();
        }
    }
}
