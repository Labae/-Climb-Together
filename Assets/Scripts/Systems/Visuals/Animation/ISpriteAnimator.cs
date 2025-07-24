using System;
using Data.Animations;

namespace Systems.Visuals.Animation
{
    public interface ISpriteAnimator
    {
        // Properties
        AnimationData CurrentAnimation { get; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        int CurrentFrame { get; }
        float CurrentTime { get; }
        float NormalizedTime { get; }

        // Events
        event Action<string> OnAnimationEvent;
        event Action<AnimationData> OnAnimationComplete;
        event Action<AnimationData> OnAnimationStart;
        event Action<int> OnFrameChanged;

        // Control Methods
        void Play(AnimationData animationData);
        void Play();
        void Pause();
        void Resume();
        void Stop();
        void SetFrame(int frameIndex);
        void SetTime(float time);
        void SetNormalizedTime(float normalizedTime);

        // Animation Management
        void SetAnimation(AnimationData animationData);
        bool HasAnimation();

        // Update
        void Update(float deltaTime);
    }
}
