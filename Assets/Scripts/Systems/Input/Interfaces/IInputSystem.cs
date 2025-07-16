namespace Systems.Input.Interfaces
{
    public interface IInputSystem
    {
        bool IsInputEnabled { get; }

        void EnableInput();
        void DisableInput();
    }
}
