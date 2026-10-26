using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Jobs;
using DarkSaver.Combat;

internal static class ScenarioRewardChecks
{
    public static void Run(Dictionary<int,SinglePlayerAbilityRule> rules,string fixture,string sourceFixture,string output)
    {
        int checks=0;
        void Check(bool ok,string name){if(!ok)throw new Exception("Scenario reward: "+name);checks++;}
        using var data=JsonDocument.Parse(File.ReadAllText(fixture));
        var entries=data.RootElement.EnumerateArray().ToDictionary(r=>r.GetProperty("id").GetInt32(),r=>r);
        JobCatalogEntry Job(int id)
        {
            if(!entries.TryGetValue(id,out var r))return null;
            var values=new long[JobCatalogEntry.NumericColumnCount];values[0]=values[1]=id;values[22]=r.GetProperty("family").GetInt32();
            foreach(var field in new[]{(2,"hp"),(4,"mp"),(6,"attack"),(8,"defense"),(10,"magicAttack"),(12,"magicDefense")})
                values[field.Item1]=r.GetProperty(field.Item2).GetInt32();
            return new JobCatalogEntry(values,"0",r.GetProperty("name").GetString(),"source class-entry fixture","player_job");
        }
        bool Abilities(int id,out int[] spells,out int[] skills)
        {
            spells=skills=Array.Empty<int>();if(!entries.TryGetValue(id,out var row))return false;
            spells=row.GetProperty("learnedMagic").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
            skills=row.GetProperty("usableSkills").EnumerateArray().Select(x=>x.GetInt32()).ToArray();return true;
        }
        bool Grant(NativeOwnedRoster current,int primary,int[] receipts,int[] ids,out NativeOwnedRoster next,out int[] claimed)
            =>ScenarioGeneralRewards.TryBuild(current,primary,receipts,ids,Job,Abilities,rules.ContainsKey,out next,out claimed,out _);
        using var sourceData=JsonDocument.Parse(File.ReadAllText(sourceFixture));
        var sourceScenes=sourceData.RootElement.GetProperty("scenarios").EnumerateArray().ToArray();
        var ids=sourceScenes.SelectMany(r=>r.GetProperty("jobs").EnumerateArray().Select(x=>x.GetInt32())).Distinct().OrderBy(x=>x).ToArray();
        foreach(var source in sourceScenes)
        {
            string key=source.GetProperty("scenario").GetString();
            int[] jobs=source.GetProperty("jobs").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
            Check(jobs.Length<=1 && ScenarioGeneralRewards.JobForScenario(key)==(jobs.Length==0?0:jobs[0]),"mapping matches original files "+key);
        }
        var inventory=new NativeInventory {gold=12345};
        int[] itemIds=sourceData.RootElement.GetProperty("clearItemIds").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
        foreach(int itemId in itemIds)
        {
            DarkSaver.Events.Scenario01Rules.Grant(inventory,itemId);
            Check(inventory.Count(itemId)==1 && inventory.IsValid && inventory.gold==12345,"common item grant handles original clear item "+itemId);
        }
        Check(ScenarioGeneralRewards.JobForScenario("10")==0,"scenario10 has no character reward");
        Check(ScenarioGeneralRewards.JobForScenario("01")==0 && ScenarioGeneralRewards.JobForScenario("17")==0,"item-only/no-general scenarios do not fabricate characters");
        Check(Grant(null,0,Array.Empty<int>(),ids,out var roster,out var receipts),"all source generals grant");
        Check(roster.IsValid && roster.units.Length==10 && receipts.Length==10,"ten unique owned generals and receipts");
        Check(roster.units.Select(u=>u.instanceId).Distinct().Count()==10 && roster.units.All(u=>u.instanceId%10==0 && u.instanceId!=100),"unique commander IDs");
        foreach(var unit in roster.units)
        {
            var source=entries[unit.jobId];Abilities(unit.jobId,out var spells,out var skills);
            Check(unit.hp==source.GetProperty("hp").GetInt32() && unit.mp==source.GetProperty("mp").GetInt32(),"native vitals "+unit.jobId);
            Check(unit.learnedMagicIds.Select(x=>(int)x).SequenceEqual(spells),"native magic ownership "+unit.jobId);
            Check(unit.usableSkillIds.Select(x=>(int)x).SequenceEqual(skills),"native skill ownership "+unit.jobId);
            var draft=roster.CreatePreparationDrafts().Single(d=>d.Unit.JobId==unit.jobId);
            Check(draft.LearnedMagicIds.SequenceEqual(spells.Concat(skills.Where(x=>x<20000)).Distinct().Select(x=>(short)x)),"combat menus include specials, exclude hiring "+unit.jobId);
            Check(draft.IsOwned && draft.FinalStateConfirmed && draft.RecordEligible==true,"deployable general "+unit.jobId);
        }
        Check(Grant(roster,0,receipts,ids.Concat(ids).ToArray(),out var repeated,out var sameReceipts),"repeat accepted without duplicate grants");
        Check(repeated.units.Length==10 && sameReceipts.SequenceEqual(receipts),"receipts prevent duplicate roster entries");
        repeated.units[0].hp=0;Check(roster.units[0].hp>0,"staged roster is a deep copy");
        Check(Grant(null,100,Array.Empty<int>(),new[]{100},out var primarySkip,out var primaryReceipts) && primarySkip==null && primaryReceipts.SequenceEqual(new[]{100}),"already owned primary class is receipted without duplicate");
        Check(Grant(null,105,primaryReceipts,new[]{100},out primarySkip,out _) && primarySkip==null,"later class changes do not replay claimed grant");
        Check(Grant(null,105,Array.Empty<int>(),new[]{100},out var promotedSkip,out var promotedReceipts)
            && promotedSkip==null && promotedReceipts.SequenceEqual(new[]{100}),"promoted primary infantry does not receive another fighter");
        var promotedOwned=roster.Clone();promotedOwned.units.Single(u=>u.jobId==102).jobId=110;
        Check(Grant(promotedOwned,0,Array.Empty<int>(),new[]{102},out var sameFamily,out _)
            && sameFamily.units.Length==10 && sameFamily.units.Count(u=>u.jobId==102)==0,"promoted owned cleric does not receive a duplicate family");
        var dead=roster.Clone();dead.units[0].hp=0;dead.units[0].lifeState=(int)UnitLifeState.Removed;
        Check(Grant(dead,0,Array.Empty<int>(),new[]{dead.units[0].jobId},out var keptDead,out var skipReceipts) && keptDead.units.Length==10 && keptDead.units[0].hp==0,"owned dead general is not replaced or revived");
        Check(!Grant(roster,0,new[]{102,102},ids,out _,out _),"duplicate receipt is rejected");
        Check(!Grant(roster,0,receipts,new[]{999},out _,out _),"unknown job rejected");
        Check(!ScenarioGeneralRewards.TryBuild(null,0,Array.Empty<int>(),new[]{102},Job,Abilities,_=>false,out _,out _,out _),"missing ability prevents grant");
        Check(!ScenarioGeneralRewards.TryBuild(null,0,Array.Empty<int>(),new[]{102},_=>null,Abilities,rules.ContainsKey,out _,out _,out _),"missing job prevents grant");
        var options=new JsonSerializerOptions {IncludeFields=true,IgnoreReadOnlyProperties=true};
        string json=JsonSerializer.Serialize(roster,options);var loaded=JsonSerializer.Deserialize<NativeOwnedRoster>(json,options);
        Check(loaded.IsValid && loaded.units[2].usableSkillIds.SequenceEqual(roster.units[2].usableSkillIds),"ownership/magic/skills survive field serialization");
        Check(Grant(loaded,0,receipts,ids,out var afterLoad,out _) && afterLoad.units.Length==10,"serialized ownership does not duplicate grants");
        File.WriteAllText(output,JsonSerializer.Serialize(new {checks,generalJobs=ids,sourceArchives=sourceScenes.Length,clearItemIds=itemIds,unityPlay=false,scope="Actual production ScenarioGeneralRewards/NativeOwnedRoster with native class-entry fixtures; no Unity import, disk transaction or scene playback"},new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"PASS {checks} production scenario-general reward checks; ten native classes, source magic/skills, duplicate receipts and ownership roundtrip");
    }
}
