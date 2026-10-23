using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class QuickMagicTests
    {
        static CombatFlow Create(out CombatUnit actor, out CombatUnit enemy)
        {
            var world = new CombatWorld();
            actor = new CombatUnit { Id = 100, Hp = 80, MaxHp = 100, Mp = 100, MaxMp = 100, Attack = 20 };
            enemy = new CombatUnit { Id = 800, Enemy = true, Hp = 100, MaxHp = 100 };
            world.Add(actor); world.Add(enemy);
            var flow = new CombatFlow(world, new LocalCombatAuthority(sample: () => 0), true);
            flow.TemporaryRulesEnabled = true;
            Assert.IsTrue(flow.ConfigureTemporaryQuickMagic(2201, 7, 5, 25, 20));
            Assert.IsTrue(flow.ConfigureMagicApCost(2201, 8));
            Assert.IsTrue(flow.ConfigureActionPointCosts(8, 8));
            actor.ActionPoints.ApplyAbsolute(actor.ActionPoints.Maximum);
            return flow;
        }

        static void Cast(CombatFlow flow, CombatUnit actor)
        {
            actor.ActionPoints.ApplyAbsolute(actor.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 100, CombatActionType.Magic, 2201), out _, out _));
        }

        [Test]
        public void MovementSpendsDiscountOnceAndKeepsSameDestination()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new UnityEngine.GameObject("Quick movement fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                var b=go.AddComponent<BattleSceneRoot>();b.SetTemporaryLocalRules(true);
                Assert.IsTrue(b.TryConfigureGrid(4,4,new[]{new CombatGridPlacement(100,new CombatGridCell(0,0)),
                    new CombatGridPlacement(800,new CombatGridCell(3,3))},(_,_)=>true,(_,_)=>true));
                var flow=(CombatFlow)typeof(BattleSceneRoot).GetField("flow",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(b);
                Assert.IsTrue(flow.ConfigureTemporaryQuickMagic(2201,0,5,25,20));flow.ConfigureMagicApCost(2201,8);
                b.AdvanceActionPoints(20);Assert.IsTrue(flow.TryExecute(new CombatRequest(100,100,CombatActionType.Magic,2201),out _,out _));
                b.AdvanceActionPoints(10);
                Assert.IsTrue(b.TryStartMovementAction(100,new CombatGridCell(1,0),8,out _,out _));
                Assert.AreEqual(GameConstants.BattleMaximumAp-6,b.PlayerAp);
                Assert.IsFalse(b.TryStartMovementAction(100,new CombatGridCell(2,0),8,out _,out _));
            }
            finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }

        [Test]
        public void QuickChangesExpenditureNotRecoveryAndDoesNotDiscountItsFirstCast()
        {
            var flow = Create(out var actor, out var enemy);
            Cast(flow, actor);
            Assert.AreEqual(actor.ActionPoints.Maximum - 8, actor.ActionPoints.Current);
            Assert.AreEqual(93, actor.Mp); Assert.AreEqual(80, actor.Hp);
            Assert.IsTrue(flow.TryGetTemporaryQuick(100, out int percent, out double seconds));
            Assert.AreEqual(25, percent); Assert.AreEqual(20, seconds);
            Assert.AreEqual(6, flow.GetAttackApCost(100)); Assert.AreEqual(8, flow.GetAttackApCost(800));
            int before = actor.ActionPoints.Current;
            flow.AdvanceActionPoints(GameConstants.BattleApRecoverySeconds);
            Assert.AreEqual(before + GameConstants.BattleApRecoveryAmount, actor.ActionPoints.Current);
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Attack), out _, out _));
            actor.ActionPoints.ApplyAbsolute(actor.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Attack), out _, out _));
            Assert.AreEqual(actor.ActionPoints.Maximum - 6, actor.ActionPoints.Current);
            actor.ActionPoints.ApplyAbsolute(actor.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Magic, 101), out _, out _));
            Assert.AreEqual(actor.ActionPoints.Maximum - 6, actor.ActionPoints.Current);
            Assert.AreEqual(100, enemy.MaxHp);
        }

        [Test]
        public void RestAndRecoveryUseSameDiscountWithoutChangingEffects()
        {
            var flow = Create(out var actor, out _); Cast(flow, actor);
            actor.ActionPoints.ApplyAbsolute(actor.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryTemporaryRest(100));
            Assert.AreEqual(actor.ActionPoints.Maximum - flow.GetEffectiveActionCost(100, GameConstants.BattleTemporaryRestApCost), actor.ActionPoints.Current);
            actor.Hp = 50; actor.ActionPoints.ApplyAbsolute(actor.ActionPoints.Maximum);
            Assert.IsTrue(flow.TryApplyItemRecovery(100, 100, 15, 0, 6, 6));
            Assert.AreEqual(65, actor.Hp); Assert.AreEqual(actor.ActionPoints.Maximum - 5, actor.ActionPoints.Current);
        }

        [Test]
        public void RefreshExpiryDeathAndDisableClearStateWithoutStacking()
        {
            var flow = Create(out var actor, out _); Cast(flow, actor);
            flow.AdvanceActionPoints(10); Cast(flow, actor);
            Assert.AreEqual(6, flow.GetAttackApCost(100));
            flow.AdvanceActionPoints(19); Assert.AreEqual(6, flow.GetAttackApCost(100));
            flow.AdvanceActionPoints(1); Assert.AreEqual(8, flow.GetAttackApCost(100));
            Cast(flow, actor); Assert.IsTrue(flow.TryApplyDeath(100, false));
            actor.Hp = 80; actor.LifeState = UnitLifeState.Active;
            Assert.IsFalse(flow.TryGetTemporaryQuick(100, out _, out _));
            Cast(flow, actor); flow.TemporaryRulesEnabled = false; flow.TemporaryRulesEnabled = true;
            Assert.AreEqual(8, flow.GetAttackApCost(100));
        }

        [Test]
        public void InvalidRulesTargetsAndDisabledCostsNeverSpendOrBecomeEnabled()
        {
            var flow = Create(out var actor, out _);
            Assert.IsFalse(flow.ConfigureTemporaryQuickMagic(2201, 7, 5, 100, 20));
            Assert.IsFalse(flow.ConfigureTemporaryQuickMagic(2201, 7, 5, 25, float.NaN));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Magic, 2201), out _, out _));
            Assert.AreEqual(100, actor.Mp); Assert.AreEqual(actor.ActionPoints.Maximum, actor.ActionPoints.Current);
            Cast(flow, actor);
            Assert.AreEqual(0, flow.GetEffectiveActionCost(100, 0));
            Assert.AreEqual(-1, flow.GetEffectiveActionCost(100, -1));
            Assert.AreEqual(int.MaxValue, flow.GetEffectiveActionCost(100, int.MaxValue));
            Assert.AreEqual(1, flow.GetEffectiveActionCost(100, 1));
        }
    }
}
