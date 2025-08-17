using System;
using Core.Behaviours;
using DG.Tweening;

namespace Gameplay.BattleSystem.UI.Base
{
    public abstract class BaseView : CoreBehaviour, IDisposable
    {
        protected override void OnInitialize()
        {
            ValidateComponents();
        }

        protected abstract void ValidateComponents();

        protected override void HandleDestruction()
        {
            base.HandleDestruction();
            Dispose();
        }

        public void Dispose()
        {
            DOTween.Kill(transform);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
