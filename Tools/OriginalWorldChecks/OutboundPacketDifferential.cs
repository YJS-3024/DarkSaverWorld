using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class OutboundPacketDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public string operation;public int source,target,sourceTerrain,targetTerrain;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>Convert.ToHexString(new TrainingActionRequest{action=c.operation=="rest"?TrainingAction.Rest:TrainingAction.Attack,source=c.source,target=c.target,sourceTerrain=c.sourceTerrain,targetTerrain=c.targetTerrain}.NativePacket).ToLowerInvariant()).ToArray()));
    }
}
