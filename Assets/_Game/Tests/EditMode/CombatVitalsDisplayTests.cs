using System.Collections.Generic;
using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class CombatVitalsDisplayTests
    {
        [Test]
        public void IndependentAbsoluteResponsesPublishActualDeltasWithoutLocalRestOrDamage()
        {
            var world=new CombatWorld();
            var ally=NativeCombatStateFixture.Received(new CombatUnit {Id=1,Hp=90,MaxHp=120,Mp=30,MaxMp=30,Attack=100});
            world.Add(ally);world.Add(NativeCombatStateFixture.Received(new CombatUnit {Id=2,Enemy=true,Hp=7,MaxHp=75}));
            var flow=new CombatFlow(world,new CombatServerBoundary(),false);
            var changes=new List<(short Id,int Delta,bool Mp)>();
            flow.VitalsDisplayChanged+=(id,delta,mp)=>changes.Add((id,delta,mp));
            Assert.IsTrue(flow.TryApplyHp(1,80));Assert.IsTrue(flow.TryApplyHp(1,80));Assert.IsFalse(flow.TryApplyHp(1,121));
            Assert.AreEqual(1,changes.Count);Assert.IsTrue(flow.TryApplyHp(1,90));
            Assert.IsTrue(flow.TryApplyMp(1,20));Assert.IsTrue(flow.TryApplyMp(1,20));
            Assert.IsFalse(flow.TryTemporaryRest(1));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(1,2,CombatActionType.Attack),out _,out _));
            Assert.AreEqual(3,changes.Count);Assert.IsTrue(flow.TryApplyHp(2,0));Assert.IsTrue(flow.TryApplyHp(2,0));
            Assert.That(changes,Is.EqualTo(new[]{((short)1,-10,false),((short)1,10,false),((short)1,-10,true),((short)2,-7,false)}));
            Assert.AreEqual(90,ally.Hp);Assert.AreEqual(20,ally.Mp);
        }
    }
}
