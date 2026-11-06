using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
internal static class NativeEndStageDifferential
{
    public static void Main(string[] args)
    {
        using var input=JsonDocument.Parse(File.ReadAllText(args[0]));
        var rows=input.RootElement.EnumerateArray().Select(c=>
        {
            byte flag=c.GetProperty("flag").GetByte();int outcome=c.GetProperty("outcome").GetInt32();var packet=OriginalScenarioEnd.Packet(flag);
            if(!OriginalScenarioEnd.TryDecode(packet,out var decoded)||decoded!=flag)throw new Exception("61 lossless BYTE codec");
            foreach(int n in new[]{0,1,2,4,5})if(OriginalScenarioEnd.TryDecode(packet.Concat(new byte[5]).Take(n).ToArray(),out _))throw new Exception("61 exact packet length");
            return OriginalScenarioEnd.Stage(outcome,decoded);
        }).ToArray();
        File.WriteAllText(args[1],JsonSerializer.Serialize(rows));
    }
}
