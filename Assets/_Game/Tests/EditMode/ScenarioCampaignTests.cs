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
        public void NativeScriptsReachClearWithConfirmedDeathFixturesAndActualJson(int number)
        {
            string key=number.ToString("00");var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/"+key+"/Content").text);
            Assert.IsTrue(definition.IsValid);var state=Scenario01Progress.Create(definition);var inventory=new NativeInventory();state.rng=73;
            void RunScript()
            {
                int count=0;
                while((state.phase==Scenario01Progress.Intro || state.phase==Scenario01Progress.Outro || state.activeEvent!=0) && count++<20000)
                {
                    var result=Scenario01Rules.Step(definition,state,inventory,100,out var next,out var items);
                    if(result==Scenario01Rules.StepResult.Waiting)
                    {if(Scenario01Rules.AdvanceDialogue(definition,state,out var page))state=page;else Scenario01Rules.AdvanceTime(state,1,6);}
                    else {state=next;inventory=items;}
                    state=JsonUtility.FromJson<Scenario01Progress>(JsonUtility.ToJson(state));Assert.IsTrue(state.Matches(definition));
                }
                Assert.Less(count,20000);
            }
            bool Pump(int x,int y)
            {
                if(!Scenario01Rules.TryActivate(definition,state,inventory,x,y,out var next,out var items))return false;
                state=next;inventory=items;RunScript();return true;
            }
            RunScript();Assert.AreEqual(Scenario01Progress.Playing,state.phase);
            int turns=0;
            while(state.phase!=Scenario01Progress.Complete && turns++<1000)
            {
                var unit=definition.units.Where(u=>u.wave==state.wave && !state.defeated.Contains(u.id)).OrderBy(u=>u.id).FirstOrDefault();if(unit==null)break;
                state.pendingBattle=unit.id;Assert.IsTrue(Scenario01Rules.RecordBattleReturn(definition,state,unit.id,true));
                for(int events=0;events<500 && state.phase!=Scenario01Progress.Complete;events++)if(!Pump(definition.entryX,definition.entryY))break;
            }
            if(state.phase!=Scenario01Progress.Complete)foreach(var c in definition.Event(99).conditions.Where(c=>c.name=="CHARPOS"))
            {if(state.phase==Scenario01Progress.Complete)break;Pump(c.Int(0),c.Int(1));}
            Assert.AreEqual(Scenario01Progress.Complete,state.phase);Assert.IsFalse(state.actors.Any(a=>a.partyProxy));
            var folder=Path.Combine(Path.GetTempPath(),"ds-scenario-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                var snapshot=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.CampaignVersion,mapScene="Kaosia_Map",runtimeScene="Town_Runtime",
                    hasCharacter=true,character=new NativeCharacterProfile {name="검증",jobId=100},hasInventory=true,inventory=inventory,campaign=new[]{state},clearedScenarios=new[]{key}};
                var store=new NativeSaveStore(Path.Combine(folder,"hero.json"));Assert.IsTrue(store.TrySave(snapshot,out _));Assert.IsTrue(store.TryLoad(out var loaded,out _));
                Assert.AreEqual(key,loaded.campaign[0].scenarioKey);Assert.AreEqual(Scenario01Progress.Complete,loaded.campaign[0].phase);
                Assert.IsFalse(Scenario01Rules.TryActivate(definition,loaded.campaign[0],loaded.inventory,definition.entryX,definition.entryY,out _,out _));
            }
            finally {Directory.Delete(folder,true);}
        }
        [Test]
        public void OlderScenarioCheckpointMayOmitNewOptionalLists()
        {
            var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/01/Content").text);
            var original=Scenario01Progress.Create(definition);
            string legacy=JsonUtility.ToJson(original).Replace("\"mapChanges\":[],","").Replace("\"unlockedGenerals\":[],","").Replace("\"deathSides\":[],","");
            var restored=JsonUtility.FromJson<Scenario01Progress>(legacy);Assert.IsTrue(restored.IsValid);Assert.IsTrue(restored.Clone().Matches(definition));
        }
    }
}
