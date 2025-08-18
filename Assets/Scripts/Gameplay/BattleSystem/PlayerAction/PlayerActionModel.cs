using System;
using System.Collections.Generic;
using Data.WeaponSystem;
using Debugging;
using Debugging.Enum;
using Gameplay.BattleSystem.Player;
using Gameplay.BattleSystem.UI.Base;
using R3;

namespace Gameplay.BattleSystem.PlayerAction
{
    /// <summary>
    /// 플레이어 액션 UI를 위한 Model
    /// 무기 선택과 액션 가능 상태를 관리
    /// </summary>
    public class PlayerActionModel : BaseModel
    {
        private readonly PlayerWeaponInventory _weaponInventory;
        private readonly TurnService _turnService;

        // Properties
        public ReactiveProperty<WeaponData> SelectedWeapon { get; } = new();

        // Readonly Properties
        public ReadOnlyReactiveProperty<List<WeaponData>> AvailableWeapons { get; private set; }
        public ReadOnlyReactiveProperty<bool> CanPerformAction { get; private set; }

        // Events
        public Subject<WeaponData> OnWeaponSelected { get; } = new();
        public Subject<Unit> OnActionCancelled { get; } = new();

        public PlayerActionModel(PlayerWeaponInventory weaponInventory, TurnService turnService)
        {
            _weaponInventory = weaponInventory ?? throw new ArgumentNullException(nameof(weaponInventory));
            _turnService = turnService;

            Initialize();
        }

        private void Initialize()
        {
            var weaponListSubject =
                new ReactiveProperty<List<WeaponData>>(new List<WeaponData>(_weaponInventory.OwnedWeapons));
            AvailableWeapons = weaponListSubject.ToReadOnlyReactiveProperty();

            _weaponInventory.OnInventoryChanged += () =>
            {
                weaponListSubject.Value = new List<WeaponData>(_weaponInventory.OwnedWeapons);
            };

            var canPerformAction = new ReactiveProperty<bool>(_turnService.IsPlayerTurn);
            CanPerformAction = canPerformAction.ToReadOnlyReactiveProperty();

            _turnService.OnTurnChanged += (turnEntry) =>
            {
                canPerformAction.Value = turnEntry.IsPlayer;
            };

            SetInitialized();
            GameLogger.Info("PlayerActionModel 초기화 완료", LogCategory.UI);
        }

        public override void Dispose()
        {
            SelectedWeapon?.Dispose();
            AvailableWeapons?.Dispose();
            CanPerformAction?.Dispose();
            OnWeaponSelected?.Dispose();
            OnActionCancelled?.Dispose();

            base.Dispose();
        }
    }
}
