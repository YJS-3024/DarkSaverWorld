using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatActionStatusTests
    {
        static CombatFlow Create(out CombatUnit player, out CombatUnit enemy)
        {
            var world=new CombatWorld();
            player=new CombatUnit {Id=100,Hp=80,MaxHp=100,Mp=100,MaxMp=100,Attack=20};
            enemy=new CombatUnit {Id=800,Enemy=true,Hp=100,MaxHp=100,Mp=100,MaxMp=100,Attack=20};
            world.Add(player);world.Add(enemy);
            var flow=new CombatFlow(world,new CombatServerBoundary(sample:()=>0),true);flow.TemporaryRulesEnabled=true;
            flow.ConfigureTemporaryStatusMagic(1801,0,7,TemporaryStatusEffect.Silence,1,20);
            flow.ConfigureTemporaryStatusMagic(6901,0,7,TemporaryStatusEffect.Sleep,1,20);
            flow.ConfigureTemporaryStatusMagic(3401,0,7,TemporaryStatusEffect.Wake,1,20);
            flow.ConfigureActionPointCosts(8,8);
            flow.ConfigureMagicApCost(1801,8);flow.ConfigureMagicApCost(6901,8);flow.ConfigureMagicApCost(3401,8);
            player.ActionPoints.ApplyAbsolute(player.ActionPoints.Maximum);enemy.ActionPoints.ApplyAbsolute(enemy.ActionPoints.Maximum);
            return flow;
        }

        [Test]
        public void SleepCancelsReservedMovementAndHidesPathsUntilWakeOrExpiry()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new UnityEngine.GameObject("Sleep movement fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                var b=go.AddComponent<BattleSceneRoot>();b.SetTemporaryLocalRules(true);
                Assert.IsTrue(b.TryConfigureGrid(4,4,new[]{new CombatGridPlacement(100,new CombatGridCell(0,0)),
                    new CombatGridPlacement(800,new CombatGridCell(3,3))},(_,_)=>true,(_,_)=>true));
                Assert.IsTrue(b.TrySelectUnit(100));b.ConfigureTemporaryStatusMagic(6901,0,7,TemporaryStatusEffect.Sleep,1,20);b.ConfigureMagicApCost(6901,8);
                b.AdvanceActionPoints(20);Assert.IsTrue(b.TryStartMovementAction(100,new CombatGridCell(1,0),8,out _,out _));int ap=b.PlayerAp;
                var flow=(CombatFlow)typeof(BattleSceneRoot).GetField("flow",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(b);
                Assert.IsTrue(flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,6901),out _,out _));
                Assert.IsFalse(b.IsMovementInProgress(100));Assert.AreEqual(ap,b.PlayerAp);
                Assert.IsFalse(b.TryFindSelectedMovementPath(new CombatGridCell(1,0),out _));
                Assert.IsFalse(b.TryGetSelectedReachableDestinations(out _));
                Assert.IsFalse(b.TryStartMovementAction(100,new CombatGridCell(1,0),8,out _,out _));
                b.AdvanceActionPoints(20);Assert.IsTrue(b.TryFindSelectedMovementPath(new CombatGridCell(1,0),out _));
            }
            finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }

        [Test]
        public void SilenceRejectsLocalAndPacketMagicWithoutSpendingButAllowsAttackAndItems()
        {
            var flow=Create(out var player,out var enemy);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,1801),out _,out _));
            int ap=player.ActionPoints.Current,mp=player.Mp;
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,101),out _,out _));
            Assert.IsFalse(flow.TryStartMagicRequest(101,100,new short[]{800},out _));
            Assert.AreEqual(ap,player.ActionPoints.Current);Assert.AreEqual(mp,player.Mp);
            Assert.IsTrue(flow.TryApplyItemMagic(100,800,101,7,6));Assert.AreEqual(mp,player.Mp);
            player.ActionPoints.ApplyAbsolute(player.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Attack),out _,out _));
            player.ActionPoints.ApplyAbsolute(player.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryTemporaryRest(100));
            flow.AdvanceActionPoints(20);Assert.IsTrue(flow.CanUnitCast(100));
        }

        [Test]
        public void SleepStopsAttackMagicRestAndItemsWhileApRecoversNormally()
        {
            var flow=Create(out var player,out _);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,6901),out _,out _));
            Assert.IsFalse(flow.CanUnitAct(100));int ap=player.ActionPoints.Current;
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Attack),out _,out _));
            Assert.IsFalse(flow.TryStartMagicRequest(101,100,new short[]{800},out _));
            Assert.IsFalse(flow.TryTemporaryRest(100));
            Assert.IsFalse(flow.TryApplyItemRecovery(100,100,15,0,6,6));
            Assert.IsFalse(flow.TryApplyItemMagic(100,800,101,7,6));
            Assert.AreEqual(ap,player.ActionPoints.Current);Assert.AreEqual(100,player.Mp);
            player.ActionPoints.ApplyAbsolute(0);flow.AdvanceActionPoints(GameConstants.BattleApRecoverySeconds);
            Assert.AreEqual(GameConstants.BattleApRecoveryAmount,player.ActionPoints.Current);
            flow.AdvanceActionPoints(20);Assert.IsTrue(flow.CanUnitAct(100));
        }

        [Test]
        public void WakeRequiresSleepingAllyAndOnlyRemovesSleep()
        {
            var flow=Create(out var player,out var enemy);
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,100,CombatActionType.Magic,3401),out _,out _));
            flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,1801),out _,out _);
            enemy.ActionPoints.ApplyAbsolute(enemy.ActionPoints.Maximum);
            flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,6901),out _,out _);
            Assert.IsFalse(flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,3401),out _,out _));
            // Another awake ally can heal a sleeper without waking them, then use a wake effect.
            var world=(CombatWorld)typeof(CombatFlow).GetField("world",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(flow);
            var ally=new CombatUnit {Id=101,Hp=100,MaxHp=100};world.Add(ally);ally.ActionPoints.ApplyAbsolute(ally.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryApplyItemRecovery(101,100,15,0,6,6));
            Assert.IsFalse(flow.CanUnitAct(100));ally.ActionPoints.ApplyAbsolute(ally.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryApplyItemMagic(101,100,3401,7,6));
            Assert.IsTrue(flow.CanUnitAct(100));Assert.IsFalse(flow.CanUnitCast(100));Assert.AreEqual(95,player.Hp);
            ally.ActionPoints.ApplyAbsolute(ally.ActionPoints.Maximum);
            Assert.IsFalse(flow.TryApplyItemMagic(101,100,3401,7,6));
        }

        [Test]
        public void ActualDamageWakesAndDeathOrDisableClearsSleepAndSilence()
        {
            var flow=Create(out var player,out var enemy);
            flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,6901),out _,out _);
            enemy.ActionPoints.ApplyAbsolute(enemy.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(800,100,CombatActionType.Attack),out var hit,out _));
            Assert.Greater(hit.Damage,0);Assert.IsTrue(flow.CanUnitAct(100));
            enemy.ActionPoints.ApplyAbsolute(enemy.ActionPoints.Maximum);flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,1801),out _,out _);
            flow.TryApplyDeath(100,false);player.LifeState=UnitLifeState.Active;player.Hp=100;
            Assert.IsTrue(flow.CanUnitCast(100));
            enemy.ActionPoints.ApplyAbsolute(enemy.ActionPoints.Maximum);flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,6901),out _,out _);
            flow.TemporaryRulesEnabled=false;flow.TemporaryRulesEnabled=true;Assert.IsTrue(flow.CanUnitAct(100));
        }
    }
}
