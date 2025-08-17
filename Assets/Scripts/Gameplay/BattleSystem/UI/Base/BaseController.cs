using System;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using R3;

namespace Gameplay.BattleSystem.UI.Base
{
    public abstract class BaseController<TModel, TView> : IDisposable
        where TModel : BaseModel
        where TView : BaseView
    {
        protected readonly TModel Model;
        protected readonly TView View;
        protected readonly CompositeDisposable _disposables = new();

        protected BaseController(TModel model, TView view)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            View = view ?? throw new ArgumentNullException(nameof(view));;
        }

        public void Initialize()
        {
            if (!ValidateInitialization())
            {
                return;
            }

            SetupBindings();
            OnInitialized();
        }

        protected virtual bool ValidateInitialization()
        {
            if (Model == null || View == null)
            {
                GameLogger.Error(ZString.Format("[{0}] Model or View is null!", GetType().Name), LogCategory.UI);
                return false;
            }

            if (!Model.IsInitialized.Value)
            {
                GameLogger.Error(ZString.Format("[{0}] Model is not initialized!", GetType().Name), LogCategory.UI);
                return false;
            }

            return true;
        }

        protected abstract void SetupBindings();

        protected virtual void OnInitialized()
        {

        }

        public virtual void Dispose()
        {
            Model?.Dispose();
            _disposables?.Dispose();
        }
    }
}
