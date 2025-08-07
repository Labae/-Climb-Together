using System;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using R3;
using Systems.UI.Core;
using Systems.UI.Interfaces;
using Systems.UI.Popups;
using UnityEngine;

namespace Systems.UI
{
    public class UISystemTester : MonoBehaviour
    {
        private IUIManager _uiManager;
        private readonly CompositeDisposable _disposables = new();

        private void Start()
        {
            _uiManager = FindAnyObjectByType<UIManager>();
            UISignalHub.GetSignal<string>()
                .AsObservable()
                .Subscribe(OnSignalReceived)
                .AddTo(_disposables);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                TestSimplePopup().Forget();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Y))
            {
                TestPopupWithData().Forget();
            }
        }

        private async UniTaskVoid TestSimplePopup()
        {
            GameLogger.Debug("Testing simple popup...");

            try
            {
                var popup = await _uiManager.ShowPopupAsync<SimplePopup>(PopupIds.SimpleTest, "Hello World!");
                if (popup != null)
                {
                    GameLogger.Debug("Simple popup shown successfully!");
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to show simple popup: ", e.Message));
            }
        }

        private async UniTaskVoid TestPopupWithData()
        {
            GameLogger.Debug("Testing popup with data object...");

            try
            {
                var data = new TestPopupData("Test!", "This is a test! \n\n Hello World!");
                var popup = await _uiManager.ShowPopupAsync<SimplePopup>(PopupIds.SimpleTest, data);
                if (popup != null)
                {
                    GameLogger.Debug("Data popup shown successfully!");
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to show data popup: ", e.Message));
            }
        }


        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnSignalReceived(string signal)
        {
            if (signal.StartsWith("TestPopup_"))
            {
                string action = signal.Replace("TestPopup_", "");

                switch (action)
                {
                    case "OK":
                        GameLogger.Debug("사용자가 확인 버튼을 클릭했습니다");
                        break;
                    case "Cancel":
                        GameLogger.Debug("사용자가 취소 버튼을 클릭했습니다");
                        break;
                    default:
                        GameLogger.Debug(ZString.Concat("알 수 없는 팝업 시그널: ", signal));
                        break;
                }
            }
        }
    }
}
