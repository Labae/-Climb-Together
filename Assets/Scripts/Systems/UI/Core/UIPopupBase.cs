using System;
using Cysharp.Threading.Tasks;
using R3;
using Systems.UI.Interfaces;
using UnityEngine;
using VContainer;

namespace Systems.UI.Core
{
    /// <summary>
    /// 모든 UI Popup의 기본 클래스
    /// </summary>
    public abstract class UIPopupBase : MonoBehaviour, IUIPopup
    {
        [Header("Popup Settings")] [SerializeField]
        protected string _popupId = "DefaultPopup";
        [SerializeField] protected bool _closeOnBackground = false;
        [SerializeField] protected bool _blockInput = true;

        protected Transform _popupTransform;
        protected CanvasGroup _canvasGroup;

        private bool _isActive = false;
        private bool _isAnimating = false;

        protected readonly CompositeDisposable _disposables = new();

        [Inject] protected IUIManager _uiManager;

        public string PopupID => _popupId;
        public bool IsActive => _isActive;
        public bool IsAnimating => _isAnimating;

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeComponents();
            SetInitialState();
        }

        private void Start()
        {
            _uiManager ??= FindAnyObjectByType<UIManager>();
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            _popupTransform = transform;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void SetInitialState()
        {
            // 초기에는 비활성화
            gameObject.SetActive(false);
        }

        #endregion

        #region IUIPopup Implementation

        public async UniTask ShowAsync()
        {
            if (_isActive || _isAnimating)
            {
                return;
            }

            _isAnimating = true;
            _isActive = true;

            OnBeforeShow();

            try
            {
                await OnShowAnimationAsync();
            }
            finally
            {
                _isAnimating = false;
            }

            OnAfterShow();
        }

        public async UniTask HideAsync()
        {
            if (!_isActive || _isAnimating)
            {
                return;
            }

            _isAnimating = true;

            OnBeforeHide();

            try
            {
                await OnHideAnimationAsync();
            }
            finally
            {
                _isAnimating = false;
                _isActive = false;
            }

            OnAfterHide();
        }

        public void SetData(object data)
        {
            OnDataSet(data);
        }

        #endregion

        #region Virtual Methdos

        protected virtual void OnBeforeShow() { }
        protected virtual void OnAfterShow() { }
        protected virtual void OnBeforeHide() { }
        protected virtual void OnAfterHide() { }
        protected virtual void OnDataSet(object data) { }

        protected virtual async UniTask OnShowAnimationAsync()
        {
            await UIAnimations.PopupShowAsync(_popupTransform);
        }

        protected virtual async UniTask OnHideAnimationAsync()
        {
            await UIAnimations.PopupHideAsync(_popupTransform);
        }

        #endregion

        #region Utility Methods

        protected void ClosePopup()
        {
            _uiManager.HidePopupAsync(_popupId).Forget();
        }

        protected void SendSignal<T>(T value)
        {
            var signal = UISignalHub.GetSignal<T>();
            signal.Send(value);
        }

        protected IDisposable SubscribeSignal<T>(System.Action<T> onReceived)
        {
            var signal = UISignalHub.GetSignal<T>();
            return signal.AsObservable()
                .Subscribe(onReceived)
                .AddTo(_disposables);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_isAnimating)
            {
                UIAnimations.KillAllTweens(_popupTransform);
            }

            _disposables.Dispose();
        }

        #endregion
    }
}
