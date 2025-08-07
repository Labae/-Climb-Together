using System;
using R3;
using Systems.UI.Interfaces;

namespace Systems.UI.Core
{
    /// <summary>
    /// UI Signal 구현체
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UISignal<T> : IUISignal<T>, IDisposable
    {
        private readonly Subject<T> _subject = new Subject<T>();
        private bool _disposed = false;

        /// <summary>
        /// Observable로 구독
        /// </summary>
        public Observable<T> AsObservable()
        {
            return _subject.AsObservable();
        }

        /// <summary>
        /// 시그널 전송
        /// </summary>
        public void Send(T value)
        {
            if (_disposed)
            {
                return;
            }

            _subject.OnNext(value);
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _subject.Dispose();
            _disposed = true;
        }
    }
}
