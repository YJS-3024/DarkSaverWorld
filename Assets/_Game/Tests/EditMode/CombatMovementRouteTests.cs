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
        private static CombatGridCell Cell(int x,int y)=>new CombatGridCell(x,y);
        private static void WithBattle(Action<BattleSceneRoot> action,Func<CombatUnitSnapshot,CombatGridCell,bool> terrain=null)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try{var go=new GameObject("native received movement fixture");SceneManager.MoveGameObjectToScene(go,scene);
                var b=go.AddComponent<BattleSceneRoot>();NativeCombatStateFixture.Initialize(b,6);
                Assert.IsTrue(b.TryConfigureGrid(4,4,new[]{new CombatGridPlacement(100,Cell(1,1)),new CombatGridPlacement(800,Cell(3,3))},
                    terrain??((u,c)=>true),(a,c)=>Math.Abs(a.X-c.X)<=1 && Math.Abs(a.Y-c.Y)<=1));action(b);}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [TestCase(0,1)] [TestCase(1,0)] [TestCase(0,-1)] [TestCase(-1,0)]
        [TestCase(1,1)] [TestCase(1,-1)] [TestCase(-1,1)] [TestCase(-1,-1)]
        public void ReceivedAlliedStepChangesOnlyPositionWithoutInferringActionCost(int dx,int dy)
        {
            WithBattle(b=>{int events=0;b.MovementStepApplied+=_=>events++;
                Assert.IsTrue(b.TryReserveMovementStep(100,Cell(1+dx,1+dy),out var ticket));
                Assert.IsTrue(b.TryApplyReservedMovementStep(ticket));Assert.IsFalse(b.TryApplyReservedMovementStep(ticket));
                b.TryGetUnit(100,out var unit);Assert.AreEqual(1+dx,unit.X);Assert.AreEqual(1+dy,unit.Y);
                Assert.AreEqual(6,unit.Ap);Assert.AreEqual(120,unit.Hp);Assert.AreEqual(30,unit.Mp);Assert.AreEqual(1,events);
            });
        }
        [TestCase(0,1)] [TestCase(1,0)] [TestCase(0,-1)] [TestCase(-1,0)]
        [TestCase(1,1)] [TestCase(1,-1)] [TestCase(-1,1)] [TestCase(-1,-1)]
        public void ReceivedEnemyStepDoesNotUseAnInventedFactionCharge(int dx,int dy)
        {
            WithBattle(b=>{
                // First explicit observation puts enemy at the center, then tests all eight directions.
                Assert.IsTrue(b.TryReserveMovementStep(800,Cell(2,2),out var start));Assert.IsTrue(b.TryApplyReservedMovementStep(start));
                if(dx==-1 && dy==-1){Assert.IsFalse(b.TryReserveMovementStep(800,Cell(1,1),out _));}
                else{Assert.IsTrue(b.TryReserveMovementStep(800,Cell(2+dx,2+dy),out var ticket));Assert.IsTrue(b.TryApplyReservedMovementStep(ticket));}
                b.TryGetUnit(800,out var unit);Assert.AreEqual(6,unit.Ap);Assert.AreEqual(75,unit.Hp);Assert.AreEqual(0,unit.Experience);
            });
        }
        [Test]public void QueryAndClockCannotAuthorizeUnreceivedMovementOrAi()
        {
            WithBattle(b=>{Assert.IsTrue(b.TrySelectUnit(100));Assert.IsTrue(b.TryFindSelectedMovementPath(Cell(1,2),out var path));Assert.Greater(path.Count,0);
                Assert.IsFalse(b.TryMoveTo(100,Cell(1,2),out _));Assert.IsTrue(b.AdvanceMovement(1000));Assert.IsFalse(b.AdvanceActionPoints(1000));
                b.TryGetUnit(100,out var unit);Assert.AreEqual(1,unit.X);Assert.AreEqual(1,unit.Y);Assert.AreEqual(6,unit.Ap);Assert.IsFalse(b.IsMovementInProgress(100));});
        }
        [Test]public void TerrainChangedAfterReceiptRejectsCommitWithoutPartialMovement()
        {
            bool passable=true;WithBattle(b=>{Assert.IsTrue(b.TryReserveMovementStep(100,Cell(1,2),out var ticket));passable=false;
                Assert.IsFalse(b.TryApplyReservedMovementStep(ticket));b.TryGetUnit(100,out var unit);Assert.AreEqual(1,unit.Y);Assert.AreEqual(6,unit.Ap);},(u,c)=>passable);
        }
        [Test]public void CancellationRejectsLateConfirmationWithoutRefundOrRecovery()
        {
            WithBattle(b=>{Assert.IsTrue(b.TryReserveMovementStep(100,Cell(1,2),out var ticket));Assert.IsTrue(b.CancelMovementStep(100));
                Assert.IsFalse(b.TryApplyReservedMovementStep(ticket));Assert.AreEqual(6,b.PlayerAp);Assert.AreEqual(120,b.PlayerHp);});
        }
        [Test]public void SeparateHpZeroResponsePreventsOldReceiptFromRevivingOrMovingUnit()
        {
            WithBattle(b=>{Assert.IsTrue(b.TryReserveMovementStep(100,Cell(1,2),out var ticket));Assert.IsTrue(b.ApplyHp(100,0));
                Assert.IsFalse(b.TryApplyReservedMovementStep(ticket));Assert.AreEqual(0,b.PlayerHp);Assert.AreEqual(6,b.PlayerAp);});
        }
    }
}
