using System;
using UnityEngine;

namespace DarkSaver.Items
{
    public static class ItemIcons
    {
        public const int Columns = 100, Rows = 10, CellSize = 32;
        private static Sprite[] icons;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => icons = null;

        public static Sprite Get(ItemCatalogEntry item) => item == null ? null : Get(item.GetNumericValue(1));
        public static Sprite Get(long index)
        {
            if (index < 0 || index >= Columns * Rows) return null;
            if (icons == null) {
                icons = new Sprite[Columns * Rows];
                foreach (var sprite in Resources.LoadAll<Sprite>("UI/ItemIcons"))
                    if (sprite.name.StartsWith("ItemIcon_", StringComparison.Ordinal)
                        && int.TryParse(sprite.name.Substring(9), out int id) && id >= 0 && id < icons.Length)
                        icons[id] = sprite;
            }
            return icons[(int)index];
        }
    }
}
