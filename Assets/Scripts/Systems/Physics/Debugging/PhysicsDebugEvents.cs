using System;

namespace Systems.Physics.Debugging
{
    public static class PhysicsDebugEvents
    {
        public static event Action<BoxCastDebugInfo> OnBoxCastPerformed;

        public static void NotifyBoxCast(BoxCastDebugInfo info)
        {
            OnBoxCastPerformed?.Invoke(info);
        }
    }
}
