using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    public readonly struct CombatGridPlacement
    {
        public CombatGridPlacement(short unitId, CombatGridCell cell) { UnitId = unitId; Cell = cell; }
        public short UnitId { get; }
        public CombatGridCell Cell { get; }
    }

    public readonly struct CombatGridCell : IEquatable<CombatGridCell>
    {
        public CombatGridCell(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public bool Equals(CombatGridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is CombatGridCell other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
    }

    // Native occupancy and read-only route queries, not an AP/terrain formula.
    // The host explicitly supplies terrain and direction policies; no MRC runtime access.
    public sealed class CombatOccupancyGrid
    {
        private readonly CombatWorld world;
        private readonly Func<CombatUnitSnapshot, CombatGridCell, bool> terrainAllows;
        private readonly Func<CombatUnitSnapshot, CombatGridCell, bool> placementAllows;
        private readonly Func<CombatGridCell, CombatGridCell, bool> directionAllows;
        private readonly bool preventDiagonalCornerCutting;
        private readonly Func<CombatUnitSnapshot, CombatGridCell, int?> movementCost;
        private readonly Dictionary<short, CombatGridCell> positions = new();
        private readonly Dictionary<CombatGridCell, short> occupants = new();
        private readonly Dictionary<short, CombatGridCell> destinations = new();
        private readonly Dictionary<CombatGridCell, short> reservations = new();

        public CombatOccupancyGrid(CombatWorld world, int width, int height,
            Func<CombatUnitSnapshot, CombatGridCell, bool> terrainAllows,
            Func<CombatGridCell, CombatGridCell, bool> directionAllows,
            Func<CombatUnitSnapshot, CombatGridCell, bool> placementAllows = null,
            bool preventDiagonalCornerCutting = false,
            Func<CombatUnitSnapshot, CombatGridCell, int?> movementCost = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            this.terrainAllows = terrainAllows ?? throw new ArgumentNullException(nameof(terrainAllows));
            this.directionAllows = directionAllows ?? throw new ArgumentNullException(nameof(directionAllows));
            this.placementAllows = placementAllows ?? this.terrainAllows;
            this.preventDiagonalCornerCutting = preventDiagonalCornerCutting;
            this.movementCost = movementCost;
            Width = width; Height = height;
        }

        public int Width { get; }
        public int Height { get; }
        public bool Contains(CombatGridCell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;
        public bool TryGetOccupant(CombatGridCell cell, out short unitId) => occupants.TryGetValue(cell, out unitId);
        public bool TryGetReservation(CombatGridCell cell, out short unitId) => reservations.TryGetValue(cell, out unitId);
        public bool TryGetPosition(short unitId, out CombatGridCell cell) => positions.TryGetValue(unitId, out cell);

        public bool TryPlace(short unitId, CombatGridCell cell)
        {
            return TryPlaceUnits(new[] { new CombatGridPlacement(unitId, cell) });
        }

        // Policies must be side-effect-free queries. Validate the complete owned input
        // before changing any position, including when a later policy rejects/throws.
        public bool TryPlaceUnits(IReadOnlyList<CombatGridPlacement> placements)
        {
            if (placements == null || placements.Count == 0) return false;
            var pending = new List<CombatGridPlacement>(placements.Count);
            var ids = new HashSet<short>(); var cells = new HashSet<CombatGridCell>();
            foreach (var placement in placements) pending.Add(placement);
            foreach (var placement in pending)
            {
                var id = placement.UnitId; var cell = placement.Cell;
                if (!ids.Add(id) || !cells.Add(cell) || !world.TryGetSnapshot(id, out var unit)
                    || !unit.Alive || positions.ContainsKey(id) || !Contains(cell)
                    || occupants.ContainsKey(cell) || reservations.ContainsKey(cell)
                    || !placementAllows(unit, cell)) return false;
            }
            foreach (var placement in pending)
            {
                positions.Add(placement.UnitId, placement.Cell); occupants.Add(placement.Cell, placement.UnitId);
                world.Get(placement.UnitId).X = placement.Cell.X; world.Get(placement.UnitId).Y = placement.Cell.Y;
            }
            return true;
        }

        private bool TryGetPlacedActor(short unitId, out CombatUnitSnapshot unit)
        {
            unit = default;
            return world.TryGetSnapshot(unitId, out unit) && unit.Alive
                && positions.TryGetValue(unitId, out var cell) && unit.X == cell.X && unit.Y == cell.Y;
        }

        // Route inspection may cross allied occupants; stopping on them remains disabled
        // until final overlap and continuous traversal semantics are decided.
        public bool CanTraverse(short unitId, CombatGridCell cell)
        {
            if (!TryGetPlacedActor(unitId, out var unit) || !Contains(cell) || !terrainAllows(unit, cell)) return false;
            if (reservations.TryGetValue(cell, out var reservedBy) && reservedBy != unitId) return false;
            if (!occupants.TryGetValue(cell, out var occupant) || occupant == unitId) return true;
            return world.TryGetSnapshot(occupant, out var other)
                && other.LifeState == UnitLifeState.Active && other.Enemy == unit.Enemy;
        }

        public bool CanStop(short unitId, CombatGridCell cell) =>
            CanTraverse(unitId, cell) && !occupants.ContainsKey(cell);

        // Fewest steps, NOT cheapest terrain/AP cost or a movement-range decision.
        // Excludes origin; includes destination. Owns the immutable returned snapshot.
        // Allied intermediate cells are query-only until continuous passing is implemented.
        public bool TryFindPath(short unitId, CombatGridCell destination, out IReadOnlyList<CombatGridCell> path,
            bool allowAlliedTransit = true)
        {
            path = Array.Empty<CombatGridCell>();
            if (!TryGetPlacedActor(unitId, out _) || !CanStop(unitId, destination)) return false;
            var origin = positions[unitId];
            var previous = ExploreRoutes(unitId, origin, destination, allowAlliedTransit);
            if (!previous.ContainsKey(destination)) return false;
            var cells = new List<CombatGridCell>();
            for (var cell = destination; !cell.Equals(origin); cell = previous[cell]) cells.Add(cell);
            cells.Reverse(); path = cells.AsReadOnly(); return true;
        }

        // Terrain/occupancy connectivity only, NOT an AP-approved movement range.
        // Allied transit cells may connect routes, but never become stopping destinations.
        public bool TryGetReachableDestinations(short unitId, out IReadOnlyList<CombatGridCell> cells)
        {
            cells = Array.Empty<CombatGridCell>();
            if (!TryGetPlacedActor(unitId, out _)) return false;
            var routes = ExploreRoutes(unitId, positions[unitId], null);
            var destinations = new List<CombatGridCell>();
            foreach (var cell in routes.Keys)
                if (CanStop(unitId, cell)) destinations.Add(cell);
            // Stable native top-to-bottom, left-to-right ordering, not dictionary order.
            destinations.Sort((a, b) => { var row = a.Y.CompareTo(b.Y); return row != 0 ? row : a.X.CompareTo(b.X); });
            cells = destinations.AsReadOnly(); return true;
        }

        // Original area -> score backtracking -> cardinal-pair compression, guarded
        // by native execution policies. Occupied transit is not executable yet.
        public bool TryFindBudgetedPath(short unitId, CombatGridCell destination, int budget, bool verticalFirst,
            out IReadOnlyList<CombatGridCell> path, out bool nextVerticalFirst)
        {
            path = Array.Empty<CombatGridCell>(); nextVerticalFirst = verticalFirst;
            return CanStop(unitId, destination) && TryBuildBudgetArea(unitId, budget, out var area)
                && TryFindBudgetedPathInArea(unitId, destination, area, verticalFirst, out path, out nextVerticalFirst);
        }

        public bool TryGetBudgetedDestinations(short unitId, int budget, bool verticalFirst,
            out IReadOnlyList<CombatGridCell> cells)
        {
            cells = Array.Empty<CombatGridCell>();
            if (!TryBuildBudgetArea(unitId, budget, out var area)) return false;
            var candidates = new List<CombatGridCell>();
            foreach (var cell in area.Keys)
                if (CanStop(unitId, cell) && TryFindBudgetedPathInArea(unitId, cell, area, verticalFirst, out _, out _))
                    candidates.Add(cell);
            candidates.Sort((a, b) => { var row = a.Y.CompareTo(b.Y); return row != 0 ? row : a.X.CompareTo(b.X); });
            cells = candidates.AsReadOnly(); return true;
        }

        private bool TryBuildBudgetArea(short unitId, int budget, out IReadOnlyDictionary<CombatGridCell, int> area)
        {
            area = null;
            if (movementCost == null || !TryGetPlacedActor(unitId, out var actor)) return false;
            // Area construction applies the original id/100 group rule. A same-group
            // cardinal intermediate can disappear during diagonal compression.
            int? Cost(CombatGridCell cell) => CanTraverse(unitId, cell) ? movementCost(actor, cell) : null;
            return CombatTerrainTraversal.TryBuildMovementArea(Width, Height, positions[unitId], budget, unitId,
                Cost, cell => occupants.TryGetValue(cell, out var other) ? other : (int?)null, out area);
        }

        private bool TryFindBudgetedPathInArea(short unitId, CombatGridCell destination,
            IReadOnlyDictionary<CombatGridCell, int> area, bool verticalFirst,
            out IReadOnlyList<CombatGridCell> path, out bool nextVerticalFirst)
        {
            path = Array.Empty<CombatGridCell>(); nextVerticalFirst = verticalFirst;
            var origin = positions[unitId];
            if (!CombatMovementPath.TryBacktrackMovementArea(Width, Height, origin, destination, area, verticalFirst,
                out var cardinal, out var next)) return false;
            var previous = origin;
            foreach (var cell in cardinal)
            {
                if (!CanTraverse(unitId, cell) || !CanTraverseEdge(unitId, previous, cell)) return false;
                previous = cell;
            }
            if (!CombatMovementPath.TryCompressCardinalPath(origin, cardinal,
                (from, to) => CanStop(unitId, to) && CanTraverseEdge(unitId, from, to), out var compressed)) return false;
            previous = origin;
            // Executed steps still require exclusive occupancy. Do not overwrite
            // another unit when compression leaves an occupied intermediate behind.
            foreach (var cell in compressed)
            {
                if (!CanStop(unitId, cell) || !CanTraverseEdge(unitId, previous, cell)) return false;
                previous = cell;
            }
            path = compressed; nextVerticalFirst = next; return true;
        }

        private Dictionary<CombatGridCell, CombatGridCell> ExploreRoutes(short unitId, CombatGridCell origin,
            CombatGridCell? destination, bool allowAlliedTransit = true)
        {
            var previous = new Dictionary<CombatGridCell, CombatGridCell> { [origin] = origin };
            var frontier = new Queue<CombatGridCell>(); frontier.Enqueue(origin);
            while (frontier.Count > 0 && (!destination.HasValue || !previous.ContainsKey(destination.Value)))
            {
                var current = frontier.Dequeue();
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var next = new CombatGridCell(current.X + dx, current.Y + dy);
                        if (previous.ContainsKey(next) || !CanTraverse(unitId, next)
                            || (!allowAlliedTransit && occupants.ContainsKey(next))
                            || !CanTraverseEdge(unitId, current, next)) continue;
                        previous.Add(next, current); frontier.Enqueue(next);
                    }
            }
            return previous;
        }

        // Reserves one next stopping cell, not a whole path or a final click destination.
        // Failed replacement retains the previous reservation.
        public bool TryReserveStep(short unitId, CombatGridCell cell)
        {
            if (!CanStop(unitId, cell)) return false;
            var origin = positions[unitId];
            if (!CanTraverseEdge(unitId, origin, cell)) return false;
            if (destinations.TryGetValue(unitId, out var previous) && previous.Equals(cell)) return true;
            CancelReservation(unitId);
            destinations.Add(unitId, cell); reservations.Add(cell, unitId);
            return true;
        }

        public bool TryCommitStep(short unitId)
        {
            if (!destinations.TryGetValue(unitId, out var cell) || !CanTraverse(unitId, cell)
                || occupants.ContainsKey(cell)) return false;
            var origin = positions[unitId];
            // Recheck the current policies immediately before the position mutation.
            if (!CanTraverseEdge(unitId, origin, cell)) return false;
            occupants.Remove(origin); occupants.Add(cell, unitId); positions[unitId] = cell;
            world.Get(unitId).X = cell.X; world.Get(unitId).Y = cell.Y;
            CancelReservation(unitId);
            return true;
        }

        private bool CanTraverseEdge(short unitId, CombatGridCell from, CombatGridCell to)
        {
            if (!IsAdjacent(from, to) || !directionAllows(from, to)) return false;
            return !preventDiagonalCornerCutting || from.X == to.X || from.Y == to.Y
                || (CanTraverse(unitId, new CombatGridCell(to.X, from.Y))
                    && CanTraverse(unitId, new CombatGridCell(from.X, to.Y)));
        }

        private static bool IsAdjacent(CombatGridCell from, CombatGridCell to)
        {
            var dx = Math.Abs((long)to.X - from.X); var dy = Math.Abs((long)to.Y - from.Y);
            return dx <= 1 && dy <= 1 && (dx != 0 || dy != 0);
        }

        public bool CancelReservation(short unitId)
        {
            if (!destinations.TryGetValue(unitId, out var cell)) return false;
            destinations.Remove(unitId); reservations.Remove(cell); return true;
        }

        // Explicit host decision only: corpse/removal packets are not yet wired here.
        // Releasing occupancy does not delete a unit, restore AP, or resurrect it.
        public bool ReleaseUnit(short unitId)
        {
            var released = CancelReservation(unitId);
            if (!positions.TryGetValue(unitId, out var cell)) return released;
            positions.Remove(unitId); occupants.Remove(cell); return true;
        }
    }
}
