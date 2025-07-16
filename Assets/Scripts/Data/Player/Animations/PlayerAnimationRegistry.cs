using System.Collections.Generic;
using Data.Animations;
using Data.Player.Enums;
using UnityEngine;

namespace Data.Player.Animations
{
    [CreateAssetMenu(fileName = nameof(PlayerAnimationRegistry), menuName = "Gameplay/Player/" + nameof(PlayerAnimationRegistry))]
    public class PlayerAnimationRegistry : AnimationRegistryBase
    {
        [System.Serializable]
        public class PlayerStateAnimationPair
        {
            public PlayerStateType state;
            public AnimationData animation;
        }

        [SerializeField] private PlayerStateAnimationPair[] _animations;
        private Dictionary<PlayerStateType, AnimationData> _cache;

        public override AnimationData GetAnimation<T>(T state)
        {
            if (state is not PlayerStateType playerState)
            {
                return null;
            }

            if (_cache == null)
            {
                BuildCache();
            }
            return _cache != null && _cache.TryGetValue(playerState, out var anim) ? anim : null;
        }

        public AnimationData GetAnimation(PlayerStateType state)
        {
            if (_cache == null)
            {
                BuildCache();
            }
            return _cache != null && _cache.TryGetValue(state, out var anim) ? anim : null;
        }

        private void BuildCache()
        {
            _cache = new Dictionary<PlayerStateType, AnimationData>();

            foreach(var pair in _animations)
            {
                if(pair.animation != null)
                {
                    _cache[pair.state] = pair.animation;
                }
            }
        }
    }
}
