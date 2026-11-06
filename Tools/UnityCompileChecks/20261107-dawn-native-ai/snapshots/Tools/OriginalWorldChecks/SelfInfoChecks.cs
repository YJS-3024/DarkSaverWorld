using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Runtime.Serialization.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class SelfInfoChecks
{
 static int checks;static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static void Main(string[] args)
 {
  using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));foreach(var row in doc.RootElement.GetProperty("cases").EnumerateArray())
  {
   var bytes=Convert.FromHexString(row.GetProperty("record").GetString());var raw=NativeSelfInformation.FromMemoryRecord(bytes);var levels=NativeSelfInformation.LevelsFromMemoryRecord(bytes);var xp=NativeSelfInformation.ExperienceFromMemoryRecord(bytes);
   var profile=new CommanderProfile{growth=new CharacterGrowthRecord{stats=new CharacterGrowthStats{nativeValues=raw},nativeLevels=levels,experience=xp[0],nextExperience=xp[1]}};var actual=NativeSelfInformation.Values(profile);int x=row.GetProperty("x").GetInt32(),y=row.GetProperty("y").GetInt32(),index=0;
   foreach(var expected in row.GetProperty("expected").EnumerateArray())
   {Check(actual[index]==expected.GetProperty("text").GetString(),"original signed general-record numeric field");var rect=expected.GetProperty("rect");Check(NativeSelfInformation.X(index)+x==rect[0].GetInt32()&&NativeSelfInformation.Y(index)+y==rect[1].GetInt32()&&NativeSelfInformation.Width(index)==rect[2].GetInt32()-rect[0].GetInt32()&&rect[3].GetInt32()-rect[1].GetInt32()==14,"original16 DrawText rectangles and order");index++;}
   Check(NativeSelfInformation.Level(profile)==levels[0]+"("+levels[1]+")","explicit raw level pair preserves native order");Check(NativeSelfInformation.Experience(profile)==xp[0]+"/"+xp[1],"native DWORD experience pair stays unsigned");
  }
  int gender=0;foreach(var literal in doc.RootElement.GetProperty("genders").EnumerateArray())Check(NativeSelfInformation.Gender(gender++)==literal.GetString(),"original shared-header gender literals0/1/2");Check(NativeSelfInformation.Gender(null)=="—","unreceived gender is not assumed male/female/neutral");
  var known=new CommanderProfile{level=2,healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,growth=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=2,revision=1,lastNotice=Guid.NewGuid().ToString("N"),magic=new[]{101},skills=Array.Empty<int>(),stats=new CharacterGrowthStats{attack=65,defense=20,magicAttack=8,magicDefense=6},experience=123}};
  var values=NativeSelfInformation.Values(known);Check(values[0]=="200"&&values[1]=="45"&&values[2]=="65"&&values[3]=="20"&&values[6]=="8"&&values[7]=="6","known totals are not wounded current HP/MP");
  Check(values[4]=="—"&&values[5]=="—"&&values.Skip(8).All(v=>v=="—"),"unreceived movement/action/ratings never become invented zeroes");Check(NativeSelfInformation.Level(known)=="2"&&NativeSelfInformation.Experience(known)=="123/—","missing raw level pair/XP target remain unguessed");
  known.growth.nativeLevels=new short[]{1,2};known.growth.stats.nativeValues=Enumerable.Range(0,16).Select(i=>(short)(i-8)).ToArray();known.growth.nextExperience=uint.MaxValue;
  var detached=known.growth.Copy();detached.nativeLevels[0]=9;detached.stats.nativeValues[0]=99;Check(known.growth.nativeLevels[0]==1&&known.growth.stats.nativeValues[0]==-8,"growth view/notice deep copies both native arrays");
  Check(CharacterGrowthRecord.Valid(known.growth),"optional exact native presentation fits existing growth record");var bad=known.growth.Copy();bad.nativeLevels=new short[1];Check(!CharacterGrowthRecord.Valid(bad),"malformed native level pair rejected");bad=known.growth.Copy();bad.stats.nativeValues=new short[15];Check(!CharacterGrowthRecord.Valid(bad),"malformed native16 fields rejected");
  using(var stream=new MemoryStream())
  {var serializer=new DataContractJsonSerializer(typeof(CommanderProfile));serializer.WriteObject(stream,known);stream.Position=0;var restored=(CommanderProfile)serializer.ReadObject(stream);Check(restored.growth.nativeLevels.SequenceEqual(known.growth.nativeLevels)&&restored.growth.stats.nativeValues.SequenceEqual(known.growth.stats.nativeValues)&&restored.growth.nextExperience==uint.MaxValue,"actual DataContract roundtrip of exact source fields");}
  foreach(int size in new[]{0,731,733})Check(NativeSelfInformation.FromMemoryRecord(new byte[size])==null&&NativeSelfInformation.LevelsFromMemoryRecord(new byte[size])==null&&NativeSelfInformation.ExperienceFromMemoryRecord(new byte[size])==null,"memory-record adapter requires exact source size; no wire layout assumption");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,mismatches=0,unityPlay=false}));
 }
}
