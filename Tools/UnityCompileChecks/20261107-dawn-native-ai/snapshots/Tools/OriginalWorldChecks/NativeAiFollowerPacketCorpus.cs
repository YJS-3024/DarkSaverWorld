using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeAiFollowerPacketCorpus
{
    private sealed class Input {public string kind;public int source,target,sourceTerrain,targetTerrain;public int[][] points;}
    private sealed class Case {public Input[] inputs;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),new JsonSerializerOptions{IncludeFields=true});
        var packets=cases.Select(c=>c.inputs.Select(i=>
        {
            byte[] bytes=i.kind=="move"?OriginalNativeMoveHints.Packet(i.source,i.points.Select(p=>new DarkSaver.Original.Tutorial.TutorialTile(p[0],p[1])).ToArray())
                :OriginalBattleRequests.Packet(new TrainingActionRequest{source=i.source,target=i.target,sourceTerrain=i.sourceTerrain,targetTerrain=i.targetTerrain,action=i.kind=="rest"?TrainingAction.Rest:TrainingAction.Attack});
            if(bytes==null)throw new Exception("Original follower input cannot be represented by the production codec");
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }).ToArray()).ToArray();
        File.WriteAllText(args[1],JsonSerializer.Serialize(packets));
    }
}
