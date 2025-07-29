using System;
using Data.Animations;
using Debugging;
using R3;
using UnityEngine;

namespace Systems.Visuals.Animation
{
    public class SpriteAnimator : ISpriteAnimator
    {
        // Interface Properties
        public AnimationData CurrentAnimation { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public int CurrentFrame { get; private set; }
        public float CurrentTime { get; private set; }
        public float NormalizedTime => CurrentAnimation != null && CurrentAnimation.GetTotalDuration() > 0
            ? CurrentTime / CurrentAnimation.GetTotalDuration() : 0f;

        public float SpeedMultiplier { get; set; } = 1f;

        // Events
        public event Action<string> OnAnimationEvent;
        public event Action<AnimationData> OnAnimationComplete;
        public event Action<AnimationData> OnAnimationStart;
        public event Action<int> OnFrameChanged;

        // Private Fields
        private int _previousFrame = -1;
        private SpriteRenderer _spriteRenderer;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        // Constructor
        public SpriteAnimator(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
            SpeedMultiplier = 1f;

            Observable.EveryUpdate()
                .Subscribe(_ => Update())
                .AddTo(_disposables);
        }

        // Update Method
        private void Update()
        {
            if (IsPlaying && !IsPaused && CurrentAnimation != null)
            {
                UpdateAnimation(Time.deltaTime);
            }
        }

        private void UpdateAnimation(float deltaTime)
        {
            CurrentTime += deltaTime * SpeedMultiplier * CurrentAnimation.SpeedMultiplier;

            var totalDuration = CurrentAnimation.GetTotalDuration();

            // Check for completion
            if (CurrentTime >= totalDuration)
            {
                if (CurrentAnimation.Loop)
                {
                    CurrentTime = 0f;
                }
                else
                {
                    CurrentTime = totalDuration;
                    IsPlaying = false;
                    OnAnimationComplete?.Invoke(CurrentAnimation);
                }
            }

            // Update current frame
            int newFrame = CurrentAnimation.GetFrameAtTime(CurrentTime);
            if (newFrame != _previousFrame)
            {
                CurrentFrame = newFrame;
                OnFrameChanged?.Invoke(CurrentFrame);

                // Trigger frame events
                var frame = CurrentAnimation.GetFrame(CurrentFrame);
                if (frame != null)
                {
                    if (frame.Sprite != null)
                    {
                        _spriteRenderer.sprite = frame.Sprite;
                    }

                    if (frame.TriggerEvent && !string.IsNullOrEmpty(frame.EventName))
                    {
                        OnAnimationEvent?.Invoke(frame.EventName);
                    }
                }

                _previousFrame = CurrentFrame;
            }
        }

        // Interface Implementation
        public void Play(AnimationData animationData)
        {
            SetAnimation(animationData);
            Play();
        }

        public void Play()
        {
            if (CurrentAnimation == null)
            {
                GameLogger.Warning("No animation set");
                return;
            }

            if (!IsPlaying)
            {
                IsPlaying = true;
                IsPaused = false;
                OnAnimationStart?.Invoke(CurrentAnimation);
            }
            else if (IsPaused)
            {
                Resume();
            }
        }

        public void Pause()
        {
            if (IsPlaying && !IsPaused)
            {
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsPlaying && IsPaused)
            {
                IsPaused = false;
            }
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentTime = 0f;
            CurrentFrame = 0;
            _previousFrame = -1;
        }

        public void SetFrame(int frameIndex)
        {
            if (CurrentAnimation == null || frameIndex < 0 || frameIndex >= CurrentAnimation.FrameCount)
            {
                return;
            }

            CurrentFrame = frameIndex;

            // Calculate time for this frame
            float timeToFrame = 0f;
            for (int i = 0; i < frameIndex && i < CurrentAnimation.Frames.Length; i++)
            {
                if (CurrentAnimation.Frames[i] != null)
                    timeToFrame += CurrentAnimation.Frames[i].Duration;
            }

            // Set to middle of the frame
            if (frameIndex < CurrentAnimation.Frames.Length && CurrentAnimation.Frames[frameIndex] != null)
            {
                timeToFrame += CurrentAnimation.Frames[frameIndex].Duration * 0.5f;
            }

            CurrentTime = timeToFrame;
            OnFrameChanged?.Invoke(CurrentFrame);
            _previousFrame = CurrentFrame;
        }

        public void SetTime(float time)
        {
            if (CurrentAnimation == null) return;

            CurrentTime = Mathf.Clamp(time, 0f, CurrentAnimation.GetTotalDuration());
            CurrentFrame = CurrentAnimation.GetFrameAtTime(CurrentTime);

            if (CurrentFrame != _previousFrame)
            {
                OnFrameChanged?.Invoke(CurrentFrame);
                _previousFrame = CurrentFrame;
            }
        }

        public void SetNormalizedTime(float normalizedTime)
        {
            if (CurrentAnimation == null) return;

            float targetTime = Mathf.Clamp01(normalizedTime) * CurrentAnimation.GetTotalDuration();
            SetTime(targetTime);
        }

        public void SetAnimation(AnimationData animationData)
        {
            if (animationData == null)
            {
                GameLogger.Warning("Trying to set null animation");
                return;
            }

            bool wasPlaying = IsPlaying;
            Stop();

            CurrentAnimation = animationData;
            CurrentTime = 0f;
            CurrentFrame = 0;
            _previousFrame = -1;

            if (wasPlaying)
            {
                Play();
            }
        }

        public bool HasAnimation()
        {
            return CurrentAnimation != null;
        }

        // Utility Methods
        public void NextFrame()
        {
            if (CurrentAnimation == null) return;

            int nextFrame = Mathf.Min(CurrentFrame + 1, CurrentAnimation.FrameCount - 1);
            SetFrame(nextFrame);
        }

        public void PreviousFrame()
        {
            if (CurrentAnimation == null) return;

            int prevFrame = Mathf.Max(CurrentFrame - 1, 0);
            SetFrame(prevFrame);
        }

        public AnimationFrame GetCurrentFrame()
        {
            return CurrentAnimation?.GetFrame(CurrentFrame);
        }

        public bool IsAnimationComplete()
        {
            return CurrentAnimation != null && !IsPlaying && CurrentTime >= CurrentAnimation.GetTotalDuration();
        }

        public float GetRemainingTime()
        {
            if (CurrentAnimation == null) return 0f;
            return Mathf.Max(0f, CurrentAnimation.GetTotalDuration() - CurrentTime);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
