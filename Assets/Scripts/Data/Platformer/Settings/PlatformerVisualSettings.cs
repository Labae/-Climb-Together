using System;
using UnityEngine;

namespace Data.Platformer.Settings
{
    [Serializable]
    public class PlatformerVisualSettings
    {
        [Header("Color Settings")]
        [Tooltip("플레이어 일반 색상")]
        public Color NormalColor =  Color.white;
        [Tooltip("플레이어 대시 색상")]
        public Color DashColor = Color.blue;

    }
}
