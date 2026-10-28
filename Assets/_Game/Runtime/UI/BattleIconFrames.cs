using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.UI
{
    public static class BattleIconFrames
    {
        private static Dictionary<string, Sprite> sprites;
        public static int FrameAt(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) return 0;
            return 1 - (int)(Math.Floor(seconds / GameConstants.BattleIconFrameSeconds) % 2);
        }
        public static Sprite Action(int column, double seconds)
        {
            if (column < 0 || column >= 20) return null;
            return Find("Action_" + column.ToString("00") + "_Frame_" + FrameAt(seconds));
        }
        public static Sprite InactiveAction(int column)
        {
            if(column<0 || column>=20)return null;
            // GameCtl1000A8DD..AA47 uses fixed y128..160, without animation or alpha fade.
            return Find("Magic_3_"+column.ToString("00")+"_Frame_0");
        }
        public static Sprite Magic(int row, int column, double seconds)
        {
            if (row < 0 || row >= 4 || column < 0 || column >= 20) return null;
            return Find("Magic_" + row + "_" + column.ToString("00") + "_Frame_" + FrameAt(seconds));
        }
        public static Sprite FromMagicCoordinates(long x, long y, double seconds)
        {
            // Catalog coordinates are pixels, not a guessed spell-to-icon mapping.
            if (x < 0 || x >= 640 || y < 0 || y >= 320 || x % 32 != 0 || y % 32 != 0) return null;
            int groupY = (int)y % 160;
            return groupY >= 32 ? Magic(groupY / 32 - 1, (int)x / 32, seconds) : null;
        }
        private static Sprite Find(string name)
        {
            if (sprites == null)
            {
                sprites = new Dictionary<string, Sprite>();
                foreach (var sprite in Resources.LoadAll<Sprite>("UI/BattleIcons")) sprites[sprite.name] = sprite;
            }
            return sprites.TryGetValue(name, out var value) ? value : null;
        }
    }
}
