using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;
internal static class ScenarioSuffixEntry
{
    public static void Main(string[] args)
    {
        if(args.Length>1&&args[1]=="decode"){Console.WriteLine(JsonSerializer.Serialize(Enumerable.Range(0,256).Select(v=>OriginalScenarioScriptSuffix.Decode((byte)v).Select(c=>(int)c).ToArray())));return;}
        ScenarioProgressChecks.RunSuffixChecks(args[0]);
    }
}
internal static partial class ScenarioProgressChecks
{
    static NativeBattleNotice SuffixNotice(Fixture f,byte value)=>new NativeBattleNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,
        expectedRevision=f.View.battle?.revision??0,packet=OriginalScenarioScriptSuffix.Packet(value)};
    public static void RunSuffixChecks(string path)
    {
        root=path;int sourceVariants=0;
        for(int value=0;value<256;value++)
        {
            Check(OriginalScenarioScriptSuffix.TryDecode(OriginalScenarioScriptSuffix.Packet((byte)value),out var decoded),"all256 suffix bytes decode");
            Check(decoded==OriginalScenarioScriptSuffix.Decode((byte)value)&&OriginalScenarioScriptSuffix.Valid(decoded),"suffix is one wrapped8bit char (including NUL) or empty");
        }
        foreach(int map in new[]{2,8,12})foreach(byte value in new byte[]{1,2})
        {
            var f=new Fixture(map,"END");f.Finish();var m=f.catalog.Map(map);
            m.outroVariants=new[]{"a","b"}.Select(s=>new CampaignOutroVariant{suffix=s,script=File.ReadAllText(Path.Combine(root,"Assets/_Original/SourceData/Decoded/map",m.key+"set.arc",m.key+s+".dee.txt"))}).ToArray();
            var n=SuffixNotice(f,value);string before=Snapshot(f.View.record);long revision=f.View.battle?.revision??0;
            f.store.fail=true;Check(f.server.ReceiveBattlePacket(n).Accepted,"66 is client session metadata, requiring no account write");f.store.fail=false;
            Check(Snapshot(f.View.record)==before&&(f.View.battle?.revision??0)==revision&&f.server.PendingCampaignRequests().Length==0,"66 neither starts DEE nor changes resources/progress");
            Check(f.server.ReceiveBattlePacket(n).Failure==WorldFailure.StaleRequest,"66 UUID replay refused");
            f.Clear();string suffix=OriginalScenarioScriptSuffix.Decode(value),expected=m.Outro(suffix);
            Check(f.View.record.scenarioVisit.nativeOutroSuffix==suffix,"actual selected suffix freezes when ending starts");
            string context=f.View.presentation.context;
            Check(context.EndsWith(OriginalCampaignScript.Fingerprint(expected)),"actual presentation uses selected original variant "+m.key+suffix);sourceVariants++;
            var late=SuffixNotice(f,(byte)(3-value));Check(f.server.ReceiveBattlePacket(late).Accepted,"late66 may update future load state");
            Check(f.View.presentation.context==context&&f.View.record.scenarioVisit.nativeOutroSuffix==suffix,"late66 does not replace running/frozen DEE");
            f.Reload();Check(f.View.presentation.context==context&&f.View.record.scenarioVisit.nativeOutroSuffix==suffix,"actual service reconnect restarts selected DEE, not base");
            string directory=Path.Combine(Path.GetTempPath(),"ds-suffix-"+Id());
            try
            {
                var store=new LocalAccountFileStore(Path.Combine(directory,"accounts.json"));
                Check(store.TryWrite(f.store.data)&&store.TryRead(out var restored)&&restored.accounts[0].character.world.scenarioVisit.nativeOutroSuffix==suffix,"real DataContract account file retains frozen selection");
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        foreach(string bad in new[]{"owner","session","revision","approval","length","trailing","opcode"})
        {
            var f=new Fixture(2,"END","END");f.Finish();var n=SuffixNotice(f,1);var before=Snapshot(f.View.record);
            if(bad=="owner")n.character=Id();if(bad=="session")n.session=Id();if(bad=="revision")n.expectedRevision++;
            if(bad=="approval")n.approved=false;if(bad=="length")n.packet=n.packet.Take(2).ToArray();
            if(bad=="trailing")n.packet=n.packet.Concat(new byte[]{0}).ToArray();if(bad=="opcode")n.packet[1]=0;
            Check(!f.server.ReceiveBattlePacket(n).Accepted&&Snapshot(f.View.record)==before,"invalid66 atomic: "+bad);
            f.Clear();Check(f.View.record.scenarioVisit.nativeOutroSuffix=="","bad66 did not install a hidden selection");
        }
        var fresh=new Fixture(2,"END","END");fresh.Finish();Check(fresh.server.ReceiveBattlePacket(SuffixNotice(fresh,1)).Accepted,"receive a before reconnect");
        fresh.Reload();fresh.Clear();Check(fresh.View.record.scenarioVisit.nativeOutroSuffix=="","unstarted66 selection is a session value; server must resend after reentry");
        foreach(byte absent in new byte[]{26,160})
        {var missing=new Fixture(2,"END","MESSAGE 1 100 0 0 base-must-not-play\nEND");missing.Finish();Check(missing.server.ReceiveBattlePacket(SuffixNotice(missing,absent)).Accepted,"unknown suffix retains nativebyte");missing.Clear();
        Check(missing.View.presentation.completed&&missing.server.PendingCampaignRequests().Length==0,"missing variant is empty/failure selection, never unsuffixed fallback or fabricated reward");
        string file=Path.Combine(Path.GetTempPath(),"ds-suffix-nul-"+Id()+".json");
        try{var disk=new LocalAccountFileStore(file);Check(disk.TryWrite(missing.store.data)&&disk.TryRead(out var copy)&&copy.accounts[0].character.world.scenarioVisit.nativeOutroSuffix==OriginalScenarioScriptSuffix.Decode(absent),"actual JSON roundtrip preserves missing/embedded-NUL suffix rather than base");}
        finally{if(File.Exists(file))File.Delete(file);}}
        Check(!OriginalScenarioScriptSuffix.Valid("ab")&&!OriginalScenarioScriptSuffix.Valid("한"),"persisted suffix bounds");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,sourceVariants,unityPlay=false,scope="Actual common66 ingress and typed-clear service, original six02/08/12 a/b DEE selection, frozen/reconnect/file persistence, invalid envelope/replay and missing-variant no-base-fallback. Untimed managed checks; native54/61 integration separately verified."}));
    }
}
