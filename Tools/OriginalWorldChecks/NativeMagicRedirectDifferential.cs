using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeMagicRedirectDifferential
{
    private sealed class Case {public MagicMenuEntry metadata;public TrainingUnit[] units;public int[] eligible,receivedTeams;public int source,x,y;}
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),options);
        var output=cases.Select(c=>{
            if(c.receivedTeams!=null)
            {
                if(c.receivedTeams.Length!=c.units.Length)throw new Exception("team fixture mismatch");
                int? Team(int id){int i=Array.FindIndex(c.units,u=>u.id==id);return i<0?(int?)null:c.receivedTeams[i];}
                var known=c.units.Select(u=>(id:u.id,allowed:OriginalNativeRelations.MagicEligible(c.metadata.team,c.source,u.id,Team,0))).ToArray();
                if(known.Any(t=>!t.allowed.HasValue))throw new Exception("unexpected unresolved relation");
                c.eligible=known.Where(t=>t.allowed==true).Select(t=>t.id).ToArray();
            }
            var cast=OriginalMagicRedirect.Select(c.metadata,c.source,c.x,c.y,c.units.FirstOrDefault(u=>u.id==c.source),c.units,c.eligible);
            if(cast==null)return(object)new{valid=false};
            return new{valid=true,magic=cast.magic,source=cast.source,x=cast.x,y=cast.y,targets=cast.targets};
        }).ToArray();File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
