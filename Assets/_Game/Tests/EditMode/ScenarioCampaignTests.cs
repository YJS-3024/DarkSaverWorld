using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ScenarioCampaignTests
    {
        [TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
        [TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)][TestCase(16)][TestCase(17)]
        [TestCase(33)][TestCase(330)][TestCase(331)][TestCase(332)][TestCase(333)][TestCase(404)][TestCase(405)][TestCase(406)]
        public void NativeScenarioDataRoundTripsWithoutInventingServerInitialization(int number)
        {
            string key=number.ToString("00");var source=Resources.Load<TextAsset>("Scenarios/"+key+"/Content");Assert.IsNotNull(source);
            var definition=JsonUtility.FromJson<Scenario01Definition>(source.text);Assert.IsTrue(definition.IsValid);
            string before=JsonUtility.ToJson(definition);
            Assert.Throws<NotSupportedException>(()=>Scenario01Progress.Create(definition));
            Assert.AreEqual(before,JsonUtility.ToJson(definition),"client cannot alter source while awaiting server initialization");
            var received=ReceivedScenarioFixture.Empty(definition);Assert.IsTrue(received.Matches(definition));
            var restored=JsonUtility.FromJson<Scenario01Progress>(JsonUtility.ToJson(received));
            Assert.IsTrue(restored.Matches(definition));Assert.IsEmpty(restored.enemies);Assert.IsEmpty(restored.defeated);
            Assert.IsFalse(restored.clearRequested);Assert.IsFalse(restored.firstClearSaved);
        }
        [Test]
        public void OlderScenarioCheckpointMayOmitNewOptionalLists()
        {
            var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/01/Content").text);
            var original=ReceivedScenarioFixture.Empty(definition);
            string legacy=JsonUtility.ToJson(original).Replace("\"mapChanges\":[],","").Replace("\"unlockedGenerals\":[],","").Replace("\"deathSides\":[],","");
            var restored=JsonUtility.FromJson<Scenario01Progress>(legacy);Assert.IsTrue(restored.IsValid);Assert.IsTrue(restored.Clone().Matches(definition));
        }
    }
}
