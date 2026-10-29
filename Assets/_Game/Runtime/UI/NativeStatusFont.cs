using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.UI
{
    public static class NativeStatusFont
    {
        private static Dictionary<string, Sprite> sprites;
        private static Dictionary<string, Sprite> worldSprites,alternateWorldSprites;
        public static int Advance(char value) => value == ' ' ? 5 : value < 128 ? 7 : 14;
        public static int Measure(string text)
        {
            int width = 0;
            if (text != null) foreach (var value in text) width += Advance(value);
            return width;
        }
        public static string[] WrapLines(string text, int width)
        {
            if (width < 14) throw new System.ArgumentOutOfRangeException(nameof(width));
            var lines = new List<string>(); var line = new System.Text.StringBuilder(); int pen = 0;
            foreach (char value in text ?? string.Empty) {
                if (value == '\r') continue;
                if (value == '\n') { lines.Add(line.ToString()); line.Clear(); pen = 0; continue; }
                int advance = Advance(value);
                if (pen + advance > width && line.Length > 0) { lines.Add(line.ToString()); line.Clear(); pen = 0; }
                line.Append(value); pen += advance;
            }
            lines.Add(line.ToString()); return lines.ToArray();
        }
        public static Sprite WorldGlyph(char value)
        {
            if (worldSprites == null)
            {
                worldSprites = new Dictionary<string, Sprite>();
                foreach (var frame in Resources.LoadAll<Sprite>("UI/NativeWorldNameFont")) worldSprites[frame.name] = frame;
            }
            return worldSprites.TryGetValue("U_" + ((int)value).ToString("X4"), out var sprite) ? sprite
                : worldSprites.TryGetValue("U_003F", out var fallback) ? fallback : null;
        }
        public static Sprite WorldGlyph(char value,int mode)
        {
            if(mode!=1)return WorldGlyph(value);
            if(alternateWorldSprites==null){alternateWorldSprites=new Dictionary<string,Sprite>();foreach(var frame in Resources.LoadAll<Sprite>("UI/NativeWorldAlternateFont"))alternateWorldSprites[frame.name]=frame;}
            return alternateWorldSprites.TryGetValue("U_"+((int)value).ToString("X4"),out var sprite)?sprite:alternateWorldSprites.TryGetValue("U_003F",out var fallback)?fallback:null;
        }
        public static Sprite Glyph(char value)
        {
            if (sprites == null)
            {
                sprites = new Dictionary<string, Sprite>();
                foreach (var frame in Resources.LoadAll<Sprite>("UI/NativeStatusFont")) sprites[frame.name] = frame;
            }
            return sprites.TryGetValue("U_" + ((int)value).ToString("X4"), out var sprite) ? sprite
                : sprites.TryGetValue("U_003F", out var fallback) ? fallback : null;
        }
    }
}
