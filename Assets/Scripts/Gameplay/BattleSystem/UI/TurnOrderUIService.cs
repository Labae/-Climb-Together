using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Core.Services;
using Gameplay.BattleSystem.Units;
using Systems.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    /// <summary>
    /// 턴 순서 UI 관리 서비스 (기존 구조에 맞춤)
    /// </summary>
    public class TurnOrderUIService : IDisposable
    {
        private readonly CanvasGroup _canvasGroup;
        private readonly TextMeshProUGUI _headerText;

        // Current Turn
        private readonly Image _currentTurnBackground;
        private readonly Image _currentTurnIcon;
        private readonly TextMeshProUGUI _currentTurnName;

        // Upcoming Turns with Pool
        private readonly Transform _upcomingContainer;
        private readonly UIObjectPool<TurnSlot> _turnSlotPool;
        private readonly List<TurnSlot> _activeTurnSlots = new();

        // Visual Settings
        private readonly Color _playerTurnColor = new Color(0.2f, 0.7f, 1f, 0.8f);
        private readonly Color _enemyTurnColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        private readonly Color _brokenUnitColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        private readonly Color _deadUnitColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        // Animation
        private readonly float _transitionDuration = 0.3f;
        private readonly int _maxUpcomingSlots = 4;

        // Dependencies
        private readonly TurnOrderService _turnOrderService;

        public TurnOrderUIService(
            CanvasGroup canvasGroup,
            TextMeshProUGUI headerText,
            Image currentTurnBackground,
            Image currentTurnIcon,
            TextMeshProUGUI currentTurnName,
            Transform upcomingContainer,
            TurnSlot turnSlotPrefab,
            TurnOrderService turnOrderService,
            int initialPoolSize = 6)
        {
            _canvasGroup = canvasGroup;
            _headerText = headerText;
            _currentTurnBackground = currentTurnBackground;
            _currentTurnIcon = currentTurnIcon;
            _currentTurnName = currentTurnName;
            _upcomingContainer = upcomingContainer;
            _turnOrderService = turnOrderService;

            // UIObjectPool 초기화
            _turnSlotPool = new UIObjectPool<TurnSlot>(turnSlotPrefab, upcomingContainer, initialPoolSize);
        }

        public void Initialize()
        {
            SetupHeader();
            RefreshTurnOrder();
        }

        private void SetupHeader()
        {
            if (_headerText != null)
            {
                _headerText.text = "Turn Order";
            }
        }

        public void RefreshTurnOrder()
        {
            if (_turnOrderService == null)
                return;

            var turnOrder = _turnOrderService.TurnOrder;
            if (turnOrder.Count == 0)
                return;

            UpdateCurrentTurn(turnOrder[0]);
            UpdateUpcomingTurns(turnOrder);
        }

        private void UpdateCurrentTurn(TurnOrderEntry entry)
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

        private void UpdateUpcomingTurns(IReadOnlyList<TurnOrderEntry> turnOrder)
        {
            // 기존 슬롯들 정리
            ClearActiveTurnSlots();

            // 새 슬롯들 생성
            for (int i = 1; i < turnOrder.Count && i <= _maxUpcomingSlots; i++)
            {
                var entry = turnOrder[i];
                if (entry != null && entry.Unit != null)
                {
                  CreateTurnSlot(entry.Unit, i - 1);
                }
            }
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

        public async UniTask AnimateTurnTransition()
        {
            if (_canvasGroup != null)
            {
                await _canvasGroup.DOFade(0.7f, _transitionDuration * 0.5f).ToUniTask();
                RefreshTurnOrder();
                await _canvasGroup.DOFade(1f, _transitionDuration * 0.5f).ToUniTask();
            }
            else
            {
                RefreshTurnOrder();
            }
        }

        public void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }

        public void Dispose()
        {
            ClearActiveTurnSlots();
        }
    }
}
