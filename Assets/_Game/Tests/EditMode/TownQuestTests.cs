using System;
using System.IO;
using DarkSaver.Core;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class TownQuestTests
    {
        [Test]
        public void UnreceivedQuestResponsesCannotMutateProgressOrGrantRewards()
        {
            var content=JsonUtility.FromJson<TownQuestContent>(Resources.Load<TextAsset>("Town/Quests").text);
            var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/01/Content").text);Assert.IsTrue(content.IsValid);
            var state=ReceivedScenarioFixture.Empty(definition);var items=new NativeInventory();string before=JsonUtility.ToJson(state);
            foreach(var quest in content.rows)foreach(bool claim in new[]{false,true})
            {
                Assert.IsFalse(TownQuestRules.TryApply(definition,state,items,quest,claim,out var next,out var candidate));
                Assert.IsNull(next);Assert.IsNull(candidate);
            }
            Assert.AreEqual(before,JsonUtility.ToJson(state));Assert.IsEmpty(items.items);Assert.AreEqual(0,items.gold);
        }
    }
}
