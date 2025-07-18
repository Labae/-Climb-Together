using System;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using UnityEngine;

namespace Core.Behaviours
{
    public abstract class CoreBehaviour : MonoBehaviour
    {
        #region Cached Components

        private GameObject _cacheGameObject;
        private Transform _cacheTransform;
        private string _cacheName;

        #endregion

        #region Initialization State

        private bool _isInitialized = false;
        private float _initializationTime;

        #endregion

        #region Events

        public event Action OnDestroyed;
        public event Action OnInitialized;

        #endregion

        #region Properties

        /// <summary>컴포넌트가 초기화되었는지 여부</summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>초기화 완료 시간</summary>
        public float InitializationTime => _initializationTime;

        public new GameObject gameObject
        {
            get
            {
                if (_cacheGameObject == null)
                {
                    _cacheGameObject = base.gameObject;
                }
                return _cacheGameObject;
            }
        }

        public new Transform transform
        {
            get
            {
                if (_cacheTransform == null)
                {
                    _cacheTransform = base.transform;
                }
                return _cacheTransform;
            }
        }

        public new string name
        {
            get
            {
                if (string.IsNullOrEmpty(_cacheName))
                {
                    _cacheName = base.name;
                }
                return _cacheName;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        protected void OnDestroy()
        {
            HandleDestruction();
            OnDestroyed?.Invoke();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                _initializationTime = Time.time;
                _isInitialized = true;

                OnInitialize();
                OnInitialized?.Invoke();
            }
            catch (Exception e)
            {
                _isInitialized = false;
                var sb = ZString.CreateStringBuilder();
                sb.Append("Failed to initialize ");
                sb.Append(GetType().Name);
                sb.Append(":");
                sb.Append("e.Message");
                sb.Append("\n");
                GameLogger.Error(sb.ToString(), LogCategory.Default);
                throw;
            }
        }

        /// <summary>
        /// 서브클래스에서 구현하는 초기화 로직
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// 초기화 상태를 확인하고 초기화되지 않았으면 예외를 던집니다
        /// </summary>
        protected void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException($"{GetType().Name} is not initialized");
            }
        }

        #endregion

        #region Destruction

        /// <summary>
        /// 서브클래스에서 구현하는 정리 로직
        /// </summary>
        protected virtual void HandleDestruction()
        {
            _isInitialized = false;
        }

        #endregion
    }
}
