using System;
using Systems.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.UI.Popups
{
    public class SimplePopup : UIPopupBase
    {
        [Header("Test Popup UI")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _contentText;
        [SerializeField] private Button _okButton;
        [SerializeField] private Button _closeButton;

        protected override void Awake()
        {
            base.Awake();
            _popupId = PopupIds.SimpleTest;

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_okButton != null)
            {
                _okButton.onClick.AddListener(OnOkButtonClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        protected override void OnDataSet(object data)
        {
            if (data is string message)
            {
                SetMessage(message);
            }
            else if (data is TestPopupData popupData)
            {
                SetTitleAndMessage(popupData.Title, popupData.Message);
            }
        }

        private void SetMessage(string message)
        {
            if (_titleText != null)
            {
                _titleText.text = "Alarm";
            }

            if (_contentText != null)
            {
                _contentText.text = message;
            }
        }

        private void SetTitleAndMessage(string title, string message)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_contentText != null)
            {
                _contentText.text = message;
            }
        }

        private void OnOkButtonClicked()
        {
            SendSignal("TestPopup_OK");
            ClosePopup();
        }

        private void OnCancelButtonClicked()
        {
            SendSignal("TestPopup_Cancel");
            ClosePopup();
        }

        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            if (_okButton != null)
            {
                _okButton.Select();
            }
        }
    }

    [Serializable]
    public class TestPopupData
    {
        public string Title;
        public string Message;

        public TestPopupData(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}
