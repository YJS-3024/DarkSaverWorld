using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatMovementPathTests
    {
        [Test]
        public void BudgetedGridUsesCardinalRangeBeforeDiagonalCompressionWithoutMutation()
        {
            var world = World(); var grid = new CombatOccupancyGrid(world, 4, 4, (_, _) => true, (_, _) => true,
                preventDiagonalCornerCutting: true, movementCost: (_, _) => 1);
            grid.TryPlace(100, Cell(0, 0));
            Assert.That(grid.TryFindBudgetedPath(100, Cell(1, 1), 1, true, out var path, out var tie), Is.False);
            Assert.That(path.Count, Is.Zero); Assert.That(tie, Is.True);
            Assert.That(grid.TryFindBudgetedPath(100, Cell(1, 1), 2, true, out path, out tie), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(1, 1) }, path); Assert.That(tie, Is.True);
            Assert.That(grid.TryGetBudgetedDestinations(100, 1, true, out var cells), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(1, 0), Cell(0, 1) }, cells);
            Assert.That(grid.TryGetBudgetedDestinations(100, 0, true, out cells), Is.True); Assert.That(cells.Count, Is.Zero);
            Assert.That(world.Get(100).X, Is.Zero); Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(6));
            Assert.That(grid.TryGetReservation(Cell(1, 1), out _), Is.False);
        }

        [Test]
        public void BudgetedGridKeepsCornerReservationsOccupiedTransitAndDirectionPolicies()
        {
            var world = World(); var grid = new CombatOccupancyGrid(world, 4, 4, (_, _) => true, (_, _) => true,
                preventDiagonalCornerCutting: true, movementCost: (_, _) => 1);
            grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(800, Cell(2, 0));
            Assert.That(grid.TryReserveStep(800, Cell(1, 0)), Is.True);
            Assert.That(grid.TryFindBudgetedPath(100, Cell(1, 1), 2, true, out var path, out _), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(0, 1), Cell(1, 1) }, path);
            Assert.That(grid.TryGetReservation(Cell(1, 0), out var id), Is.True); Assert.That(id, Is.EqualTo(800));
            grid.CancelReservation(800);
            Assert.That(grid.TryFindBudgetedPath(100, Cell(1, 1), 2, true, out path, out _), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(1, 1) }, path);
            var corridor = new CombatOccupancyGrid(world, 4, 1, (_, _) => true, Cardinal, movementCost: (_, _) => 1);
            corridor.TryPlace(100, Cell(0, 0)); corridor.TryPlace(101, Cell(1, 0));
            Assert.That(corridor.TryFindBudgetedPath(100, Cell(3, 0), 4, true, out _, out _), Is.False);
            var blocked = new CombatOccupancyGrid(World(), 4, 4, (_, _) => true, (_, _) => false,
                movementCost: (_, _) => 1); blocked.TryPlace(100, Cell(0, 0));
            Assert.That(blocked.TryFindBudgetedPath(100, Cell(1, 0), 4, true, out _, out _), Is.False);
            Assert.That(blocked.TryGetBudgetedDestinations(100, 4, true, out var cells), Is.True); Assert.That(cells.Count, Is.Zero);
        }

        [Test]
        public void BudgetedGridNeverInventsMissingCostsOrFallsBackToUnboundedConnectivity()
        {
            var missing = Grid(World(), Cardinal); missing.TryPlace(100, Cell(0, 0));
            Assert.That(missing.TryFindPath(100, Cell(3, 0), out _), Is.True);
            Assert.That(missing.TryFindBudgetedPath(100, Cell(1, 0), 4, true, out _, out _), Is.False);
            Assert.That(missing.TryGetBudgetedDestinations(100, 4, true, out _), Is.False);
            var cost = new CombatOccupancyGrid(World(), 4, 1, (_, _) => true, Cardinal,
                movementCost: (_, c) => c.X == 1 ? null : 1); cost.TryPlace(100, Cell(0, 0));
            Assert.That(cost.TryFindBudgetedPath(100, Cell(2, 0), 4, true, out _, out _), Is.False);
        }

        [Test]
        public void OriginalBacktrackingUsesBothTieOrdersAndOwnedUnchangedArea()
        {
            var origin = Cell(0, 0); var target = Cell(1, 1);
            CombatTerrainTraversal.TryBuildMovementArea(3, 3, origin, 4, 100, _ => 1, _ => null, out var area);
            foreach (var vertical in new[] { false, true })
            {
                Assert.That(CombatMovementPath.TryBacktrackMovementArea(3, 3, origin, target, area, vertical,
                    out var path, out var nextFlag), Is.True);
                CollectionAssert.AreEqual(new[] { vertical ? Cell(1, 0) : Cell(0, 1), target }, path);
                Assert.That(nextFlag, Is.EqualTo(vertical)); // Two reverse moves toggle twice.
                Assert.That(area[target], Is.EqualTo(3)); Assert.That(area[origin], Is.EqualTo(5));
                Assert.Throws<NotSupportedException>(() => ((IList<CombatGridCell>)path).Add(Cell(2, 2)));
            }
            Assert.That(CombatMovementPath.TryBacktrackMovementArea(3, 3, origin, Cell(1, 0), area, false,
                out _, out var flag), Is.True); Assert.That(flag, Is.True);
        }

        [Test]
        public void OriginalBacktrackingUsesPrePaymentScoresEvenWhenTheyPreferExpensiveCell()
        {
            var origin = Cell(0, 1); var target = Cell(2, 1);
            CombatTerrainTraversal.TryBuildMovementArea(4, 3, origin, 6, 100,
                c => c.Equals(Cell(1, 1)) ? 4 : 1, _ => null, out var area);
            Assert.That(CombatMovementPath.TryBacktrackMovementArea(4, 3, origin, target, area, true,
                out var path, out _), Is.True);
            Assert.That(area[target], Is.EqualTo(3)); // Area found a cheaper detour, but reverse selection compares pre-payment scores.
            CollectionAssert.AreEqual(new[] { Cell(1, 1), target }, path);
            Assert.That(path[path.Count - 1], Is.EqualTo(target));
            Assert.That(CombatMovementPath.TryCompressCardinalPath(origin, path, (_, _) => true,
                out var compressed), Is.True); Assert.That(compressed.Count, Is.EqualTo(2));
            Assert.That(compressed[compressed.Count - 1], Is.EqualTo(target));
        }

        [Test]
        public void OriginalBacktrackingBlockedTerrainDetourFeedsDiagonalCompression()
        {
            var origin = Cell(0, 1); var target = Cell(2, 1);
            CombatTerrainTraversal.TryBuildMovementArea(4, 3, origin, 6, 100,
                c => c.Equals(Cell(1, 1)) ? 99 : 1, _ => null, out var area);
            Assert.That(CombatMovementPath.TryBacktrackMovementArea(4, 3, origin, target, area, true,
                out var path, out _), Is.True);
            Assert.That(path.Count, Is.EqualTo(4)); CollectionAssert.DoesNotContain(path, Cell(1, 1));
            Assert.That(CombatMovementPath.TryCompressCardinalPath(origin, path, (_, _) => true,
                out var compressed), Is.True); Assert.That(compressed.Count, Is.EqualTo(2));
            // A production host must still reject corner merges beside the blocked cell.
            Assert.That(CombatMovementPath.TryCompressCardinalPath(origin, path, (_, _) => false,
                out var unmerged), Is.True); CollectionAssert.AreEqual(path, unmerged);
        }

        [Test]
        public void OriginalBacktrackingAllowsExpensiveFinalCellButNotAnUnreachableTarget()
        {
            var origin = Cell(0, 0);
            CombatTerrainTraversal.TryBuildMovementArea(3, 1, origin, 1, 100, _ => 4, _ => null, out var area);
            Assert.That(CombatMovementPath.TryBacktrackMovementArea(3, 1, origin, Cell(1, 0), area, false,
                out var path, out var flag), Is.True); CollectionAssert.AreEqual(new[] { Cell(1, 0) }, path);
            Assert.That(flag, Is.True);
            Assert.That(CombatMovementPath.TryBacktrackMovementArea(3, 1, origin, Cell(2, 0), area, true,
                out path, out flag), Is.False); Assert.That(path.Count, Is.Zero); Assert.That(flag, Is.True);
            Assert.That(CombatMovementPath.TryBacktrackMovementArea(3, 1, origin, origin, area, true,
                out path, out flag), Is.True); Assert.That(path.Count, Is.Zero); Assert.That(flag, Is.True);
        }

        [Test]
        public void OriginalBacktrackingRejectsMalformedAreaWithoutPublishingPartialRoute()
        {
            var origin = Cell(0, 0); var target = Cell(2, 0);
            foreach (var area in new[] {
                new Dictionary<CombatGridCell, int> { [origin] = 4, [target] = 1 },
                new Dictionary<CombatGridCell, int> { [origin] = 0, [Cell(1, 0)] = 2, [target] = 3 },
                new Dictionary<CombatGridCell, int> { [origin] = 4, [Cell(1, 0)] = 2, [target] = 1, [Cell(-1, 0)] = 1 } })
            {
                Assert.That(CombatMovementPath.TryBacktrackMovementArea(3, 1, origin, target, area, false,
                    out var path, out var flag), Is.False); Assert.That(path.Count, Is.Zero); Assert.That(flag, Is.False);
            }
            Assert.That(CombatMovementPath.TryBacktrackMovementArea(3, 1, origin, target, null, false,
                out _, out _), Is.False);
        }

        [Test]
        public void OriginalCompressionMatchesAllSixteenCardinalPairs()
        {
            var origin = Cell(10, 10);
            var directions = new[] { Cell(-1, 0), Cell(1, 0), Cell(0, -1), Cell(0, 1) };
            foreach (var first in directions)
                foreach (var second in directions)
                {
                    var middle = Cell(origin.X + first.X, origin.Y + first.Y);
                    var end = Cell(middle.X + second.X, middle.Y + second.Y);
                    Assert.That(CombatMovementPath.TryCompressCardinalPath(origin, new[] { middle, end },
                        (_, _) => true, out var path), Is.True);
                    var perpendicular = (first.X == 0) != (second.X == 0);
                    Assert.That(path.Count, Is.EqualTo(perpendicular ? 1 : 2));
                    Assert.That(path[path.Count - 1], Is.EqualTo(end));
                    if (!perpendicular) Assert.That(path[0], Is.EqualTo(middle));
                }
        }

        [Test]
        public void OriginalCompressionGreedyPairsDoNotConsumeTheThirdStep()
        {
            Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(3, 3),
                new[] { Cell(2, 3), Cell(2, 2), Cell(3, 2), Cell(3, 1), Cell(2, 1) },
                (_, _) => true, out var path), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(2, 2), Cell(3, 1), Cell(2, 1) }, path);
        }

        [Test]
        public void CompressionPreservesCardinalRouteWhenDiagonalPolicyRejectsCorner()
        {
            var source = new[] { Cell(1, 0), Cell(1, 1) };
            Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(0, 0), source,
                (_, _) => false, out var path), Is.True);
            CollectionAssert.AreEqual(source, path);
            Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(0, 0), source,
                (from, to) => from.Equals(Cell(0, 0)) && to.Equals(Cell(1, 1)), out path), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(1, 1) }, path);
        }

        [Test]
        public void CompressionRejectsInvalidInputAndOwnsItsReadOnlyOutput()
        {
            foreach (var source in new[] { new[] { Cell(1, 1) }, new[] { Cell(0, 0) },
                new[] { Cell(2, 0) }, new[] { Cell(int.MinValue, 0) } })
            {
                Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(0, 0), source,
                    (_, _) => true, out var invalid), Is.False); Assert.That(invalid.Count, Is.Zero);
            }
            Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(0, 0), null,
                (_, _) => true, out _), Is.False);
            Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(0, 0), Array.Empty<CombatGridCell>(),
                null, out _), Is.False);
            Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(0, 0), Array.Empty<CombatGridCell>(),
                (_, _) => true, out var empty), Is.True); Assert.That(empty.Count, Is.Zero);
            var mutable = new List<CombatGridCell> { Cell(1, 0), Cell(1, 1) };
            Assert.That(CombatMovementPath.TryCompressCardinalPath(Cell(0, 0), mutable,
                (_, _) => { mutable.Clear(); return true; }, out var path), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(1, 1) }, path);
            Assert.Throws<NotSupportedException>(() => ((IList<CombatGridCell>)path).Add(Cell(3, 3)));
        }

        [Test]
        public void FewestStepsRespectExplicitDirectionsWithoutSpendingOrReserving()
        {
            var world = World(); var grid = Grid(world, (_, _) => true);
            grid.TryPlace(100, Cell(0, 0)); var ap = world.Get(100).ActionPoints.Current;
            Assert.That(grid.TryFindPath(100, Cell(3, 3), out var path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3)); Assert.That(path[0], Is.EqualTo(Cell(1, 1)));
            Assert.That(path[2], Is.EqualTo(Cell(3, 3)));
            Assert.Throws<NotSupportedException>(() => ((IList<CombatGridCell>)path)[0] = Cell(3, 0));
            Assert.That(grid.TryGetReservation(Cell(1, 1), out _), Is.False);
            Assert.That(world.Get(100).X, Is.EqualTo(0)); Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(ap));
            var cardinal = Grid(world, Cardinal); cardinal.TryPlace(100, Cell(0, 0));
            Assert.That(cardinal.TryFindPath(100, Cell(3, 3), out path), Is.True); Assert.That(path.Count, Is.EqualTo(6));
        }

        [Test]
        public void AlliedIntermediateCellsAreQueriesNotLegalStoppingCells()
        {
            var world = World(); var grid = new CombatOccupancyGrid(world, 4, 1, (_, _) => true, Cardinal);
            grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(101, Cell(1, 0));
            Assert.That(grid.TryFindPath(100, Cell(3, 0), out var path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3)); Assert.That(path[0], Is.EqualTo(Cell(1, 0)));
            Assert.That(grid.CanStop(100, Cell(1, 0)), Is.False);
            Assert.That(grid.TryReserveStep(100, path[0]), Is.False);
            Assert.That(grid.TryFindPath(100, Cell(1, 0), out path), Is.False); Assert.That(path, Is.Empty);
            world.Get(101).LifeState = UnitLifeState.Corpse;
            Assert.That(grid.TryFindPath(100, Cell(3, 0), out path), Is.False);
        }

        [Test]
        public void EnemiesTerrainAndReservationsBlockAndRequeryUsesCurrentState()
        {
            var world = World(); var allowed = true;
            var grid = new CombatOccupancyGrid(world, 4, 1, (_, _) => allowed, Cardinal);
            grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(800, Cell(2, 0));
            Assert.That(grid.TryReserveStep(800, Cell(1, 0)), Is.True);
            Assert.That(grid.TryFindPath(100, Cell(1, 0), out _), Is.False);
            grid.CancelReservation(800);
            Assert.That(grid.TryFindPath(100, Cell(1, 0), out var old), Is.True);
            Assert.That(grid.TryFindPath(100, Cell(3, 0), out _), Is.False);
            allowed = false; Assert.That(grid.TryFindPath(100, Cell(1, 0), out var path), Is.False);
            Assert.That(path, Is.Empty); Assert.That(old[0], Is.EqualTo(Cell(1, 0)));
            allowed = true; Assert.That(grid.TryFindPath(100, Cell(1, 0), out _), Is.True);
        }

        [Test]
        public void InvalidDeadDriftedAndDirectionBlockedQueriesFailWithoutChangingReservation()
        {
            var world = World(); var grid = Grid(world, Cardinal); grid.TryPlace(100, Cell(0, 0));
            Assert.That(grid.TryReserveStep(100, Cell(1, 0)), Is.True);
            foreach (var goal in new[] { Cell(-1, 0), Cell(4, 0), Cell(0, 0) })
                Assert.That(grid.TryFindPath(100, goal, out var path), Is.False);
            Assert.That(grid.TryFindPath(999, Cell(2, 0), out _), Is.False);
            world.Get(100).Hp = 0; Assert.That(grid.TryFindPath(100, Cell(2, 0), out _), Is.False);
            world.Get(100).Hp = 50; world.Get(100).X = 2;
            Assert.That(grid.TryFindPath(100, Cell(2, 0), out _), Is.False);
            world.Get(100).X = 0;
            Assert.That(grid.TryGetReservation(Cell(1, 0), out var owner), Is.True); Assert.That(owner, Is.EqualTo(100));
            var blocked = Grid(world, (_, _) => false); blocked.TryPlace(100, Cell(0, 0));
            Assert.That(blocked.TryFindPath(100, Cell(3, 3), out _), Is.False);
        }

        [Test]
        public void SelectedPathRequiresActiveSelectionAndDoesNotPublishMovement()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Movement path query check"); SceneManager.MoveGameObjectToScene(go, scene);
                var battle = go.AddComponent<BattleSceneRoot>(); NativeCombatStateFixture.Initialize(battle,6);
                Assert.That(battle.TryFindSelectedMovementPath(Cell(2, 2), out _), Is.False);
                Assert.That(battle.TryConfigureGrid(4, 4, new[] { new CombatGridPlacement(100, Cell(0, 0)),
                    new CombatGridPlacement(800, Cell(3, 3)) }, (_, _) => true, (_, _) => true), Is.True);
                var events = 0; battle.MovementStepApplied += _ => events++;
                Assert.That(battle.TryFindSelectedMovementPath(Cell(2, 2), out _), Is.False);
                battle.TrySelectUnit(100);
                Assert.That(battle.TryFindSelectedMovementPath(Cell(2, 2), out var path), Is.True);
                Assert.That(path.Count, Is.EqualTo(2)); Assert.That(events, Is.EqualTo(0)); Assert.That(battle.PlayerAp, Is.EqualTo(6));
                battle.enabled = false; Assert.That(battle.TryFindSelectedMovementPath(Cell(2, 2), out path), Is.False);
                Assert.That(path, Is.Empty);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void BudgetedDiagonalCanSkipSameGroupIntermediateWithoutOverwritingItsOccupant()
        {
            var world = World();
            var grid = new CombatOccupancyGrid(world, 2, 2, (_, _) => true, (_, _) => true,
                preventDiagonalCornerCutting: true, movementCost: (_, cell) => cell.Equals(Cell(0, 1)) ? (int?)null : 1);
            Assert.That(grid.TryPlace(100, Cell(0, 0)), Is.True);
            Assert.That(grid.TryPlace(101, Cell(1, 0)), Is.True);
            Assert.That(grid.TryFindBudgetedPath(100, Cell(1, 1), 2, true, out var path, out _), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(1, 1) }, path);
            Assert.That(grid.TryFindBudgetedPath(100, Cell(1, 0), 2, true, out _, out _), Is.False);
            Assert.That(grid.TryGetBudgetedDestinations(100, 2, true, out var cells), Is.True);
            CollectionAssert.AreEqual(new[] { Cell(1, 1) }, cells);
            Assert.That(grid.TryReserveStep(100, path[0]), Is.True);
            Assert.That(grid.TryCommitStep(100), Is.True);
            Assert.That(grid.TryGetOccupant(Cell(1, 0), out var owner), Is.True);
            Assert.That(owner, Is.EqualTo(101));
            Assert.That(world.Get(101).X, Is.EqualTo(1)); Assert.That(world.Get(101).Y, Is.Zero);
            Assert.That(grid.TryGetOccupant(Cell(0, 0), out _), Is.False);
            Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(6));
        }

        [Test]
        public void BudgetedCompressionDoesNotGrantOccupiedLandingOrDifferentGroupTraversal()
        {
            foreach (short occupant in new short[] { 101, 200, 800 })
            {
                var world = World();
                if (occupant == 200) world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = 200, Enemy = false, Hp = 50, MaxHp = 50 }));
                var grid = new CombatOccupancyGrid(world, 2, 2, (_, _) => true,
                    occupant == 101 ? Cardinal : (_, _) => true, preventDiagonalCornerCutting: true,
                    movementCost: (_, cell) => cell.Equals(Cell(0, 1)) ? (int?)null : 1);
                grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(occupant, Cell(1, 0));
                Assert.That(grid.TryFindBudgetedPath(100, Cell(1, 1), 2, true, out var path, out var tie), Is.False);
                Assert.That(path.Count, Is.Zero); Assert.That(tie, Is.True);
                Assert.That(grid.TryGetOccupant(Cell(1, 0), out var owner), Is.True);
                Assert.That(owner, Is.EqualTo(occupant));
                Assert.That(grid.TryGetReservation(Cell(1, 1), out _), Is.False);
            }
        }

        private static CombatGridCell Cell(int x, int y) => new CombatGridCell(x, y);
        private static bool Cardinal(CombatGridCell a, CombatGridCell b) =>
            Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y) == 1;
        private static CombatOccupancyGrid Grid(CombatWorld world, Func<CombatGridCell, CombatGridCell, bool> direction) =>
            new CombatOccupancyGrid(world, 4, 4, (_, _) => true, direction);
        private static CombatWorld World()
        {
            var world = new CombatWorld();
            foreach (short id in new short[] { 100, 101, 800 })
                world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = id, Enemy = id == 800, Hp = 50, MaxHp = 50 }));
            return world;
        }
    }
}
