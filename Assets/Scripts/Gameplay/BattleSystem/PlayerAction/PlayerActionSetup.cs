using System;
using Core.Behaviours;
using Gameplay.BattleSystem.Events;
using Gameplay.BattleSystem.Units;
using R3;
using Systems.EventBus;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.PlayerAction
{
    public class PlayerActionSetup : CoreBehaviour
    {
        [Header("View Reference")] [SerializeField]
        private PlayerActionView _playerActionView;

        [Inject] private PlayerUnit _playerUnit;
        [Inject] private TurnService _turnService;

        [Inject] private IEventBus _eventBus;

        private PlayerActionController _controller;
        private readonly CompositeDisposable _disposables = new();

        private void Start()
        {
            var model = new PlayerActionModel(_playerUnit.WeaponInventory, _turnService);
            _controller = new PlayerActionController(model, _playerActionView);
            _controller.Initialize();

            model.OnWeaponSelected
                .Subscribe(weapon =>
                {
                    var evt = new WeaponSelectedEvent(weapon, _playerUnit);
                    _eventBus.Publish(evt);
                })
                .AddTo(_disposables);

            _eventBus.Subscribe<ActionCompletedEvent>(_ =>
                {
                    _controller.CompleteAction();
                })
                .AddTo(_disposables);

            _eventBus.Subscribe<ActionCancelledEvent>(_ =>
                {
                    _controller.CancelAction();
                })
                .AddTo(_disposables);
        }

        protected override void HandleDestruction()
        {
            _controller?.Dispose();
            _disposables?.Dispose();
            base.HandleDestruction();
        }
    }
}
