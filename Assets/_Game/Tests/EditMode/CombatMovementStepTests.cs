using System;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatMovementStepTests
    {
        [Test]
        public void ConfirmationCommitsOnlyReservedCoordinatesBeforePublishingWithoutSpendingAp()
        {
            WithBattle(battle => {
                Configure(battle); battle.TrySelectUnit(100); var calls = 0;
                battle.MovementStepApplied += step => {
                    calls++; Assert.That(step.UnitId, Is.EqualTo(100));
                    battle.TryGetUnit(100, out var unit); Assert.That(unit.X, Is.EqualTo(1)); Assert.That(unit.Y, Is.EqualTo(2));
                    Assert.That(battle.TryGetGridOccupant(Cell(1, 1), out _), Is.False);
                    Assert.That(battle.TryGetGridOccupant(Cell(1, 2), out var id), Is.True); Assert.That(id, Is.EqualTo(100));
                };
                Assert.That(battle.TryReserveMovementStep(100, Cell(1, 2), out var result), Is.True);
                Assert.That(battle.TryApplyReservedMovementStep(result), Is.True);
                Assert.That(battle.TryApplyReservedMovementStep(result), Is.False); Assert.That(calls, Is.EqualTo(1));
                Assert.That(battle.PlayerHp, Is.EqualTo(120)); Assert.That(battle.PlayerMp, Is.EqualTo(30));
                Assert.That(battle.PlayerAp, Is.EqualTo(6)); Assert.That(battle.PlayerExperience, Is.EqualTo(0));
                Assert.That(battle.SelectedUnitId, Is.EqualTo(100)); Assert.That(battle.LastAttackPresentation, Is.Null);
            });
        }

        [Test]
        public void WrongOriginDestinationOrActorPreservesThePendingReservation()
        {
            WithBattle(battle => {
                Configure(battle); battle.TryReserveMovementStep(100, Cell(1, 2), out var ticket);
                Assert.That(battle.TryApplyReservedMovementStep(new CombatMovementStep(100, Cell(0, 0), ticket.To, ticket.ReservationId)), Is.False);
                Assert.That(battle.TryApplyReservedMovementStep(new CombatMovementStep(100, ticket.From, Cell(2, 1), ticket.ReservationId)), Is.False);
                Assert.That(battle.TryApplyReservedMovementStep(new CombatMovementStep(800, ticket.From, ticket.To, ticket.ReservationId)), Is.False);
                Assert.That(battle.TryApplyReservedMovementStep(ticket), Is.True);
            });
        }

        [Test]
        public void DeathCancellationAndDisabledInputRejectLateConfirmations()
        {
            WithBattle(battle => {
                Configure(battle); battle.TryReserveMovementStep(100, Cell(1, 2), out var step);
                Assert.That(battle.CancelMovementStep(100), Is.True);
                Assert.That(battle.TryApplyReservedMovementStep(step), Is.False); Assert.That(battle.CancelMovementStep(100), Is.False);
                battle.TryReserveMovementStep(100, Cell(1, 2), out step); battle.enabled = false;
                Assert.That(battle.TryApplyReservedMovementStep(step), Is.False);
                Assert.That(battle.TryReserveMovementStep(100, Cell(2, 1), out _), Is.False);
                battle.enabled = true; battle.ApplyDeath(100, true);
                Assert.That(battle.TryApplyReservedMovementStep(step), Is.False);
                battle.ApplyDeath(100, false); Assert.That(battle.CancelMovementStep(100), Is.False);
                Assert.That(battle.PlayerAp, Is.EqualTo(6));
            });
        }

        [Test]
        public void CommitRechecksChangedTerrainAndNeverMovesAnUnconfiguredGrid()
        {
            WithBattle(battle => {
                Assert.That(battle.TryReserveMovementStep(100, Cell(1, 2), out var step), Is.False);
                Assert.That(battle.TryApplyReservedMovementStep(step), Is.False);
                var allowed = true; Configure(battle, (_, _) => allowed); battle.TryReserveMovementStep(100, Cell(1, 2), out step);
                allowed = false; Assert.That(battle.TryApplyReservedMovementStep(step), Is.False);
                battle.TryGetUnit(100, out var unit); Assert.That(unit.Y, Is.EqualTo(1));
                allowed = true; Assert.That(battle.TryApplyReservedMovementStep(step), Is.True);
            });
        }

        [Test]
        public void ReturningToSameOriginNeverMakesAnOldConfirmationCurrentAgain()
        {
            WithBattle(battle => {
                Configure(battle); battle.TryReserveMovementStep(100, Cell(1, 2), out var old);
                Assert.That(battle.TryApplyReservedMovementStep(old), Is.True);
                battle.TryReserveMovementStep(100, Cell(1, 1), out var back); battle.TryApplyReservedMovementStep(back);
                battle.TryReserveMovementStep(100, Cell(1, 2), out var fresh);
                Assert.That(fresh.ReservationId, Is.Not.EqualTo(old.ReservationId));
                Assert.That(battle.TryApplyReservedMovementStep(old), Is.False);
                Assert.That(battle.TryApplyReservedMovementStep(fresh), Is.True);
            });
        }

        [Test]
        public void HpZeroImmediatelyCancelsTicketAndRecoveryCannotReplayIt()
        {
            WithBattle(battle => {
                Configure(battle); battle.TrySelectUnit(100);
                Assert.That(battle.TryReserveMovementStep(100, Cell(2, 1), out var old), Is.True);
                Assert.That(battle.ApplyHp(100, -1), Is.False);
                Assert.That(battle.IsMovementInProgress(100), Is.True);
                Assert.That(battle.ApplyHp(100, 0), Is.True);
                Assert.That(battle.IsMovementInProgress(100), Is.False);
                Assert.That(battle.SelectedUnitId, Is.Null);
                Assert.That(battle.TryGetGridOccupant(Cell(1, 1), out var owner), Is.True);
                Assert.That(owner, Is.EqualTo(100));
                Assert.That(battle.ApplyHp(100, 120), Is.True);
                Assert.That(battle.TryApplyReservedMovementStep(old), Is.False);
                Assert.That(battle.TryReserveMovementStep(100, Cell(2, 1), out var fresh), Is.True);
                Assert.That(fresh.ReservationId, Is.Not.EqualTo(old.ReservationId));
                Assert.That(battle.TryApplyReservedMovementStep(fresh), Is.True);
                Assert.That(battle.PlayerAp, Is.EqualTo(6));
            });
        }

        [Test]
        public void DeathImmediatelyFreesPendingDestinationAndOriginWithoutApRefund()
        {
            foreach (var leaveCorpse in new[] { true, false })
                WithBattle(battle => {
                    Assert.That(battle.TryConfigureGrid(4, 4, new[] {
                        new CombatGridPlacement(100, Cell(1, 1)), new CombatGridPlacement(800, Cell(3, 1)) },
                        (_, _) => true, (from, to) => Math.Abs((long)to.X - from.X) + Math.Abs((long)to.Y - from.Y) == 1), Is.True);
                    battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                    Assert.That(battle.TryMoveTo(100, Cell(2, 1), out _), Is.True);
                    Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost));
                    Assert.That(battle.ApplyDeath(100, leaveCorpse), Is.True);
                    Assert.That(battle.IsMovementInProgress(100), Is.False);
                    Assert.That(battle.TryGetGridOccupant(Cell(1, 1), out _), Is.False);
                    Assert.That(battle.TryReserveMovementStep(800, Cell(2, 1), out var next), Is.True);
                    Assert.That(battle.TryApplyReservedMovementStep(next), Is.True);
                    Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost)); Assert.That(battle.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp));
                });
        }

        private static CombatGridCell Cell(int x, int y) => new CombatGridCell(x, y);
        private static void Configure(BattleSceneRoot battle, Func<CombatUnitSnapshot, CombatGridCell, bool> terrain = null)
        {
            Assert.That(battle.TryConfigureGrid(4, 4, new[] { new CombatGridPlacement(100, Cell(1, 1)),
                new CombatGridPlacement(800, Cell(3, 3)) }, terrain ?? ((_, _) => true),
                (from, to) => Math.Abs((long)to.X - from.X) + Math.Abs((long)to.Y - from.Y) == 1), Is.True);
        }
        private static void WithBattle(Action<BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try { var go = new GameObject("Native movement check"); SceneManager.MoveGameObjectToScene(go, scene);
                var battle = go.AddComponent<BattleSceneRoot>(); check(battle); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
