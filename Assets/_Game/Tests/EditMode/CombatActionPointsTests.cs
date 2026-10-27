using System;
using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    // Replacement for archived starts-at-six/faction-cap/local-regeneration expectations.
    public sealed class CombatActionPointsTests
    {
        [Test]
        public void InitialTicksAndMaximumRemainUnknownForBothFactions()
        {
            foreach(bool enemy in new[]{false,true})
            {
                var ap=new CombatActionPoints(enemy);
                Assert.AreEqual(0,ap.Current);Assert.AreEqual(0,ap.Maximum);Assert.IsFalse(ap.HasConfirmedMaximum);
                Assert.IsFalse(ap.CanStart(2));Assert.IsFalse(ap.TrySpend(2));
            }
        }
        [TestCase(23)] [TestCase(int.MaxValue)]
        public void ReceivedTicksAreNotClampedToAnInventedFactionMaximum(int value)
        {
            var ap=new CombatActionPoints();Assert.IsTrue(ap.ApplyAbsolute(value));Assert.AreEqual(value,ap.Current);
            Assert.IsFalse(ap.TrySpend(2));Assert.AreEqual(value,ap.Current);
            Assert.IsTrue(ap.ConfigureMaximum(30));Assert.AreEqual(Math.Min(30,value),ap.Current);
        }
        [TestCase(0f)] [TestCase(.25f)] [TestCase(1000f)] [TestCase(float.MaxValue)]
        [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void TimeNeverManufacturesReceivedAp(float seconds)
        {
            var ap=new CombatActionPoints();ap.ConfigureMaximum(30);ap.ApplyAbsolute(17);
            Assert.IsFalse(ap.Advance(seconds));Assert.AreEqual(17,ap.Current);Assert.AreEqual(30,ap.Maximum);
        }
        [Test]
        public void ExplicitMaximumAndCostAuthorizeOneRequestOnlyAtFullTicks()
        {
            var ap=new CombatActionPoints();ap.ConfigureMaximum(10);ap.ApplyAbsolute(8);
            Assert.IsFalse(ap.TrySpend(2));Assert.AreEqual(8,ap.Current);
            ap.ApplyAbsolute(10);Assert.IsTrue(ap.TrySpend(2));Assert.AreEqual(8,ap.Current);
            Assert.IsFalse(ap.TrySpend(2));ap.Advance(1000);Assert.AreEqual(8,ap.Current);
            ap.ApplyAbsolute(10);Assert.IsTrue(ap.CanStart(2));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(11)]
        public void InvalidCostPreservesConfirmedTicks(int cost)
        {
            var ap=new CombatActionPoints();ap.ConfigureMaximum(10);ap.ApplyAbsolute(10);
            Assert.IsFalse(ap.TrySpend(cost));Assert.AreEqual(10,ap.Current);
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(32768)]
        public void InvalidMaximumDoesNotReplaceReceivedState(int maximum)
        {
            var ap=new CombatActionPoints();ap.ConfigureMaximum(30);ap.ApplyAbsolute(17);
            Assert.IsFalse(ap.ConfigureMaximum(maximum));Assert.AreEqual(30,ap.Maximum);Assert.AreEqual(17,ap.Current);
        }
        private static CombatFlow Fixture(out CombatUnit actor,out CombatUnit enemy)
        {
            var world=new CombatWorld();actor=new CombatUnit {Id=100,Hp=120,MaxHp=120,Mp=17,MaxMp=30};
            enemy=new CombatUnit {Id=800,Enemy=true,Hp=75,MaxHp=75};world.Add(actor);world.Add(enemy);
            return new CombatFlow(world,new CombatServerBoundary(),true);
        }
        [Test]
        public void ExplicitCostsUseConfirmedUnitMaximaInsteadOfRemovedFactionDefaults()
        {
            var flow=Fixture(out var actor,out var enemy);
            Assert.IsFalse(flow.ConfigureAttackApCost(100,2));
            actor.ActionPoints.ConfigureMaximum(10);enemy.ActionPoints.ConfigureMaximum(20);
            Assert.IsTrue(flow.ConfigureAttackApCost(100,2));Assert.IsTrue(flow.ConfigureAttackApCost(800,12));
            Assert.IsTrue(flow.ConfigureMagicApCost(101,6));Assert.IsTrue(flow.ConfigureMagicApCost(301,11));
            Assert.AreEqual(2,flow.GetAttackApCost(100));Assert.AreEqual(12,flow.GetAttackApCost(800));
            Assert.AreEqual(6,flow.GetMagicApCost(101));Assert.AreEqual(11,flow.GetMagicApCost(301));
            Assert.IsFalse(flow.ConfigureAttackApCost(100,11));Assert.AreEqual(2,flow.GetAttackApCost(100));
            Assert.IsFalse(flow.ConfigureMagicApCost(101,short.MaxValue+1));Assert.AreEqual(6,flow.GetMagicApCost(101));
            Assert.AreEqual(0,actor.ActionPoints.Current);Assert.AreEqual(0,enemy.ActionPoints.Current);
        }
        [Test]
        public void MissingAndInsufficientCostsCannotNotifyOrChangeReceivedVitals()
        {
            var flow=Fixture(out var actor,out var enemy);actor.ActionPoints.ConfigureMaximum(10);enemy.ActionPoints.ConfigureMaximum(20);
            actor.ActionPoints.ApplyAbsolute(10);var request=new CombatRequest(100,800,CombatActionType.Attack);int notifications=0;
            flow.ActionStarted+=_=>notifications++;
            Assert.IsFalse(flow.TryStartAction(request,out _));Assert.AreEqual(0,notifications);
            Assert.IsTrue(flow.ConfigureAttackApCost(100,2));actor.ActionPoints.ApplyAbsolute(8);
            Assert.IsFalse(flow.TryStartAction(request,out _));Assert.AreEqual(0,notifications);
            Assert.AreEqual((120,17,75,8),(actor.Hp,actor.Mp,enemy.Hp,actor.ActionPoints.Current));
        }
        [Test]
        public void FullReceivedTicksPermitRequestStartWithoutResolvingDamageOrReward()
        {
            var flow=Fixture(out var actor,out var enemy);actor.ActionPoints.ConfigureMaximum(10);
            flow.ConfigureAttackApCost(100,2);actor.ActionPoints.ApplyAbsolute(10);int starts=0;flow.ActionStarted+=_=>starts++;
            Assert.IsTrue(flow.TryStartAction(new CombatRequest(100,800,CombatActionType.Attack),out _));
            Assert.AreEqual(1,starts);Assert.AreEqual(8,actor.ActionPoints.Current);
            Assert.AreEqual((120,17,75,0u),(actor.Hp,actor.Mp,enemy.Hp,actor.Experience));
        }
    }
}
