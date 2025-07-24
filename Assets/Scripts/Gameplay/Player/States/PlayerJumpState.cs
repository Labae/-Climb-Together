using System;
using Cysharp.Text;
using Data.Player.Abilities;
using Data.Player.Enums;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Enums;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Core;
using Gameplay.Player.Events;
using R3;
using Systems.Physics;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player.States
{
    public class PlayerJumpState : PlayerStateBase
    {
        public override PlayerStateType StateType => PlayerStateType.Jump;
    }
}
