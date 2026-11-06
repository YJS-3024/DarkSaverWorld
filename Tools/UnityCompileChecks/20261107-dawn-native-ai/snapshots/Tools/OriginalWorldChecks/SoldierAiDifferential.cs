using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;

internal sealed class AiCase
{
    public string name;public int width,height,member=1,hint;public double clock;public int[] random,attributes,terrain;public TrainingUnit[] units;
}
internal static class SoldierAiDifferential
{
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<AiCase[]>(File.ReadAllText(args[0]),options);
        var catalog=new OriginalCombatCatalog{version=3,jobs=new[]{new OriginalCombatJob{job=100,rawMovementType=0},new OriginalCombatJob{job=302,rawMovementType=0}}};
        var results=cases.Select(c=>
        {
            var map=new WorldMapDefinition{map=new TutorialMap{width=c.width,height=c.height,attributes=c.attributes,terrain=c.terrain},npcs=Array.Empty<WorldNpc>(),portals=Array.Empty<WorldPortal>()};
            int randomCalls=0;var plan=OriginalSoldierActions.Plan(map,catalog,c.units,c.member,c.hint,count=>c.random[randomCalls++%c.random.Length]%count,c.clock);
            var source=c.units.Single(u=>u.id==c.member);
            object[] packets=plan.action==TrainingAction.SoldierMove?new object[]{new{kind="move",source=c.member,x=plan.movement.x,y=plan.movement.y}}:
                plan.action==TrainingAction.Attack?new object[]{new{kind="attack",source=c.member,target=plan.target,sourceTerrain=map.map.Attribute(source.Tile),targetTerrain=map.map.Attribute(c.units.Single(u=>u.id==plan.target).Tile)}}:
                plan.action==TrainingAction.Rest?new object[]{new{kind="rest",source=c.member}}:Array.Empty<object>();
            return new{c.name,packets,state=plan.unavailable?source.nativeState.Value:plan.stateAfter,hint=plan.hintAfter,ap=plan.action==TrainingAction.Attack||plan.action==TrainingAction.Rest?0:source.ap,randomCalls,unavailable=plan.unavailable};
        }).ToArray();File.WriteAllText(args[1],JsonSerializer.Serialize(results,options));
    }
}
