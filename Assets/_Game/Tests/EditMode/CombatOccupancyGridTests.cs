using System;
using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatOccupancyGridTests
    {
        [Test]
        public void PlacementValidatesBoundsLifeAndOccupancyWithoutChangingVitals()
        {
            var world = World(); var grid = Grid(world);
            Assert.That(grid.TryPlace(100, Cell(0, 0)), Is.True);
            Assert.That(grid.TryPlace(101, Cell(0, 0)), Is.False);
            Assert.That(grid.TryPlace(101, Cell(-1, 0)), Is.False);
            Assert.That(grid.TryPlace(101, Cell(4, 0)), Is.False);
            Assert.That(grid.TryPlace(999, Cell(1, 0)), Is.False);
            world.Get(101).Hp = 0; Assert.That(grid.TryPlace(101, Cell(1, 0)), Is.False);
            Assert.That(world.Get(100).Hp, Is.EqualTo(50)); Assert.That(world.Get(100).Mp, Is.EqualTo(10));
            Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(6));
            Assert.That(grid.TryPlace(100, Cell(1, 0)), Is.False);
        }

        [Test]
        public void RoutesCanInspectAlliedCellsButCannotStopOnThemOrCrossEnemies()
        {
            var world = World(); var grid = Grid(world);
            grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(101, Cell(1, 0)); grid.TryPlace(800, Cell(0, 1));
            Assert.That(grid.CanTraverse(100, Cell(1, 0)), Is.True);
            Assert.That(grid.CanTraverse(100, Cell(0, 1)), Is.False);
            Assert.That(grid.TryReserveStep(100, Cell(1, 0)), Is.False);
            Assert.That(grid.TryReserveStep(100, Cell(0, 1)), Is.False);
            Assert.That(grid.TryGetPosition(100, out var cell), Is.True); Assert.That(cell, Is.EqualTo(Cell(0, 0)));
            world.Get(101).LifeState = UnitLifeState.Corpse;
            Assert.That(grid.CanTraverse(100, Cell(1, 0)), Is.False);
            grid.ReleaseUnit(101);
            Assert.That(grid.CanTraverse(100, Cell(1, 0)), Is.True);
        }

        [Test]
        public void ConflictingReservationsAreRejectedAndFailedReplacementPreservesPrevious()
        {
            var grid = Grid(World()); grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(101, Cell(2, 0));
            Assert.That(grid.TryReserveStep(100, Cell(1, 0)), Is.True);
            Assert.That(grid.TryReserveStep(100, Cell(1, 0)), Is.True);
            Assert.That(grid.TryReserveStep(101, Cell(1, 0)), Is.False);
            Assert.That(grid.TryReserveStep(100, Cell(3, 3)), Is.False);
            Assert.That(grid.TryGetReservation(Cell(1, 0), out var owner), Is.True); Assert.That(owner, Is.EqualTo(100));
            Assert.That(grid.TryReserveStep(100, Cell(0, 1)), Is.True);
            Assert.That(grid.TryGetReservation(Cell(1, 0), out _), Is.False);
            Assert.That(grid.TryReserveStep(101, Cell(1, 0)), Is.True);
            Assert.That(grid.CancelReservation(100), Is.True); Assert.That(grid.CancelReservation(100), Is.False);
        }

        [Test]
        public void CommitRechecksTerrainAndChangesOnlyOccupancyAndCoordinates()
        {
            var world = World(); var blocked = false;
            var grid = new CombatOccupancyGrid(world, 4, 4, (_, _) => !blocked, Cardinal);
            grid.TryPlace(100, Cell(0, 0)); grid.TryReserveStep(100, Cell(1, 0));
            blocked = true; Assert.That(grid.TryCommitStep(100), Is.False);
            Assert.That(grid.TryGetOccupant(Cell(0, 0), out var id), Is.True); Assert.That(id, Is.EqualTo(100));
            Assert.That(grid.TryGetReservation(Cell(1, 0), out _), Is.True);
            blocked = false; Assert.That(grid.TryCommitStep(100), Is.True);
            Assert.That(grid.TryGetOccupant(Cell(0, 0), out _), Is.False);
            Assert.That(grid.TryGetOccupant(Cell(1, 0), out id), Is.True); Assert.That(id, Is.EqualTo(100));
            Assert.That(grid.TryGetReservation(Cell(1, 0), out _), Is.False);
            Assert.That(world.Get(100).X, Is.EqualTo(1)); Assert.That(world.Get(100).Y, Is.EqualTo(0));
            Assert.That(world.Get(100).Hp, Is.EqualTo(50)); Assert.That(world.Get(100).Mp, Is.EqualTo(10));
            Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(6));
            Assert.That(world.Get(100).Experience, Is.EqualTo(0)); Assert.That(grid.TryCommitStep(100), Is.False);
        }

        [Test]
        public void DirectionPolicyIsExplicitAndTeleportOrStationaryReservationIsRejected()
        {
            var world = World(); var allowDiagonal = false;
            var grid = new CombatOccupancyGrid(world, 4, 4, (_, _) => true,
                (a, b) => allowDiagonal || Cardinal(a, b));
            grid.TryPlace(100, Cell(0, 0));
            Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.False);
            Assert.That(grid.TryReserveStep(100, Cell(0, 0)), Is.False);
            allowDiagonal = true;
            Assert.That(grid.TryReserveStep(100, Cell(3, 3)), Is.False);
            Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.True);
            allowDiagonal = false; Assert.That(grid.TryCommitStep(100), Is.False);
            allowDiagonal = true; Assert.That(grid.TryCommitStep(100), Is.True);
        }

        [Test]
        public void DeathAndCoordinateDriftFailClosedUntilHostExplicitlyReleasesOccupancy()
        {
            var world = World(); var grid = Grid(world); grid.TryPlace(100, Cell(0, 0));
            grid.TryReserveStep(100, Cell(1, 0)); world.Get(100).LifeState = UnitLifeState.Corpse;
            Assert.That(grid.TryCommitStep(100), Is.False);
            Assert.That(grid.TryGetOccupant(Cell(0, 0), out _), Is.True);
            Assert.That(grid.ReleaseUnit(100), Is.True);
            Assert.That(grid.TryGetReservation(Cell(1, 0), out _), Is.False);
            Assert.That(world.Get(100).LifeState, Is.EqualTo(UnitLifeState.Corpse));
            Assert.That(grid.TryPlace(100, Cell(0, 0)), Is.False);
            grid.TryPlace(101, Cell(0, 0)); world.Get(101).X = 2;
            Assert.That(grid.TryReserveStep(101, Cell(1, 0)), Is.False);
            Assert.That(grid.ReleaseUnit(101), Is.True); Assert.That(grid.ReleaseUnit(101), Is.False);
        }

        [Test]
        public void MissingPoliciesAndInvalidDimensionsAreNeverAssumedWalkable()
        {
            var world = World();
            Assert.Throws<ArgumentNullException>(() => new CombatOccupancyGrid(null, 4, 4, (_, _) => true, Cardinal));
            Assert.Throws<ArgumentNullException>(() => new CombatOccupancyGrid(world, 4, 4, null, Cardinal));
            Assert.Throws<ArgumentNullException>(() => new CombatOccupancyGrid(world, 4, 4, (_, _) => true, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOccupancyGrid(world, 0, 4, (_, _) => true, Cardinal));
            var grid = new CombatOccupancyGrid(world, 4, 4, (_, _) => false, Cardinal);
            Assert.That(grid.TryPlace(100, Cell(0, 0)), Is.False);
        }

        private static CombatGridCell Cell(int x, int y) => new CombatGridCell(x, y);
        private static bool Cardinal(CombatGridCell a, CombatGridCell b) =>
            Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y) == 1;
        private static CombatOccupancyGrid Grid(CombatWorld world) =>
            new CombatOccupancyGrid(world, 4, 4, (_, _) => true, Cardinal);
        private static CombatWorld World()
        {
            var world = new CombatWorld();
            foreach (short id in new short[] { 100, 101, 800 })
                world.Add(new CombatUnit { Id = id, Enemy = id == 800, Hp = 50, MaxHp = 50, Mp = 10, MaxMp = 10 });
            return world;
        }
    }
}
