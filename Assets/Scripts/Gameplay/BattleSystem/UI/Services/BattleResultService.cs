using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.Units;
using TMPro;
using UnityEngine;

namespace Gameplay.BattleSystem.UI.Services
{
    /// <summary>
    /// 전투 결과 표시 서비스
    /// </summary>
    public class BattleResultService
    {
        private readonly Transform _resultContainer;
        private readonly TextMeshProUGUI _resultText;
        private readonly TextMeshProUGUI _subText;

        public BattleResultService(Transform resultContainer, TextMeshProUGUI resultText, TextMeshProUGUI subText)
        {
            _resultContainer = resultContainer;
            _resultText = resultText;
            _subText = subText;
        }

        public void ShowBattleResult(BattleUnit winner)
        {
            bool isPlayerWin = winner != null && winner is PlayerUnit;

            if (isPlayerWin)
            {
                ShowVictory();
            }
            else
            {
                ShowDefeat();
            }

            _resultContainer.gameObject.SetActive(true);
        }

        public void HideBattleResult()
        {
            _resultContainer.gameObject.SetActive(false);
        }

        private void ShowVictory()
        {
            _resultText.text = "Victory!";
            _resultText.color = new Color(0.3f, 0.7f, 0.3f);

            if (_subText != null)
            {
                _subText.text = "모든 적을 물리쳤습니다!";
            }
        }

        private void ShowDefeat()
        {
            _resultText.text = "Defeat...";
            _resultText.color = new Color(0.95f, 0.25f, 0.2f);

            if (_subText != null)
            {
                _subText.text = "다시 도전해보세요!";
            }
        }
    }
}
