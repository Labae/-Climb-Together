using System;

namespace Gameplay.Player.Interfaces
{
    public interface IPlayerAction : IDisposable
    {
        bool CanExecute();
        void Execute();
    }
}
