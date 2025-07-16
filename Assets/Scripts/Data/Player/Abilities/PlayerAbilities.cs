using Data.Player.Abilities;
using Data.Player.Settings;

namespace Data.Player
{
    using NaughtyAttributes;
    using UnityEngine;

    namespace Data.Player
    {
        [CreateAssetMenu(fileName = nameof(PlayerAbilities), menuName = "Gameplay/Player/" + nameof(PlayerAbilities))]
        public class PlayerAbilities : ScriptableObject
        {
            [Header("Player Info")] public string PlayerName = "MegaPlayer";

            [TextArea(2, 3)] public string Description;

            public Sprite PlayerIcon;

            [Header("Abilities")]
            public PlayerMovementAbility Movement;
            public PlayerCombatAbility Combat;

            [Header("Settings")]
            public PlayerPhysicsSettings PhysicsSettings;

            [Header("Calculated Values")] [ReadOnly]
            public float MaxJumpHeight;

            [ReadOnly] public float TimeToApex;

            [ReadOnly] public float MaxHorizontalDistance;

            private void OnValidate()
            {
                CalculateJumpPhysics();
            }

            private void CalculateJumpPhysics()
            {
                if (Movement == null) return;

                // 최대 점프 높이 계산
                float gravity = Physics2D.gravity.y * PhysicsSettings.NormalGravity;
                TimeToApex = Movement.JumpPower / Mathf.Abs(gravity);
                MaxJumpHeight = Movement.JumpPower * TimeToApex + 0.5f * gravity * TimeToApex * TimeToApex;

                // 최대 수평 이동 거리 (점프 중)
                MaxHorizontalDistance = Movement.RunSpeed * TimeToApex * 2f;
            }
        }
    }
}
