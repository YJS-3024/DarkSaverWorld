using System;
using UnityEngine;

namespace DarkSaver.UI
{
    public static class InventoryGridLayout
    {
        public const int Columns = 7, Rows = 4, PageSize = Columns * Rows, CellSize = 32, Pitch = 35;
        public static Rect Cell(int slot, Vector2 origin)
        {
            if (slot < 0 || slot >= PageSize) throw new ArgumentOutOfRangeException(nameof(slot));
            return new Rect(origin.x + slot % Columns * Pitch, origin.y + slot / Columns * Pitch, CellSize, CellSize);
        }
        public static int PageCount(int count) => count <= 0 ? 1 : (count - 1) / PageSize + 1;
    }
}
