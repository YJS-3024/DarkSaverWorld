using System;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatMovementRouteTests
    {
        [Test]
        public void ExplicitBudgetStartsOnlyApprovedPathAndQueryOrFailedApNeverChangesTieOrder()
        {
            WithBattle(b => {
                Assert.That(b.TryConfigureGrid(4, 4, new[] { new CombatGridPlacement(100, Cell(0, 0)),
                    new CombatGridPlacement(800, Cell(3, 3)) }, (_, _) => true,
                    (a, c) => Math.Abs(a.X - c.X) + Math.Abs(a.Y - c.Y) == 1,
                    movementCost: (_, _) => 1), Is.True);
                Assert.That(b.ConfigureMovementBudget(100, 2), Is.True); b.TrySelectUnit(100);
                b.TryFindSelectedMovementPath(Cell(1, 0), out _); // Odd reverse query must not change shared tie state.
                Assert.That(b.TryMoveTo(100, Cell(1, 0), out _), Is.False); // AP6, likewise unchanged.
                b.TryFindSelectedMovementPath(Cell(1, 1), out var path); Assert.That(path[0], Is.EqualTo(Cell(1, 0)));
                Assert.That(b.TryGetSelectedReachableDestinations(out var cells), Is.True);
                CollectionAssert.DoesNotContain(cells, Cell(3, 0));
                b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); Assert.That(b.TryMoveTo(100, Cell(3, 0), out _), Is.False);
                Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Assert.That(b.TryMoveTo(100, Cell(1, 0), out _), Is.True);
                Assert.That(b.ConfigureMovementBudget(100, 99), Is.False); Ticks(b, 2);
                Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost));
                b.TryFindSelectedMovementPath(Cell(2, 1), out path); Assert.That(path[0], Is.EqualTo(Cell(1, 1)));
                Assert.That(b.ConfigureMovementBudget(100, -1), Is.False);
                Assert.That(b.TryFindSelectedMovementPath(Cell(3, 1), out _), Is.False);
            });
        }

        [Test]
        public void ExplicitBudgetWithoutTerrainCostsFailsWithoutSpendingOrBfsFallback()
        {
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); Assert.That(b.ConfigureMovementBudget(100, 2), Is.True);
                Assert.That(b.TryMoveTo(100, Cell(1, 0), out _), Is.False); Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                Assert.That(b.ConfigureMovementBudget(999, 2), Is.False);
                Assert.That(b.ConfigureMovementBudget(100, int.MaxValue), Is.False);
                b.enabled = false; Assert.That(b.ConfigureMovementBudget(100, 2), Is.False);
            });
        }

        [Test]
        public void AllyLongRouteSpendsFourOnceAndConfirmsEveryCell()
        {
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); var steps = 0;
                b.MovementStepApplied += s => { steps++; Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost));
                    Assert.That(s.To, Is.EqualTo(Cell(steps, 0))); };
                Assert.That(b.TryMoveTo(100, Cell(3, 0), out _), Is.True);
                Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost)); b.TryGetUnit(100, out var u); Assert.That(u.X, Is.EqualTo(0));
                Ticks(b, 6);
                b.TryGetUnit(100, out u); Assert.That(u.X, Is.EqualTo(3)); Assert.That(steps, Is.EqualTo(3));
                Assert.That(b.IsMovementInProgress(100), Is.False); Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost));
                Assert.That(b.PlayerHp, Is.EqualTo(120)); Assert.That(b.PlayerMp, Is.EqualTo(30));
                Assert.That(b.AdvanceMovement(10), Is.True); Assert.That(steps, Is.EqualTo(3));
            });
        }

        [Test]
        public void EnemyRouteSpendsSixIndependentlyOfAlly()
        {
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                Assert.That(b.TryMoveTo(800, Cell(1, 3), out _), Is.True); Assert.That(b.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp - GameConstants.BattleEnemyMoveApCost));
                Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Ticks(b, 4);
                b.TryGetUnit(800, out var u); Assert.That(u.X, Is.EqualTo(1)); Assert.That(b.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp - GameConstants.BattleEnemyMoveApCost));
            });
        }

        [Test]
        public void InvalidOrUnreadyDestinationsNeverSpendAndRecoveryDoesNotUnlockActiveRoute()
        {
            WithBattle(b => {
                Configure(b); Assert.That(b.TryMoveTo(100, Cell(3, 0), out _), Is.False);
                Assert.That(b.PlayerAp, Is.EqualTo(6)); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                foreach (var goal in new[] { Cell(-1, 0), Cell(0, 0), Cell(3, 3) })
                    Assert.That(b.TryMoveTo(100, goal, out _), Is.False);
                Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Assert.That(b.TryMoveTo(100, Cell(3, 0), out _), Is.True);
                b.AdvanceActionPoints(GameConstants.BattleAllyMoveApCost); Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                Assert.That(b.TryMoveTo(100, Cell(2, 2), out _), Is.False);
                Assert.That(b.TryStartAttackRequest(800, 100, out _), Is.False);
                Assert.That(b.AdvanceMovement(float.NaN), Is.False); Assert.That(b.AdvanceMovement(-1), Is.False);
                Assert.That(b.IsMovementInProgress(100), Is.True);
            });
        }

        [Test]
        public void ChangedTerrainStopsAtLastConfirmedCellWithoutFurtherCharge()
        {
            WithBattle(b => {
                var blocked = false; Configure(b, (_, c) => !blocked || !c.Equals(Cell(2, 0)));
                b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); b.TryMoveTo(100, Cell(3, 0), out _);
                Ticks(b, 2); blocked = true;
                Ticks(b, 2);
                b.TryGetUnit(100, out var u); Assert.That(u.X, Is.EqualTo(1));
                Assert.That(b.IsMovementInProgress(100), Is.False); Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost));
            });
        }

        [Test]
        public void CancelDisableDeathAndReentrantCancelNeverContinueOldRoute()
        {
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); b.TryMoveTo(100, Cell(3, 0), out _);
                b.MovementStepApplied += _ => b.CancelMovementStep(100);
                Ticks(b, 6); b.TryGetUnit(100, out var u); Assert.That(u.X, Is.EqualTo(1));
                Assert.That(b.IsMovementInProgress(100), Is.False); Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost));
                b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); b.TryMoveTo(100, Cell(3, 0), out _); b.enabled = false;
                Assert.That(b.AdvanceMovement(0), Is.False); // Preview scenes do not execute normal runtime callbacks.
                Assert.That(b.IsMovementInProgress(100), Is.False); b.enabled = true;
                b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); b.TryMoveTo(100, Cell(3, 0), out _); b.ApplyDeath(100, true);
                b.AdvanceMovement(10); b.TryGetUnit(100, out u); Assert.That(u.X, Is.EqualTo(1));
                Assert.That(b.IsMovementInProgress(100), Is.False);
            });
        }

        [Test]
        public void MonsterReturnKeepsInitialPlacementAndSpendsOneMovementAction()
        {
            WithBattle(b => {
                Assert.That(b.TryGetInitialGridPosition(800, out _), Is.False);
                Configure(b);
                var hpBeforeReturn = b.EnemyHp;
                Assert.That(b.TryGetInitialGridPosition(800, out var initial), Is.True);
                Assert.That(initial, Is.EqualTo(Cell(3, 3)));
                Assert.That(b.TryReturnMonsterToInitialPosition(800, out _), Is.False);
                Assert.That(b.TryReturnMonsterToInitialPosition(100, out _), Is.False);
                b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                Assert.That(b.TryMoveTo(800, Cell(1, 3), out _), Is.True); Ticks(b, 4);
                Assert.That(b.TryGetInitialGridPosition(800, out initial), Is.True);
                Assert.That(initial, Is.EqualTo(Cell(3, 3)));
                Assert.That(b.TryReturnMonsterToInitialPosition(800, out _), Is.False); // AP14, not ready.
                b.AdvanceActionPoints(GameConstants.BattleEnemyMoveApCost);
                Assert.That(b.TryReturnMonsterToInitialPosition(800, out _), Is.True);
                Assert.That(b.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp - GameConstants.BattleEnemyMoveApCost));
                Assert.That(b.TryReturnMonsterToInitialPosition(800, out _), Is.False); // Already moving.
                Ticks(b, 4); b.TryGetUnit(800, out var unit);
                Assert.That((unit.X, unit.Y), Is.EqualTo((3, 3)));
                Assert.That(b.TryReturnMonsterToInitialPosition(800, out _), Is.False);
                Assert.That(b.EnemyHp, Is.EqualTo(hpBeforeReturn)); // Return does not invent rest recovery.
                Assert.That(b.ApplyDeath(800, false), Is.True);
                Assert.That(b.TryGetInitialGridPosition(800, out _), Is.False);
            });
        }

        [Test]
        public void FailedPlacementOrBlockedReturnCannotCommitOriginOrSpendAp()
        {
            WithBattle(b => {
                Assert.That(b.TryConfigureGrid(4, 4, new[] {
                    new CombatGridPlacement(100, Cell(0, 0)),
                    new CombatGridPlacement(800, Cell(0, 0)) }, (_, _) => true, (_, _) => true), Is.False);
                Assert.That(b.TryGetInitialGridPosition(800, out _), Is.False);
                var blocked = false;
                Configure(b, (_, c) => !blocked || !c.Equals(Cell(3, 3)));
                b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                Assert.That(b.TryMoveTo(800, Cell(1, 3), out _), Is.True); Ticks(b, 4);
                b.AdvanceActionPoints(GameConstants.BattleEnemyMoveApCost); blocked = true;
                Assert.That(b.TryReturnMonsterToInitialPosition(800, out _), Is.False);
                Assert.That(b.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp));
                Assert.That(b.IsMovementInProgress(800), Is.False);
                Assert.That(b.TryGetInitialGridPosition(800, out var initial), Is.True);
                Assert.That(initial, Is.EqualTo(Cell(3, 3)));
            });
        }

        [Test]
        public void TroopOrdersKeepCommanderManualAndDefenseStationaryThenAttackApproaches()
        {
            WithBattle(b => {
                Assert.IsTrue(b.TryRegisterGroup(new[] { new CombatUnit { Id=101, Name="Soldier", Hp=80, MaxHp=80, Mp=0, MaxMp=0, Attack=20, Defense=2 } }));
                Assert.IsTrue(b.TryConfigureGrid(8,8,new[] {
                    new CombatGridPlacement(100,Cell(0,0)),new CombatGridPlacement(101,Cell(1,1)),
                    new CombatGridPlacement(800,Cell(5,1)) },(_,_)=>true,
                    (a,c)=>Math.Abs(a.X-c.X)+Math.Abs(a.Y-c.Y)==1));
                b.SetTemporaryLocalRules(true); b.AdvanceActionPoints(20);
                Assert.IsTrue(b.TryGetTroopOrder(101,out var order));Assert.AreEqual(BattleSceneRoot.TroopOrder.Direct,order);
                Assert.IsFalse(b.TrySetTroopOrder(800,BattleSceneRoot.TroopOrder.AutomaticAttack));
                Assert.IsFalse(b.TrySetTroopOrder(100,(BattleSceneRoot.TroopOrder)99));
                Assert.IsTrue(b.TrySetTroopOrder(100,BattleSceneRoot.TroopOrder.AutomaticDefense));
                b.AdvanceTemporaryMonsterAi(.2f);
                Assert.IsFalse(b.IsMovementInProgress(101));Assert.IsFalse(b.IsMovementInProgress(100));
                Assert.IsTrue(b.TrySetTroopOrder(101,BattleSceneRoot.TroopOrder.AutomaticAttack));
                b.AdvanceTemporaryMonsterAi(.2f);
                Assert.IsTrue(b.IsMovementInProgress(101));Assert.IsFalse(b.IsMovementInProgress(100));
                b.TryGetUnit(101,out var before);int ap=before.Ap;
                Assert.IsTrue(b.TrySetTroopOrder(100,BattleSceneRoot.TroopOrder.Direct));
                Assert.IsFalse(b.IsMovementInProgress(101)); b.TryGetUnit(101,out var after);Assert.AreEqual(ap,after.Ap);
                Assert.AreEqual(1,after.X);Assert.AreEqual(1,after.Y);
                b.SetTemporaryLocalRules(false);Assert.IsFalse(b.TrySetTroopOrder(100,BattleSceneRoot.TroopOrder.AutomaticAttack));
            });
        }
        [Test]
        public void AutomaticDefenseAttacksAdjacentEnemyWithoutMovingOrSpendingCommanderAp()
        {
            WithBattle(b => {
                Assert.IsTrue(b.TryRegisterGroup(new[] { new CombatUnit { Id=101, Name="Soldier", Hp=80, MaxHp=80, Mp=0, MaxMp=0, Attack=30, Defense=2 } }));
                Assert.IsTrue(b.TryConfigureGrid(6,6,new[] {
                    new CombatGridPlacement(100,Cell(0,0)),new CombatGridPlacement(101,Cell(2,2)),
                    new CombatGridPlacement(800,Cell(3,2)) },(_,_)=>true,(_,_)=>true));
                b.SetTemporaryLocalRules(true);b.ConfigurePhysicalAccuracy(101,100,0);b.ConfigurePhysicalAccuracy(800,100,0);b.AdvanceActionPoints(20);
                int hp=b.EnemyHp,ap=b.PlayerAp;
                Assert.IsTrue(b.TrySetTroopOrder(100,BattleSceneRoot.TroopOrder.AutomaticDefense));
                b.AdvanceTemporaryMonsterAi(.2f);
                Assert.Less(b.EnemyHp,hp);Assert.AreEqual(ap,b.PlayerAp);Assert.IsFalse(b.IsMovementInProgress(101));
                Assert.IsTrue(b.TryGetLatestAttacker(800,out var attacker));Assert.AreEqual(101,attacker);
            });
        }

        [Test]
        public void CommanderDeathCancelsOnlyItsAutomaticSoldierMovement()
        {
            WithBattle(b => {
                Assert.IsTrue(b.TryRegisterGroup(new[] {
                    new CombatUnit {Id=200,Name="Commander",Hp=50,MaxHp=50,Mp=0,MaxMp=0},
                    new CombatUnit {Id=201,Name="Soldier",Hp=50,MaxHp=50,Mp=0,MaxMp=0} }));
                Assert.IsTrue(b.TryConfigureGrid(8,8,new[] {
                    new CombatGridPlacement(100,Cell(0,0)),new CombatGridPlacement(200,Cell(0,1)),
                    new CombatGridPlacement(201,Cell(1,1)),new CombatGridPlacement(800,Cell(5,1)) },(_,_)=>true,
                    (a,c)=>Math.Abs(a.X-c.X)+Math.Abs(a.Y-c.Y)==1));
                b.SetTemporaryLocalRules(true);b.AdvanceActionPoints(20);
                Assert.IsTrue(b.TrySetTroopOrder(200,BattleSceneRoot.TroopOrder.AutomaticAttack));
                b.AdvanceTemporaryMonsterAi(.2f);Assert.IsTrue(b.IsMovementInProgress(201));
                Assert.IsTrue(b.TryGetTroopOrder(100,out var primary));Assert.AreEqual(BattleSceneRoot.TroopOrder.Direct,primary);
                Assert.IsTrue(b.ApplyHp(200,0));Assert.IsFalse(b.TryGetTroopOrder(201,out _));
                b.AdvanceTemporaryMonsterAi(.2f);Assert.IsFalse(b.IsMovementInProgress(201));
                Assert.IsFalse(b.TrySetTroopOrder(201,BattleSceneRoot.TroopOrder.AutomaticDefense));
            });
        }

        private static CombatGridCell Cell(int x, int y) => new CombatGridCell(x, y);
        [Test]
        public void TemporaryMonsterAiActuallyApproachesAndAttacksOnRecoveredAp()
        {
            WithBattle(b => {
                Configure(b); b.SetTemporaryLocalRules(true);
                b.ConfigurePhysicalAccuracy(100, 100, 0); b.ConfigurePhysicalAccuracy(800, 100, 0);
                var before = b.PlayerHp;
                b.AdvanceActionPoints(4);
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.False); // Enemy AP10, not ready20.
                b.AdvanceActionPoints(10);
                for (var i = 0; i < 4; i++)
                {
                    Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                    Ticks(b, 8); b.AdvanceActionPoints(6);
                }
                Assert.That(b.PlayerHp, Is.LessThan(before));
                Assert.That(b.TryGetLatestAttacker(100, out var attacker), Is.True);
                Assert.That(attacker, Is.EqualTo(800));
                b.SetTemporaryLocalRules(false);
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.False);
            });
        }

        [Test]
        public void MonsterWithoutAttackHistoryDoesNotTreatUnitZeroAsItsAttacker()
        {
            WithBattle(b => {
                Assert.That(b.TryRegisterGroup(new[] { new CombatUnit { Id = 0, Name = "Distant ally",
                    Enemy = false, Hp = 100, MaxHp = 100, Mp = 0, MaxMp = 0, Attack = 1, Defense = 0 } }), Is.True);
                Assert.That(b.TryConfigureGrid(8, 8, new[] {
                    new CombatGridPlacement(100, Cell(3, 2)), new CombatGridPlacement(800, Cell(3, 3)),
                    new CombatGridPlacement(0, Cell(0, 3)) }, (_, _) => true,
                    (a, c) => Math.Abs(a.X-c.X) + Math.Abs(a.Y-c.Y) == 1), Is.True);
                b.SetTemporaryLocalRules(true); b.ConfigurePhysicalAccuracy(800, 100, 0);
                b.ConfigurePhysicalAccuracy(100, 100, 0); b.AdvanceActionPoints(14);
                Assert.That(b.TryGetLatestAttacker(800, out _), Is.False);
                int hp = b.PlayerHp;
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.False);
                Assert.That(b.PlayerHp, Is.LessThan(hp));
                Assert.That(b.TryGetLatestAttacker(100, out var attacker), Is.True);
                Assert.That(attacker, Is.EqualTo(800));
                Assert.That(b.TryGetUnit(0, out var distant), Is.True);
                Assert.That(distant.Hp, Is.EqualTo(100));
                // Zero is a valid attacker ID when an accepted notification actually names it.
                Assert.That(b.ApplyAttackPresentation(800, 0, 0), Is.True);
                b.AdvanceActionPoints(GameConstants.BattleEnemyMaximumAp);
                int afterFirstAttack = b.PlayerHp;
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.True);
                Assert.That(b.PlayerHp, Is.EqualTo(afterFirstAttack));
                Assert.That(b.TryGetLatestAttacker(800, out var actualAttacker), Is.True);
                Assert.That(actualAttacker, Is.Zero);
            });
        }

        [Test]
        public void EscapingTargetIsReplacedByAnApproachableEnemyInsideSpawnRange()
        {
            WithBattle(b => {
                Assert.That(b.TryRegisterGroup(new[] { new CombatUnit { Id = 0, Name = "Replacement target",
                    Enemy = false, Hp = 100, MaxHp = 100, Mp = 0, MaxMp = 0, Attack = 1, Defense = 0 } }), Is.True);
                Assert.That(b.TryConfigureGrid(22, 22, new[] {
                    new CombatGridPlacement(100, Cell(16, 10)), new CombatGridPlacement(800, Cell(10, 10)),
                    new CombatGridPlacement(0, Cell(17, 11)) }, (_, _) => true,
                    (a, c) => Math.Abs(a.X-c.X) + Math.Abs(a.Y-c.Y) == 1), Is.True);
                b.SetTemporaryLocalRules(true);
                b.ConfigurePhysicalAccuracy(800, 100, 0); b.ConfigurePhysicalAccuracy(0, 100, 0);
                b.AdvanceActionPoints(14);
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.True);
                Assert.That(b.TryMoveTo(100, Cell(17, 10), out _), Is.True);
                Assert.That(b.TryMoveTo(0, Cell(16, 11), out _), Is.True);
                Ticks(b, 2);
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.False); // Old route cancelled; AP is still14.
                int playerHp = b.PlayerHp;
                for (int i = 0; i < 5; i++) {
                    b.AdvanceActionPoints(GameConstants.BattleEnemyMoveApCost);
                    Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                    Ticks(b, 8);
                }
                Assert.That(b.PlayerHp, Is.EqualTo(playerHp));
                Assert.That(b.TryGetUnit(0, out var replacement), Is.True);
                Assert.That(replacement.Hp, Is.LessThan(100));
                Assert.That(b.TryGetLatestAttacker(0, out var attacker), Is.True);
                Assert.That(attacker, Is.EqualTo(800));
                Assert.That(b.TryGetUnit(800, out var monster), Is.True);
                Assert.That(Cell(monster.X, monster.Y), Is.Not.EqualTo(Cell(10, 10)));
            });
        }

        [Test]
        public void PursuitStopsWhenTargetLeavesSpawnRangeAndReturnWaitsForFullAp()
        {
            WithBattle(b => {
                Assert.That(b.TryConfigureGrid(22, 22, new[] {
                    new CombatGridPlacement(100, Cell(16, 10)), new CombatGridPlacement(800, Cell(10, 10)) },
                    (_, _) => true, (a, c) => Math.Abs(a.X-c.X) + Math.Abs(a.Y-c.Y) == 1), Is.True);
                b.SetTemporaryLocalRules(true); b.ApplyHp(800, 10); b.AdvanceActionPoints(14);
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.True);
                Assert.That(b.EnemyAp, Is.EqualTo(20 - GameConstants.BattleEnemyMoveApCost));
                Assert.That(b.TryMoveTo(100, Cell(17, 10), out _), Is.True);
                Ticks(b, 2); // Ally crosses the spawn-centred boundary while pursuit is still active.
                Assert.That(b.TryGetUnit(100, out var ally), Is.True); Assert.That(ally.X, Is.EqualTo(17));
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.False);
                Assert.That(b.TryGetUnit(800, out var stopped), Is.True);
                var stoppedCell = Cell(stopped.X, stopped.Y);
                Assert.That(stoppedCell, Is.Not.EqualTo(Cell(10, 10)));
                Ticks(b, 8); b.TryGetUnit(800, out var unchanged);
                Assert.That(Cell(unchanged.X, unchanged.Y), Is.EqualTo(stoppedCell));
                Assert.That(b.EnemyHp, Is.EqualTo(10));
                b.AdvanceActionPoints(GameConstants.BattleEnemyMoveApCost);
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.IsMovementInProgress(800), Is.True);
                Assert.That(b.EnemyHp, Is.EqualTo(10));
                Ticks(b, 8); b.TryGetUnit(800, out var returned);
                Assert.That(Cell(returned.X, returned.Y), Is.EqualTo(Cell(10, 10)));
                Assert.That(b.EnemyHp, Is.EqualTo(10));
                b.AdvanceActionPoints(GameConstants.BattleEnemyMoveApCost);
                Assert.That(b.AdvanceTemporaryMonsterAi(.2f), Is.True);
                Assert.That(b.EnemyHp, Is.GreaterThan(10));
            });
        }

        [Test]
        public void TemporaryMonsterAiReturnsThenRestsWithoutHealingDuringMovement()
        {
            WithBattle(b => {
                Assert.That(b.TryConfigureGrid(20, 20, new[] {new CombatGridPlacement(100, Cell(0, 0)),
                    new CombatGridPlacement(800, Cell(10, 10))}, (_, _) => true,
                    (a, c) => Math.Abs(a.X - c.X) + Math.Abs(a.Y - c.Y) == 1), Is.True);
                b.SetTemporaryLocalRules(true); b.ApplyHp(800, 10); b.AdvanceActionPoints(14);
                Assert.That(b.TryMoveTo(800, Cell(12, 10), out _), Is.True); Ticks(b, 4);
                b.AdvanceActionPoints(6); b.AdvanceTemporaryMonsterAi(.2f);
                Assert.That(b.IsMovementInProgress(800), Is.True); Assert.That(b.EnemyHp, Is.EqualTo(10));
                Ticks(b, 4); b.TryGetUnit(800, out var monster);
                Assert.That((monster.X, monster.Y), Is.EqualTo((10, 10)));
                b.AdvanceTemporaryMonsterAi(.2f); Assert.That(b.EnemyHp, Is.EqualTo(10));
                b.AdvanceActionPoints(6); b.AdvanceTemporaryMonsterAi(.2f);
                Assert.That(b.EnemyHp, Is.EqualTo(25)); // ceil(75 * temporary 20%).
                Assert.That(b.EnemyAp, Is.EqualTo(20 - GameConstants.BattleTemporaryRestApCost));
            });
        }

        private static void Ticks(BattleSceneRoot b, int count)
        { for (var i = 0; i < count; i++) b.AdvanceMovement(GameConstants.BattleMoveTickSeconds); }

        [Test]
        public void SharedClockKeepsIdlePhaseAndAdvancesBothActorsOncePerCall()
        {
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
                b.AdvanceMovement(GameConstants.BattleMoveTickSeconds / 2);
                b.TryMoveTo(100, Cell(3, 0), out _); b.TryMoveTo(800, Cell(1, 3), out _);
                b.AdvanceMovement(GameConstants.BattleMoveTickSeconds / 2);
                b.TryGetMovementPresentation(100, out _, out var a);
                b.TryGetMovementPresentation(800, out _, out var e);
                Assert.That(a, Is.EqualTo(.5f)); Assert.That(e, Is.EqualTo(.5f));
                b.AdvanceMovement(.24f); // Only ONE phase, not three catch-up ticks.
                b.TryGetUnit(100, out var ally); b.TryGetUnit(800, out var enemy);
                Assert.That(ally.X, Is.EqualTo(1)); Assert.That(enemy.X, Is.EqualTo(2));
                b.AdvanceMovement(0); // Remaining short debt advances on the next call.
                b.TryGetMovementPresentation(100, out _, out a); Assert.That(a, Is.EqualTo(.5f));
                Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost)); Assert.That(b.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp - GameConstants.BattleEnemyMoveApCost));
            });
        }

        [Test]
        public void LagExactlyFiveHundredMillisecondsKeepsDebtButLongerStallDropsIt()
        {
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); b.TryMoveTo(100, Cell(3, 0), out _);
                b.AdvanceMovement(.5f); b.TryGetUnit(100, out var unit); Assert.That(unit.X, Is.Zero);
                b.AdvanceMovement(0); b.TryGetUnit(100, out unit); Assert.That(unit.X, Is.EqualTo(1));
            });
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); b.TryMoveTo(100, Cell(3, 0), out _);
                b.AdvanceMovement(.501f); b.TryGetMovementPresentation(100, out _, out var offset);
                Assert.That(offset, Is.EqualTo(.5f)); b.AdvanceMovement(0);
                b.TryGetUnit(100, out var unit); Assert.That(unit.X, Is.Zero);
                b.AdvanceMovement(GameConstants.BattleMoveTickSeconds / 2);
                b.TryGetUnit(100, out unit); Assert.That(unit.X, Is.Zero);
                b.AdvanceMovement(GameConstants.BattleMoveTickSeconds / 2);
                b.TryGetUnit(100, out unit); Assert.That(unit.X, Is.EqualTo(1));
            });
        }

        [Test]
        public void OriginalCardinalTicksShowHalfCellBeforeConfirmation()
        {
            WithBattle(b => {
                Configure(b); b.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); b.TryMoveTo(100, Cell(1, 0), out _);
                b.AdvanceMovement(GameConstants.BattleMoveTickSeconds / 2);
                Assert.That(b.TryGetMovementPresentation(100, out _, out var offset), Is.True);
                Assert.That(offset, Is.Zero);
                b.AdvanceMovement(GameConstants.BattleMoveTickSeconds / 2);
                b.TryGetMovementPresentation(100, out _, out offset); Assert.That(offset, Is.EqualTo(.5f));
                b.TryGetUnit(100, out var unit); Assert.That(unit.X, Is.Zero);
                b.AdvanceMovement(GameConstants.BattleMoveTickSeconds);
                b.TryGetUnit(100, out unit); Assert.That(unit.X, Is.EqualTo(1));
                Assert.That(b.TryGetMovementPresentation(100, out _, out _), Is.False);
                Assert.That(b.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - GameConstants.BattleAllyMoveApCost));
            });
        }
        private static void Configure(BattleSceneRoot b, Func<CombatUnitSnapshot, CombatGridCell, bool> terrain = null) =>
            Assert.That(b.TryConfigureGrid(4, 4, new[] { new CombatGridPlacement(100, Cell(0, 0)),
                new CombatGridPlacement(800, Cell(3, 3)) }, terrain ?? ((_, _) => true),
                (a, c) => Math.Abs(a.X - c.X) + Math.Abs(a.Y - c.Y) == 1,
                preventDiagonalCornerCutting: true), Is.True);
        private static void WithBattle(Action<BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try { var go = new GameObject("Native route execution check"); SceneManager.MoveGameObjectToScene(go, scene);
                check(go.AddComponent<BattleSceneRoot>()); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
