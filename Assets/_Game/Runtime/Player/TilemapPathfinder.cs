using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkSaver.Player
{
    public static class TilemapPathfinder
    {
        private static readonly Vector3Int[] Directions =
        {
            Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left,
            new(1, 1), new(1, -1), new(-1, -1), new(-1, 1)
        };

        public static bool TryFind(Tilemap ground, Tilemap terrain, Vector2 origin, Vector2 destination,
            List<Vector3> waypoints)
        {
            if (ground == null || terrain == null || waypoints == null) return false;
            var start = ground.WorldToCell(origin);
            var goal = ground.WorldToCell(destination);
            if (!IsWalkable(ground, terrain, goal)) return false;

            var frontier = new Queue<Vector3Int>();
            var previous = new Dictionary<Vector3Int, Vector3Int> { [start] = start };
            frontier.Enqueue(start);
            while (frontier.Count > 0 && !previous.ContainsKey(goal))
            {
                var current = frontier.Dequeue();
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (previous.ContainsKey(next) || !IsWalkable(ground, terrain, next)) continue;
                    if (direction.x != 0 && direction.y != 0 &&
                        (!IsWalkable(ground, terrain, current + new Vector3Int(direction.x, 0)) ||
                         !IsWalkable(ground, terrain, current + new Vector3Int(0, direction.y)))) continue;
                    previous.Add(next, current);
                    frontier.Enqueue(next);
                }
            }

            if (!previous.ContainsKey(goal)) return false;
            waypoints.Clear();
            for (var cell = goal; cell != start; cell = previous[cell])
                waypoints.Add(ground.GetCellCenterWorld(cell));
            waypoints.Reverse();
            return true;
        }

        private static bool IsWalkable(Tilemap ground, Tilemap terrain, Vector3Int cell) =>
            ground.HasTile(cell) && terrain.GetColliderType(cell) == Tile.ColliderType.None;
    }
}
