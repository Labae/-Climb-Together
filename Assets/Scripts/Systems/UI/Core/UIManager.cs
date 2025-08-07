using System.Collections.Generic;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using Debugging.Enum;
using Systems.UI.Interfaces;
using UnityEngine;
using VContainer;

namespace Systems.UI.Core
{
    /// <summary>
    /// UI Popup 관리하는 매니저
    /// </summary>
    public class UIManager : MonoBehaviour, IUIManager
    {
        [Header("UI Manager Settings")] [SerializeField]
        private Canvas _uiCanvas;

        [SerializeField] private Transform _popupParent;

        private readonly Dictionary<string, IUIPopup> _activePopups = new();
        private readonly Dictionary<string, GameObject> _popupPrefabs = new();

        [Inject] private IObjectResolver _container;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUIManager();
        }

        private void OnDestroy()
        {
            CleanupUIManager();
        }

        #endregion

        #region Initialization

        private void InitializeUIManager()
        {
            _uiCanvas ??= GetComponentInParent<Canvas>();
            _popupParent ??= transform;

            LoadPopupPrefabs();

            GameLogger.Debug("UIManager initialized", LogCategory.System);
        }

        private void LoadPopupPrefabs()
        {
            var popupPrefabs = Resources.LoadAll<GameObject>("UI/Popups");
            foreach (var prefab in popupPrefabs)
            {
                var popup = prefab.GetComponent<IUIPopup>();
                if (popup != null)
                {
                    _popupPrefabs[popup.PopupID] = prefab;
                    GameLogger.Debug(ZString.Concat("Loaded popup prefab: ", popup.PopupID), LogCategory.System);
                }
            }

            GameLogger.Debug(ZString.Format("Total {0} popup prefabs loaded", _popupPrefabs.Count), LogCategory.System);
        }

        private void CleanupUIManager()
        {
            foreach (var popup in _activePopups.Values)
            {
                popup?.Dispose();
            }
            _activePopups.Clear();

            GameLogger.Debug("UIManager cleaned up", LogCategory.System);
        }

        #endregion

        public async UniTask<T> ShowPopupAsync<T>(string popupId, object data = null) where T : class, IUIPopup
        {
            if (_activePopups.TryGetValue(popupId, out var existingPopup))
            {
                GameLogger.Debug(ZString.Concat("Popup already active: ", popupId), LogCategory.System);
                return existingPopup as T;
            }

            var popup = await CreatePopupAsync<T>(popupId);
            if (popup == null)
            {
                GameLogger.Error(ZString.Concat("Failed to create popup: ", popupId), LogCategory.System);
                return null;
            }

            if (data != null)
            {
                popup.SetData(data);
            }

            _activePopups[popupId] = popup;
            await popup.ShowAsync();

            GameLogger.Debug(ZString.Concat("Popup show: ", popup.PopupID), LogCategory.System);
            return popup;
        }

        public async UniTask HidePopupAsync(string popupId)
        {
            if (!_activePopups.TryGetValue(popupId, out var popup))
            {
                GameLogger.Debug(ZString.Concat("Popup not found: ", popupId), LogCategory.System);
                return;
            }

            await popup.HideAsync();

            _activePopups.Remove(popupId);

            if (popup is MonoBehaviour monoBehaviour && monoBehaviour != null)
            {
                Destroy(monoBehaviour.gameObject);
            }

            GameLogger.Debug(ZString.Concat("Popup hidden: ", popupId), LogCategory.System);
        }

        public async UniTask HideAllPopupsAsync()
        {
            var hideTasksList = new List<UniTask>();
            var popupIds = new List<string>(_activePopups.Keys);

            foreach (var popupId in popupIds)
            {
                hideTasksList.Add(HidePopupAsync(popupId));
            }

            await UniTask.WhenAll(hideTasksList);

            GameLogger.Debug("All popups hidden", LogCategory.System);
        }

        public bool IsPopupActive(string popupId)
        {
            return _activePopups.TryGetValue(popupId, out var popup) && popup.IsActive;
        }

        public T GetPopup<T>(string popupId) where T : class, IUIPopup
        {
            _activePopups.TryGetValue(popupId, out var popup);
            return popup as T;
        }

        #region Private Methods

        private async UniTask<T> CreatePopupAsync<T>(string popupId) where T : class, IUIPopup
        {
            if (!_popupPrefabs.TryGetValue(popupId, out var prefab))
            {
                GameLogger.Error(ZString.Concat("Popup not found: ", popupId), LogCategory.System);
                return null;
            }

            var popupObject = Instantiate(prefab, _popupParent);
            _container?.Inject(popupObject);

            var popup = popupObject.GetComponent<T>();
            if (popup != null)
            {
                return popup;
            }

            GameLogger.Error(ZString.Concat("Popup component not found: ", popupId), LogCategory.System);
            Destroy(popupObject);
            return null;
        }

        #endregion
    }
}
