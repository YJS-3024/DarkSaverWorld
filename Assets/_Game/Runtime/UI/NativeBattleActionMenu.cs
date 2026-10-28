using UnityEngine;

namespace DarkSaver.UI
{
    // GameCtl.dll 0x1000B8C0: destination rectangles around a 32px character cell.
    // TUTORIAL.DEO confirms attack above V-finger skill, with computer/options bottom-right.
    // This table describes the commander's eight entries, not unimplemented command behavior.
    public static class NativeBattleActionMenu
    {
        public const int Count = 8;
        public const int Size = 96;
        private static readonly Vector2Int[] Cells = {
            new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(0, 2),
            new Vector2Int(0, 1), new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(1, 0)
        };
        private static readonly int[] Columns = { 0, 1, 12, 8, 2, 5, 4, 3 };
        // Native menu painter uses the static grey row at atlas y128 for masked entries.
        private static readonly int[] InactiveColumns={4,5,9,8,6,11,10,7};
        public static int InactiveIconColumn(int index)=>index>=0 && index<Count?InactiveColumns[index]:-1;
        // Original soldier menu exposes move/attack/rest only; commander instances end in0.
        public static bool AllowsIndex(int unitId,int index)=>unitId>=0 && index>=0 && index<Count
            && (unitId%10==0 || index==0 || index==1 || index==3);
        public static Rect ButtonRect(Rect bounds, int index)
        {
            if (index < 0 || index >= Count) return default;
            var cell = Cells[index];
            return new Rect(bounds.x + cell.x * 32, bounds.y + cell.y * 32, 32, 32);
        }
        public static int IconColumn(int index) => index >= 0 && index < Count ? Columns[index] : -1;
    }
}
