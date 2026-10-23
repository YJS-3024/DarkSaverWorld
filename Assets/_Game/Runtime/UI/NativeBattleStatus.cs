using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.UI
{
    public static class NativeBattleStatus
    {
        private static Dictionary<string, Sprite> sprites;
        private static Sprite[] buttonSprites;
        public static int CloseButtonState(bool hovered, bool pressed) => hovered ? pressed ? 2 : 1 : 0;
        public static Sprite CloseButton(int state, bool minimize = false)
        {
            if (buttonSprites == null) buttonSprites = Resources.LoadAll<Sprite>("UI/NativeStatusButtons");
            var name = "Status_" + (minimize ? "37_" : "38_") + Mathf.Clamp(state, 0, 2);
            foreach (var frame in buttonSprites) if (frame.name == name) return frame;
            return null;
        }
        public static int FillWidth(int value, int maximum, int width = 80) => maximum <= 0 ? 0
            : (int)((long)Mathf.Clamp(width, 0, 80) * System.Math.Min(maximum, System.Math.Max(0, value)) / maximum);
        public static Sprite Gauge(int row) => Find("Gauge_" + Mathf.Clamp(row, 0, 2));
        public static Sprite Digit(char value) => value == '/' ? Find("Digit_-1")
            : value >= '0' && value <= '9' ? Find("Digit_" + value) : null;
        private static Sprite Find(string name)
        {
            if (sprites == null)
            {
                sprites = new Dictionary<string, Sprite>();
                foreach (var frame in Resources.LoadAll<Sprite>("UI/NativeStatusGauges")) sprites[frame.name] = frame;
            }
            return sprites.TryGetValue(name, out var sprite) ? sprite : null;
        }
    }
}
