using System;

namespace Gameplay.Player.Interfaces
{
    public interface IPlayerLocomotion : IDisposable
    {
        bool CanExecute(float horizontalInput);
        void Execute(float horizontalInput);

        string GetName();
    }
}
