using UnityEngine;

namespace DarkSaver.World
{
    public static class NpcInteractionRules
    {
        // One-unit map cells. Diagonals and standing on the NPC are not adjacent.
        public static bool IsAdjacent(Vector2 player, Vector2 npc)
        {
            if (!Finite(player) || !Finite(npc)) return false;
            int dx = Mathf.Abs(Mathf.FloorToInt(player.x) - Mathf.FloorToInt(npc.x));
            int dy = Mathf.Abs(Mathf.FloorToInt(player.y) - Mathf.FloorToInt(npc.y));
            return dx + dy == 1;
        }
        public static bool IsCounterFront(Vector2 player, Vector2Int cell)
            => Finite(player) && Mathf.FloorToInt(player.x) == cell.x && Mathf.FloorToInt(player.y) == cell.y;
        private static bool Finite(Vector2 point)
            => !float.IsNaN(point.x) && !float.IsInfinity(point.x) && !float.IsNaN(point.y) && !float.IsInfinity(point.y);
    }
}
