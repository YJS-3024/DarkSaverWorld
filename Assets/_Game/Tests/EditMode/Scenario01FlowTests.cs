using System;
using System.IO;
using DarkSaver.Core;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class Scenario01FlowTests
    {
        [TestCase(false)] [TestCase(true)]
        public void LocalBattleReturnNeverInfersScenarioClearOrRewards(bool defeated)
        {
            var definition=Content();var state=ReceivedScenarioFixture.Empty(definition);state.phase=Scenario01Progress.Playing;state.pendingBattle=401;
            string before=JsonUtility.ToJson(state);var observed=new Scenario01EnemyState{id=401,hp=defeated?0:70,mp=0,x=13,y=30};
            Assert.IsFalse(Scenario01Rules.RecordBattleReturn(definition,state,401,defeated,observed),"a local battle return is not an authoritative scenario result");
            Assert.AreEqual(before,JsonUtility.ToJson(state));Assert.IsEmpty(state.defeated);Assert.AreEqual(401,state.pendingBattle);
            Assert.IsFalse(state.clearRequested);
        }

        private static Scenario01Definition Content()
        {
            var asset = Resources.Load<TextAsset>("Scenarios/01/Content"); Assert.That(asset, Is.Not.Null);
            var definition = JsonUtility.FromJson<Scenario01Definition>(asset.text);
            Assert.That(definition.IsValid, Is.True); return definition;
        }

        [Test]
        public void MissingScenarioAuthorityCannotAdvanceOrMutateReceivedCheckpoint()
        {
            var definition=Content();Assert.Throws<NotSupportedException>(()=>Scenario01Progress.Create(definition));
            var state=ReceivedScenarioFixture.Empty(definition);var inventory=new NativeInventory();
            string before=JsonUtility.ToJson(state),itemsBefore=JsonUtility.ToJson(inventory);
            Scenario01Rules.Step(definition,state,inventory,100,out _,out _);
            Assert.AreEqual(before,JsonUtility.ToJson(state));Assert.AreEqual(itemsBefore,JsonUtility.ToJson(inventory));
            Assert.IsTrue(state.Matches(definition));
        }

        [Test]
        public void LocalEventActivationNeverInventsGoldOrConsumesItems()
        {
            var definition=Content();var state=ReceivedScenarioFixture.Empty(definition);state.phase=Scenario01Progress.Playing;
            var inventory=new NativeInventory();string before=JsonUtility.ToJson(state);
            foreach(var position in new[]{new[]{6,21},new[]{14,5}})
                Assert.IsFalse(Scenario01Rules.TryActivate(definition,state,inventory,position[0],position[1],out _,out _));
            Assert.AreEqual(before,JsonUtility.ToJson(state));Assert.AreEqual(0,inventory.gold);Assert.IsEmpty(inventory.items);
        }
    }
}
