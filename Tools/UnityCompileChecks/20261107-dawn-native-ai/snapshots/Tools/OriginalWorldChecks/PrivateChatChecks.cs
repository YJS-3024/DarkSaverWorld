using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class PrivateChatChecks
{
 static int checks;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};static string Id()=>Guid.NewGuid().ToString("N");
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static string[] Strings(JsonElement values)=>values.EnumerateArray().Select(v=>v.GetString()).ToArray();
 sealed class Store:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}public bool TryWrite(LocalAccountDocument d){data=d.Copy();return true;}}
 static void Main(string[] args)
 {
  var codec=JsonSerializer.Deserialize<NativeCodePage949>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalCodePage949.json")),Json);using var document=JsonDocument.Parse(File.ReadAllText(args[1]));
  foreach(var row in document.RootElement.GetProperty("routes").EnumerateArray())
  {
   var inbox=new NativePrivateChatInbox();var names=Strings(row.GetProperty("senders"));foreach(string name in names)Check(inbox.Receive(name+">seed",0,1000,codec),"seed decoded existing native private window");
   long before=inbox.Revision;Check(inbox.Receive(row.GetProperty("text").GetString(),row.GetProperty("suppression").GetInt32(),1000,codec),"valid decoded private message consumed");var events=row.GetProperty("expected");
   if(events.GetArrayLength()==0)Check(inbox.Revision==before&&inbox.Views().Length==names.Length,"native client gate blocks private update");
   else
   {
    var e=events[0];bool added=e.GetProperty("op").GetString()=="create";var views=inbox.Views();Check(views.Length==names.Length+(added?1:0),"native exact case-sensitive12byte sender lookup/create");
    var view=views.First(v=>v.sender==e.GetProperty("sender").GetString());Check(view.lines.Last().Text==e.GetProperty("body").GetString(),"native sender/body split");
    if(added)Check(view.x==442&&view.y==names.Length*24,"original new-window cascade");
   }
  }
  foreach(var row in document.RootElement.GetProperty("filters").EnumerateArray())
  {var filter=new NativeWorldChatFilter();Check(filter.SetTerms(Strings(row.GetProperty("terms")),codec)&&filter.Apply(row.GetProperty("text").GetString(),false)==row.GetProperty("expected").GetString(),"native mode2 masks all private body, including slash input");}
  foreach(var row in document.RootElement.GetProperty("renders").EnumerateArray())
  {
   var native=row.GetProperty("expected").EnumerateArray().Where(v=>v.GetProperty("color").GetInt32()!=0).ToArray();var actual=NativePrivateChatInbox.VisibleLines(Strings(row.GetProperty("rows")).Select(s=>new NativeChatLine(s,4)).ToArray(),row.GetProperty("first").GetInt32());
   Check(actual.Length==native.Length,"actual422500 fully-contained visible rows");for(int i=0;i<actual.Length;i++){Check(actual[i].Text==native[i].GetProperty("text").GetString()&&actual[i].Kind==native[i].GetProperty("color").GetInt32(),"native2row marker lookback with explicit0 residue input");var r=native[i].GetProperty("rect");Check(r[0].GetInt32()==4&&r[1].GetInt32()==27+i*14&&r[2].GetInt32()==181&&r[3].GetInt32()==41+i*14,"original private row positions");}
   var shadows=row.GetProperty("expected").EnumerateArray().Where(v=>v.GetProperty("color").GetInt32()==0).ToArray();Check(shadows.Length==(row.GetProperty("depth").GetInt32()==16?actual.Length:0),"original16bit black shadow vs8bit direct text");
  }
  foreach(var row in document.RootElement.GetProperty("creates").EnumerateArray())
  {
   var model=new NativePrivateChatInbox();int count=row.GetProperty("count").GetInt32();for(int i=0;i<count;i++)model.Receive(("N"+i).PadRight(12)+">seed",0,1000,codec);
   Check(model.Receive("New         >hello",0,row.GetProperty("room").GetInt32(),codec),"source creator fixture consumed");var expected=row.GetProperty("expected");var views=model.Views();Check(views.Length==expected.GetProperty("count").GetInt32(),"actual477520 new-window room gate and registration");
   if(views.Length>count)Check(views.Last().x==expected.GetProperty("x").GetInt32()&&views.Last().y==expected.GetProperty("y").GetInt32(),"actual new-window cascade, including outside-screen initial Y");
  }
  var store=new Store();var service=new EntryAccountService(store);var client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"귓속말검사");store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();var definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Data/World/KaosiaWorld.json")),Json);Check(service.BeginWorld(definition).Accepted,"isolated authenticated world");var world=service.WorldSnapshot();string baseline=JsonSerializer.Serialize(store.data,Json);
  WorldPrivateMessageNotice Notice(string text)=>new WorldPrivateMessageNotice{id=Id(),approved=true,character=world.characterId,session=world.commanderSession,text=text,nativeSuppression=0,nativeRoomNumber=1000};
  foreach(var row in document.RootElement.GetProperty("replies").EnumerateArray())
  {
   var manager=new LocalServerManager(service);manager.ConfigureClientText(codec);string name=row.GetProperty("sender").GetString();Check(manager.Notify_PrivateMessage(Notice(name+">seed")),"receive initial private message");var view=manager.PrivateChats.Single();
   var terms=new WorldChatTermsNotice{id=Id(),approved=true,character=world.characterId,session=world.commanderSession,terms=Strings(row.GetProperty("terms"))};Check(manager.Notify_WorldChatTerms(terms),"explicit current terms");bool whisper=row.GetProperty("whisper").GetBoolean();manager.Request_WhisperReception(whisper);var sent=new List<string>();manager.Notify_WorldTextPacket+=p=>sent.Add(Convert.ToHexString(p).ToLowerInvariant());manager.Notify_ClientSettingsPacket+=p=>sent.Add(Convert.ToHexString(p).ToLowerInvariant());WorldPrivateTextRequest request=null;manager.Notify_PrivateTextRequested+=r=>request=r;
   bool accepted=manager.Request_PrivateText(view.id,world.commanderSession,row.GetProperty("text").GetString());var expected=row.GetProperty("expected");Check(sent.SequenceEqual(Strings(expected.GetProperty("packets"))),"actual4537A0/453930 packet bytes and7D-before-empty-read order");Check(accepted==expected.GetProperty("cleared").GetBoolean(),"native nonempty edit consumption");Check(manager.WhisperReception==expected.GetProperty("whisper").GetBoolean(),"automatic request does not apply an unreceived7D acknowledgment");Check(manager.PrivateChats.Single().count==1,"sending creates no invented server echo");
   if(accepted){Check(request!=null&&request.thread==view.id&&request.session==world.commanderSession&&request.recipient==name.TrimEnd(' ')&&request.text==row.GetProperty("processed").GetString(),"detached typed private intent");request.packet[0]=0;Check(sent.Last().StartsWith("7a09"),"request observer cannot mutate prior transport bytes");}
  }
  Check(JsonSerializer.Serialize(store.data,Json)==baseline,"private UI/client packets do not change account, wallet, character or saved world");
  var guarded=new LocalServerManager(service);guarded.ConfigureClientText(codec);var notice=Notice("Hero        >hello");Check(guarded.Notify_PrivateMessage(notice)&&!guarded.Notify_PrivateMessage(notice),"approved current notice once");var thread=guarded.PrivateChats.Single();
  notice.id=Id();notice.approved=false;Check(!guarded.Notify_PrivateMessage(notice),"unapproved update refused");notice.approved=true;notice.nativeSuppression=null;Check(!guarded.Notify_PrivateMessage(notice),"unknown native client gate is not invented");notice.nativeSuppression=0;notice.nativeRoomNumber=null;Check(!guarded.Notify_PrivateMessage(notice),"unknown room context is not aliased to map ID");notice.nativeRoomNumber=68036;Check(!guarded.Notify_PrivateMessage(notice),"out-of-range native16bit room cannot silently alias2500");notice.nativeRoomNumber=1000;notice.character="other";Check(!guarded.Notify_PrivateMessage(notice),"other owner refused");notice.character=world.characterId;notice.text="😀";Check(!guarded.Notify_PrivateMessage(notice),"unrepresentable payload refused");
  Check(!guarded.Request_ClosePrivateChat(thread.id,"stale")&&!guarded.Request_PrivateText(thread.id,"stale","hello"),"stale UI cannot target current thread");Check(guarded.Request_ClosePrivateChat(thread.id,world.commanderSession)&&!guarded.Request_PrivateText(thread.id,world.commanderSession,"hello"),"closed sender identity cannot be reused by stale input");
  Check(guarded.Notify_PrivateMessage(Notice("Hero        >new"))&&guarded.PrivateChats.Single().id!=thread.id,"reopened name gets a new local identity");var next=guarded.PrivateChats.Single();next.sender="mutated";next.lines[0]=new NativeChatLine("mutated",1);Check(guarded.PrivateChats.Single().sender=="Hero        "&&guarded.PrivateChats.Single().lines[0].Text==">new","view detached");
  Check(guarded.AcceptsPrivateText(new string('x',60))&&!guarded.AcceptsPrivateText(new string('x',61))&&guarded.AcceptsPrivateText(new string('가',30))&&!guarded.AcceptsPrivateText(new string('가',31)),"bounded CP949 edit adapter keeps60native bytes");Check(!guarded.Request_PrivateText(next.id,world.commanderSession,new string('x',61))&&!guarded.Request_PrivateText(next.id,world.commanderSession,"😀"),"unrepresentable/over-limit typed input rejected without local echo");
  var inboxGate=new NativePrivateChatInbox();Check(inboxGate.Receive("Hero        >new",0,2500,codec)&&inboxGate.Views().Length==0,"source477520 room2500 prevents new private window");Check(inboxGate.Receive("Hero        >new",0,1000,codec)&&inboxGate.Receive("Hero        >more",0,2500,codec)&&inboxGate.Views().Single().count==2,"existing sender append precedes new-window room gate");
  for(int i=0;i<320;i++)inboxGate.Receive("Hero        >"+i,0,1000,codec);var bounded=inboxGate.Views().Single();Check(bounded.count==300&&bounded.lines.Length==6,"source300row/6visible bounds");Check(inboxGate.Scroll(bounded.id,-1)&&inboxGate.Views().Single().first==293,"private up scroll");Check(inboxGate.Close(bounded.id)&&!inboxGate.Scroll(bounded.id,1),"closed thread scroll blocked");
  client.Logout();Check(guarded.PrivateChats.Length==0&&!guarded.Request_PrivateText(next.id,world.commanderSession,"hello"),"logout hides/blocks previous inbox");client.OpenLocalProfile();service.BeginWorld(definition);Check(guarded.PrivateChats.Length==0&&!guarded.Notify_PrivateMessage(Notice("Hero        >old")),"new session drops old inbox and payload");guarded.isActiveAndEnabled=false;Check(guarded.PrivateChats.Length==0,"disabled component hides inbox");
  foreach(string phase in new[]{"settings","raw","intent"})foreach(string change in new[]{"logout","reconnect","disable","close"})
  {
   client.OpenLocalProfile();service.BeginWorld(definition);var current=service.WorldSnapshot();var m=new LocalServerManager(service);m.ConfigureClientText(codec);
   Check(m.Notify_PrivateMessage(new WorldPrivateMessageNotice{id=Id(),character=current.characterId,session=current.commanderSession,approved=true,text="Hero        >seed",nativeSuppression=0,nativeRoomNumber=1000}),"private callback fixture");var t=m.PrivateChats.Single();m.Request_WhisperReception(false);int raw=0,typed=0,settings=0;
   void Change(){if(change=="disable")m.isActiveAndEnabled=false;else if(change=="close")m.Request_ClosePrivateChat(t.id,current.commanderSession);else{client.Logout();if(change=="reconnect"){client.OpenLocalProfile();service.BeginWorld(definition);}}}
   m.Notify_ClientSettingsPacket+=_=>{settings++;if(phase=="settings")Change();};m.Notify_WorldTextPacket+=_=>{raw++;if(phase=="raw")Change();};m.Notify_PrivateTextRequested+=_=>{typed++;if(phase=="intent")Change();};
   Check(!m.Request_PrivateText(t.id,current.commanderSession,"reply"),"closed/private or expired-session request stops subsequent callbacks");
   Check(settings==1&&raw==(phase=="settings"?0:1)&&typed==(phase=="intent"?1:0),"private original7D/send order preserved until context cancellation");
   Check(m.PrivateChats.Length==0&&JsonSerializer.Serialize(store.data,Json)==baseline,"cancelled private dispatch grants no local echo or saved-state update");
  }
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,mismatches=0,unityPlay=false}));
 }
}
