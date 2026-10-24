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
        public void SourceQuestRewardsUsePersistentReceiptsAndDoNotMutateCandidates()
        {
            var content = JsonUtility.FromJson<TownQuestContent>(Resources.Load<TextAsset>("Town/Quests").text);
            var definition = JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/01/Content").text);
            Assert.IsTrue(content.IsValid);
            var state = Scenario01Progress.Create(definition); var items = new NativeInventory();
            foreach (var quest in content.rows)
            {
                Assert.IsFalse(TownQuestRules.TryApply(definition,state,items,quest,true,out _,out _));
                Assert.IsTrue(TownQuestRules.TryApply(definition,state,items,quest,false,out var accepted,out var funds));
                Assert.AreEqual(0,TownQuestRules.Stage(state,quest.id));
                state=accepted;items=funds;
                // Objective fixture. Production source EVT52/12 is separately
                // exercised end-to-end by TownQuestChecks, not reproduced here.
                state.dutySteps.Add(quest.id+":2");
                Assert.IsTrue(TownQuestRules.TryApply(definition,state,items,quest,true,out var claimed,out funds));
                Assert.AreEqual(2,TownQuestRules.Stage(state,quest.id));
                state=claimed;items=funds;
                Assert.IsFalse(TownQuestRules.TryApply(definition,state,items,quest,true,out _,out _));
            }
            Assert.AreEqual(20000,items.gold);Assert.AreEqual(1,items.Count(929));
            var folder=Path.Combine(Path.GetTempPath(),"ds-town-quest-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                var store=new NativeSaveStore(Path.Combine(folder,"town.json"));
                var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.ScenarioVersion,
                    mapScene="Kaosia_Map",runtimeScene="Town_Runtime",worldX=21,worldY=21,
                    hasCharacter=true,character=new NativeCharacterProfile {name="의뢰",jobId=100},
                    hasInventory=true,inventory=items,hasScenario01=true,scenario01=state,clearedScenarios=Array.Empty<string>()};
                Assert.IsTrue(store.TrySave(save,out _));Assert.IsTrue(store.TryLoad(out var loaded,out _));
                Assert.AreEqual(3,TownQuestRules.Stage(loaded.scenario01,1));
                Assert.AreEqual(3,TownQuestRules.Stage(loaded.scenario01,2));
                Assert.AreEqual(20000,loaded.inventory.gold);Assert.AreEqual(1,loaded.inventory.Count(929));
            }
            finally {Directory.Delete(folder,true);}
        }
    }
}
