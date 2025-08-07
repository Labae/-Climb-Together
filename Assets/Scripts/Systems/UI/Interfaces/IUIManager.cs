using Cysharp.Threading.Tasks;

namespace Systems.UI.Interfaces
{
    /// <summary>
    /// UI 팝업들을 관리하는 매니저 인터페이스
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// 팝업을 표시하고 반환
        /// </summary>
        UniTask<T> ShowPopupAsync<T>(string popupId, object data = null) where T : class, IUIPopup;

        /// <summary>
        /// 특정 팝업 숨기기
        /// </summary>
        UniTask HidePopupAsync(string popupId);

        /// <summary>
        /// 모든 팝업 숨기기
        /// </summary>
        UniTask HideAllPopupsAsync();

        /// <summary>
        /// 팝업 활성화 상태 확인
        /// </summary>
        bool IsPopupActive(string popupId);

        /// <summary>
        /// 특정 팝업 가져오기
        /// </summary>
        T GetPopup<T>(string popupId) where T : class, IUIPopup;
    }
}
