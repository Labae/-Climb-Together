using R3;

namespace Systems.UI.Interfaces
{
    /// <summary>
    /// UI 시그널 기본 인터페이스
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUISignal<T>
    {
        /// <summary>
        /// Observable로 구독 가능
        /// </summary>
        /// <returns></returns>
        Observable<T> AsObservable();

        /// <summary>
        /// 시그널 전송
        /// </summary>
        /// <param name="value"></param>
        void Send(T value);
    }
}
