using System.Collections.Generic;
using UnityEngine;
namespace DarkSaver.UI
{
    public static class BattleFeedbackIcons
    {
        static Dictionary<string, Sprite> sprites;
        static Sprite Get(string name)
        {
            if (sprites == null)
            {
                sprites = new Dictionary<string, Sprite>();
                foreach (var sprite in Resources.LoadAll<Sprite>("UI/BattleFeedbackIcons")) sprites[sprite.name] = sprite;
            }
            return sprites.TryGetValue(name, out var result) ? result : null;
        }
        // Native number cells: 1..9, zero=10, plus=11, minus=12.
        public static Sprite Number(char value, int row = 0)
        {
            if (row < 0 || row > 3) return null;
            var column = value switch { '0' => 10, '+' => 11, '-' => 12, _ => value >= '1' && value <= '9' ? value - '0' : -1 };
            return column < 0 ? null : Get("Number_" + row + "_" + column);
        }
        public static Sprite Miss => Get("MISS");
    }
}
