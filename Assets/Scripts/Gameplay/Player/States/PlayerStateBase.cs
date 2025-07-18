using Data.Player.Enums;
using Systems.StateMachine.Interfaces;

namespace Gameplay.Player.States
{
    public abstract class PlayerStateBase : IState<PlayerStateType>
    {
        public abstract PlayerStateType StateType { get; }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}
