namespace DarkSaver.Combat
{
    // Reconstructed TileAttr movement columns, not AP costs or world Collider rules.
    // Explicit column order: infantry, flying, cavalry, naval, spear, archer,
    // monk, priest, mage, final original '사' column (class-name mapping unresolved).
    public static class CombatTerrainTraversal
    {
        public const int TerrainCount = 27;
        public const int TroopColumnCount = 10;
        public const int ImpassableValue = 99;
        // Shared by preparation and live combat: catalog order never resolves ambiguity.
        public static bool TryGetTroopColumn(int jobId,
            System.Collections.Generic.IReadOnlyList<DarkSaver.Jobs.JobCatalog> catalogs, out int column)
        {
            column = -1;
            if (jobId <= 0 || catalogs == null || catalogs.Count == 0) return false;
            DarkSaver.Jobs.JobCatalogEntry found = null;
            foreach (var catalog in catalogs)
            {
                if (catalog == null) return false;
                if (!catalog.TryGet(jobId, out var entry)) continue;
                if (found != null) return false;
                found = entry;
            }
            if (found == null) return false;
            var value = found.GetNumericValue(22);
            if (found.GetNumericValue(23) != 0 || value < 0 || value >= TroopColumnCount) return false;
            column = (int)value; return true;
        }
        private static readonly byte[,] movement = {
            { 1,1,1,99,1,1,1,1,1,1 }, // 0
            { 1,1,1,99,1,1,1,1,1,1 }, // 1
            { 1,1,1,99,1,1,1,1,1,1 }, // 2
            { 99,1,99,99,99,99,99,99,99,99 }, // 3
            { 2,1,3,99,2,2,2,2,2,2 }, // 4
            { 99,1,99,99,99,99,99,99,99,99 }, // 5
            { 99,1,99,1,99,99,99,99,99,99 }, // 6
            { 1,1,99,99,1,1,1,1,1,1 }, // 7
            { 1,1,2,99,1,1,1,1,1,1 }, // 8
            { 99,1,99,99,99,99,99,99,99,99 }, // 9
            { 1,1,2,99,1,1,1,1,1,1 }, // A
            { 1,1,2,99,1,1,1,1,1,1 }, // B
            { 2,1,4,99,1,2,2,2,2,2 }, // C
            { 1,1,1,99,1,1,1,1,1,1 }, // D
            { 1,1,1,99,1,1,1,1,1,1 }, // E
            { 99,1,99,99,99,99,99,99,99,99 }, // F
            { 1,1,2,99,1,1,1,1,1,1 }, // G
            { 99,1,99,99,99,99,99,99,99,99 }, // H
            { 1,1,1,99,1,1,1,1,1,1 }, // I
            { 1,1,2,99,1,1,1,1,1,1 }, // J
            { 2,1,2,99,2,2,2,2,2,2 }, // K
            { 2,1,99,99,2,2,2,2,2,2 }, // L
            { 1,1,1,99,1,1,1,1,1,1 }, // M
            { 1,1,1,99,1,1,1,1,1,1 }, // N
            { 99,99,99,99,99,99,99,99,99,99 }, // O
            { 4,1,4,99,4,4,4,4,4,4 }, // P
            { 2,1,2,99,2,2,2,2,2,2 } // Q
        };

        // The caller must provide normalized terrain and an explicit original
        // troop column, not an instance/job ID. Missing data does not mean floor.
        public static bool TryGetMovementValue(int normalizedAttribute, int troopColumn, out int value)
        {
            value = 0;
            if (normalizedAttribute < 0 || normalizedAttribute >= TerrainCount
                || troopColumn < 0 || troopColumn >= TroopColumnCount) return false;
            value = movement[normalizedAttribute, troopColumn];
            return true;
        }

        public static bool CanTraverse(int normalizedAttribute, int troopColumn) =>
            TryGetMovementValue(normalizedAttribute, troopColumn, out var value) && value != ImpassableValue;

        // Recovered SetArea2/SetArea4 calculation, NOT an executable route or AP cost.
        // Callers supply the already-resolved budget (the original caller adds 7).
        // Values record budget BEFORE paying this cell's terrain cost. An expensive
        // final cell can therefore be included even when it cannot expand further.
        public static bool TryBuildMovementArea(int width, int height, CombatGridCell origin,
            int budget, int unitId, System.Func<CombatGridCell, int?> movementCost,
            System.Func<CombatGridCell, int?> occupant,
            out System.Collections.Generic.IReadOnlyDictionary<CombatGridCell, int> area)
        {
            var scores = new System.Collections.Generic.Dictionary<CombatGridCell, int>();
            area = new System.Collections.ObjectModel.ReadOnlyDictionary<CombatGridCell, int>(scores);
            if (width <= 0 || height <= 0 || origin.X < 0 || origin.Y < 0
                || origin.X >= width || origin.Y >= height || budget < 0 || budget == int.MaxValue
                || unitId < 0 || movementCost == null || occupant == null) return false;
            scores.Add(origin, budget + 1);
            var frontier = new System.Collections.Generic.Queue<(CombatGridCell cell, int remaining)>();
            void Expand(CombatGridCell cell, int remaining)
            {
                frontier.Enqueue((new CombatGridCell(cell.X - 1, cell.Y), remaining));
                frontier.Enqueue((new CombatGridCell(cell.X + 1, cell.Y), remaining));
                frontier.Enqueue((new CombatGridCell(cell.X, cell.Y - 1), remaining));
                frontier.Enqueue((new CombatGridCell(cell.X, cell.Y + 1), remaining));
            }
            Expand(origin, budget);
            while (frontier.Count > 0)
            {
                var (cell, remaining) = frontier.Dequeue();
                if (remaining <= 0 || cell.X < 0 || cell.Y < 0 || cell.X >= width || cell.Y >= height
                    || (scores.TryGetValue(cell, out var best) && remaining <= best)) continue;
                var cost = movementCost(cell);
                // Existing native terrain table contains positive costs and 99 only;
                // unknown/nonpositive values are not guessed into a valid native area.
                if (!cost.HasValue || cost.Value <= 0 || cost.Value == 99) continue;
                var occupiedBy = occupant(cell);
                if (occupiedBy.HasValue && occupiedBy.Value >= 0 && occupiedBy.Value / 100 != unitId / 100) continue;
                scores[cell] = remaining;
                var next = remaining - cost.Value;
                if (next > 0) Expand(cell, next);
            }
            return true;
        }
    }
}
