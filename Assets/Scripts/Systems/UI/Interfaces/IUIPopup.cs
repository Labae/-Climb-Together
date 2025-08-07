using System;
using Cysharp.Threading.Tasks;

namespace Systems.UI.Interfaces
{
    /// <summary>
    /// UI 팝업의 기본 인터페이스
    /// </summary>
    public interface IUIPopup : IDisposable
    {
        /// <summary>
        /// 팝업 고유 ID
        /// </summary>
        string PopupID { get; }

        /// <summary>
        /// 현재 활성화 상태
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 애니메이션 중인지 여부
        /// </summary>
        bool IsAnimating { get; }

        /// <summary>
        /// 팝업을 비동기로 표시
        /// </summary>
        /// <returns></returns>
        UniTask ShowAsync();

        /// <summary>
        /// 팝업을 비동기로 숨김
        /// </summary>
        /// <returns></returns>
        UniTask HideAsync();

        /// <summary>
        /// 팝업에 데이터 설정
        /// </summary>
        /// <param name="data"></param>
        void SetData(object data);
    }
}
