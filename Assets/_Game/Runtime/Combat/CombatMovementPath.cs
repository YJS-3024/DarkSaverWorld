using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    public static class CombatMovementPath
    {
        // Recovered general MoveChar score backtracking (0x1000D2C0).
        // Does not mutate the supplied area or retain a hidden global tie flag.
        // A failed native query returns no partial route and preserves the flag.
        public static bool TryBacktrackMovementArea(int width, int height, CombatGridCell origin,
            CombatGridCell destination, IReadOnlyDictionary<CombatGridCell, int> area, bool verticalFirst,
            out IReadOnlyList<CombatGridCell> cardinal, out bool nextVerticalFirst)
        {
            cardinal = Array.Empty<CombatGridCell>(); nextVerticalFirst = verticalFirst;
            bool Inside(CombatGridCell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
            if (width <= 0 || height <= 0 || area == null || !Inside(origin) || !Inside(destination)) return false;
            var scores = new Dictionary<CombatGridCell, int>();
            foreach (var entry in area)
            {
                if (!Inside(entry.Key)) return false;
                scores.Add(entry.Key, entry.Value);
            }
            int Score(CombatGridCell cell) => Inside(cell) && scores.TryGetValue(cell, out var value) ? value : -1;
            if (Score(origin) < 0 || Score(destination) < 0) return false;
            var horizontal = new[] { new CombatGridCell(-1, 0), new CombatGridCell(1, 0),
                new CombatGridCell(0, -1), new CombatGridCell(0, 1) };
            var vertical = new[] { horizontal[2], horizontal[3], horizontal[0], horizontal[1] };
            var backwards = new List<CombatGridCell>(); var current = destination; var preferVertical = verticalFirst;
            for (var remaining = scores.Count; remaining > 0; remaining--)
            {
                var currentScore = Score(current); var maximum = currentScore;
                var order = preferVertical ? vertical : horizontal;
                foreach (var delta in order)
                    maximum = Math.Max(maximum, Score(new CombatGridCell(current.X + delta.X, current.Y + delta.Y)));
                if (maximum < 0) return false;
                scores[current] = -64; // Same visited marker as the original, on our owned copy.
                var moved = false;
                foreach (var delta in order)
                {
                    var candidate = new CombatGridCell(current.X + delta.X, current.Y + delta.Y);
                    if (Score(candidate) != maximum) continue;
                    backwards.Add(current); current = candidate; moved = true; preferVertical = !preferVertical; break;
                }
                if (moved) continue;
                if (!current.Equals(origin) || currentScore != maximum) return false;
                backwards.Reverse(); cardinal = backwards.AsReadOnly(); nextVerticalFirst = preferVertical;
                return true;
            }
            return false;
        }

        // Native equivalent of userunit.dll 0x1000D720's adjacent cardinal-pair
        // compression. Input excludes origin. This is NOT a path search or range.
        // The host supplies diagonal/corner permission; the original compressor
        // itself did not inspect terrain. Rejected merges retain cardinal steps.
        public static bool TryCompressCardinalPath(CombatGridCell origin,
            IReadOnlyList<CombatGridCell> path, Func<CombatGridCell, CombatGridCell, bool> allowsDiagonal,
            out IReadOnlyList<CombatGridCell> compressed)
        {
            compressed = Array.Empty<CombatGridCell>();
            if (path == null || allowsDiagonal == null) return false;
            var source = new List<CombatGridCell>(path);
            var previous = origin;
            foreach (var cell in source)
            {
                if (Math.Abs((long)cell.X - previous.X) + Math.Abs((long)cell.Y - previous.Y) != 1) return false;
                previous = cell;
            }
            var output = new List<CombatGridCell>(); previous = origin;
            for (var index = 0; index < source.Count; index++)
            {
                var next = source[index];
                if (index + 1 < source.Count)
                {
                    var pair = source[index + 1];
                    if (Math.Abs((long)pair.X - previous.X) == 1
                        && Math.Abs((long)pair.Y - previous.Y) == 1 && allowsDiagonal(previous, pair))
                    { next = pair; index++; }
                }
                output.Add(next); previous = next;
            }
            compressed = output.AsReadOnly(); return true;
        }
    }
}
