using Core.Behaviours;
using Gameplay.BattleSystem.Events;
using R3;
using Systems.EventBus;
using UnityEngine;
using VContainer;

namespace Gameplay.BattleSystem.TargetSelection
{
    public class TargetSelectionSetup : CoreBehaviour
    {
        [Header("View Reference")] [SerializeField]
        private TargetSelectionView _view;

        [Inject] private IEventBus _eventBus;

        private TargetSelectionController _controller;
        private readonly CompositeDisposable _disposables = new();

        private void Start()
        {
            var model = new TargetSelectionModel();
            _controller = new TargetSelectionController(model, _view);
            _controller.Initialize();

            model.OnTargetConfirmed
                .Subscribe(data =>
                {
                    var evt = new TargetSelectedEvent(data.Target, data.Weapon);
                    _eventBus.Publish(evt);
                })
                .AddTo(_disposables);

            model.OnSelectionCancelled
                .Subscribe(_ =>
                {
                    _eventBus.Publish(new ActionCancelledEvent());
                })
                .AddTo(_disposables);

            _eventBus.Subscribe<StartTargetSelectionEvent>(evt =>
                {
                    _controller.StartSelection(evt.AvailableTargets, evt.SelectedWeapon);
                })
                .AddTo(_disposables);

            _eventBus.Subscribe<TargetSelectedEvent>(evt =>
                {
                    _controller.EndSelection();
                })
                .AddTo(_disposables);

            _eventBus.Subscribe<BattleEndedEvent>(_ =>
                {
                    _controller?.EndSelection();
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
