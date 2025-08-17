using System.Collections.Generic;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using Debugging.Enum;
using DG.Tweening;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.UI;
using Gameplay.BattleSystem.UI.Base;
using Gameplay.BattleSystem.Units;
using Systems.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.TurnOrder
{
    public class TurnOrderView : BaseView
    {
        [Header("Container")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI _headerText;

        [Header("Current Turn")] [SerializeField]
        private Image _currentTurnBackground;
        [SerializeField] private Image _currentTurnIcon;
        [SerializeField] private TextMeshProUGUI _currentTurnName;

        [Header("Upcoming Turns")] [SerializeField]
        private RectTransform _upcomingContainer;
        [SerializeField] private TurnSlot _turnSlotPrefab;
        [SerializeField] private int _maxUpcomingSlots = 4;

        [Header("Round Display")]
        [SerializeField] private TextMeshProUGUI _roundText;

        [Header("Animation")] [SerializeField] private float _transitionDuration = 0.3f;
        [SerializeField] private float _roundChangeAnimDuration = 0.5f;

        [Header("Visual Settings")] [SerializeField]
        private Color _playerTurnColor = new Color(0.2f, 0.7f, 1f, 0.8f);
        [SerializeField] private Color _enemyTurnColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color _brokenUnitColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        [SerializeField] private Color _deadUnitColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        private readonly List<TurnSlot> _activeTurnSlots = new();
        private UIObjectPool<TurnSlot> _turnSlotPool;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _turnSlotPool = new UIObjectPool<TurnSlot>(_turnSlotPrefab, _upcomingContainer, 5);
            if (_headerText != null)
            {
                _headerText.text = "Turn Order";
            }
        }

        protected override void ValidateComponents()
        {
            if (_currentTurnBackground == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Current Turn Background is not set!", name), LogCategory.UI);
            }

            if (_currentTurnIcon == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Current Turn Icon is not set!", name), LogCategory.UI);
            }

            if (_currentTurnName == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Current Turn Name is not set!", name), LogCategory.UI);
            }

            if (_upcomingContainer == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Upcoming Container is not set!", name), LogCategory.UI);
            }

            if (_turnSlotPrefab == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Turn Slot Prefab is not set!", name), LogCategory.UI);
            }
        }

        #region Public Methods

        public void UpdateCurrentTurn(TurnOrderEntry entry)
        {
            // 아이콘 설정
            if (_currentTurnIcon != null && entry.Unit != null)
            {
                _currentTurnIcon.sprite = GetUnitIcon(entry.Unit);
                _currentTurnIcon.color = GetUnitIconColor(entry.Unit);
            }

            // 이름 설정
            if (_currentTurnName != null && entry.Unit != null)
            {
                _currentTurnName.text = entry.Unit.UnitName;
            }

            // 배경 색상 설정
            if (_currentTurnBackground != null && entry.Unit != null)
            {
                _currentTurnBackground.color = GetBackgroundColor(entry.Unit);
            }
        }

        public void UpdateTurnOrder(IReadOnlyList<TurnOrderEntry> turnOrder, int currentIndex)
        {
            // 기존 슬롯들 정리
            ClearActiveTurnSlots();

            if (turnOrder == null || turnOrder.Count <= 1)
            {
                return;
            }

            int totalUnits = turnOrder.Count;
            int slotsToShow = Mathf.Min(_maxUpcomingSlots, turnOrder.Count - 1);
            for (int i = 0; i < slotsToShow; i++)
            {
                var nextIndex = (currentIndex + i + 1) % totalUnits;
                var entry = turnOrder[nextIndex];
                if (entry != null && entry.Unit != null)
                {
                    CreateTurnSlot(entry.Unit, i - 1);
                }
            }
        }

        public void UpdateRound(int round)
        {
            if (_roundText != null)
            {
                _roundText.text = ZString.Format("Round {0}", round);
            }
        }

        public async UniTask AnimateTurnTransition()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            await _canvasGroup.DOFade(0.7f, _transitionDuration * 0.5f).ToUniTask();
            await _canvasGroup.DOFade(1f, _transitionDuration * 0.5f).ToUniTask();
        }

        public async UniTask AnimateRoundChange(int round)
        {
            if (_roundText == null)
            {
                return;
            }

            var originalScale = _roundText.transform.localScale;

            await _roundText.transform
                .DOScale(originalScale * 1.3f, _roundChangeAnimDuration * 0.5f)
                .SetEase(Ease.OutBack)
                .ToUniTask();

            _roundText.text = ZString.Format("Round {0}", round);

            await _roundText.transform
                .DOScale(originalScale, _roundChangeAnimDuration * 0.5f)
                .SetEase(Ease.InBack)
                .ToUniTask();
        }

        #endregion

        #region Private Methods

        private void ClearActiveTurnSlots()
        {
            foreach (var slotComponent in _activeTurnSlots)
            {
                if (slotComponent != null)
                {
                    _turnSlotPool.Return(slotComponent);
                }
            }

            _activeTurnSlots.Clear();
        }

        private void CreateTurnSlot(BattleUnit unit, int index)
        {
            var slotComponent = _turnSlotPool.Get();

            // 슬롯 설정
            slotComponent.Setup(
                GetUnitIcon(unit),
                GetUnitIconColor(unit),
                unit.UnitName
            );

            // 순서별 투명도 설정
            float alpha = 1f - (index * 0.15f);
            slotComponent.SetOpacity(Mathf.Max(alpha, 0.4f));

            _activeTurnSlots.Add(slotComponent);
        }

        #endregion

        #region Getter

        private Sprite GetUnitIcon(BattleUnit unit)
        {
            return unit.UnitConfig.UnitIcon;
        }

        private Color GetUnitIconColor(BattleUnit unit)
        {
            if (!unit.Health.IsAlive)
                return _deadUnitColor;

            if (unit.Shield.IsBroken)
                return _brokenUnitColor;

            return Color.white;
        }

        private Color GetBackgroundColor(BattleUnit unit)
        {
            if (!unit.Health.IsAlive)
                return _deadUnitColor;

            if (unit.Shield.IsBroken)
                return _brokenUnitColor;

            return unit is PlayerUnit ? _playerTurnColor : _enemyTurnColor;
        }

        #endregion

        protected override void HandleDestruction()
        {
            ClearActiveTurnSlots();
            base.HandleDestruction();
        }
    }
}
