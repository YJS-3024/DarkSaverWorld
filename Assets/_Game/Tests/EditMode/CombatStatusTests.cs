using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class CombatStatusTests
    {
        private static CombatFlow Create(out CombatUnit actor,out CombatUnit enemy)
        {
            var world=new CombatWorld();
            actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=80,MaxHp=100,Mp=40,MaxMp=100,Attack=20},20);
            enemy=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=100,MaxHp=100,Mp=30,MaxMp=100,Defense=10},20);
            world.Add(actor);world.Add(enemy);return new CombatFlow(world,new CombatServerBoundary(),true);
        }
        [Test]public void PoisonRequiresReceivedResultsInsteadOfElapsedLocalTime()
        {
            var flow=Create(out var actor,out var enemy);var before=(actor.Hp,actor.Mp,actor.ActionPoints.Current,enemy.Hp,enemy.Mp,enemy.LifeState);
            Assert.IsFalse(flow.ConfigureTemporaryStatusMagic(1601,3,5,TemporaryStatusEffect.Poison,8,20));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1601),out _,out _));
            Assert.IsFalse(flow.TryGetTemporaryStatus(800,TemporaryStatusEffect.Poison,out _,out _));
            Assert.IsFalse(flow.AdvanceActionPoints(1000));
            Assert.AreEqual(before,(actor.Hp,actor.Mp,actor.ActionPoints.Current,enemy.Hp,enemy.Mp,enemy.LifeState));
        }
        [Test]public void MovementReductionDoesNotInventBudgetChanges()
        {
            var flow=Create(out var actor,out var enemy);var before=(actor.Hp,actor.Mp,actor.ActionPoints.Current,enemy.Hp,enemy.Mp,enemy.LifeState);
            Assert.IsFalse(flow.ConfigureTemporaryStatusMagic(1801,3,5,TemporaryStatusEffect.MovementReduction,8,20));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1801),out _,out _));
            Assert.IsFalse(flow.TryGetTemporaryStatus(800,TemporaryStatusEffect.MovementReduction,out _,out _));
            Assert.IsFalse(flow.AdvanceActionPoints(1000));
            Assert.AreEqual(before,(actor.Hp,actor.Mp,actor.ActionPoints.Current,enemy.Hp,enemy.Mp,enemy.LifeState));
        }
        [Test]public void CureCannotConsumeMpOrHealWithoutApproval()
        {
            var flow=Create(out var actor,out var enemy);var before=(actor.Hp,actor.Mp,actor.ActionPoints.Current,enemy.Hp,enemy.Mp,enemy.LifeState);
            Assert.IsFalse(flow.ConfigureTemporaryStatusMagic(3101,3,5,TemporaryStatusEffect.CurePoison,8,20));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,3101),out _,out _));
            Assert.IsFalse(flow.TryGetTemporaryStatus(800,TemporaryStatusEffect.CurePoison,out _,out _));
            Assert.IsFalse(flow.AdvanceActionPoints(1000));
            Assert.AreEqual(before,(actor.Hp,actor.Mp,actor.ActionPoints.Current,enemy.Hp,enemy.Mp,enemy.LifeState));
        }
        [Test]public void ReceivedHpAndMpRemainIndependentOfUnresolvedStatus()
        {
            var flow=Create(out var actor,out var enemy);
            Assert.IsTrue(flow.TryApplyHp(800,17));Assert.IsTrue(flow.TryApplyMp(800,3));
            Assert.AreEqual(17,enemy.Hp);Assert.AreEqual(3,enemy.Mp);Assert.AreEqual(UnitLifeState.Active,enemy.LifeState);
            Assert.AreEqual(20,enemy.ActionPoints.Current);Assert.AreEqual(80,actor.Hp);
        }
        [Test]public void StatusConfigurationCannotReviveAnObservedCorpse()
        {
            var flow=Create(out var actor,out var enemy);Assert.IsTrue(flow.TryApplyDeath(800,true));
            Assert.IsFalse(flow.ConfigureTemporaryStatusMagic(3401,0,5,TemporaryStatusEffect.Wake,0,0));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,3401),out _,out _));
            Assert.AreEqual(0,enemy.Hp);Assert.AreEqual(UnitLifeState.Corpse,enemy.LifeState);Assert.AreEqual(40,actor.Mp);
        }
    }
}
