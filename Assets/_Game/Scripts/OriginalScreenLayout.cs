using UnityEngine;

namespace DarkSaver.Prototype
{
    public readonly struct OriginalScreenLayout
    {
        public readonly float Scale;
        public readonly Vector2 Offset;

        public Matrix4x4 Matrix => Matrix4x4.TRS(
            new Vector3(Offset.x, Offset.y, 0f),
            Quaternion.identity,
            new Vector3(Scale, Scale, 1f));

        private OriginalScreenLayout(float scale, Vector2 offset)
        {
            Scale = scale;
            Offset = offset;
        }

        public static OriginalScreenLayout Create(
            float screenWidth,
            float screenHeight,
            Rect safeArea,
            float referenceWidth,
            float referenceHeight)
        {
            var safeWidth = Mathf.Max(1f, safeArea.width);
            var safeHeight = Mathf.Max(1f, safeArea.height);
            var scale = Mathf.Min(safeWidth / referenceWidth, safeHeight / referenceHeight);
            var safeTop = screenHeight - safeArea.yMax;
            var offset = new Vector2(
                safeArea.x + (safeWidth - referenceWidth * scale) * .5f,
                safeTop + (safeHeight - referenceHeight * scale) * .5f);
            return new OriginalScreenLayout(scale, offset);
        }

        public Vector2 ScreenToReference(Vector2 screenPoint, float screenHeight)
        {
            var guiPoint = new Vector2(screenPoint.x, screenHeight - screenPoint.y);
            return (guiPoint - Offset) / Scale;
        }
    }
}
