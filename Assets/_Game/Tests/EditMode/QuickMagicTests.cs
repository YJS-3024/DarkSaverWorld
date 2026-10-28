using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class QuickMagicTests
    {
        private static CombatFlow Create(out CombatUnit actor,out CombatUnit enemy)
        {
            var world=new CombatWorld();
            actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=80,MaxHp=100,Mp=40,MaxMp=100,Attack=20},20);
            enemy=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=100,MaxHp=100,Mp=30,MaxMp=100,Defense=10},20);
            world.Add(actor);world.Add(enemy);return new CombatFlow(world,new CombatServerBoundary(),true);
        }
        [Test]public void MissingQuickRuleCannotDiscountConfirmedAttackCost()
        {
            var flow=Create(out var actor,out var enemy);Assert.IsTrue(flow.ConfigureActionPointCosts(8,8));
            Assert.IsFalse(flow.ConfigureTemporaryQuickMagic(2201,7,5,25,20));
            Assert.AreEqual(8,flow.GetEffectiveActionCost(100,8));
            Assert.IsTrue(flow.TryStartAction(new CombatRequest(100,800,CombatActionType.Attack),out _));
            Assert.AreEqual(12,actor.ActionPoints.Current);Assert.AreEqual(100,enemy.Hp);Assert.AreEqual(40,actor.Mp);
        }
        [Test]public void QuickCannotCreateMovementBudgetOrNaturalRecovery()
        {
            var flow=Create(out var actor,out var enemy);actor.ActionPoints.ApplyAbsolute(7);
            Assert.IsFalse(flow.ConfigureTemporaryQuickMagic(2201,7,5,25,20));
            Assert.AreEqual(12,flow.GetEffectiveMovementBudget(100,12));Assert.IsFalse(flow.AdvanceActionPoints(1000));
            Assert.AreEqual(7,actor.ActionPoints.Current);Assert.AreEqual(80,actor.Hp);
        }
        [Test]public void QuickCannotEnableLocalRestOrItemRecovery()
        {
            var flow=Create(out var actor,out var enemy);
            Assert.IsFalse(flow.ConfigureTemporaryQuickMagic(2201,7,5,25,20));Assert.IsFalse(flow.TryTemporaryRest(100));
            Assert.AreEqual(80,actor.Hp);Assert.AreEqual(40,actor.Mp);Assert.AreEqual(20,actor.ActionPoints.Current);
        }
        [Test]public void RepeatedConfigurationAndModeChangesCannotRestoreRemovedQuickSimulator()
        {
            var flow=Create(out var actor,out var enemy);
            for(int i=0;i<3;i++){flow.TemporaryRulesEnabled=true;Assert.IsFalse(flow.TemporaryRulesEnabled);Assert.IsFalse(flow.ConfigureTemporaryQuickMagic(2201,7,5,25,20));}
            Assert.AreEqual(8,flow.GetEffectiveActionCost(100,8));Assert.AreEqual(40,actor.Mp);
        }
        [Test]public void UnknownQuickCastNeverSpendsOrStartsPresentation()
        {
            var flow=Create(out var actor,out var enemy);int started=0;flow.ActionStarted+=_=>started++;
            Assert.IsTrue(flow.ConfigureMagicApCost(2201,8));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,100,CombatActionType.Magic,2201),out _,out _));
            Assert.AreEqual(0,started);Assert.AreEqual(20,actor.ActionPoints.Current);Assert.AreEqual(40,actor.Mp);
        }
    }
}
