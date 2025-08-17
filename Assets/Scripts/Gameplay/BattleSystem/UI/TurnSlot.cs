using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    /// <summary>
    /// Turn Slot 컴포넌트 - UIObjectPool 사용을 위한 래퍼
    /// </summary>
    public class TurnSlot : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image _unitIcon;
        [SerializeField] private TextMeshProUGUI _unitName;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            // CanvasGroup 추가
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void SetIcon(Sprite icon)
        {
            if (_unitIcon != null)
            {
                _unitIcon.sprite = icon;
            }
        }

        public void SetIconColor(Color color)
        {
            if (_unitIcon != null)
            {
                _unitIcon.color = color;
            }
        }

        public void SetName(string unitName)
        {
            if (_unitName != null)
            {
                _unitName.text = unitName;
            }
        }

        public void SetOpacity(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }

        public void Setup(Sprite icon, Color iconColor, string unitName, float opacity = 1f)
        {
            SetIcon(icon);
            SetIconColor(iconColor);
            SetName(unitName);
            SetOpacity(opacity);
        }
    }
}
