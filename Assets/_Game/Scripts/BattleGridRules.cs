using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Prototype
{
    public static class BattleGridRules
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        public static bool CanReach(
            Vector2Int start,
            Vector2Int destination,
            int range,
            int columns,
            int rows,
            ISet<Vector2Int> obstacles,
            Func<Vector2Int, bool> isOccupied)
        {
            if (destination == start || !IsInside(destination, columns, rows) ||
                obstacles.Contains(destination) || isOccupied(destination))
                return false;

            var queue = new Queue<Vector2Int>();
            var distance = new Dictionary<Vector2Int, int> { { start, 0 } };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDistance = distance[current];
                if (currentDistance >= range) continue;
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (!IsInside(next, columns, rows) || obstacles.Contains(next) ||
                        distance.ContainsKey(next) || (next != destination && isOccupied(next)))
                        continue;
                    if (next == destination) return true;
                    distance.Add(next, currentDistance + 1);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        public static Vector2Int FindNextStep(
            Vector2Int start,
            Vector2Int target,
            int columns,
            int rows,
            ISet<Vector2Int> obstacles,
            Func<Vector2Int, bool> isOccupied)
        {
            var queue = new Queue<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int> { { start, start } };
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target) break;
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (!IsInside(next, columns, rows) || obstacles.Contains(next) ||
                        previous.ContainsKey(next))
                        continue;
                    if (isOccupied(next) && next != target)
                        continue;
                    previous.Add(next, current);
                    queue.Enqueue(next);
                }
            }

            if (!previous.ContainsKey(target)) return start;
            var step = target;
            while (previous[step] != start)
                step = previous[step];
            return step;
        }

        private static bool IsInside(Vector2Int cell, int columns, int rows)
        {
            return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
        }
    }
}
