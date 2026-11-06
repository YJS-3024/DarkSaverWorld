using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeMagicDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public MagicMenuEntry metadata;public int source,x,y,mode,item=-1,token=-1,width,height;public int[] targets,packet;public int? localTeam,sourceTeam;public bool levelEffect;public TrainingUnit[] units;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>
        {
            if(c.levelEffect)
            {
                if(!OriginalNativeMagic.TryLevelEffectNotice(c.packet.Select(i=>(byte)i).ToArray(),out int unit))throw new Exception("Invalid level fixture");
                var p=OriginalNativeMagic.LevelEffectRequest(unit,c.metadata,c.units,c.targets);
                var bytes=c.mode==0x200||c.mode==0x400?p==null?null:OriginalNativeMagic.Request(p):null;
                var actor=c.units.FirstOrDefault(u=>u.id==unit);var focus=actor==null?(NativeViewport?)null:NativeViewport.Centered(actor.x,actor.y,c.width,c.height);
                return (object)new{unit,packet=bytes==null?null:Convert.ToHexString(bytes).ToLowerInvariant(),focusLeft=focus?.left,focusTop=focus?.top};
            }
            if(c.packet!=null)
            {
                if(!OriginalNativeMagic.TryNotice(c.packet.Select(i=>(byte)i).ToArray(),out var p))throw new Exception("Invalid fixture");
                bool call=OriginalNativeMagic.Dispatches(p,c.localTeam,c.sourceTeam,c.packet[1])??throw new Exception("Unknown fixture team");
                return (object)new{call,magic=call?p.magic:0,source=call?p.source:0,x=call?p.x:0,y=call?p.y:0,targets=call?p.targets:Array.Empty<int>(),wordReads=4+p.targets.Length,byteReads=1};
            }
            return Convert.ToHexString(OriginalNativeMagic.Request(new NativeMagicPacket{item=c.item,itemToken=c.token,magic=c.metadata.id,source=c.source,x=c.x,y=c.y,mode=c.mode,signature=OriginalNativeMagic.Signature(c.metadata),targets=c.targets})).ToLowerInvariant();
        }).ToArray()));
    }
}
