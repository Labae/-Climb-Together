using System;
using System.Collections.Concurrent;
using R3;

namespace Systems.EventBus
{
    public class EventBus : IEventBus, IDisposable
    {
        private readonly ConcurrentDictionary<Type, object> _subjects = new();
        private bool _disposed = false;

        #region Event Publishing

        /// <summary>
        /// 이벤트 발행
        /// </summary>
        public void Publish<T>(T eventData) where T : class
        {
            if (eventData == null || _disposed) return;

            var subject = GetOrCreateSubject<T>();
            subject.OnNext(eventData);
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        public IDisposable Subscribe<T>(Action<T> onNext) where T : class
        {
            if (_disposed) return Disposable.Empty;

            var subject = GetOrCreateSubject<T>();
            return subject.Subscribe(onNext);
        }

        /// <summary>
        /// 이벤트 구독 (Observer 사용)
        /// </summary>
        public IDisposable Subscribe<T>(Observer<T> observer) where T : class
        {
            if (_disposed) return Disposable.Empty;

            var subject = GetOrCreateSubject<T>();
            return subject.Subscribe(observer);
        }

        /// <summary>
        /// 조건부 이벤트 구독
        /// </summary>
        public IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> onNext) where T : class
        {
            if (_disposed) return Disposable.Empty;

            var subject = GetOrCreateSubject<T>();
            return subject.Where(predicate).Subscribe(onNext);
        }

        /// <summary>
        /// 한 번만 실행되는 이벤트 구독
        /// </summary>
        public IDisposable SubscribeOnce<T>(Action<T> onNext) where T : class
        {
            if (_disposed) return Disposable.Empty;

            var subject = GetOrCreateSubject<T>();
            return subject.Take(1).Subscribe(onNext);
        }

        #endregion

        #region Private Methods

        private Subject<T> GetOrCreateSubject<T>() where T : class
        {
            var eventType = typeof(T);
            return (Subject<T>)_subjects.GetOrAdd(eventType, _ => new Subject<T>());
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var subject in _subjects.Values)
            {
                try
                {
                    if (subject is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch
                {
                    // 무시
                }
            }

            _subjects.Clear();
            _disposed = true;
        }

        #endregion
    }
}
