using UnityEngine;

namespace DarkSaver.UI
{
    public static class NativeBattleMagicMenu
    {
        // GameCtl.dll 0x10009460: (0,-32),(0,32),(-32,0),(32,0),
        // (-32,-32),(-32,32),(32,-32),(32,32),(0,0), relative to the character cell.
        public const int Capacity = 9;
        private static readonly Vector2Int[] Cells = {
            new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(0, 1), new Vector2Int(2, 1),
            new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(2, 2), new Vector2Int(1, 1)
        };
        public static Rect ButtonRect(int slot)
        {
            if (slot < 0 || slot >= Capacity) return default;
            return new Rect(Cells[slot].x * 32, Cells[slot].y * 32, 32, 32);
        }
        // Overflow paging is a Unity safety fallback, not a recovered native menu feature.
        public static int PageCount(int count) => count <= 0 ? 0 : 1 + (count - 1) / Capacity;
        public static int DisplayIndex(int count, int page, int slot)
        {
            if (page < 0 || page >= PageCount(count) || slot < 0 || slot >= Capacity) return -1;
            long index = (long)page * Capacity + slot;
            return index < count ? (int)index : -1;
        }
    }
}
