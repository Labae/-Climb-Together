using System;
using NaughtyAttributes;
using UnityEngine;

namespace Data.Animations
{
    [Serializable]
    public class AnimationFrame
    {
        [Header("Frame Data")]
        public Sprite Sprite;

        [Range(0.01f, 2.0f)]
        public float Duration = 0.1f;

        [Header("Transform")]
        public Vector2 Offset = Vector2.zero;

        public Vector2 Scale = Vector2.one;

        [Range(-180.0f, 180.0f)]
        public float Rotation = 0.0f;

        [Header("Effects")]
        [Range(0f, 1.0f)]
        public float Alpha = 1.0f;

        [Header("Events")]
        public bool TriggerEvent = false;

        [ShowIf("TriggerEvent")]
        public string EventName = "";
    }

    [CreateAssetMenu(fileName = "New " + nameof(AnimationData), menuName = "Gameplay/Animation/" + nameof(AnimationData))]
    public class AnimationData : ScriptableObject
    {
        [Header("Animation Settings")]
        public string AnimationName = "";

        [TextArea(2, 4)]
        public string Description = "";

        [Space]
        public bool Loop = true;

        public bool PlayOnAwake = false;

        [Range(0.1f, 5.0f)]
        public float SpeedMultiplier = 1.0f;

        [Header("Frames")]
        [ReorderableList]
        public AnimationFrame[] Frames;

        [Header("Preview")]
        [ReadOnly]
        public float TotalDuration;

        [ReadOnly]
        public int FrameCount;

        // 프리뷰 관련 데이터는 Editor에서만 사용
        [HideInInspector]
        public float PreviewTime = 0f;

        [HideInInspector]
        public int CurrentPreviewFrame = 0;

        private void OnValidate()
        {
            UpdateAnimationInfo();
        }

        private void UpdateAnimationInfo()
        {
            if (Frames == null || Frames.Length == 0)
            {
                TotalDuration = 0.0f;
                FrameCount = 0;
                return;
            }

            FrameCount = Frames.Length;
            TotalDuration = GetTotalDuration();
        }

        public float GetTotalDuration()
        {
            if (Frames == null || Frames.Length == 0)
                return 0f;

            float total = 0f;
            foreach (var frame in Frames)
            {
                if (frame != null)
                {
                    total += frame.Duration;
                }
            }
            return total;
        }

        public int GetFrameAtTime(float time)
        {
            if (Frames == null || Frames.Length == 0)
                return 0;

            if (time <= 0f)
                return 0;

            float currentTime = 0f;
            for (int i = 0; i < Frames.Length; i++)
            {
                if (Frames[i] != null)
                {
                    float frameDuration = Frames[i].Duration;
                    if (time <= currentTime + frameDuration)
                        return i;
                    currentTime += frameDuration;
                }
            }
            return Frames.Length - 1;
        }

        public AnimationFrame GetFrame(int index)
        {
            if (Frames == null || index < 0 || index >= Frames.Length)
            {
                return null;
            }

            return Frames[index];
        }

        public int GetFrameCount()
        {
            return Frames?.Length ?? 0;
        }
    }
}
