using System;
using R3;

namespace Gameplay.BattleSystem.UI.Base
{
    public abstract class BaseModel : IDisposable
    {
        protected readonly CompositeDisposable _disposables = new();

        public ReactiveProperty<bool> IsInitialized { get; } = new(false);

        protected void SetInitialized()
        {
            IsInitialized.Value = true;
        }

        public virtual void Dispose()
        {
            IsInitialized?.Dispose();
            _disposables?.Dispose();
        }
    }
}
