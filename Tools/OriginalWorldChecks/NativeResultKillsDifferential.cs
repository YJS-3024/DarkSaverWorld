using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using DarkSaver.Original.Campaign;
internal static class NativeResultKillsDifferential
{
    public sealed class Delivery {public int step;public int[] jobs;}
    public sealed class Case {public string name;public int[] jobs;public Dictionary<int,int> pictures;public int steps;public Delivery[] deliveries;}
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),options);var result=new List<object>();
        foreach(var c in cases)
        {
            var state=new NativeResultKillAnimation();var report=new NativeResultKills{jobs=c.jobs};var packet=OriginalResultKillPackets.Packet(report);
            if(!OriginalResultKillPackets.TryDecode(packet,out var decoded))throw new Exception("valid31 decode");state.Receive(decoded);var frames=new List<object>();
            int? Picture(int job)=>c.pictures!=null&&c.pictures.TryGetValue(job,out var picture)?picture<0?(int?)null:picture:job;
            for(int step=0;step<c.steps;step++)
            {
                foreach(var delivery in c.deliveries??Array.Empty<Delivery>())if(delivery.step==step)state.Receive(new NativeResultKills{jobs=delivery.jobs});
                var frame=state.Step(Picture);
                frames.Add(new{remaining=state.Remaining,page=state.PageStart,counter=state.Counter,
                    icons=frame.icons.Select(i=>new{i.kind,id=i.kind=="body"?Picture(i.id).Value:0,i.x,i.y,i.sourceX,i.sourceY,i.width,i.height}).ToArray(),
                    texts=frame.text==null?Array.Empty<object>():new object[]{new{x=81,y=215,text=frame.text,color=0x101010}}});
            }
            result.Add(new{jobs=decoded.jobs,roundtrip=OriginalResultKillPackets.Packet(decoded).SequenceEqual(packet),frames});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(result));
    }
}
