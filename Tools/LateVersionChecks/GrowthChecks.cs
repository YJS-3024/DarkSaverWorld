using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver;
using DarkSaver.Core;
using DarkSaver.Combat;
using DarkSaver.Magic;
class GrowthChecks {
 static int checks;
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
 static NativeCharacterProfile Profile(int job,int level)=>new NativeCharacterProfile {name="후기검증",jobId=job,hasLevels=true,baseLevel=600,jobLevel=level,
  hasClassAbilities=true,learnedMagicIds=new[]{101},usableSkillIds=Array.Empty<int>(),unspentClassChangeStatPoints=3};
 static void Main(string[] args) {
  var options=new JsonSerializerOptions {IncludeFields=true};
  var rows=JsonSerializer.Deserialize<ExperienceLevelEntry[]>(File.ReadAllText(args[0]),options);
  var table=new ExperienceProgressionTable();table.Initialize(rows);Check(table.Count==3177,"complete later growth data");
  using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));
  var magic=new MagicCatalog();magic.Initialize(doc.RootElement.GetProperty("entries").EnumerateArray().Select(r=>new MagicCatalogEntry(r.GetProperty("numericValues").EnumerateArray().Select(x=>x.GetInt64()).ToArray(),r.GetProperty("magicName").GetString())).ToArray());
  foreach(int starter in new[]{100,101,103}) {
   Check(NativeCharacterProfile.TryQueue("초기직업",starter,Path.Combine(Path.GetTempPath(),"ds-start-"+starter+".json"),out _),"all three native starter queues");
   Check(NativeCharacterProfile.TakePending(out var profile,out _) && profile.IsValid && profile.jobId==starter && profile.tutorial.step==0,"selected class and onboarding preserved");
   Check(table.TryGetClassEntryAbilities(starter,out var startMagic,out var startSkills),"starter first-row exists");
   Check(magic.TryMergeLearnedMagic(Array.Empty<int>(),startMagic,out var learned),"starter first-row source spells resolve");
   profile.hasClassAbilities=true;profile.learnedMagicIds=learned;profile.usableSkillIds=startSkills;
   Check(profile.IsValid,"starter profile with source spells validates");
  }
  Check(!NativeCharacterProfile.TryQueue("불가",102,Path.Combine(Path.GetTempPath(),"ds-invalid-starter.json"),out _),"cleric remains recruit reward, not a starting choice");
  Check(!NativeCharacterProfile.TakePending(out _,out _),"rejected creation clears pending choice");
  foreach(var pair in new[]{(167,170),(168,171),(169,172)}) {
   int before=pair.Item1,next=pair.Item2;Check(!table.TryGetEligibleClassChange(before,99,out _),"not before level100");
   Check(table.TryGetEligibleClassChange(before,100,out var edge)&&edge.nextJobId==next,"source edge");
   var old=Profile(before,100);Check(old.TryBuildEligibleClassChange(table,magic,out var changed),"class change candidate");
   Check(old.jobId==before&&old.jobLevel==100&&old.unspentClassChangeStatPoints==3,"candidate did not mutate source");
   Check(changed.jobId==next&&changed.jobLevel==1&&changed.baseLevel==600,"new class first level/base level preserved");
   Check(changed.unspentClassChangeStatPoints==3+GameConstants.ClassChangeAdditionalStatPoints,"confirmed point policy");
   Check(changed.learnedMagicIds.Contains(101),"previous learned spell preserved");
   Check(table.TryGet(next,299,out var row),"level299 source");
   changed.jobLevel=299;
   Check(changed.TryAdvanceLevels(table,(uint)row.requiredXp-1,out var waiting,out var remaining)&&waiting.jobLevel==299&&remaining==(uint)row.requiredXp-1,"insufficient xp preserved");
   Check(changed.TryAdvanceLevels(table,(uint)row.requiredXp+1,out var advanced,out remaining)&&advanced.jobLevel==300&&remaining==1,"exact source cost and cap300");
   Check(changed.jobLevel==299,"growth did not mutate source");
   Check(advanced.TryAdvanceLevels(table,uint.MaxValue,out var capped,out remaining)&&capped.jobLevel==300&&remaining==uint.MaxValue,"cap spends no more xp");
   var restored=JsonSerializer.Deserialize<NativeCharacterProfile>(JsonSerializer.Serialize(advanced,options),options);
   Check(restored.IsValid&&restored.jobLevel==300&&restored.jobId==next&&restored.usableSkillIds.SequenceEqual(advanced.usableSkillIds),"managed serialization roundtrip; not Unity JsonUtility");
   restored.jobLevel=301;Check(!restored.IsValid,"301 invalid");
   old.jobLevel=101;Check(!old.IsValid&&!table.TryGetEligibleClassChange(before,101,out _),"legacy cap unchanged");
  }
  var report=new {checks,passed=true,unityPlay=false,scope="Actual production profile, inventory, magic and progression sources with managed Unity attribute/base-class stubs; .NET JSON roundtrip, no Unity import/JsonUtility or save IO"};
  File.WriteAllText(args[2],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS {checks} actual late class-change and growth checks");
 }
}
