using System;
using System.Collections.Generic;
using Data.Player.Abilities;
using Debugging;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Actions;
using Gameplay.Player.Interfaces;
using R3;

namespace Gameplay.Player
{
    public class PlayerJump : IDisposable
    {
        private readonly List<IPlayerAction> _jumpActions;
        private readonly Subject<Unit> _onJumpExecuted = new();

        private DisposableBag _disposableBag;

        public Observable<Unit> OnJumpExecuted => _onJumpExecuted.AsObservable();

        public PlayerJump(PlayerMovementAbility movementAbility,
            Observable<bool> jumpPressed,
            IPhysicsController physicsController,
            IGroundChecker groundChecker)
        {
            GameLogger.Assert(movementAbility != null, "Movement Ability Null");
            GameLogger.Assert(jumpPressed != null, "jumpPressed Null");
            GameLogger.Assert(physicsController != null, "IPhysicsController Null");
            GameLogger.Assert(groundChecker != null, "IGroundChecker Null");

            _jumpActions = new List<IPlayerAction>
            {
                new GroundJumpAction(movementAbility, physicsController, groundChecker),
                new AirJumpAction(movementAbility, physicsController, groundChecker)
            };

            jumpPressed.Subscribe(OnJumpPressed).AddTo(ref _disposableBag);
        }

        public void Dispose()
        {
            foreach (var jumpAction in _jumpActions)
            {
                jumpAction.Dispose();
            }
            _onJumpExecuted.Dispose();
            _disposableBag.Dispose();
        }

        private void OnJumpPressed(bool pressed)
        {
            if (!pressed)
            {
                return;
            }

            foreach (var jumpAction in _jumpActions)
            {
                if (!jumpAction.CanExecute())
                {
                    continue;
                }

                jumpAction.Execute();
                _onJumpExecuted.OnNext(Unit.Default);
                break;
            }
        }
    }
}
