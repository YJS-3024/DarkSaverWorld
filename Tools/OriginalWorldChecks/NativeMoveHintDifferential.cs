using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;
internal static class NativeMoveHintDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public int width,height,source=1,x,y,axis;public int[] attributes,terrain;public TrainingUnit[] units;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        var catalog=new OriginalCombatCatalog{version=3,jobs=new[]{new OriginalCombatJob{job=100,rawMovementType=0}}};
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>
        {
            var map=new TutorialMap{width=c.width,height=c.height,attributes=c.attributes,terrain=c.terrain};var hint=OriginalNativeMoveHints.Calculate(map,catalog,c.units,c.source,c.x,c.y,c.axis);
            return hint==null?(object)new{success=false}:new{success=true,axis=hint.axisAfter,points=hint.points.Select(p=>new[]{p.X,p.Y}).ToArray(),packet=Convert.ToHexString(OriginalNativeMoveHints.Packet(c.source,hint.points)).ToLowerInvariant()};
        }).ToArray()));
    }
}
