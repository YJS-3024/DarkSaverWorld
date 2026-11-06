using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeCommanderPacketCorpus
{
    private sealed class Input {public string kind;public int magic,source,target,sourceTerrain,targetTerrain,x,y,mode;public int[] targets;}
    private sealed class Case {public MagicMenuEntry metadata;public Input[] inputs;}
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),options);
        var packets=cases.Select(c=>c.inputs.Select(i=>
        {
            byte[] bytes=i.kind=="magic"?OriginalNativeMagic.Request(new NativeMagicPacket{magic=i.magic,source=i.source,x=i.x,y=i.y,mode=i.mode,targets=i.targets,signature=OriginalNativeMagic.Signature(c.metadata)})
                :OriginalBattleRequests.Packet(new TrainingActionRequest{source=i.source,target=i.target,sourceTerrain=i.sourceTerrain,targetTerrain=i.targetTerrain,action=i.kind=="rest"?TrainingAction.Rest:TrainingAction.Attack});
            if(bytes==null)throw new Exception("Original AI send cannot be represented by the production codec");
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }).ToArray()).ToArray();
        File.WriteAllText(args[1],JsonSerializer.Serialize(packets));
    }
}
