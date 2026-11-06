using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.Entry;
internal static class MemoTransferChecks
{
    static int count;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static void Check(bool value,string label){count++;if(!value)throw new Exception(label);}
    static string Id()=>Guid.NewGuid().ToString("N");static string State(object value)=>JsonSerializer.Serialize(value,Json);
    static void Main(string[] args)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var rows=doc.RootElement.GetProperty("valid");
        foreach(var row in rows.EnumerateArray())
        {
            var body=Convert.FromHexString(row.GetProperty("body").GetString());var expected=Convert.FromHexString(row.GetProperty("plain").GetString());var native=row.GetProperty("native");
            Check(native.GetProperty("result").GetInt32()==0&&native.GetProperty("overflow").GetInt32()==0&&native.GetProperty("payload").GetString()==row.GetProperty("plain").GetString(),"unmodified native DCL callbacks decode the same literal/copy stream");
            Check(NativeMemoTransfer.TryDecode(body,out var payload)&&payload.SequenceEqual(expected),"current decoder versus actual native bytes");
            Check(NativeMemoTransfer.Checksum(payload)==native.GetProperty("crc").GetUInt32(),"actual native running CRC across output blocks");
        }
        foreach(var row in doc.RootElement.GetProperty("outer").EnumerateArray())
        {
            var body=Convert.FromHexString(row.GetProperty("body").GetString());bool expected=row.GetProperty("expected").GetProperty("accepted").GetBoolean();
            Check(NativeMemoTransfer.TryDecode(body,out _)==expected,"actual427D30 length/CRC/decoder/overflow acceptance gates");
        }
        var sample=Convert.FromHexString(rows[0].GetProperty("body").GetString());
        for(int length=0;length<sample.Length;length++)Check(!NativeMemoTransfer.TryDecode(sample.Take(length).ToArray(),out var invalid)&&invalid==null,"every framed-body truncation is atomic");
        Check(!NativeMemoTransfer.TryDecode(sample.Concat(new byte[]{0}).ToArray(),out _)&&!NativeMemoTransfer.TryDecode(null,out _),"strict complete-body framing rejects trailing bytes and missing data");
        foreach(int encoded in new[]{-32768,-1,0,8193,32767}){var body=(byte[])sample.Clone();body[0]=(byte)encoded;body[1]=(byte)(encoded>>8);Check(!NativeMemoTransfer.TryDecode(body,out _),"unsafe compressed count is bounded before native-size stack copy");}
        foreach(int decoded in new[]{-32768,-1,0}){var body=(byte[])sample.Clone();body[2]=(byte)decoded;body[3]=(byte)(decoded>>8);Check(!NativeMemoTransfer.TryDecode(body,out _),"unsafe output count cannot allocate or produce a batch");}
        var codec=JsonSerializer.Deserialize<NativeCodePage949>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalCodePage949.json")),Json);var f=MemoChecks.CreateFixture(args[0],codec);var before=State(f.store.data);int changes=0;f.manager.Notify_MemosChanged+=()=>changes++;
        NativeMemoTransferNotice Notice(byte[] body)=>new NativeMemoTransferNotice{id=Id(),character=f.service.WorldSnapshot().characterId,session=f.service.WorldSnapshot().commanderSession,approved=true,body=body};
        var valid=Notice(sample);Check(f.manager.Notify_MemoTransfer(valid)&&f.manager.MemoSnapshot.Length==1&&changes==1,"production compressed body reaches the original memo inbox");
        Check(!f.manager.Notify_MemoTransfer(valid)&&changes==1,"compressed notice cannot replay");
        sample[4]^=1;Check(!f.manager.Notify_MemoTransfer(Notice(sample))&&changes==1&&f.manager.MemoSnapshot.Length==1,"bad CRC does not partially append or signal a changed inbox");sample[4]^=1;
        foreach(string kind in new[]{"owner","session","unapproved","id","disabled"})
        {
            var n=Notice(sample);if(kind=="owner")n.character=Id();else if(kind=="session")n.session=Id();else if(kind=="unapproved")n.approved=false;else if(kind=="id")n.id="bad";else f.manager.isActiveAndEnabled=false;
            Check(!f.manager.Notify_MemoTransfer(n),"invalid compressed notice context "+kind);f.manager.isActiveAndEnabled=true;
        }
        var malformed=Convert.FromHexString(doc.RootElement.GetProperty("badMemo").GetString());Check(!f.manager.Notify_MemoTransfer(Notice(malformed))&&changes==1&&f.manager.MemoSnapshot.Length==1,"valid compression with malformed records is still atomic");
        var late=Notice(sample);f.client.Logout();Check(!f.manager.Notify_MemoTransfer(late)&&f.manager.MemoSnapshot==null,"logout rejects compressed old-session data");f.client.OpenLocalProfile();f.service.BeginWorld(f.definition);Check(f.manager.MemoSnapshot.Length==0&&!f.manager.Notify_MemoTransfer(late),"reconnect clears transient inbox and rejects old transfer");
        Check(State(f.store.data)==before,"memo transfer never changes player save or grants values");
        foreach(bool reconnect in new[]{false,true})
        {
            var old=MemoChecks.CreateFixture(args[0],codec);int calls=0;old.manager.MemoAuthority=(c,s)=>{calls++;return null;};old.manager.Notify_MemoPacket+=_=>{old.client.Logout();if(reconnect){old.client.OpenLocalProfile();old.service.BeginWorld(old.definition);}};
            Check(!old.manager.Request_ViewMemos()&&calls==0,"raw query callback logout/reconnect cannot dispatch the old authority");
            if(reconnect)Check(old.manager.MemoState.rows.Length==0,"old request cannot append or invalidate the replacement session inbox");
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0,unityPlay=false}));
    }
}
