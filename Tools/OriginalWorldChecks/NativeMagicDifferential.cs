using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeMagicDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public MagicMenuEntry metadata;public int source,x,y,mode,item=-1,token=-1;public int[] targets,packet;public int? localTeam,sourceTeam;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>
        {
            if(c.packet!=null)
            {
                if(!OriginalNativeMagic.TryNotice(c.packet.Select(i=>(byte)i).ToArray(),out var p))throw new Exception("Invalid fixture");
                bool call=OriginalNativeMagic.Dispatches(p,c.localTeam,c.sourceTeam)??throw new Exception("Unknown fixture team");
                return (object)new{call,magic=call?p.magic:0,source=call?p.source:0,x=call?p.x:0,y=call?p.y:0,targets=call?p.targets:Array.Empty<int>(),wordReads=4+p.targets.Length,byteReads=1};
            }
            return Convert.ToHexString(OriginalNativeMagic.Request(new NativeMagicPacket{item=c.item,itemToken=c.token,magic=c.metadata.id,source=c.source,x=c.x,y=c.y,mode=c.mode,signature=OriginalNativeMagic.Signature(c.metadata),targets=c.targets})).ToLowerInvariant();
        }).ToArray()));
    }
}
