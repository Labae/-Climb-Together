using System;
using Data.Common;
using UnityEngine;

namespace Data.Player.Settings
{
    [Serializable]
    public class PlayerPhysicsSettings : PhysicsSettings
    {
        [Header("Gravity")]
        [Range(-30f, -3f)] public float JumpHoldGravity = -12f;
    }
}
