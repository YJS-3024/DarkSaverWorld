using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.UI
{
    public static class NativeBattleStatus
    {
        public static Sprite UnitPicture(SpriteRenderer body, int jobId)
        {
            if (body == null) return null;
            var fixedFrame = body.GetComponent<DarkSaver.Player.DirectionalSpriteAnimator>()?.StatusPicture;
            if (fixedFrame != null) return fixedFrame;
            // Static field actors have only a world frame; use their separately converted native crop.
            var native = Resources.Load<Sprite>("UI/UnitStatusPictures/" + jobId);
            // Unmapped/custom artwork is unchanged; do not claim it as recovered native artwork.
            return native != null ? native : body.sprite;
        }
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
