using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
internal static class NativeCampaignMessageDifferential
{
    public sealed class Case {public string command,ownName;public string[] names;public int[] jobs;}
    public static void Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};
        var rows=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),json);
        var output=rows.Select(r=>
        {
            bool ok=OriginalCampaignMessage.TryResolve(new CampaignCommand("MESSAGE "+r.command,1),i=>r.names[i],i=>r.jobs[i],r.ownName,out var message);
            return new{ok,text=message?.text,face=message?.face};
        }).ToArray();
        File.WriteAllText(args[1],JsonSerializer.Serialize(output,json));
    }
}
