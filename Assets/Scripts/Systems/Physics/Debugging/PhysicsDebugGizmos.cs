using UnityEngine;

namespace Systems.Physics.Debugging
{
    public class PhysicsDebugGizmos : MonoBehaviour
    {
        [SerializeField] private bool _showBoxCasts = true;
        [SerializeField] private bool _showHitPoints = true;
        [SerializeField] private bool _showDirectionLines = true;

        private PhysicsSystemBase _physicsSystem;

        public void Initialize(PhysicsSystemBase system)
        {
            _physicsSystem = system;
        }

        private void OnDrawGizmos()
        {
            if (_physicsSystem == null || !_showBoxCasts)
            {
                return;
            }

            foreach (var boxCast in _physicsSystem.DebugBoxCasts)
            {
                DrawBoxCast(boxCast);
            }
        }

        private void DrawBoxCast(BoxCastDebugInfo boxCast)
        {
            Gizmos.color = boxCast.Color;

            if (_showBoxCasts)
            {
                Gizmos.DrawWireCube(boxCast.Center, boxCast.Size);
            }

            if (_showDirectionLines)
            {
                var endPos = boxCast.Center + boxCast.Direction * boxCast.Direction;
                Gizmos.DrawLine(boxCast.Center, endPos);
                Gizmos.DrawWireCube(endPos, boxCast.Size);
            }

            if (boxCast.HasHit && _showHitPoints)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(boxCast.Hit.point, boxCast.Size * 0.8f);
                Gizmos.DrawSphere(boxCast.Hit.point, 0.1f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(boxCast.Hit.point, boxCast.Hit.point + boxCast.Hit.normal * 0.5f);
            }
        }
    }
}
