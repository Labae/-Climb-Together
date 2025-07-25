using System;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.Player.Interfaces;
using R3;

namespace Gameplay.Player.Core
{
    public class PlayerEventBus : IDisposable
    {
        private readonly Subject<IPlayerEvent> _eventStream = new();

        public void Publish<TEvent>(TEvent e) where TEvent : IPlayerEvent
        {
            GameLogger.Debug(ZString.Concat("[PlayerEventBus] Publishing event", typeof(TEvent).Name), LogCategory.Player);
            _eventStream.OnNext(e);
        }

        public Observable<TEvent> Subscribe<TEvent>() where TEvent : IPlayerEvent
        {
            return _eventStream.Where(e => e is TEvent)
                .Cast<IPlayerEvent, TEvent>();
        }

        public void Dispose() => _eventStream.Dispose();
    }
}
