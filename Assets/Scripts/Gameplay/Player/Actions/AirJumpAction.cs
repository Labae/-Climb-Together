using Cysharp.Text;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using R3;

namespace Gameplay.Player.Actions
{
    public class AirJumpAction : IPlayerAction
    {
        private readonly PlayerMovementAbility _movementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundChecker _groundChecker;

        private readonly CompositeDisposable _disposables = new();

        private int _remainingJumps;

        public AirJumpAction(PlayerMovementAbility movementAbility, IPhysicsController physicsController,
            IGroundChecker groundChecker)
        {
            _movementAbility = movementAbility;
            _physicsController = physicsController;
            _groundChecker = groundChecker;
            _groundChecker.OnGroundEntered.Subscribe(_ =>
            {
                _remainingJumps = 1;
            }).AddTo(_disposables);
        }

        public bool CanExecute()
        {
            return !_groundChecker.IsGrounded.CurrentValue && _movementAbility.HasDoubleJump && _remainingJumps > 0;
        }

        public void Execute()
        {
            var airJumpPower = _movementAbility.JumpPower * _movementAbility.DoubleJumpMultiplier;
            _physicsController.Jump(airJumpPower);
            _remainingJumps--;

            GameLogger.Debug(ZString.Format("Player Air Jump: {0}",
                airJumpPower), LogCategory.Player);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
