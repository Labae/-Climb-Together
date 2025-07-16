using UnityEngine;

namespace Data.Animations
{
    public abstract class AnimationRegistryBase : ScriptableObject
    {
        public abstract AnimationData GetAnimation<T>(T state) where T : System.Enum;
    }
}
