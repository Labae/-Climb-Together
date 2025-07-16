using Data.Animations;
using UnityEditor;
using UnityEngine;

namespace Editor.Data.Animations
{
    [CustomPropertyDrawer(typeof(AnimationFrame))]
    public class AnimationFramePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 스프라이트 프로퍼티 찾기
            var spriteProperty = property.FindPropertyRelative("Sprite");

            // 스프라이트가 있으면 썸네일 표시
            if (spriteProperty.objectReferenceValue != null)
            {
                var sprite = spriteProperty.objectReferenceValue as Sprite;
                var texture = AssetPreview.GetAssetPreview(sprite);
                if (texture != null)
                {
                    var thumbnailRect = new Rect(position.x, position.y, 50, 50);
                    EditorGUI.DrawTextureTransparent(thumbnailRect, texture);
                    position.x += 55;
                    position.width -= 55;
                }
            }

            // 기본 프로퍼티 필드 그리기
            EditorGUI.PropertyField(position, property, label, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
