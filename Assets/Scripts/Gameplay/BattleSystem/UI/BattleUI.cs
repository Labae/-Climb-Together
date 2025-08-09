using System;
using System.Collections.Generic;
using Core.Utilities;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using Debugging.Enum;
using DG.Tweening;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Enum;
using Gameplay.BattleSystem.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BattleSystem.UI
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField]
        private Button _swordButton;

        [SerializeField] private Button _bowButton;
        [SerializeField] private Button _fireButton;

        [Header("Target Selection")]
        [SerializeField]
        private GameObject _targetSelectionPanel;

        [SerializeField] private Button _targetButtonPrefab;
        [SerializeField] private Transform _targetButtonParent;
        [SerializeField] private TextMeshProUGUI _targetSelectionTitle;

        [Header("Battle Result")]
        [SerializeField]
        private GameObject _battleResultContainer;

        [SerializeField] private TextMeshProUGUI _battleResultText; // ← 메인 텍스트
        [SerializeField] private TextMeshProUGUI _battleSubText; // ← 서브 텍스트
        [SerializeField] private TextMeshProUGUI _battleStatsText; // ← 통계 텍스트

        [Header("Result Buttons")]
        [SerializeField]
        private Button _continueButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Enemy Stats UI")]
        [SerializeField]
        private RectTransform _enemyStatsContainer;

        [SerializeField] private GameObject _enemyStatsUIPrefab;

        [Header("Enemy UI Entrance Animation Settings")]
        [SerializeField]
        private bool _enableUIEntranceAnimation = true;

        [SerializeField] private float _entranceAnimationDuration = 0.3f;
        [SerializeField] private float _entranceMoveDistance = 50f;
        [SerializeField] private Ease _entranceEase = Ease.OutBack;

        // Events
        public event Action<WeaponType> OnAttackButtonClicked;
        public event Action<EnemyUnit, WeaponType> OnTargetSelected;

        private List<Button> _activeTargetButtons = new();
        private List<EnemyStatsUI> _enemyStatsUIs = new();
        private WeaponType _selectedWeaponType; // 현재 선택된 무기

        private void Awake()
        {
            SetupButtons();
            HideActionButtons();
            HideTargetSelection();
            HideBattleResult();
        }

        public void Initialize()
        {
            GameLogger.Debug("BattleUI Initialized", LogCategory.Battle);
        }

        private void SetupButtons()
        {
            // 무기별 공격 버튼 설정
            if (_swordButton != null)
                _swordButton.onClick.AddListener(() => OnAttackButtonClicked?.Invoke(WeaponType.Sword));

            if (_bowButton != null)
                _bowButton.onClick.AddListener(() => OnAttackButtonClicked?.Invoke(WeaponType.Bow));

            if (_fireButton != null)
                _fireButton.onClick.AddListener(() => OnAttackButtonClicked?.Invoke(WeaponType.Fire));
        }

        public void ShowActionButtons()
        {
            SetActionButtonsActive(true);
            GameLogger.Debug("Action buttons shown", LogCategory.Battle);
        }

        public void HideActionButtons()
        {
            SetActionButtonsActive(false);
            HideTargetSelection(); // 타겟 선택도 함께 숨김
            GameLogger.Debug("Action buttons hidden", LogCategory.Battle);
        }

        private void SetActionButtonsActive(bool active)
        {
            if (_swordButton != null) _swordButton.gameObject.SetActive(active);
            if (_bowButton != null) _bowButton.gameObject.SetActive(active);
            if (_fireButton != null) _fireButton.gameObject.SetActive(active);
        }

        #region Enemy Stats UI Management

        public async UniTask SetupEnemyStatsUIs(List<EnemyUnit> enemyUnits)
        {
            if (_enemyStatsContainer == null || _enemyStatsUIPrefab == null)
            {
                GameLogger.Error("Enemy Stats Container 또는 EnemyStatsUIPrefab이 Null입니다", LogCategory.Battle);
                return;
            }

            await ClearEnemyStatsUIs();

            var canvasGroup = _enemyStatsContainer.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;

            for (int i = 0; i < enemyUnits.Count; i++)
            {
                var enemy = enemyUnits[i];
                if (enemy != null)
                {
                    await CreateEnemyStatsUIs(enemy, i);
                }
            }

            if (_enableUIEntranceAnimation)
            {
                await PlayEntranceAnimation(canvasGroup);
            }
        }

        private async UniTask CreateEnemyStatsUIs(EnemyUnit enemy, int index)
        {
            var uiObject = Instantiate(_enemyStatsUIPrefab, _enemyStatsContainer);
            var statsUI = uiObject.GetComponent<EnemyStatsUI>();
            if (statsUI != null)
            {
                statsUI.Initialize(enemy);
                _enemyStatsUIs.Add(statsUI);
                GameLogger.Debug(ZString.Format("Enemy Stats UI 생성: {0} (인덱스: {1})"
                    , enemy.UnitName, index), LogCategory.Battle);
            }
            else
            {
                GameLogger.Error("EnemyStatsUI를 찾을 수 없습니다", LogCategory.Battle);
                Destroy(uiObject);
            }

            await UniTask.Yield();
        }

        private async UniTask ClearEnemyStatsUIs()
        {
            foreach (var statsUI in _enemyStatsUIs)
            {
                if (statsUI != null)
                {
                    Destroy(statsUI.gameObject);
                }
            }

            _enemyStatsUIs.Clear();
            await UniTask.Yield();
        }

        #endregion

        #region Target Selection

        public void ShowTargetSelection(List<EnemyUnit> availableTargets, WeaponType weaponType)
        {
            if (_targetSelectionPanel == null || _targetButtonPrefab == null || _targetButtonParent == null)
            {
                GameLogger.Error("Target selection UI components not set up!", LogCategory.Battle);
                return;
            }

            _selectedWeaponType = weaponType;

            // 제목 업데이트
            if (_targetSelectionTitle != null)
            {
                _targetSelectionTitle.text = $"{weaponType} 공격 대상 선택";
            }

            // 기존 버튼들 제거
            ClearTargetButtons();

            // 각 적에 대한 버튼 생성
            foreach (var enemy in availableTargets)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    CreateTargetButton(enemy);
                }
            }

            _targetSelectionPanel.SetActive(true);
            GameLogger.Debug($"Target selection shown with {availableTargets.Count} targets for {weaponType}",
                LogCategory.Battle);
        }

        private void CreateTargetButton(EnemyUnit enemy)
        {
            var button = Instantiate(_targetButtonPrefab, _targetButtonParent);

            // 버튼 텍스트 설정
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            bool isWeakness = IsWeakToSelectedWeapon(enemy);
            if (buttonText != null)
            {
                buttonText.text = isWeakness ? ZString.Concat(enemy.UnitName, "(약점!)") : enemy.UnitName;
            }

            var weaknessIcon = CoreHelpers.FindComponentInChildren<Image>(button.transform);
            if (weaknessIcon != null)
            {
                weaknessIcon.gameObject.SetActive(isWeakness);
            }

            // 클릭 이벤트 설정
            button.onClick.AddListener(() => OnTargetButtonClicked(enemy));

            _activeTargetButtons.Add(button);
        }

        private bool IsWeakToSelectedWeapon(EnemyUnit enemy)
        {
            if (enemy.Weaknesses == null) return false;

            foreach (var weakness in enemy.Weaknesses)
            {
                if (weakness == _selectedWeaponType)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnTargetButtonClicked(EnemyUnit target)
        {
            HideTargetSelection();
            OnTargetSelected?.Invoke(target, _selectedWeaponType);
        }

        public void HideTargetSelection()
        {
            if (_targetSelectionPanel != null)
            {
                _targetSelectionPanel.SetActive(false);
            }

            ClearTargetButtons();
        }

        private void ClearTargetButtons()
        {
            foreach (var button in _activeTargetButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _activeTargetButtons.Clear();
        }

        #endregion

        #region Battle Events

        public void ShowBattleResult(BattleUnit winner)
        {
            if (_battleResultContainer != null)
            {
                _battleResultContainer.SetActive(true);
            }

            if (_battleResultText != null)
            {
                bool isPlayerWin = winner != null && winner is PlayerUnit;

                if (isPlayerWin)
                {
                    ShowVictoryResult();
                }
                else
                {
                    ShowDefeatResult();
                }
            }

            GameLogger.Info($"Battle Result: {winner?.UnitName ?? "Draw"}", LogCategory.Battle);
        }

        private void ShowVictoryResult()
        {
            _battleResultText.text = " 승리! ";
            _battleResultText.color = new Color(0.3f, 0.69f, 0.31f, 1f); // 초록색

            if (_battleSubText != null)
            {
                _battleSubText.text = "모든 적을 물리쳤습니다!";
            }

            // 승리 애니메이션
            VictoryAnimationAsync().Forget();
        }

        private void ShowDefeatResult()
        {
            _battleResultText.text = " 패배... ";
            _battleResultText.color = new Color(0.96f, 0.26f, 0.21f, 1f); // 빨간색

            if (_battleSubText != null)
            {
                _battleSubText.text = "다시 도전해보세요!";
            }

            // 패배 애니메이션
            DefeatAnimationAsync().Forget();
        }

        public void HideBattleResult()
        {
            if (_battleResultContainer != null)
            {
                _battleResultContainer.SetActive(false);
            }
        }

        // 승리 애니메이션
        private async UniTaskVoid VictoryAnimationAsync()
        {
            if (_battleResultText == null)
            {
                return;
            }

            Transform textTransform = _battleResultText.transform;
            Vector3 originalScale = textTransform.localScale;

            // 크기 애니메이션
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 0.5 → 1.1 → 1.0 크기 변화
                float scale;
                if (progress < 0.5f)
                {
                    scale = Mathf.Lerp(0.5f, 1.1f, progress * 2f);
                }
                else
                {
                    scale = Mathf.Lerp(1.1f, 1f, (progress - 0.5f) * 2f);
                }

                textTransform.localScale = originalScale * scale;
                await UniTask.Yield();
            }

            textTransform.localScale = originalScale;
        }

        // 패배 애니메이션
        private async UniTaskVoid DefeatAnimationAsync()
        {
            if (_battleResultText == null)
            {
                return;
            }

            // 깜빡임 효과
            Color originalColor = _battleResultText.color;

            for (int i = 0; i < 3; i++)
            {
                _battleResultText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
                await UniTask.WaitForSeconds(0.2f);

                _battleResultText.color = originalColor;
                await UniTask.WaitForSeconds(0.2f);
            }
        }

        #endregion

        #region Entrance Animation Methods

        private async UniTask PlayEntranceAnimation(CanvasGroup canvasGroup)
        {
            var originalPosition = _enemyStatsContainer.anchoredPosition;
            var startPosition = originalPosition + Vector2.up * _entranceMoveDistance;
            var originalScale = _enemyStatsContainer.localScale;

            _enemyStatsContainer.anchoredPosition = startPosition;
            _enemyStatsContainer.localScale = originalScale * 0.8f;

            var sequence = DOTween.Sequence();

            sequence.Append(_enemyStatsContainer.DOAnchorPos(originalPosition, _entranceAnimationDuration))
                .SetEase(_entranceEase);
            sequence.Join(_enemyStatsContainer.DOScale(originalScale, _entranceAnimationDuration))
                .SetEase(_entranceEase);
            sequence.Join(canvasGroup.DOFade(1f, _entranceAnimationDuration))
                .SetEase(_entranceEase);

            await sequence.ToUniTask();
        }

        #endregion

        private void OnDestroy()
        {
            ClearTargetButtons();
            ClearEnemyStatsUIs().Forget();
        }
    }
}
