using Cysharp.Text;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.UI.Base;
using R3;

namespace Gameplay.BattleSystem.PlayerAction
{
    public class PlayerActionController : BaseController<PlayerActionModel, PlayerActionView>
    {
        public PlayerActionController(PlayerActionModel model, PlayerActionView view) : base(model, view)
        {
        }

        protected override void SetupBindings()
        {
            Model.AvailableWeapons
                .Subscribe(weapons =>
                {
                    View.UpdateWeaponButtons(weapons);
                })
                .AddTo(_disposables);

            Model.CanPerformAction
                .Subscribe(canPerform =>
                {
                    if (canPerform)
                    {
                        View.ShowButtons(false);
                        View.SetButtonsInteractable(true);
                    }
                    else
                    {
                        View.HideButtons(false);
                    }
                })
                .AddTo(_disposables);

            Model.SelectedWeapon
                .Subscribe(weapon =>
                {
                    View.HighlightSelectedWeapon(weapon);
                })
                .AddTo(_disposables);

            View.OnWeaponButtonClicked += OnWeaponClicked;
        }

        private void OnWeaponClicked(WeaponData weapon)
        {
            Model.SelectedWeapon.Value = weapon;

            Model.OnWeaponSelected.OnNext(weapon);
            View.HideButtons(false);
            GameLogger.Warning(ZString.Format("무기 선택: {0}", weapon.WeaponName), LogCategory.UI);
        }

        public void CompleteAction()
        {
            Model.SelectedWeapon.Value = null;

            if (Model.CanPerformAction.CurrentValue)
            {
                View.ShowButtons(false);
                View.SetButtonsInteractable(true);
            }
        }

        public void CancelAction()
        {
            Model.SelectedWeapon.Value = null;
            Model.OnActionCancelled.OnNext(Unit.Default);
        }

        protected override void OnInitialized()
        {
            if (Model.AvailableWeapons.CurrentValue != null)
            {
                View.UpdateWeaponButtons(Model.AvailableWeapons.CurrentValue);
            }

            if (Model.CanPerformAction.CurrentValue)
            {
                View.ShowButtons(false);
            }
            else
            {
                View.HideButtons(false);
            }

            base.OnInitialized();
        }

        public override void Dispose()
        {
            if (View != null)
            {
                View.OnWeaponButtonClicked -= OnWeaponClicked;
            }

            base.Dispose();
        }
    }
}
