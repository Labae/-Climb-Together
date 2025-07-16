namespace Systems.Input.Interfaces
{
    public interface IGlobalInputSystem
    {
        InputSystemActions Actions { get; }
        float InputBuffer { get; }
    }
}
