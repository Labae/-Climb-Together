using System;
using Data.Player.Enums;
using R3;
using Systems.StateMachine.Interfaces;
using UnityEngine;

namespace Gameplay.Player.States
{
    public abstract class PlayerStateBase : IState<PlayerStateType>
    {
        public abstract PlayerStateType StateType { get; }

        public void OnEnter()
        {
        }

        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }

        public virtual void OnExit()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}
