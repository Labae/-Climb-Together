using Data.Common;
using Data.Platformer.Settings;
using UnityEngine;

namespace Data.Platformer.Abilities
{
    namespace Data.Player
    {
        [CreateAssetMenu(fileName = nameof(PlatformerPlayerSettings), menuName = "Gameplay/Player/" + nameof(PlatformerPlayerSettings))]
        public class PlatformerPlayerSettings : ScriptableObject
        {
            [Header("Player Info")] public string PlayerName = "MegaPlayer";

            [TextArea(2, 3)] public string Description;

            public Sprite PlayerIcon;

            [Header("Abilities")]
            public PlatformerMovementSettings PlatformerMovement;
            public CombatSettings Combat;

            [Header("Settings")]
            public PlatformerPhysicsSettings PhysicsSettings;
        }
    }
}
