using System.Collections.Generic;
using Data.Animations;
using Data.Platformer.Enums;
using UnityEngine;

namespace Data.Player.Animations
{
    [CreateAssetMenu(fileName = nameof(PlayerAnimationRegistry), menuName = "Gameplay/Player/" + nameof(PlayerAnimationRegistry))]
    public class PlayerAnimationRegistry : AnimationRegistryBase
    {
        [System.Serializable]
        public class PlayerStateAnimationPair
        {
            public PlatformerStateType state;
            public AnimationData animation;
        }

        [SerializeField] private PlayerStateAnimationPair[] _animations;
        private Dictionary<PlatformerStateType, AnimationData> _cache;

        public override AnimationData GetAnimation<T>(T state)
        {
            if (state is not PlatformerStateType playerState)
            {
                return null;
            }

            if (_cache == null)
            {
                BuildCache();
            }
            return _cache != null && _cache.TryGetValue(playerState, out var anim) ? anim : null;
        }

        public AnimationData GetAnimation(PlatformerStateType state)
        {
            if (_cache == null)
            {
                BuildCache();
            }
            return _cache != null && _cache.TryGetValue(state, out var anim) ? anim : null;
        }

        private void BuildCache()
        {
            _cache = new Dictionary<PlatformerStateType, AnimationData>();

            foreach (var pair in _animations)
            {
                if (pair.animation != null)
                {
                    _cache[pair.state] = pair.animation;
                }
            }
        }
    }
}
