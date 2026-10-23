using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatStatusTests
    {
        static CombatFlow Create(out CombatUnit actor, out CombatUnit enemy)
        {
            var world = new CombatWorld();
            actor = new CombatUnit { Id = 100, Hp = 100, MaxHp = 100, Mp = 100, MaxMp = 100 };
            enemy = new CombatUnit { Id = 800, Enemy = true, Hp = 100, MaxHp = 100, Mp = 100, MaxMp = 100 };
            world.Add(actor); world.Add(enemy);
            var flow = new CombatFlow(world, new LocalCombatAuthority()); flow.TemporaryRulesEnabled = true;
            Assert.IsTrue(flow.ConfigureTemporaryStatusMagic(1601, 3, 5, TemporaryStatusEffect.Poison, 8, 20));
            Assert.IsTrue(flow.ConfigureTemporaryStatusMagic(1701, 6, 5, TemporaryStatusEffect.CurePoison, 1, 20));
            Assert.IsTrue(flow.ConfigureTemporaryStatusMagic(3601, 5, 5, TemporaryStatusEffect.MovementReduction, 25, 20));
            return flow;
        }

        [Test]
        public void MovementStatusCancelsReservationAndLimitsActualReachablePathUntilExpiry()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new UnityEngine.GameObject("Movement status fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                var b=go.AddComponent<BattleSceneRoot>();b.SetTemporaryLocalRules(true);
                Assert.IsTrue(b.TryConfigureGrid(5,5,new[]{new CombatGridPlacement(100,new CombatGridCell(0,0)),
                    new CombatGridPlacement(800,new CombatGridCell(4,4))},(_,_)=>true,
                    (a,c)=>System.Math.Abs(a.X-c.X)+System.Math.Abs(a.Y-c.Y)==1,movementCost:(_,_)=>1));
                Assert.IsTrue(b.ConfigureMovementBudget(100,4));Assert.IsTrue(b.TrySelectUnit(100));
                b.ConfigureTemporaryStatusMagic(3601,0,5,TemporaryStatusEffect.MovementReduction,50,20);b.ConfigureMagicApCost(3601,8);
                b.AdvanceActionPoints(20);
                Assert.IsTrue(b.TryStartMovementAction(100,new CombatGridCell(1,0),8,out _,out _));int ap=b.PlayerAp;
                var flow=(CombatFlow)typeof(BattleSceneRoot).GetField("flow",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(b);
                Assert.IsTrue(flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,3601),out _,out _));
                Assert.IsFalse(b.IsMovementInProgress(100));Assert.AreEqual(ap,b.PlayerAp);
                Assert.IsTrue(b.TryFindSelectedMovementPath(new CombatGridCell(2,0),out _));
                Assert.IsFalse(b.TryFindSelectedMovementPath(new CombatGridCell(3,0),out _));
                b.AdvanceActionPoints(20);Assert.IsTrue(b.TryFindSelectedMovementPath(new CombatGridCell(3,0),out _));
            }
            finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }

        [Test]
        public void PoisonTicksRefreshesExpiresAndPublishesDamageWithOriginalSource()
        {
            var flow = Create(out var actor, out var enemy);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Magic, 1601), out _, out _));
            Assert.AreEqual(100, enemy.Hp); Assert.AreEqual(97, actor.Mp);
            int hits=0; flow.ResultApplied += result => { if(result.Damage>0){hits++;Assert.AreEqual(100,result.ActorId);} };
            flow.AdvanceActionPoints(1); Assert.AreEqual(100, enemy.Hp);
            flow.AdvanceActionPoints(1); Assert.AreEqual(92, enemy.Hp);
            flow.AdvanceActionPoints(2); Assert.AreEqual(84, enemy.Hp);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Magic, 1601), out _, out _));
            flow.AdvanceActionPoints(2); Assert.AreEqual(76, enemy.Hp); Assert.AreEqual(3,hits);
            flow.AdvanceActionPoints(30); Assert.AreEqual(4, enemy.Hp);
            Assert.IsFalse(flow.TryGetTemporaryStatus(800,TemporaryStatusEffect.Poison,out _,out _));
            flow.AdvanceActionPoints(30); Assert.AreEqual(4, enemy.Hp);
        }

        [Test]
        public void CureRequiresPoisonAndLeavesHealthAndOtherStatusUntouched()
        {
            var flow = Create(out var actor, out var enemy);
            Assert.IsFalse(flow.TryExecute(new CombatRequest(800,800,CombatActionType.Magic,1701),out _,out _));
            Assert.AreEqual(100,enemy.Mp);
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,100,CombatActionType.Magic,1601),out _,out _));
            flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1601),out _,out _);
            flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,3601),out _,out _);
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1701),out _,out _));
            Assert.IsTrue(flow.TryExecute(new CombatRequest(800,800,CombatActionType.Magic,1701),out _,out _));
            Assert.AreEqual(94,enemy.Mp);Assert.AreEqual(100,enemy.Hp);
            Assert.IsFalse(flow.TryGetTemporaryStatus(800,TemporaryStatusEffect.Poison,out _,out _));
            Assert.IsTrue(flow.TryGetTemporaryStatus(800,TemporaryStatusEffect.MovementReduction,out _,out _));
            flow.AdvanceActionPoints(10);Assert.AreEqual(100,enemy.Hp);
        }

        [Test]
        public void PoisonDeathClearsStatusesAndDisablingNeverResurrectsThem()
        {
            var flow = Create(out _, out var enemy); enemy.Hp=3;
            flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1601),out _,out _);
            flow.AdvanceActionPoints(2);Assert.AreEqual(0,enemy.Hp);Assert.AreEqual(UnitLifeState.Removed,enemy.LifeState);
            enemy.Hp=100;enemy.LifeState=UnitLifeState.Active;
            Assert.IsFalse(flow.TryGetTemporaryStatus(800,TemporaryStatusEffect.Poison,out _,out _));
            flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1601),out _,out _);
            flow.TemporaryRulesEnabled=false;flow.TemporaryRulesEnabled=true;
            flow.AdvanceActionPoints(20);Assert.AreEqual(100,enemy.Hp);
        }

        [Test]
        public void MovementReductionChangesBudgetWithoutChangingApOrRecovery()
        {
            var flow=Create(out _,out var enemy);int ap=enemy.ActionPoints.Current;
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,3601),out _,out _));
            Assert.AreEqual(3,flow.GetEffectiveMovementBudget(800,5));
            Assert.AreEqual(5,flow.GetEffectiveMovementBudget(100,5));Assert.AreEqual(ap,enemy.ActionPoints.Current);
            Assert.IsFalse(flow.ConfigureTemporaryStatusMagic(3601,5,5,TemporaryStatusEffect.MovementReduction,100,20));
            flow.AdvanceActionPoints(20);Assert.AreEqual(5,flow.GetEffectiveMovementBudget(800,5));
        }
    }
}
