using UnityEngine;
namespace DarkSaver.Rendering
{
    // Reconstruction reference size from the visual design, not a recovered display-mode API.
    public static class GameScreen
    {
        public const int Width = GameConstants.ReferenceScreenWidth, Height = GameConstants.ReferenceScreenHeight;
        public static Rect Viewport => Fit(Screen.width, Screen.height);
        public static Rect Fit(float width, float height)
        {
            float scale = Mathf.Max(0, Mathf.Min(width / Width, height / Height));
            return new Rect((width - Width * scale) * .5f, (height - Height * scale) * .5f, Width * scale, Height * scale);
        }
        // Physical bottom-left screen coordinates -> logical top-left UI coordinates.
        public static bool TryScreenToGui(Vector2 point, out Vector2 logical)
            => TryScreenToGui(point, new Vector2(Screen.width, Screen.height), out logical);
        public static bool TryScreenToGui(Vector2 point, Vector2 screenSize, out Vector2 logical)
        {
            var viewport = Fit(screenSize.x, screenSize.y); logical = default;
            if (viewport.width <= 0 || viewport.height <= 0) return false;
            logical = new Vector2((point.x - viewport.x) * Width / viewport.width,
                (screenSize.y - point.y - viewport.y) * Height / viewport.height);
            return logical.x >= 0 && logical.x < Width && logical.y >= 0 && logical.y < Height;
        }
        public static Vector2 GuiToScreen(Vector2 logical)
        {
            var viewport = Viewport; float scale = viewport.width / Width;
            return new Vector2(viewport.x + logical.x * scale, Screen.height - viewport.y - logical.y * scale);
        }
        public static Vector2 ScreenToGui(Vector2 point)
        { TryScreenToGui(point, out var logical); return logical; }
        public static Matrix4x4 GuiMatrix
        {
            get { var v = Viewport; return Matrix4x4.TRS(new Vector3(v.x, v.y, 0), Quaternion.identity, new Vector3(v.width / Width, v.height / Height, 1)); }
        }
        public static void ApplyCamera(Camera camera)
        {
            if (camera == null || camera.targetTexture != null || Screen.width <= 0 || Screen.height <= 0) return;
            var viewport = Viewport;
            camera.rect = new Rect(viewport.x / Screen.width, viewport.y / Screen.height, viewport.width / Screen.width, viewport.height / Screen.height);
            camera.aspect = (float)Width / Height;
        }
    }
}
