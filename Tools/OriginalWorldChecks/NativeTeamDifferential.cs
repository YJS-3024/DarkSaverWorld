using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeTeamDifferential
{
    public sealed class Case{public int[] packet,teams;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),new JsonSerializerOptions{IncludeFields=true});var output=new List<object>();
        foreach(var c in cases)
        {
            var bytes=c.packet.Select(i=>(byte)i).ToArray();if(!OriginalBattleTeamNames.TryDecode(bytes,out var rows))throw new Exception("Team fixture decode");
            var unit=new NativeUnitState{fields=new int[44],className=Array.Empty<byte>(),name=System.Text.Encoding.ASCII.GetBytes("Commander")};
            var names=new List<string>();foreach(int team in c.teams){unit.fields[24]=team;names.Add(Convert.ToHexString(OriginalBattleTeamNames.DisplayName(unit,rows).TakeWhile(b=>b!=0).ToArray()));}
            output.Add(new{rows=rows.Select(r=>new{team=r.team,name=Convert.ToHexString(r.name.TakeWhile(b=>b!=0).ToArray())}).ToArray(),names,roundtrip=OriginalBattleTeamNames.Packet(rows).SequenceEqual(bytes)});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
