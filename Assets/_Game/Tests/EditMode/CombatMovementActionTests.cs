using System;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatMovementActionTests
    {
        [Test]
        public void InvalidCostReadinessDestinationOrTerrainNeverSpendsAp()
        {
            WithBattle(battle => {
                Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), 2, out _, out _), Is.False);
                var allowed = true; Configure(battle, (_, _) => allowed);
                Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), 2, out _, out _), Is.False);
                Assert.That(battle.PlayerAp, Is.EqualTo(6)); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                foreach (var cost in new[] { -1, 0, GameConstants.BattleMaximumAp + 1 })
                    Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), cost, out var bad, out _), Is.False);
                foreach (var cell in new[] { Cell(-1, 0), Cell(1, 1), Cell(3, 3), Cell(1, 3) })
                    Assert.That(battle.TryStartMovementAction(100, cell, 2, out _, out _), Is.False);
                allowed = false; Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), 2, out _, out _), Is.False);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Assert.That(battle.PlayerHp, Is.EqualTo(120));
                allowed = true; battle.enabled = false;
                Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), 2, out _, out _), Is.False);
                battle.enabled = true; battle.ApplyDeath(100, true);
                Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), 2, out _, out _), Is.False);
            });
        }

        [Test]
        public void ApprovedActionSpendsOnceButCoordinatesWaitForIndependentConfirmation()
        {
            WithBattle(battle => {
                Configure(battle); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); var events = 0;
                battle.MovementStepApplied += _ => { events++; Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 2)); };
                Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), 2, out var ticket, out var reason), Is.True);
                Assert.That(reason, Is.Empty); Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 2));
                battle.TryGetUnit(100, out var before); Assert.That(before.Y, Is.EqualTo(1)); Assert.That(events, Is.EqualTo(0));
                Assert.That(battle.TryStartMovementAction(100, Cell(2, 1), 2, out _, out _), Is.False);
                Assert.That(battle.TryApplyReservedMovementStep(ticket), Is.True);
                Assert.That(battle.TryApplyReservedMovementStep(ticket), Is.False); Assert.That(events, Is.EqualTo(1));
                battle.TryGetUnit(100, out var after); Assert.That(after.Y, Is.EqualTo(2));
                Assert.That(battle.PlayerMp, Is.EqualTo(30)); Assert.That(battle.PlayerExperience, Is.EqualTo(0));
            });
        }

        [Test]
        public void PendingMovementBlocksOtherStartsEvenAfterApRecoveryAndCancelDoesNotInventRefund()
        {
            WithBattle(battle => {
                Configure(battle); battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                Assert.That(battle.TryStartMovementAction(100, Cell(1, 2), 3, out var ticket, out _), Is.True);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3)); battle.AdvanceActionPoints(3);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                Assert.That(battle.TryStartMovementAction(100, Cell(2, 1), 3, out _, out _), Is.False);
                Assert.That(battle.CanStartAttackRequest(800, 100, out _), Is.False);
                Assert.That(battle.TryStartAttackRequest(800, 100, out _), Is.False);
                Assert.That(battle.CanStartMagicRequest(101, 100, new short[] { 800 }, out _), Is.False);
                Assert.That(battle.TryStartMagicRequest(101, 100, new short[] { 800 }, out _), Is.False);
                Assert.That(battle.TryApplyReservedMovementStep(ticket), Is.True);
                Assert.That(battle.TryStartMovementAction(100, Cell(2, 2), 3, out ticket, out _), Is.True);
                Assert.That(battle.CancelMovementStep(100), Is.True); Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
                Assert.That(battle.TryApplyReservedMovementStep(ticket), Is.False);
                Assert.That(battle.TryStartMovementAction(800, Cell(3, 2), 4, out _, out _), Is.True);
                Assert.That(battle.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp - 4)); Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
            });
        }

        [Test]
        public void CornerRuleIsSharedByPathReservationAndCommitRecheck()
        {
            var world = World(); var sideAllowed = true;
            var grid = new CombatOccupancyGrid(world, 2, 2,
                (_, cell) => sideAllowed || !cell.Equals(Cell(1, 0)), (_, _) => true,
                preventDiagonalCornerCutting: true);
            grid.TryPlace(100, Cell(0, 0));
            Assert.That(grid.TryFindPath(100, Cell(1, 1), out var path), Is.True); Assert.That(path.Count, Is.EqualTo(1));
            Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.True);
            sideAllowed = false;
            Assert.That(grid.TryCommitStep(100), Is.False); Assert.That(world.Get(100).X, Is.EqualTo(0));
            Assert.That(grid.TryGetReservation(Cell(1, 1), out _), Is.True);
            Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.False);
            Assert.That(grid.TryFindPath(100, Cell(1, 1), out path), Is.True); Assert.That(path.Count, Is.EqualTo(2));
            sideAllowed = true; Assert.That(grid.TryCommitStep(100), Is.True);
        }

        [Test]
        public void BothBlockedCornersHaveNoRouteAndSideEnemyOrReservationBlocksDiagonal()
        {
            var world = World();
            var blocked = new CombatOccupancyGrid(world, 2, 2,
                (_, c) => c.X == c.Y, (_, _) => true, preventDiagonalCornerCutting: true);
            blocked.TryPlace(100, Cell(0, 0));
            Assert.That(blocked.TryFindPath(100, Cell(1, 1), out _), Is.False);
            var grid = new CombatOccupancyGrid(world, 3, 3, (_, _) => true, (_, _) => true,
                preventDiagonalCornerCutting: true);
            grid.TryPlace(100, Cell(0, 0)); grid.TryPlace(800, Cell(2, 0));
            grid.TryReserveStep(800, Cell(1, 0));
            Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.False);
            grid.TryCommitStep(800); Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.False);
            world.Get(800).LifeState = UnitLifeState.Corpse;
            Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.False);
            grid.ReleaseUnit(800); Assert.That(grid.TryReserveStep(100, Cell(1, 1)), Is.True);
        }

        private static CombatGridCell Cell(int x, int y) => new CombatGridCell(x, y);
        private static CombatWorld World()
        {
            var world = new CombatWorld();
            foreach (short id in new short[] { 100, 800 })
                world.Add(new CombatUnit { Id = id, Enemy = id == 800, Hp = 50, MaxHp = 50 });
            return world;
        }
        private static void Configure(BattleSceneRoot battle, Func<CombatUnitSnapshot, CombatGridCell, bool> terrain = null) =>
            Assert.That(battle.TryConfigureGrid(4, 4, new[] { new CombatGridPlacement(100, Cell(1, 1)),
                new CombatGridPlacement(800, Cell(3, 3)) }, terrain ?? ((_, _) => true), (_, _) => true,
                preventDiagonalCornerCutting: true), Is.True);
        private static void WithBattle(Action<BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try { var go = new GameObject("Movement action approval check"); SceneManager.MoveGameObjectToScene(go, scene);
                check(go.AddComponent<BattleSceneRoot>()); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
