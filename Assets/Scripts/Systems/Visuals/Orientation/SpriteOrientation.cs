using Systems.Animations;
using UnityEngine;

namespace Systems.Visuals.Orientation
{
    public class SpriteOrientation : ISpriteOrientation
    {
        private readonly SpriteRenderer _spriteRenderer;
        private FacingDirection _currentDirection;

        public FacingDirection CurrentDirection => _currentDirection;
        public bool IsFlippedHorizontally => _spriteRenderer.flipX;

        // Constructor
        public SpriteOrientation(SpriteRenderer spriteRenderer, FacingDirection initialDirection)
        {
            _spriteRenderer = spriteRenderer;
            _currentDirection = initialDirection;

            UpdateSpriteFlip();
        }

        private void UpdateSpriteFlip()
        {
            _spriteRenderer.flipX = _currentDirection == FacingDirection.Left;
        }

        public void SetDirection(FacingDirection direction)
        {
            if (_currentDirection == direction)
            {
                return;
            }

            _currentDirection = direction;
            UpdateSpriteFlip();
        }
    }
}
