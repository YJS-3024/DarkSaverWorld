using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Campaign;
internal static class MapMusicChecks
{
    static int assertions;
    static void Check(bool condition,string label){assertions++;if(!condition)throw new Exception(label);}
    static void Main(string[] args)
    {
        string source=File.ReadAllText(Path.Combine(args[0],"Assets/_Original/SourceData/Decoded/gameres/set.arc/midiset.txt.txt"));
        var table=OriginalMapMusic.Parse(source);Check(table.Count==47,"declared count");
        Check(table.Lookup(1000)=="BGM\\game3.mid"&&table.Lookup(1010)=="BGM\\game2.mid","native town and field mappings");
        Check(table.Lookup(1501)=="BGM\\1stF.mid"&&table.Lookup(1502)=="BGM\\2ndF.mid","last stored slots");
        foreach(int id in new[]{1,26,101,1503,1504,1505,9999})Check(table.Lookup(id)=="","no inferred fallback/trailing-row lookup "+id);
        Check(OriginalMapMusic.Parse("2\n1000 first.mid\n1000 second.mid\n1001 ignored.mid").Lookup(1000)=="BGM\\first.mid","first matching duplicate wins");
        foreach(string invalid in new[]{"","-1\n","2\n1000 a.mid\n","1\ninvalid\n"})
        {bool rejected=false;try{OriginalMapMusic.Parse(invalid);}catch(InvalidDataException){rejected=true;}Check(rejected,"malformed table is rejected without fabricated entries");}
        using var cases=JsonDocument.Parse(File.ReadAllText(args[1]));using var expected=JsonDocument.Parse(File.ReadAllText(args[2]));
        int sequence=0;
        foreach(var steps in cases.RootElement.EnumerateArray())
        {
            var calls=new List<string[]>();bool success=true;
            var state=new NativeMapMusicState(table.Lookup,p=>{calls.Add(new[]{"play",p});return success;},()=>calls.Add(new[]{"stop"}));
            int index=0;
            foreach(var step in steps.EnumerateArray())
            {
                calls.Clear();success=!step.TryGetProperty("success",out var ok)||ok.GetBoolean();string op=step.GetProperty("op").GetString();
                switch(op)
                {case "map":state.EnterMap(step.GetProperty("value").GetInt32());break;case "enable":state.SetEnabled(step.GetProperty("value").GetInt32()!=0);break;case "play":state.Play(step.GetProperty("path").GetString());break;case "stop":state.Stop();break;}
                var native=expected.RootElement[sequence][index++];
                Check(state.Map==native.GetProperty("map").GetInt32(),"native map field sequence"+sequence);
                Check(state.Accepted==native.GetProperty("accepted").GetBoolean(),"native accepted flag sequence"+sequence);
                Check(state.Enabled==native.GetProperty("enabled").GetBoolean(),"native enabled flag sequence"+sequence);
                var wanted=native.GetProperty("events").EnumerateArray().Select(e=>e.EnumerateArray().Select(v=>v.GetString()).ToArray()).ToArray();
                Check(calls.Count==wanted.Length&&calls.Select((call,i)=>call.SequenceEqual(wanted[i])).All(v=>v),"native lookup/play/stop calls sequence"+sequence);
            }
            sequence++;
        }
        var events=new List<string>();var model=new NativeMapMusicState(table.Lookup,path=>{events.Add(path);return !path.Contains("missing");},()=>events.Add("stop"));
        model.EnterMap(1000);model.EnterMap(1);Check(model.ActivePath=="BGM\\game3.mid","missing mapping preserves old audio");
        model.Play("BGM\\missing.mid");Check(model.ActivePath==null&&!model.Accepted,"failed explicit open leaves no audio and does not make map ready");
        model.EnterMap(1000);model.Stop();int before=events.Count;model.EnterMap(1000);Check(events.Count==before&&model.Accepted,"direct stop preserves native map accepted cache");
        model.SetEnabled(false);model.SetEnabled(true);Check(events.Last()=="BGM\\game3.mid","disable/enable refreshes current map");
        Check(NativeMapMusicState.RepeatCount==1000,"native public DirectMusic repeat count");
        var repeat=new NativeScoreRepeat();repeat.Begin();
        for(int i=0;i<300;i++)Check(!repeat.Observe(false)&&repeat.Starts==1,"waiting for first playback cannot consume repeats");
        for(int i=0;i<1000;i++){Check(!repeat.Observe(true),"playing observation");Check(repeat.Observe(false),"natural end restarts");Check(!repeat.Observe(false),"pending next start does not repeat");}
        Check(repeat.Starts==1001,"initial playback plus1000 repeats");repeat.Observe(true);Check(!repeat.Observe(false)&&repeat.Starts==1001,"final playback ends without restart");
        repeat.Stop();Check(!repeat.Observe(false)&&repeat.Starts==0,"explicit stop cancels repeats");repeat.Begin();Check(repeat.Starts==1,"new track resets counter");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,declaredMapRows=table.Count,trailingRowsIgnored=3}));
    }
}
