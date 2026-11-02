using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;
internal static class NativeMovementPathDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public int width,height,source=1,x,y,axis;public int[] attributes,terrain;public TrainingUnit[] units;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);var catalog=new OriginalCombatCatalog{version=3,jobs=new[]{new OriginalCombatJob{job=100,rawMovementType=0}}};
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>
        {
            var actor=c.units.Single(u=>u.id==c.source);var map=new TutorialMap{width=c.width,height=c.height,attributes=c.attributes,terrain=c.terrain};
            var path=OriginalNativeMovement.Path(map,catalog,c.units,c.source,actor.Tile,new TutorialTile(c.x,c.y),c.axis);
            return path==null?(object)new{success=false}:new{success=true,axis=path.axisAfter,steps=path.cardinalSteps,directions=path.directions};
        }).ToArray()));
    }
}
