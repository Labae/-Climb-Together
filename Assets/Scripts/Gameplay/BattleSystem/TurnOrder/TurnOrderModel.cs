using System.Collections.Generic;
using Gameplay.BattleSystem.UI.Base;
using R3;

namespace Gameplay.BattleSystem.TurnOrder
{
    public class TurnOrderModel : BaseModel
    {
        private readonly TurnOrderService _turnOrderService;

        // Properties
        public ReadOnlyReactiveProperty<TurnOrderEntry> CurrentTurn => _turnOrderService.CurrentTurn;
        public ReadOnlyReactiveProperty<List<TurnOrderEntry>> TurnOrder => _turnOrderService.TurnOrder;
        public ReadOnlyReactiveProperty<int> CurrentTurnIndex => _turnOrderService.CurrentTurnIndex;
        public ReadOnlyReactiveProperty<int> RoundNumber => _turnOrderService.RoundNumber;

        public ReadOnlyReactiveProperty<bool> IsPlayerTurn => _turnOrderService.CurrentTurn
            .Select(turn => turn is { IsPlayer: false })
            .ToReadOnlyReactiveProperty();

        public ReadOnlyReactiveProperty<bool> IsEnemyTurn => _turnOrderService.CurrentTurn
            .Select(turn => turn?.IsPlayer ?? false)
            .ToReadOnlyReactiveProperty();

        // Events
        public Observable<TurnTransition> OnTurnTransition => _turnOrderService.OnTurnTransition.AsObservable();
        public Observable<RoundChangeInfo> OnRoundStarted => _turnOrderService.OnRoundStarted.AsObservable();

        public TurnOrderModel(TurnOrderService turnOrderService)
        {
            _turnOrderService = turnOrderService ?? throw new System.ArgumentNullException(nameof(turnOrderService));
            SetInitialized();
        }
    }
}
