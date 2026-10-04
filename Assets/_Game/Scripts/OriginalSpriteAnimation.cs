using UnityEngine;

namespace DarkSaver.Prototype
{
    public enum SpriteFacing
    {
        Up,
        Right,
        Down,
        Left
    }

    public static class OriginalSpriteAnimation
    {
        public const int FramesPerDirection = 3;

        public static int GetColumn(SpriteFacing facing, int frame)
        {
            return (int)facing * FramesPerDirection + Mathf.Clamp(frame, 0, FramesPerDirection - 1);
        }

        public static SpriteFacing GetFacing(Vector2Int delta, SpriteFacing fallback)
        {
            if (delta.x < 0) return SpriteFacing.Left;
            if (delta.x > 0) return SpriteFacing.Right;
            if (delta.y < 0) return SpriteFacing.Up;
            if (delta.y > 0) return SpriteFacing.Down;
            return fallback;
        }
    }
}
