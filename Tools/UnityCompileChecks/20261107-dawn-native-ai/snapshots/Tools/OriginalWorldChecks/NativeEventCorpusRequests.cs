using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;

internal static class NativeEventCorpusRequests
{
    public static void Main(string[] args)
    {
        // Reuse the existing production Events parser harness unchanged.
        NativeImmediateEventsDifferential.Main(args);
        using var bodies=JsonDocument.Parse(File.ReadAllText(args[1]));
        var packets=bodies.RootElement.EnumerateArray()
            .SelectMany(body=>body.GetProperty("body").EnumerateArray().Select(line=>line.GetString()))
            .Distinct().Select(line=>new{line,packet=OriginalCampaignScript.Packet(CampaignCommand.Parse(line).Single())})
            .Where(row=>row.packet!=null).ToDictionary(row=>row.line,row=>row.packet.Select(value=>(int)value).ToArray());
        File.WriteAllText(args[1]+".requests.json",JsonSerializer.Serialize(packets));
    }
}
