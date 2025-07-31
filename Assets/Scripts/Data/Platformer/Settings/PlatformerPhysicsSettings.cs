using System;
using Data.Common;
using UnityEngine;

namespace Data.Platformer.Settings
{
    [Serializable]
    public class PlatformerPhysicsSettings : PhysicsSettings
    {
        [Header("Variable Gravity")]
        [Range(-30f, -5f)]
        [Tooltip("Gravity when holding jump button (weaker for floating feel)")]
        public float JumpHoldGravity = -12f;

        [Range(-50f, -15f)]
        [Tooltip("Gravity after jump cut (stronger for quick fall)")]
        public float JumpCutGravity = -35f;

        [Range(-40f, -10f)]
        [Tooltip("Gravity when falling normally")]
        public float FallGravity = -25f;

        [Header("Apex Handling")]
        [Range(-15f, -3f)]
        [Tooltip("Gravity at jump apex (weaker for floating feel)")]
        public float ApexGravity = -8f;
    }
}
