using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Enums;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Actions;
using Gameplay.Player.Core;
using Gameplay.Player.Events;
using Gameplay.Player.Interfaces;

namespace Gameplay.Player.Jump
{
    public class JumpExecutor : IDisposable
    {
        private readonly List<IPlayerAction> _jumpActions;
        private readonly PlayerEventBus _eventBus;
        private readonly IPhysicsController _physicsController;

        public IPlayerAction LastJumpAction { get;private set; }

        public JumpExecutor( List<IPlayerAction> jumpActions, PlayerEventBus eventBus, IPhysicsController physicsController)
        {
            _jumpActions = jumpActions ?? throw new ArgumentNullException(nameof(jumpActions));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _physicsController = physicsController ?? throw new ArgumentNullException(nameof(physicsController));
        }

        public bool TryExecuteJump(bool canJumpFromGround)
        {
            foreach (var jumpAction in _jumpActions)
            {
                if (ShouldExecuteJump(jumpAction, canJumpFromGround))
                {
                    return ExecuteJump(jumpAction);
                }
            }

            return false;
        }

        private bool ShouldExecuteJump(IPlayerAction jumpAction, bool canJumpFromGround)
        {
            return jumpAction switch
            {
                GroundJumpAction => canJumpFromGround,
                WallJumpAction => jumpAction.CanExecute(),
                AirJumpAction => jumpAction.CanExecute(),
                _ => jumpAction.CanExecute()
            };
        }

        private bool ExecuteJump(IPlayerAction jumpAction)
        {
            try
            {
                jumpAction.Execute();
                LastJumpAction = jumpAction;

                var jumpType = GetJumpType(jumpAction);
                var jumpVelocity = _physicsController.GetVelocity();
                _eventBus.Publish(new JumpExecutedEvent(jumpType, jumpVelocity));

                GameLogger.Debug(ZString.Concat("Jump executed: ", jumpAction.GetType().Name), LogCategory.Player);
                return true;
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error executing jump: ", e.Message), LogCategory.Player);
                return false;
            }
        }

        private JumpType GetJumpType(IPlayerAction jumpAction)
        {
            return jumpAction switch
            {
                GroundJumpAction => JumpType.Ground,
                WallJumpAction => JumpType.Wall,
                AirJumpAction => JumpType.Air,
                _ => JumpType.Ground
            };
        }

        public bool HasAvailableJumpActions()
        {
            return _jumpActions.Any(jumpAction => jumpAction.CanExecute());
        }

        public bool CanExecuteJumpType<T>() where T : IPlayerAction
        {
            foreach (var jumpAction in _jumpActions)
            {
                if (jumpAction is T && jumpAction.CanExecute())
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            foreach (var jumpAction in _jumpActions)
            {
                jumpAction?.Dispose();
            }
        }
    }
}
