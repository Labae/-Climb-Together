using System;
using R3;

namespace Systems.EventBus
{
    public interface IEventBus
    {
        void Publish<T>(T eventData) where T : class;
        IDisposable Subscribe<T>(Action<T> onNext) where T : class;
        IDisposable Subscribe<T>(Observer<T> observer) where T : class;
        IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> onNext) where T : class;
        IDisposable SubscribeOnce<T>(Action<T> onNext) where T : class;
    }
}
