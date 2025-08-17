using Cysharp.Threading.Tasks;
using Gameplay.BattleSystem.UI.Base;
using R3;

namespace Gameplay.BattleSystem.TurnOrder
{
    public class TurnOrderController : BaseController<TurnOrderModel, TurnOrderView>
    {
        public TurnOrderController(TurnOrderModel model, TurnOrderView view) : base(model, view)
        {
        }

        protected override void SetupBindings()
        {
            Model.CurrentTurn
                .Subscribe(entry =>
                {
                    if (entry != null)
                    {
                        View.UpdateCurrentTurn(entry);
                    }
                })
                .AddTo(_disposables);

            Model.TurnOrder.CombineLatest(Model.CurrentTurnIndex,
                    (order, index) => (order, index))
                .Subscribe(data =>
                {
                    View.UpdateTurnOrder(data.order, data.index);
                })
                .AddTo(_disposables);

            Model.RoundNumber
                .Subscribe(round =>
                {
                    View.UpdateRound(round);
                })
                .AddTo(_disposables);

            Model.OnTurnTransition
                .Subscribe(transition =>
                {
                    View.AnimateTurnTransition().Forget();
                })
                .AddTo(_disposables);

            Model.OnRoundStarted
                .Subscribe(info =>
                {
                    View.AnimateRoundChange(info.NewRound).Forget();
                }).AddTo(_disposables);
        }

        protected override void OnInitialized()
        {
            if (Model.CurrentTurn.CurrentValue != null)
            {
                View.UpdateCurrentTurn(Model.CurrentTurn.CurrentValue);
            }

            if (Model.TurnOrder.CurrentValue != null)
            {
                View.UpdateTurnOrder(Model.TurnOrder.CurrentValue, Model.CurrentTurnIndex.CurrentValue);
            }

            View.UpdateRound(Model.RoundNumber.CurrentValue);
        }

        public void Show()
        {
            View.SetVisible(true);
        }

        public void Hide()
        {
            View.SetVisible(false);
        }
    }
}
