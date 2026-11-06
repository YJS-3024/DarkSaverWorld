using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;
internal sealed class TargetCase
{
    public string name;public int family,source,radius;public int[] random,permitted;public TrainingUnit[] units;
}
internal static class NativeAiTargetDifferential
{
    public static void Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<TargetCase[]>(File.ReadAllText(args[0]),json);
        var result=cases.Select(c=>
        {
            var terrain=new int[810];for(int i=2;i<810;i+=3)terrain[i]=1;
            var map=new WorldMapDefinition{map=new TutorialMap{width=12,height=12,attributes=new int[144],terrain=terrain},npcs=Array.Empty<WorldNpc>(),portals=Array.Empty<WorldPortal>()};
            var actor=c.units.Single(u=>u.id==c.source).Copy();actor.attackRange=c.radius;actor.movementType=c.family==1?0:5;actor.attackTargets=c.permitted;
            var candidates=OriginalSoldierActions.Candidates(map,null,c.units,actor,false);int draws=0,target=-1;
            if(candidates?.Length>0){target=candidates[c.random[draws++%c.random.Length]%candidates.Length].id;}
            return new{c.name,target,draws,valid=target>=0?1:0};
        }).ToArray();File.WriteAllText(args[1],JsonSerializer.Serialize(result,json));
    }
}
