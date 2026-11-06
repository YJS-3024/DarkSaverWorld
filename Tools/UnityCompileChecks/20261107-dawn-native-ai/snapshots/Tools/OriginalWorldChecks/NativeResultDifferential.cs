using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Campaign;
internal static class NativeResultDifferential
{
    public sealed class Case{public int[] packet;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),new JsonSerializerOptions{IncludeFields=true});var output=new List<object>();
        foreach(var c in cases)
        {
            var bytes=c.packet.Select(i=>(byte)i).ToArray();if(!OriginalResultPackets.TryDecode(bytes,out var report))throw new Exception("result fixture decode");
            output.Add(new{report=new{report.outcome,report.eventTotal,report.eventCompleted,report.spoils,rows=report.rows.Select(r=>new{r.words,r.experience}).ToArray()},
                icons=OriginalResultPackets.Icons(report,item=>item).Select(f=>new{f.kind,id=f.kind=="body"?f.id:0,f.x,f.y,f.sourceX,f.sourceY,f.width,f.height}).ToArray(),
                texts=OriginalResultPackets.Texts(report).Select(t=>new{t.x,t.y,t.text,t.color}).ToArray(),roundtrip=OriginalResultPackets.Packet(report).SequenceEqual(bytes)});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
