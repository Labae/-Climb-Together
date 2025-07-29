using System;
using Data.Common;
using UnityEngine;

namespace Data.Platformer.Settings
{
    [Serializable]
    public class PlatformerPhysicsSettings : PhysicsSettings
    {
        [Header("Gravity")]
        [Range(-30f, -3f)] public float JumpHoldGravity = -12f;
    }
}
