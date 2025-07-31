using System;
using UnityEngine;

namespace Systems.Physics.Debugging
{
    [Serializable]
    public struct BoxCastDebugInfo
    {
        public Vector2 Center;
        public Vector2 Size;
        public Vector2 Direction;

        public float Distance;

        public RaycastHit2D Hit;

        public bool HasHit;
        public Color Color;
    }
}
