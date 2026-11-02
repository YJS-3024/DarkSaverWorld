using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class MemoChecks
{
 static int assertions,nativeDeleteComparisons;static string root;static NativeCodePage949 codec;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
 static string Id()=>Guid.NewGuid().ToString("N");static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static string Hex(byte[] bytes)=>bytes==null?null:Convert.ToHexString(bytes).ToLowerInvariant();
 static byte[] Batch(params byte[][] packets)=>new[]{(byte)packets.Length}.Concat(packets.SelectMany(p=>p.Skip(1))).ToArray();
 static byte[] Record(int id)=>new byte[]{1,(byte)id}.Concat(new byte[12]).Concat(new byte[4]).ToArray();
 sealed class Store:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}public bool TryWrite(LocalAccountDocument d){data=d.Copy();return true;}}
 sealed class Fixture
 {
  public Store store=new Store();public EntryAccountService service;public EntryClient client;public LocalServerManager manager;public WorldDefinition definition;
  public Fixture()
  {
   definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Json);
   service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"메모검사");store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"isolated world session");
   manager=new LocalServerManager(service);manager.ConfigureClientText(codec);
  }
  public NativeMemoNotice Notice(byte[] payload)=>new NativeMemoNotice{id=Id(),approved=true,character=service.WorldSnapshot().characterId,session=service.WorldSnapshot().commanderSession,payload=payload};
 }
 static void Main(string[] args)
 {
  root=args[0];codec=JsonSerializer.Deserialize<NativeCodePage949>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Resources/OriginalCodePage949.json")),Json);
  Check(codec.unicode.Length==17176,"complete canonical CP949 BMP mapping");
  for(int i=1;i<codec.unicode.Length;i++)
  {
   string value=((char)codec.unicode[i]).ToString();Check(codec.TryEncode(value,out var b)&&codec.TryCString(b,0,b.Length,out var decoded)&&decoded==value,"canonical CP949 encode/decode roundtrip");
  }
  Check(!codec.TryEncode("가\0A",out _)&&!codec.TryEncode("😀",out _)&&!codec.TryCString(new byte[]{0x81},0,1,out _),"NUL injection, unrepresentable Unicode and truncated double byte rejected");
  Check(codec.TryCString(new byte[]{65,0,255},0,3,out var visible)&&visible=="A","native CString stops at first NUL regardless remaining fixed field bytes");
  using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var rows=doc.RootElement;
  Check(Hex(NativeMemoPackets.ViewRequest())==rows.GetProperty("viewPacket").GetString(),"memo query source literal and7A0509 constants");
  foreach(var test in rows.GetProperty("records").EnumerateArray())
  {
   var packet=Convert.FromHexString(test.GetProperty("packet").GetString());Check(NativeMemoPackets.TryBatch(packet,codec,out var result)&&result.Length==1,"native batch record parse");
   var expected=test.GetProperty("expected");Check(result[0].id==expected.GetProperty("id").GetByte()&&result[0].sender==expected.GetProperty("sender").GetString()&&result[0].date==expected.GetProperty("date").GetString()&&result[0].body==expected.GetProperty("body").GetString(),"native record fields incl empty/max buffers");
   for(int length=0;length<packet.Length;length++)Check(!NativeMemoPackets.TryBatch(packet.Take(length).ToArray(),codec,out _),"every truncated boundary rejected atomically");
  }
  foreach(var test in rows.GetProperty("deletes").EnumerateArray())
  {
   int count=test.GetProperty("count").GetInt32(),page=test.GetProperty("page").GetInt32(),row=test.GetProperty("row").GetInt32();var inbox=new NativeMemoInbox();inbox.Append(Batch(Enumerable.Range(0,count).Select(i=>Record(i*7)).ToArray()),codec);
   if(page==1&&count>10)inbox.SetPage(1);else if(page==1)continue;
   nativeDeleteComparisons++;int index=page*10+row;byte id=(byte)(index*7);bool deleted=inbox.Delete(row,inbox.Revision,id,out var packet);var expected=test.GetProperty("expected");string wire=expected.GetProperty("packet").ValueKind==JsonValueKind.Null?null:expected.GetProperty("packet").GetString();
   Check(Hex(packet)==wire&&deleted==(wire!=null),"native delete bytes and bounds");Check(inbox.Count==count-(deleted?1:0),"native row removal count");if(deleted)Check(inbox.Rows.Select(m=>(int)m.id).SequenceEqual(Enumerable.Range(0,count).Where(i=>i!=index).Select(i=>i*7)),"native deletion removes indicated row only");
  }
  var emptyPage=new NativeMemoInbox();emptyPage.Append(Batch(Enumerable.Range(0,11).Select(i=>Record(i)).ToArray()),codec);emptyPage.SetPage(1);emptyPage.Delete(0,emptyPage.Revision,10,out _);Check(emptyPage.Count==10&&emptyPage.Page==1&&!emptyPage.Delete(0,emptyPage.Revision,0,out _),"source keeps second page after last row deletion");
  var f=new Fixture();var sent=new List<string>();f.manager.Notify_MemoPacket+=p=>sent.Add(Hex(p));string save=JsonSerializer.Serialize(f.store.data,Json);
  Check(f.manager.Request_ViewMemos()&&f.manager.Request_ViewMemos()&&sent.Count==1,"source query gate sends once across repeated opens");
  var notice=f.Notice(Batch(Record(7),Record(9)));Check(f.manager.Notify_MemoPayload(notice)&&f.manager.MemoSnapshot.Length==2,"approved decompressed batch arrives");Check(!f.manager.Notify_MemoPayload(notice),"batch notice replay rejected");
  var before=f.manager.MemoState;var external=before.rows;external[0].body="changed";Check(f.manager.MemoSnapshot[0].body=="","snapshot cannot mutate inbox");
  Check(!f.manager.Request_DeleteMemo(0,before.revision,7,Id(),0)&&!f.manager.Request_DeleteMemo(0,before.revision,7,before.session,1),"old session or changed page cannot delete an unseen row");
  Check(f.manager.Request_DeleteMemo(0,before.revision,7,before.session,0)&&sent.Last()=="8007"&&f.manager.MemoSnapshot.Single().id==9,"production RPC applies client removal and emits native2byte request");
  Check(!f.manager.Request_DeleteMemo(0,before.revision,9,before.session,0),"old displayed revision cannot delete next row");
  var malformed=f.Notice(new byte[]{2,0});Check(!f.manager.Notify_MemoPayload(malformed)&&f.manager.MemoSnapshot.Single().id==9,"partial batch leaves existing rows intact");
  var mismatch=f.Notice(Batch(Record(10)));mismatch.character=Id();Check(!f.manager.Notify_MemoPayload(mismatch),"foreign owner rejected");
  int received=0;bool whisper=false;f.manager.Notify_WhisperReceptionChanged+=allowed=>{received++;whisper=allowed;};var w=f.service.WorldSnapshot();var settings=new ClientWhisperNotice{id=Id(),character=w.characterId,session=w.commanderSession,packet=new byte[]{0x7d,1,0}};
  Check(f.manager.Notify_ClientSettingsState(settings)&&whisper&&received==1&&!f.manager.Notify_ClientSettingsState(settings),"production settings notification validates session and rejects replay");
  Check(JsonSerializer.Serialize(f.store.data,Json)==save,"memo/client settings never modify player save or grant values");
  f.client.Logout();Check(f.manager.MemoSnapshot==null&&!f.manager.Notify_MemoPayload(notice)&&!f.manager.Notify_ClientSettingsState(new ClientWhisperNotice{id=Id(),character=w.characterId,session=w.commanderSession,packet=new byte[]{0x7d,2,0}}),"logout blocks old client-window messages");
  f.client.OpenLocalProfile();f.service.BeginWorld(f.definition);Check(f.manager.MemoSnapshot.Length==0,"new connection starts with transient empty client inbox");Check(f.manager.Notify_MemoPayload(f.Notice(Batch(Record(1)))),"new session receives its own inbox payload");
  Check(!f.manager.Request_DeleteMemo(0,before.revision,1,before.session,0),"old display cannot delete after reconnect even if record id/revision match");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions,nativeDeleteComparisons,unityPlay=false,scope="Canonical CP949 mapping; native record/delete differential; real account/local-manager memo and settings integration; isolated transient data only."}));
 }
}
