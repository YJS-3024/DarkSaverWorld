using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Campaign;

// The native oracle supplies commands, ownership and clocks. No Unity or server
// trigger policy is simulated by this presentation-only comparison host.
internal static class NativeDirectedEventDifferential
{
    internal sealed class Case
    {
        public string key; public string[] body; public int subject,team,localTeam,randomValue;
        public Tick[] ticks;
    }
    internal sealed class Tick { public int elapsed;public bool dismiss,moving,resultVisible; }
    public static void Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};
        var input=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),json);
        var output=new List<object>();
        foreach(var c in input)
        {
            var packets=new List<byte[]>();var messages=new List<string>();var mutations=new List<string>();
            var profiles=new List<string>();var music=new List<string>();var localCalls=new List<object>();
            bool moving=false,resultVisible=false;
            var execution=new CampaignNativeExecution{subject=c.subject,team=c.team,localTeam=()=>c.localTeam,moving=_=>moving,resultVisible=()=>resultVisible};
            var p=new CampaignPresentation(string.Join("\n",c.body),100,26,execute:command=>
            {
                if(command.name=="FIRSTCLEAR"){profiles.Add("FIRSTCLEAR");return 1;}
                if(command.name=="SAVECLEAR"){profiles.Add("SAVECLEAR");return 1;}
                if(command.name=="RANDOM")return c.randomValue%101<command.Int(0)?1:0;
                if(command.name=="CHANGESPRITE"||command.name=="CHANGETILE2")mutations.Add(command.raw);
                if(command.name=="MAGIC")localCalls.Add(new{kind="CheckMagicTarget-no-target",args=command.args.Select(int.Parse).ToArray()});
                if(command.name=="KILLCHAR1"||command.name=="KILLCHAR2")localCalls.Add(new{kind=command.name=="KILLCHAR1"?"dmKillChar":"dmKillChar2",args=new[]{command.Int(0)}});
                var packet=OriginalCampaignScript.Packet(command);if(packet!=null)packets.Add(packet);
                return 1;
            },context:c.key,native:execution);
            p.Effect+=command=>music.Add(command.raw);
            var frames=new List<object>();
            foreach(var tick in c.ticks)
            {
                if(tick.dismiss)for(int i=0;i<400&&p.Snapshot().waitingMessage;i++)p.Continue();
                moving=tick.moving;resultVisible=tick.resultVisible;
                p.Advance(tick.elapsed);var v=p.Snapshot();
                frames.Add(new{line=v.nativeLine,active=v.completed?0:1,execution=v.nativeExecution,testResult=v.nativeTestResult,
                    waiting=v.waitingMessage,packets=packets.Select(b=>b.Select(v=>(int)v).ToArray()).ToArray(),mutations=mutations.ToArray(),profiles=profiles.ToArray(),localCalls=localCalls.ToArray(),music=music.ToArray()});
            }
            output.Add(new{c.key,supported=OriginalDirectedEvents.Supports(CampaignCommand.Parse(string.Join("\n",c.body)),c.subject),frames});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output,json));
    }
}
