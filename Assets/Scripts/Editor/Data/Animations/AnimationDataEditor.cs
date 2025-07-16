using Data.Animations;
using UnityEditor;
using UnityEngine;

namespace Editor.Data.Animations
{
    [CustomEditor(typeof(AnimationData))]
    public class AnimationDataEditor : UnityEditor.Editor
    {
        private AnimationData _animationData;
        private float _previewTime = 0f;
        private int _currentPreviewFrame = 0;

        // 애니메이션 재생 관련
        private bool _isPlaying = false;
        private float _currentTime = 0f;
        private double _lastTime;

        private void OnEnable()
        {
            _animationData = (AnimationData)target;
            UpdatePreviewFrame();
            _lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_isPlaying && _animationData != null && _animationData.Frames != null &&
                _animationData.Frames.Length > 0)
            {
                double currentEditorTime = EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentEditorTime - _lastTime);
                _lastTime = currentEditorTime;

                _currentTime += deltaTime * _animationData.SpeedMultiplier;

                float totalDuration = _animationData.GetTotalDuration();
                if (totalDuration > 0)
                {
                    if (_currentTime >= totalDuration)
                    {
                        if (_animationData.Loop)
                        {
                            _currentTime = 0f;
                        }
                        else
                        {
                            _isPlaying = false;
                            _currentTime = totalDuration;
                        }
                    }

                    _previewTime = _currentTime / totalDuration;
                    _currentPreviewFrame = _animationData.GetFrameAtTime(_currentTime);
                    Repaint();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // 기본 Inspector 그리기
            DrawDefaultInspector();

            // 프리뷰 섹션 추가
            if (_animationData.Frames != null && _animationData.Frames.Length > 0)
            {
                DrawPreviewSection();
            }
        }

        private void DrawPreviewSection()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);

