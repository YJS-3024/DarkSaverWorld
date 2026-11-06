using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
internal static class NativeImmediateEventsDifferential
{
    public static void Main(string[] args)
    {
        using var input=JsonDocument.Parse(File.ReadAllText(args[0]));
        var rows=input.RootElement.EnumerateArray().SelectMany(p=>OriginalCampaignScript.Events(File.ReadAllText(p.GetString())).Select(e=>
        {
            bool supported=OriginalCampaignScript.TryImmediateChanges(e.Value,out var changes);
            return new{file=p.GetString(),number=e.Key,body=e.Value.Select(c=>c.raw).ToArray(),supported,
                changes=changes?.Select(c=>new{name=c.name,args=c.args.Select(a=>int.Parse(a)).ToArray()}).ToArray()};
        })).ToArray();
        File.WriteAllText(args[1],JsonSerializer.Serialize(rows));
    }
}
