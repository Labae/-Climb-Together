using System;
using UnityEngine;

namespace Core.Behaviours
{
    public abstract class CoreBehaviour : MonoBehaviour
    {
        private GameObject _cacheGameObject;
        private Transform _cacheTransform;
        private string _cacheName;
        
        private bool _isInitialized = false;
        public event Action OnDestroyed;
        
        public new GameObject gameObject
        {
            get
            {
                if (_cacheGameObject == null)
                {
                    _cacheGameObject =  base.gameObject;
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

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            HandleDestruction();
            OnDestroyed?.Invoke();
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void HandleDestruction()
        {
            
        }
    }
}
