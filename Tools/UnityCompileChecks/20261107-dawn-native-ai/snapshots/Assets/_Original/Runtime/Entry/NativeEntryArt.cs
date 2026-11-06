using UnityEngine;

namespace DarkSaver.Original.Entry
{
    public sealed class NativeEntryArt : ScriptableObject
    {
        public Texture2D background, login, secondary, creation, buttons, serverPanel, serverSelected;
        public Color serverNameColor;
        public Texture2D[] faces;
        public bool IsConfigured => background != null && login != null && secondary != null && creation != null && buttons != null
            && serverPanel != null && serverSelected != null && faces != null && faces.Length == 3 && faces[0] != null && faces[1] != null && faces[2] != null;
    }
    public static class NativeEntryLayout
    {
        public const int Width = 640, Height = 480;
        public static readonly Rect LoginPanel = new Rect(226, 279, 197, 87);
        public static readonly Rect IdHit = new Rect(298, 296, 110, 20);
        public static readonly Rect PasswordHit = new Rect(298, 320, 110, 18);
        // Saver.exe stage3 0x40EF53..0x40EFD7 uses login2.bmp, not the early three-server atlas.
        public static readonly Rect ServerPanel = new Rect(277, 276, 100, 104);
        public static readonly Rect ServerHit = new Rect(289, 311, 72, 16);
        public static readonly Rect ServerConnectHit = new Rect(286, 355, 35, 16);
        public static readonly Rect ServerExitHit = new Rect(331, 356, 35, 15);
        public static readonly Rect CreationSize = new Rect(0, 0, 397, 300);
        // Dlg45 profile0,0 is not evidence of an absolute screen origin. Center the initial creation window.
        public static readonly Vector2 CreationOrigin = new Vector2(121, 90);
        public static readonly Rect NameHit = new Rect(254, 241, 88, 15);
        public static readonly int[] Jobs = { 100, 101, 103 };
        public static Rect Card(int index) => new Rect(index == 0 ? 37 : index == 1 ? 153 : 268, 54, 90, 176);
        // Native 62x62 CDDSurface/GetFace at card origin+14,14; do not stretch to the card.
        public static Rect Face(int index) { var card = Card(index); return new Rect(card.x + 14, card.y + 14, 62, 62); }
        public static bool ContainsExclusive(Rect rect, Vector2 point) => point.x > rect.xMin && point.x < rect.xMax && point.y > rect.yMin && point.y < rect.yMax;
        public static Rect Uv(Texture texture, Rect topLeftPixels) => new Rect(topLeftPixels.x / texture.width,
            1 - topLeftPixels.yMax / texture.height, topLeftPixels.width / texture.width, topLeftPixels.height / texture.height);
    }
}
