using System;
using Core.Behaviours;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.TurnOrder
{
    public class TurnOrderSetup : CoreBehaviour
    {
        [Header("View Reference")]
        [SerializeField]
        private TurnOrderView _turnOrderView;

        [Inject]
        private TurnOrderService _turnOrderService;

        private TurnOrderController _turnOrderController;

        private void Start()
        {
            var model = new TurnOrderModel(_turnOrderService);
            _turnOrderController = new TurnOrderController(model, _turnOrderView);
            _turnOrderController.Initialize();
        }

        protected override void HandleDestruction()
        {
            _turnOrderController?.Dispose();
            base.HandleDestruction();
        }
    }
}
