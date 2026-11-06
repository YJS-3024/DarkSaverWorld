using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;
internal static class NativeVictoryDifferential
{
    internal sealed class TestCase {public string key;public string[] body;public Tick[] ticks;}
    internal sealed class Tick {public string op,name;public int ignored,elapsed;}
    public static void Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};var output=new List<object>();
        foreach(var c in JsonSerializer.Deserialize<TestCase[]>(File.ReadAllText(args[0]),json))
        {
            var d=new NativeCampaignDialogue();var sent=new List<int[]>();
            var p=new CampaignPresentation(string.Join("\n",c.body),100,26,execute:command=>{var b=OriginalCampaignScript.Packet(command);if(b!=null)sent.Add(b.Select(x=>(int)x).ToArray());return 1;},
                native:new CampaignNativeExecution{subject=0,team=0,localTeam=()=>0,dialogue=d});
            var frames=new List<object>();
            foreach(var t in c.ticks)
            {
                if(t.op=="notice")
                {
                    var b=OriginalVictoryAnnouncement.Packet((byte)t.ignored,Encoding.UTF8.GetBytes(t.name));
                    if(!OriginalVictoryAnnouncement.TryDecode(b,out _,out var raw))throw new Exception("codec");
                    if(!OriginalVictoryAnnouncement.SameName(raw,Encoding.ASCII.GetBytes("Local")))d.Show(Encoding.UTF8.GetString(OriginalVictoryAnnouncement.CString(raw))+"님이 승리하셨습니다.",0);
                }
                if(t.op=="click")p.Continue();
                if(t.op=="frame"){d.Advance(t.elapsed);p.Advance(t.elapsed);}
                var v=p.Snapshot();var m=d.Snapshot();
                frames.Add(new{line=v.nativeLine,active=v.completed?0:1,visible=m.visible,first=m.firstLine,waiting=v.waitingMessage,mode=m.mode,packets=sent.ToArray()});
            }
            output.Add(new{c.key,frames});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output,json));
    }
}
