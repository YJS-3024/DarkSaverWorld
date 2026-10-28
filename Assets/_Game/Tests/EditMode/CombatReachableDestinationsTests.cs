using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatReachableDestinationsTests
    {
        [Test]
        public void DestinationsAreOwnedSortedAndMatchIndividualPathsWithoutMutation()
        {
            var world = World(); var grid = Grid(world);
            grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(800, Cell(3, 3));
            Assert.That(grid.TryReserveStep(100, Cell(1, 0)), Is.True);
            Assert.That(grid.TryGetReachableDestinations(100, out var cells), Is.True);
            Assert.That(cells.Count, Is.EqualTo(14)); Assert.That(cells[0], Is.EqualTo(Cell(1, 0)));
            Assert.Throws<NotSupportedException>(() => ((IList<CombatGridCell>)cells).Clear());
            var unique = new HashSet<CombatGridCell>(cells); Assert.That(unique.Count, Is.EqualTo(cells.Count));
            for (var y = 0; y < 4; y++)
                for (var x = 0; x < 4; x++)
                    Assert.That(unique.Contains(Cell(x, y)), Is.EqualTo(grid.TryFindPath(100, Cell(x, y), out _)));
            for (var i = 1; i < cells.Count; i++)
                Assert.That(cells[i].Y > cells[i - 1].Y ||
                    (cells[i].Y == cells[i - 1].Y && cells[i].X > cells[i - 1].X), Is.True);
            Assert.That(grid.TryGetReservation(Cell(1, 0), out var owner), Is.True); Assert.That(owner, Is.EqualTo(100));
            Assert.That(world.Get(100).X, Is.EqualTo(0)); Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(6));
            grid.TryReserveStep(800, Cell(2, 3));
            Assert.That(grid.TryGetReachableDestinations(100, out var latest), Is.True);
            Assert.That(latest.Count, Is.EqualTo(13)); Assert.That(cells.Count, Is.EqualTo(14));
        }

        [Test]
        public void AlliedTransitIsExcludedAndCorpseCutsTheCorridor()
        {
            var world = World(); var grid = new CombatOccupancyGrid(world, 4, 1, (_, _) => true, (_, _) => true);
            grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(101, Cell(1, 0));
            Assert.That(grid.TryGetReachableDestinations(100, out var cells), Is.True);
            Assert.That(cells, Is.EqualTo(new[] { Cell(2, 0), Cell(3, 0) }));
            world.Get(101).LifeState = UnitLifeState.Corpse;
            Assert.That(grid.TryGetReachableDestinations(100, out var current), Is.True); Assert.That(current, Is.Empty);
            Assert.That(cells.Count, Is.EqualTo(2));
            grid.ReleaseUnit(101); grid.TryGetReachableDestinations(100, out current);
            Assert.That(current.Count, Is.EqualTo(3));
        }

        [Test]
        public void CornerTerrainAndDirectionPoliciesDefineConnectivityNotAp()
        {
            var world = World(); var openSides = false;
            var grid = new CombatOccupancyGrid(world, 2, 2,
                (_, c) => openSides || c.X == c.Y, (_, _) => true, preventDiagonalCornerCutting: true);
            grid.TryPlace(100, Cell(0, 0));
            Assert.That(grid.TryGetReachableDestinations(100, out var cells), Is.True); Assert.That(cells, Is.Empty);
            openSides = true; grid.TryGetReachableDestinations(100, out cells); Assert.That(cells.Count, Is.EqualTo(3));
            var blocked = new CombatOccupancyGrid(world, 2, 2, (_, _) => true, (_, _) => false);
            blocked.TryPlace(100, Cell(0, 0));
            Assert.That(blocked.TryGetReachableDestinations(100, out cells), Is.True); Assert.That(cells, Is.Empty);
            Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(6));
        }

        [Test]
        public void MissingDeadRemovedAndDriftedActorsFailRatherThanReturnValidEmptyQuery()
        {
            var world = World(); var grid = Grid(world);
            Assert.That(grid.TryGetReachableDestinations(100, out var cells), Is.False); Assert.That(cells, Is.Empty);
            grid.TryPlace(100, Cell(0, 0));
            Assert.That(grid.TryGetReachableDestinations(999, out cells), Is.False);
            world.Get(100).Hp = 0; Assert.That(grid.TryGetReachableDestinations(100, out cells), Is.False);
            world.Get(100).Hp = 50; world.Get(100).X = 2;
            Assert.That(grid.TryGetReachableDestinations(100, out cells), Is.False);
            world.Get(100).X = 0; world.Get(100).LifeState = UnitLifeState.Removed;
            Assert.That(grid.TryGetReachableDestinations(100, out cells), Is.False);
        }

        [Test]
        public void SelectionQueryRequiresActiveLivingSelectionAndKeepsPendingTicket()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Reachable destinations check"); SceneManager.MoveGameObjectToScene(go, scene);
                var battle = go.AddComponent<BattleSceneRoot>(); NativeCombatStateFixture.Initialize(battle,6);
                Assert.That(battle.TryGetSelectedReachableDestinations(out _), Is.False);
                Assert.That(battle.TryConfigureGrid(4, 4, new[] { new CombatGridPlacement(100, Cell(0, 0)),
                    new CombatGridPlacement(800, Cell(3, 3)) }, (_, _) => true, (_, _) => true), Is.True);
                Assert.That(battle.TryGetSelectedReachableDestinations(out _), Is.False); battle.TrySelectUnit(100);
                battle.TryReserveMovementStep(100, Cell(1, 0), out var ticket); var events = 0;
                battle.MovementStepApplied += _ => events++;
                Assert.That(battle.TryGetSelectedReachableDestinations(out var cells), Is.True);
                Assert.That(cells.Count, Is.EqualTo(14)); Assert.That(events, Is.EqualTo(0));
                Assert.That(battle.PlayerAp, Is.EqualTo(6)); Assert.That(battle.SelectedUnitId, Is.EqualTo(100));
                Assert.That(battle.TryApplyReservedMovementStep(ticket), Is.True);
                battle.enabled = false; Assert.That(battle.TryGetSelectedReachableDestinations(out cells), Is.False);
                Assert.That(cells, Is.Empty); battle.enabled = true; battle.ApplyDeath(100, true);
                Assert.That(battle.TryGetSelectedReachableDestinations(out cells), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static CombatGridCell Cell(int x, int y) => new CombatGridCell(x, y);
        private static CombatOccupancyGrid Grid(CombatWorld world) =>
            new CombatOccupancyGrid(world, 4, 4, (_, _) => true, (_, _) => true, preventDiagonalCornerCutting: true);
        private static CombatWorld World()
        {
            var world = new CombatWorld();
            foreach (short id in new short[] { 100, 101, 800 })
                world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = id, Enemy = id == 800, Hp = 50, MaxHp = 50 }));
            return world;
        }
    }
}