            // 재생 컨트롤 버튼들
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(_isPlaying ? "⏸ Pause" : "▶ Play"))
            {
                _isPlaying = !_isPlaying;
                if (_isPlaying)
                {
                    _lastTime = EditorApplication.timeSinceStartup;
                    _currentTime = _previewTime * _animationData.GetTotalDuration();
                }
            }

            if (GUILayout.Button("⏹ Stop"))
            {
                _isPlaying = false;
                _currentTime = 0f;
                _previewTime = 0f;
                UpdatePreviewFrame();
            }

            GUILayout.EndHorizontal();

            // 프리뷰 타임 슬라이더
            EditorGUI.BeginChangeCheck();
            float newPreviewTime = EditorGUILayout.Slider("Preview Time", _previewTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                _previewTime = newPreviewTime;
                _currentTime = _previewTime * _animationData.GetTotalDuration();
                _isPlaying = false; // 슬라이더 조작 시 재생 중지
                UpdatePreviewFrame();
            }

            // 현재 프레임 정보
            EditorGUILayout.LabelField("Current Frame", $"{_currentPreviewFrame + 1}/{_animationData.FrameCount}");

            // 프레임 탐색 버튼들
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("◀◀ First"))
            {
                _isPlaying = false;
                _previewTime = 0f;
                _currentTime = 0f;
                UpdatePreviewFrame();
            }

            if (GUILayout.Button("◀ Previous"))
            {
                _isPlaying = false;
                if (_currentPreviewFrame > 0)
                {
                    _currentPreviewFrame--;
                    _previewTime = GetNormalizedTimeForFrame(_currentPreviewFrame);
                    _currentTime = _previewTime * _animationData.GetTotalDuration();
                    // 데이터 업데이트
                    _animationData.PreviewTime = _previewTime;
                    _animationData.CurrentPreviewFrame = _currentPreviewFrame;
                }
            }

            if (GUILayout.Button("Next ▶"))
            {
                _isPlaying = false;
                if (_currentPreviewFrame < _animationData.FrameCount - 1)
                {
                    _currentPreviewFrame++;
                    _previewTime = GetNormalizedTimeForFrame(_currentPreviewFrame);
                    _currentTime = _previewTime * _animationData.GetTotalDuration();
                    // 데이터 업데이트
                    _animationData.PreviewTime = _previewTime;
                    _animationData.CurrentPreviewFrame = _currentPreviewFrame;
                }
            }

            if (GUILayout.Button("Last ▶▶"))
            {
                _isPlaying = false;
                _previewTime = 1f;
                _currentTime = _animationData.GetTotalDuration();
                UpdatePreviewFrame();
            }

            GUILayout.EndHorizontal();

            // 현재 프레임 정보 표시
            var currentFrame = _animationData.GetFrame(_currentPreviewFrame);
            if (currentFrame != null)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Frame Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Duration", $"{currentFrame.Duration:F2}s");
                EditorGUILayout.LabelField("Sprite", currentFrame.Sprite != null ? currentFrame.Sprite.name : "None");
                EditorGUILayout.LabelField("Transform",
                    $"Offset: {currentFrame.Offset} | Scale: {currentFrame.Scale} | Rotation: {currentFrame.Rotation:F1}°");
                EditorGUILayout.LabelField("Alpha", $"{currentFrame.Alpha:F2}");

                if (currentFrame.TriggerEvent && !string.IsNullOrEmpty(currentFrame.EventName))
                {
                    EditorGUILayout.LabelField("Event", currentFrame.EventName);
                }

                // 스프라이트 미리보기 - 크게 표시
                if (currentFrame.Sprite != null)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Sprite Preview", EditorStyles.boldLabel);

                    // AssetPreview 강제 로드
                    var texture = AssetPreview.GetAssetPreview(currentFrame.Sprite);
                    if (texture == null)
                    {
                        // AssetPreview가 아직 로드되지 않은 경우, 스프라이트 텍스처 직접 사용
                        if (currentFrame.Sprite.texture != null)
                        {
                            texture = currentFrame.Sprite.texture;
                        }
                    }

                    if (texture != null)
                    {
                        // 더 큰 프리뷰 크기
                        float maxWidth = EditorGUIUtility.currentViewWidth - 40;
                        float aspectRatio = (float)texture.width / texture.height;
                        float previewWidth = Mathf.Min(300, maxWidth);
                        float previewHeight = previewWidth / aspectRatio;

                        // 최대 높이 제한
                        if (previewHeight > 300)
                        {
                            previewHeight = 300;
                            previewWidth = previewHeight * aspectRatio;
                        }

                        var rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false));

                        // 센터 정렬
                        rect.x = (EditorGUIUtility.currentViewWidth - rect.width) * 0.5f;

                        // 알파값 적용 시뮬레이션
                        var oldColor = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, currentFrame.Alpha);

                        // 회전 적용
                        var matrix = GUI.matrix;
                        GUIUtility.RotateAroundPivot(currentFrame.Rotation, rect.center);

                        // 스케일 적용된 크기로 그리기
                        var scaledRect = new Rect(
                            rect.center.x - (rect.width * currentFrame.Scale.x * 0.5f),
                            rect.center.y - (rect.height * currentFrame.Scale.y * 0.5f),
                            rect.width * currentFrame.Scale.x,
                            rect.height * currentFrame.Scale.y
                        );

                        // 오프셋 적용
                        scaledRect.x += currentFrame.Offset.x;
                        scaledRect.y -= currentFrame.Offset.y; // Y축 반전

                        // 스프라이트가 있으면 올바른 UV로 그리기
                        if (currentFrame.Sprite.texture == texture)
                        {
                            // 스프라이트의 실제 UV 좌표 계산
                            Rect spriteRect = currentFrame.Sprite.rect;
                            Rect uvRect = new Rect(
                                spriteRect.x / texture.width,
                                spriteRect.y / texture.height,
                                spriteRect.width / texture.width,
                                spriteRect.height / texture.height
                            );
                            GUI.DrawTextureWithTexCoords(scaledRect, texture, uvRect);
                        }
                        else
                        {
                            EditorGUI.DrawTextureTransparent(scaledRect, texture);
                        }

                        GUI.matrix = matrix;
                        GUI.color = oldColor;

                        // 프리뷰 설정 표시
                        GUILayout.Space(5);
                        var infoStyle = new GUIStyle(EditorStyles.helpBox);
                        infoStyle.fontSize = 10;
                        EditorGUILayout.LabelField(
                            $"Preview shows: Alpha({currentFrame.Alpha:F2}) Scale({currentFrame.Scale}) Rotation({currentFrame.Rotation:F1}°) Offset({currentFrame.Offset})",
                            infoStyle);
                    }
                    else
                    {
                        // 텍스처를 로드할 수 없는 경우
                        EditorGUILayout.LabelField("Sprite preview not available", EditorStyles.centeredGreyMiniLabel);

                        // AssetPreview 다시 요청 (비동기 로딩)
                        if (AssetPreview.IsLoadingAssetPreview(currentFrame.Sprite.GetInstanceID()))
                        {
                            EditorGUILayout.LabelField("Loading preview...", EditorStyles.centeredGreyMiniLabel);
                            Repaint(); // 로딩이 완료되면 다시 그리기
                        }
                    }
                }
            }

            // 재생 상태 표시
            if (_isPlaying)
            {
                GUILayout.Space(5);
                var playingStyle = new GUIStyle(EditorStyles.helpBox);
                playingStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("▶ Playing...", playingStyle);
            }
        }

        private void UpdatePreviewFrame()
        {
            if (_animationData.Frames == null || _animationData.Frames.Length == 0)
            {
                _currentPreviewFrame = 0;
                return;
            }

            float totalTime = _animationData.GetTotalDuration();
            float currentTime = _previewTime * totalTime;
            _currentPreviewFrame = _animationData.GetFrameAtTime(currentTime);

            // 데이터 업데이트 (에디터 전용)
            _animationData.PreviewTime = _previewTime;
            _animationData.CurrentPreviewFrame = _currentPreviewFrame;
        }

        private float GetNormalizedTimeForFrame(int frameIndex)
        {
            if (_animationData.Frames == null || _animationData.Frames.Length == 0 || frameIndex < 0)
                return 0f;

            float totalDuration = _animationData.GetTotalDuration();
            if (totalDuration <= 0f)
                return 0f;

            float timeToFrame = 0f;
            for (int i = 0; i < frameIndex && i < _animationData.Frames.Length; i++)
            {
                if (_animationData.Frames[i] != null)
                    timeToFrame += _animationData.Frames[i].Duration;
            }

            // 프레임의 시작점을 반환 (중간점이 아닌)
            return Mathf.Clamp01(timeToFrame / totalDuration);
        }
    }
}
