using System;
using System.Collections.Generic;
using Systems.UI.Interfaces;

namespace Systems.UI.Core
{
    /// <summary>
    /// 전역 UI Signal을 관리하는 허브
    /// 각 시스템에서 필요한 시그널을 등록
    /// </summary>
    public static class UISignalHub
    {
        private static readonly Dictionary<Type, object> _signals = new();

        /// <summary>
        /// 특정 타입의 시그널 가져오기(없으면 생성)
        /// </summary>
        public static IUISignal<T> GetSignal<T>()
        {
            var type = typeof(T);

            if (!_signals.TryGetValue(type, out var signal))
            {
                signal = new UISignal<T>();
                _signals[type] = signal;
            }

            return (IUISignal<T>)signal;
        }

        /// <summary>
        /// 시그널 등록(이미 있으면 무시)
        /// </summary>
        public static void RegisterSignal<T>(IUISignal<T> signal)
        {
            var type = typeof(T);
            _signals.TryAdd(type, signal);
        }

        /// <summary>
        /// 모든 시그널 정리(게임 종료시)
        /// </summary>
        public static void DisposeAll()
        {
            foreach (var signal in _signals.Values)
            {
                if (signal is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _signals.Clear();
        }
    }
}
