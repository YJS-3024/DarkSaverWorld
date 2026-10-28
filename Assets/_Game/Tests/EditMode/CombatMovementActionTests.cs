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
                Assert.That(battle.PlayerAp, Is.EqualTo(6)); NativeCombatStateFixture.ReceiveFullTicks(battle);
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
        public void MovementReceiptDoesNotInventAPaymentBeforeIndependentTickUpdate()
        {
            WithBattle(battle=>{
                Configure(battle);NativeCombatStateFixture.ReceiveFullTicks(battle);
                Assert.IsFalse(battle.TryMoveTo(100,Cell(1,2),out _));Assert.AreEqual(20,battle.PlayerAp);
                Assert.IsTrue(battle.TryReserveMovementStep(100,Cell(1,2),out var ticket));
                Assert.AreEqual(20,battle.PlayerAp);Assert.IsTrue(battle.TryApplyReservedMovementStep(ticket));
                battle.TryGetUnit(100,out var unit);Assert.AreEqual(2,unit.Y);Assert.AreEqual(20,unit.Ap);
            });
        }

        [Test]
        public void ReservedReceiptBlocksConcurrentRequestsAndCancellationNeverInventsRefund()
        {
            WithBattle(battle=>{
                Configure(battle);battle.ConfigureActionPointCosts(2,6);NativeCombatStateFixture.ReceiveFullTicks(battle);
                Assert.IsTrue(battle.TryReserveMovementStep(100,Cell(1,2),out var ticket));
                Assert.IsFalse(battle.TryReserveMovementStep(100,Cell(2,1),out _));
                Assert.IsFalse(battle.CanStartAttackRequest(800,100,out _));
                Assert.IsFalse(battle.TryStartMagicRequest(101,100,new short[]{800},out _));
                Assert.IsFalse(battle.AdvanceActionPoints(3));Assert.AreEqual(20,battle.PlayerAp);
                Assert.IsTrue(battle.TryApplyReservedMovementStep(ticket));
                Assert.IsTrue(battle.TryReserveMovementStep(100,Cell(2,2),out ticket));
                Assert.IsTrue(battle.CancelMovementStep(100));Assert.AreEqual(20,battle.PlayerAp);
                Assert.IsFalse(battle.TryApplyReservedMovementStep(ticket));
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
                world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = id, Enemy = id == 800, Hp = 50, MaxHp = 50 }));
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
                var receivedBattle=go.AddComponent<BattleSceneRoot>();NativeCombatStateFixture.Initialize(receivedBattle,6);check(receivedBattle); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
