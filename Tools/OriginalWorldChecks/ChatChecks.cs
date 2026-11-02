using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class ChatChecks
{
 static int checks;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 sealed class Store:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}public bool TryWrite(LocalAccountDocument value){data=value.Copy();return true;}}
 static void Main(string[] args)
 {
  var codec=JsonSerializer.Deserialize<NativeCodePage949>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Resources/OriginalCodePage949.json")),Json);
  using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var root=doc.RootElement;
  int kind=0;foreach(var color in root.GetProperty("colors").EnumerateArray())
  {int bgr=color.GetInt32(),rgb=(bgr&255)<<16|(bgr&0xff00)|(bgr>>16&255);Check(NativeWorldChatLog.Rgb(++kind)==rgb,"original DDColorMatch RGB literal");}
  foreach(var row in root.GetProperty("routes").EnumerateArray())
  {
   int k=row.GetProperty("kind").GetInt32();bool expected=row.GetProperty("received").GetArrayLength()!=0;
   var settings=new OriginalClientSettings{GlobalChat=row.GetProperty("enabled").GetBoolean()};
   Check((NativeWorldChatLog.IsMainKind(k)&&settings.AcceptsMessage(k)&&row.GetProperty("exists").GetBoolean())==expected,"original main-chat dispatch/filter; private messages use a separate route");
  }
  foreach(var row in root.GetProperty("splits").EnumerateArray())
  {
   Check(NativeWorldChatLog.TryLines(row.GetProperty("text").GetString(),codec,row.GetProperty("width").GetInt32(),out var lines),"known CP949 source accepted");
   Check(lines.SequenceEqual(row.GetProperty("lines").EnumerateArray().Select(r=>r.GetString())),"original43FCC0/43FDF0 split before boundary; CR/LF and fallback metrics");
  }
  foreach(var row in root.GetProperty("scrolls").EnumerateArray())
  {
   int count=row.GetProperty("count").GetInt32(),visible=row.GetProperty("visible").GetInt32(),first=row.GetProperty("first").GetInt32();var log=new NativeWorldChatLog();log.SetExpanded(visible==23);
   for(int i=0;i<count;i++)log.Append(i.ToString(),1,codec);
   while(log.First>first)log.Scroll(-1);log.Scroll(row.GetProperty("direction").GetInt32());Check(log.First==row.GetProperty("expected").GetInt32(),"native bounded scroll origin");
  }
  foreach(var row in root.GetProperty("resizes").EnumerateArray())
  {
   var log=new NativeWorldChatLog();bool expanded=row.GetProperty("expanded").GetBoolean();log.SetExpanded(!expanded);
   for(int i=0;i<row.GetProperty("count").GetInt32();i++)log.Append("fixture",1,codec);log.Scroll(-1);log.SetExpanded(expanded);var expected=row.GetProperty("expected");
   Check(log.VisibleLines==expected.GetProperty("visible").GetInt32()&&log.First==expected.GetProperty("first").GetInt32(),"native viewport resize returns to last page");
  }
  var retained=new NativeWorldChatLog();for(int i=0;i<305;i++)retained.Append(i.ToString(),1,codec);
  Check(retained.Count==300&&retained.Lines[0].Text=="5"&&retained.Visible().Last().Text=="304","300 retained native rows and incoming auto-scroll");
  retained.Scroll(-1);int before=retained.First;retained.Append("new",4,codec);Check(retained.First==before,"trimmed append returns zero extra scroll steps");
  Check(!retained.Append("😀",1,codec)&&!retained.Append("bad\0text",1,codec)&&!retained.Append("private",9,codec)&&retained.Count==300,"invalid codec and unknown/private categories preserve existing log");
  var store=new Store();var service=new EntryAccountService(store);var client=new EntryClient(service);var manager=new LocalServerManager(service);manager.ConfigureClientText(codec);
  var notice=new WorldMessageNotice{id=Guid.NewGuid().ToString("N"),approved=true,kind=4,text="원본 공지"};Check(!manager.Notify_WorldMessage(notice),"prelogin notification rejected");
  client.OpenLocalProfile();client.Create(100,"대화검사");store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();var definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Data/World/KaosiaWorld.json")),Json);service.BeginWorld(definition);var world=service.WorldSnapshot();notice.character=world.characterId;notice.session=world.commanderSession;
  string saved=JsonSerializer.Serialize(store.data,Json);int received=0;manager.Notify_WorldMessageReceived+=n=>{received++;n.text="observer changed";};
  Check(manager.Notify_WorldMessage(notice)&&received==1&&notice.text=="원본 공지"&&!manager.Notify_WorldMessage(notice),"approved current message delivered as copy once");
  var wrong=notice.Copy();wrong.id=Guid.NewGuid().ToString("N");wrong.character=wrong.id;Check(!manager.Notify_WorldMessage(wrong),"foreign owner rejected");
  wrong=notice.Copy();wrong.id=Guid.NewGuid().ToString("N");wrong.approved=false;Check(!manager.Notify_WorldMessage(wrong),"unapproved delivery rejected");
  wrong=notice.Copy();wrong.id=Guid.NewGuid().ToString("N");wrong.kind=9;Check(!manager.Notify_WorldMessage(wrong),"private channel cannot accidentally show in public log");
  Check(JsonSerializer.Serialize(store.data,Json)==saved,"chat never changes account/player save");client.Logout();Check(!manager.Notify_WorldMessage(notice),"old connection delivery rejected after logout");
  client.OpenLocalProfile();service.BeginWorld(definition);wrong=notice.Copy();wrong.id=Guid.NewGuid().ToString("N");Check(!manager.Notify_WorldMessage(wrong),"old session cannot reach new connection");
  wrong.session=service.WorldSnapshot().commanderSession;Check(manager.Notify_WorldMessage(wrong),"current reconnected session receives its own message");manager.isActiveAndEnabled=false;wrong.id=Guid.NewGuid().ToString("N");Check(!manager.Notify_WorldMessage(wrong),"disabled manager rejects message");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,mismatches=0,unityPlay=false}));
 }
}
