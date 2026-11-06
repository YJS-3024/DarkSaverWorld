using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;
internal sealed class PositionCase
{
    public string name;public int width,height,member,hint,cursor,centerX,centerY,radius,mode;
    public int[] random,attributes,terrain;public TrainingUnit[] units;
}
internal static class NativeAiPositionDifferential
{
    public static void Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<PositionCase[]>(File.ReadAllText(args[0]),json);
        var results=cases.Select(c=>
        {
            var map=new WorldMapDefinition{map=new TutorialMap{width=c.width,height=c.height,attributes=c.attributes,terrain=c.terrain},npcs=Array.Empty<WorldNpc>(),portals=Array.Empty<WorldPortal>()};
            int randomCalls=0;var p=OriginalSoldierMovement.FindPosition(map,null,c.units,c.member,c.centerX,c.centerY,c.radius,c.mode,c.hint,count=>c.random[randomCalls++%c.random.Length]%count,false,c.cursor);
            return new{c.name,p.x,p.y,p.cursor,p.restHint,randomCalls,p.unavailable};
        }).ToArray();File.WriteAllText(args[1],JsonSerializer.Serialize(results,json));
    }
}
