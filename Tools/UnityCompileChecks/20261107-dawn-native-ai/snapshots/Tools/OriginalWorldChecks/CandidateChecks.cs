using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.Entry;
internal static class CandidateChecks
{
    static int checks;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string Id()=>Guid.NewGuid().ToString("N");static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static string[] Strings(JsonElement value)=>value.EnumerateArray().Select(v=>v.GetString()).ToArray();
    static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var codec=JsonSerializer.Deserialize<NativeCodePage949>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalCodePage949.json")),Json);
        foreach(var row in doc.RootElement.GetProperty("parsers").EnumerateArray())
        {
            byte[] payload=Convert.FromHexString(row.GetProperty("payload").GetString());var expected=row.GetProperty("expected");
            Check(NativeCandidateData.TryParse(payload,codec,out var data)&&data.terms.SequenceEqual(Strings(expected.GetProperty("terms")))&&data.windowClasses.SequenceEqual(Strings(expected.GetProperty("windowClasses"))),"actual4161E0 byte comma/sentinel/scanf/pointer results");
            var copy=data.Copy();copy.header[0]^=1;Check(!copy.header.SequenceEqual(data.header),"candidate header snapshot detached");
            if(data.terms.Length>0){copy.terms[0]="mutated";Check(data.terms[0]!="mutated","candidate term snapshot detached");}
        }
        foreach(var row in doc.RootElement.GetProperty("protocol").EnumerateArray())
        {
            var cache=Convert.FromHexString(row.GetProperty("cache").GetString());var packet=Convert.FromHexString(row.GetProperty("packet").GetString());var expected=row.GetProperty("expected");
            bool accepted=NativeCandidateData.TryCompressed(packet[1]==0x12?cache:packet.Skip(2).ToArray(),codec,out var data);
            Check(accepted==expected.GetProperty("accepted").GetBoolean(),"actual415BA0 cache-read/transfer acceptance gates");
            if(accepted)Check(data.terms.SequenceEqual(Strings(expected.GetProperty("terms")))&&data.windowClasses.SequenceEqual(Strings(expected.GetProperty("windowClasses"))),"actual415BA0 invokes the same parser with verified output");
            string written=expected.GetProperty("written").GetString();Check(written==(accepted&&packet[1]==0x13?Convert.ToHexString(cache).ToLowerInvariant():""),"actual native cache writes preserve exact compressed header/data and occur only on validated transfer");
        }
        foreach(var row in doc.RootElement.GetProperty("windowChecks").EnumerateArray())
        {
            var data=new NativeCandidateData{windowClasses=Strings(row.GetProperty("classes"))};int native=row.GetProperty("expected").GetProperty("posts").GetArrayLength();
            Check(data.CapturedWindowClassMatchCount(row.GetProperty("captured").GetString())==native,"actual416430 captured class-name exact case and duplicate match count");
        }
        var valid=Convert.FromHexString(doc.RootElement.GetProperty("original").GetString());Check(NativeCandidateData.TryCompressed(valid,codec,out var original)&&original.terms.Length==182&&original.windowClasses.SequenceEqual(new[]{"TWME1"}),"actual packaged Candid.Dat lists");
        Check(original.terms.Count(v=>v.Contains('\n'))==10,"native comma fields retain original embedded CRLF");
        foreach(var row in doc.RootElement.GetProperty("unsigned").EnumerateArray())
        {
            var body=Convert.FromHexString(row.GetProperty("body").GetString());
            Check(NativeCandidateData.TryCompressed(body,codec,out var parsed)&&parsed.windowClasses.Length==0,"candidate unsigned WORD output over signed32767 boundary");
            Check(!NativeMemoTransfer.TryDecode(body,out _),"same unsigned-sized cache is not accepted as a signed-length memo transfer");
        }
        foreach(var row in doc.RootElement.GetProperty("invalid").EnumerateArray())Check(!NativeCandidateData.TryParse(Convert.FromHexString(row.GetString()),codec,out var rejected)&&rejected==null,"malformed parser payload rejected atomically without native unsafe reads/loops");
        for(int n=0;n<valid.Length;n++)Check(!NativeCandidateData.TryCompressed(valid.Take(n).ToArray(),codec,out _),"every compressed cache truncation rejected before state mutation");
        var fixture=MemoChecks.CreateFixture(args[0],codec);var m=fixture.manager;var initial=JsonSerializer.Serialize(fixture.store.data,Json);int writes=0;byte[] stored=null;
        Check(m.ConfigureCandidateCache(valid,p=>{writes++;stored=(byte[])p.Clone();p[0]=0;return true;})&&m.CandidateSnapshot==null,"configuration does not activate filtering before server metadata");
        NativeCandidateNotice Notice(byte[] packet)=>new NativeCandidateNotice{id=Id(),character=fixture.service.WorldSnapshot().characterId,session=fixture.service.WorldSnapshot().commanderSession,approved=true,packet=packet};
        var cached=Notice(NativeCandidatePackets.Cached());cached.id="abcdef0123456789abcdef0123456789";Check(m.Notify_CandidateMetadata(cached)&&m.CandidateSnapshot.terms.Length==182&&writes==0,"11/12 loads configured cache only on current approved metadata");
        Check(!m.Notify_CandidateMetadata(cached)&&m.CandidateSnapshot.terms.Length==182,"metadata notification replay cannot append twice");
        var upper=Notice(NativeCandidatePackets.Cached());upper.id=cached.id.ToUpperInvariant();Check(!m.Notify_CandidateMetadata(upper)&&m.CandidateSnapshot.terms.Length==182,"GUID spelling case cannot replay metadata");
        Check(m.Notify_CandidateMetadata(Notice(NativeCandidatePackets.Cached()))&&m.CandidateSnapshot.terms.Length==364,"native4161E0 appends lists rather than replacing/normalizing terms");
        var detached=m.CandidateSnapshot;detached.terms[0]="mutated";Check(m.CandidateSnapshot.terms[0]!="mutated","manager candidate snapshot detached");
        byte[] sent=null;m.Notify_WorldTextPacket+=p=>sent=(byte[])p.Clone();string word=original.terms.First(t=>t.Length>0&&!t.Contains('\n')&&!t.Contains('\r'));
        Check(m.Request_WorldText(word,1)&&sent.Skip(3).All(v=>v==42),"approved candidate primary list drives production chat filter");
        Check(m.Request_WorldText("TWME1",1)&&sent.Skip(3).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("TWME1")),"windowClasses list is not invented as a second chat filter");
        var update=Notice(NativeCandidatePackets.Transfer(valid));Check(m.Notify_CandidateMetadata(update)&&writes==1&&stored.SequenceEqual(valid)&&m.CandidateSnapshot.terms.Length==546,"11/13 validated cache bytes persist separately and append original lists");
        Check(!m.Notify_CandidateMetadata(update)&&writes==1,"update replay cannot write twice");
        foreach(string fault in new[]{"owner","session","id","approval","disabled","opcode","subtype","trailing-cache","checksum"})
        {
            var notice=Notice(NativeCandidatePackets.Cached());if(fault=="owner")notice.character=Id();else if(fault=="session")notice.session=Id();else if(fault=="id")notice.id="bad";else if(fault=="approval")notice.approved=false;else if(fault=="disabled")m.isActiveAndEnabled=false;else if(fault=="opcode")notice.packet[0]=0;else if(fault=="subtype")notice.packet[1]=0;else if(fault=="trailing-cache")notice.packet=new byte[]{0x11,0x12,0};else {notice.packet=NativeCandidatePackets.Transfer(valid);notice.packet[6]^=1;}
            int before=m.isActiveAndEnabled?m.CandidateSnapshot.terms.Length:546;Check(!m.Notify_CandidateMetadata(notice),"candidate receipt guard "+fault);m.isActiveAndEnabled=true;Check(m.CandidateSnapshot.terms.Length==before&&writes==1,"rejected metadata preserves live arrays/cache "+fault);
        }
        Check(JsonSerializer.Serialize(fixture.store.data,Json)==initial,"candidate/filter/cache changes never touch player save");
        var old=Notice(NativeCandidatePackets.Cached());fixture.client.Logout();Check(m.CandidateSnapshot==null&&!m.Notify_CandidateMetadata(old),"logout hides candidate lists and rejects old metadata");fixture.client.OpenLocalProfile();fixture.service.BeginWorld(fixture.definition);
        Check(m.CandidateSnapshot==null&&m.Notify_CandidateMetadata(Notice(NativeCandidatePackets.Cached()))&&m.CandidateSnapshot.terms.Length==182,"new session waits for metadata and reloads cache without previous active lists");
        foreach(string fault in new[]{"false","throw","logout","reconnect","nested","writer"})
        {
            var f=MemoChecks.CreateFixture(args[0],codec);var manager=f.manager;NativeCandidateNotice N()=>new NativeCandidateNotice{id=Id(),character=f.service.WorldSnapshot().characterId,session=f.service.WorldSnapshot().commanderSession,approved=true,packet=NativeCandidatePackets.Transfer(valid)};
            Check(manager.ConfigureCandidateCache(valid,p=>{if(fault=="false")return false;if(fault=="throw")throw new IOException("fixture");if(fault=="logout"||fault=="reconnect"){f.client.Logout();if(fault=="reconnect"){f.client.OpenLocalProfile();f.service.BeginWorld(f.definition);}}if(fault=="writer")manager.ConfigureCandidateCacheWriter(_=>true);if(fault=="nested"){var n=N();n.packet=NativeCandidatePackets.Cached();Check(manager.Notify_CandidateMetadata(n),"nested current cache receipt");}return true;}),"isolated candidate write fault fixture");
            Check(!manager.Notify_CandidateMetadata(N()),"failed/reentrant cache persistence cannot commit old live filter "+fault);
            Check(manager.CandidateSnapshot?.terms.Length==(fault=="nested"?182:(int?)null),"write failure preserves the current live projection "+fault);
        }
        var fresh=MemoChecks.CreateFixture(args[0],codec);int freshWrites=0;fresh.manager.ConfigureCandidateCacheWriter(_=>{freshWrites++;return true;});
        Check(!fresh.manager.ConfigureCandidateCache(new byte[8],_=>false),"invalid pre-existing cache is never configured as valid metadata");
        var freshNotice=new NativeCandidateNotice{id=Id(),character=fresh.service.WorldSnapshot().characterId,session=fresh.service.WorldSnapshot().commanderSession,approved=true,packet=NativeCandidatePackets.Cached()};
        Check(!fresh.manager.Notify_CandidateMetadata(freshNotice),"11/12 cannot fabricate a missing/corrupt cache");freshNotice.id=Id();freshNotice.packet=NativeCandidatePackets.Transfer(valid);
        Check(fresh.manager.Notify_CandidateMetadata(freshNotice)&&freshWrites==1&&fresh.manager.CandidateSnapshot.terms.Length==182,"11/13 can repair missing/corrupt cache through separately configured writer");
        string path=Path.Combine(args[2],"Candid.Dat");var file=new NativeCandidateCacheFile(path);Check(!file.Exists&&!file.TryRead(out _),"missing cache reported without creating files");
        Check(file.TryWrite(valid)&&file.TryRead(out var disk)&&disk.SequenceEqual(valid),"first cache creation keeps exact original compressed bytes");Check(file.TryWrite(valid)&&file.TryRead(out disk)&&disk.SequenceEqual(valid),"atomic existing-cache replacement supported by current runtime");
        Check(Directory.GetFiles(args[2],"*.tmp").Length==0,"no cache temporary files remain");var blocked=new NativeCandidateCacheFile(Path.Combine(path,"blocked"));Check(!blocked.TryWrite(valid)&&file.TryRead(out disk)&&disk.SequenceEqual(valid),"cache I/O failure cannot overwrite the prior valid file");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,mismatches=0,unityPlay=false}));
    }
}
