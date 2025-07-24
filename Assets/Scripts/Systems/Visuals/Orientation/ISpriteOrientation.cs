using Systems.Animations;

namespace Systems.Visuals.Orientation
{
    public interface ISpriteOrientation
    {
        FacingDirection CurrentDirection { get; }
        bool IsFlippedHorizontally { get; }

        void SetDirection(FacingDirection direction);
    }
}
