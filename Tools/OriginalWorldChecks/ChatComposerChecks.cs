using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class ChatComposerChecks
{
 static int checks;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};static string Id()=>Guid.NewGuid().ToString("N");
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static string[] Strings(JsonElement row)=>row.EnumerateArray().Select(v=>v.GetString()).ToArray();
 sealed class Store:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}public bool TryWrite(LocalAccountDocument d){data=d.Copy();return true;}}
 static void Main(string[] args)
 {
  var codec=JsonSerializer.Deserialize<NativeCodePage949>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalCodePage949.json")),Json);using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));
  foreach(var row in doc.RootElement.GetProperty("modes").EnumerateArray())
  {
   var model=new NativeWorldChatComposer();int initial=row.GetProperty("initial").GetInt32();if(initial>1)model.Toggle(initial-2);int i=0;
   foreach(var control in row.GetProperty("controls").EnumerateArray())
   {Check(model.Toggle(control.GetInt32()),"valid original button");var expected=row.GetProperty("expected")[i++];Check(model.Channel==expected.GetProperty("channel").GetInt32()&&model.Checked(0)==expected.GetProperty("checked")[0].GetBoolean()&&model.Checked(1)==expected.GetProperty("checked")[1].GetBoolean(),"original mouse-down mutual exclusion and channel state");}
   int before=model.Channel;Check(!model.Toggle(2)&&model.Channel==before,"unknown UI control preserves mode");
  }
  foreach(var row in doc.RootElement.GetProperty("filters").EnumerateArray())
  {var filter=new NativeWorldChatFilter();Check(filter.SetTerms(Strings(row.GetProperty("terms")),codec)&&filter.Apply(row.GetProperty("text").GetString())==row.GetProperty("expected").GetString(),"original ordered CP949 byte-count mask and command/recipient exception");}
  var store=new Store();var service=new EntryAccountService(store);var client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"입력검사");store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();var definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Data/World/KaosiaWorld.json")),Json);Check(service.BeginWorld(definition).Accepted,"isolated authenticated world");var world=service.WorldSnapshot();string saved=JsonSerializer.Serialize(store.data,Json);
  foreach(var row in doc.RootElement.GetProperty("submits").EnumerateArray())
  {
   var manager=new LocalServerManager(service);manager.ConfigureClientText(codec);manager.Request_WhisperReception(row.GetProperty("whisper").GetBoolean());var terms=new WorldChatTermsNotice{id=Id(),approved=true,character=world.characterId,session=world.commanderSession,terms=Strings(row.GetProperty("terms"))};
   Check(manager.Notify_WorldChatTerms(terms)&&!manager.Notify_WorldChatTerms(terms),"approved current filter terms accepted once");
   var sent=new List<string>();manager.Notify_WorldTextPacket+=p=>sent.Add(Convert.ToHexString(p).ToLowerInvariant());manager.Notify_ClientSettingsPacket+=p=>sent.Add(Convert.ToHexString(p).ToLowerInvariant());WorldClientTextRequest request=null;manager.Notify_WorldTextRequested+=r=>request=r;
   string text=row.GetProperty("text").GetString();bool accepted=manager.Request_WorldText(text,row.GetProperty("channel").GetInt32());var expected=row.GetProperty("expected");
   Check(accepted==(text.Length>0)&&sent.SequenceEqual(Strings(expected.GetProperty("packets"))),"full original preprocessor/sender and7D auto request order, including256-byte consumption");
   string recipient=expected.GetProperty("recipient").ValueKind==JsonValueKind.Null?null:expected.GetProperty("recipient").GetString();
   Check(manager.LastChatRecipient==recipient&&manager.WhisperReception==expected.GetProperty("whisper").GetBoolean(),"source recipient case/trim and auto whisper request awaits acknowledgment");
   if(request!=null)Check(request.command==row.GetProperty("processed").GetString()&&request.character==world.characterId&&request.session==world.commanderSession,"typed intent uses filtered input and current connection");
  }
  Check(JsonSerializer.Serialize(store.data,Json)==saved,"input/filter/modes never change player state or invent server echo");
  var guards=new LocalServerManager(service);guards.ConfigureClientText(codec);var notice=new WorldChatTermsNotice{id=Id(),approved=true,character=world.characterId,session=world.commanderSession,terms=new[]{"bad"}};Check(guards.Notify_WorldChatTerms(notice),"guard fixture terms");notice.terms[0]="changed";byte[] last=null;guards.Notify_WorldTextPacket+=p=>last=p;
  Check(guards.Request_WorldText("bad",1)&&last.SequenceEqual(new byte[]{0x7a,1,3,42,42,42}),"received term array detached");
  notice.id=Id();notice.terms=new[]{"😀"};Check(!guards.Notify_WorldChatTerms(notice)&&guards.Request_WorldText("bad",1)&&last[3]==42,"invalid codec update leaves prior rules intact");
  Check(!guards.Request_WorldText("bad\0input",1)&&!guards.Request_WorldText("😀",1)&&!guards.Request_WorldText("bad",9)&&!guards.Request_WorldText("",1),"unrepresentable input and native-suppressed modes refused");
  Check(guards.Request_WorldText("/to Name hi",1)&&guards.LastChatRecipient=="Name","source recipient cached for current client session");client.Logout();Check(guards.LastChatRecipient==null&&!guards.Request_WorldText("bad",1)&&!guards.Notify_WorldChatTerms(notice),"logout blocks old terms/message/recipient state");
  client.OpenLocalProfile();service.BeginWorld(definition);Check(guards.Request_WorldText("bad",1)&&last[3]==(byte)'b'&&guards.LastChatRecipient==null,"new session has no old account filter or recipient");
  foreach(string phase in new[]{"raw","intent","settings"})foreach(string change in new[]{"logout","reconnect","disable"})
  {
   client.OpenLocalProfile();service.BeginWorld(definition);var current=service.WorldSnapshot();var m=new LocalServerManager(service);m.ConfigureClientText(codec);Check(m.Request_WhisperReception(false),"current-session setting fixture");int raw=0,intents=0,settings=0;
   void Change(){if(change=="disable")m.isActiveAndEnabled=false;else{client.Logout();if(change=="reconnect"){client.OpenLocalProfile();service.BeginWorld(definition);}}}
   m.Notify_WorldTextPacket+=_=>{raw++;if(phase=="raw")Change();};m.Notify_WorldTextRequested+=_=>{intents++;if(phase=="intent")Change();};m.Notify_ClientSettingsPacket+=_=>{settings++;if(phase=="settings")Change();};
   Check(!m.Request_WorldText("/to Other hello",1),"connection changes cancel remaining chat callbacks "+phase+change);
   Check(raw==1&&intents==(phase=="raw"?0:1)&&settings==(phase=="settings"?1:0),"native packet order retained until its captured context ends");
   Check(m.LastChatRecipient==null&&m.WhisperReception==null,"recipient and whisper preference from old session never appear in replacement context");
   Check(JsonSerializer.Serialize(store.data,Json)==saved,"callback cancellation preserves saved player state");
  }
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,mismatches=0,unityPlay=false}));
 }
}
