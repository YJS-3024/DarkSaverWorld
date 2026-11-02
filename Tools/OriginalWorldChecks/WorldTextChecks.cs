using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class WorldTextChecks
{
 static int checks;static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 sealed class Store:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}public bool TryWrite(LocalAccountDocument value){data=value.Copy();return true;}}
 static void Main(string[] args)
 {
  using var cases=JsonDocument.Parse(File.ReadAllText(args[1]));foreach(var row in cases.RootElement.EnumerateArray())
  {var p=NativeWorldTextPackets.Build(Convert.FromHexString(row.GetProperty("data").GetString()),row.GetProperty("channel").GetInt32());string expected=row.GetProperty("packet").ValueKind==JsonValueKind.Null?null:row.GetProperty("packet").GetString();Check((p==null?null:Convert.ToHexString(p).ToLowerInvariant())==expected,"native text packet including low-byte length/channel");}
  Check(Convert.ToHexString(NativeWorldTextPackets.SpaceMove()).ToLowerInvariant()=="7a01092fb0f8b0a3c0ccb5bf","F12 source command uses channel1, distinct from memo channel5");
  var store=new Store();var service=new EntryAccountService(store);var client=new EntryClient(service);var manager=new LocalServerManager(service);Check(!manager.Request_SpaceMove(),"pre-login request rejected");
  client.OpenLocalProfile();client.Create(100,"명령검사");store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();var json=new JsonSerializerOptions{IncludeFields=true};var definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Data/World/KaosiaWorld.json")),json);service.BeginWorld(definition);
  string before=JsonSerializer.Serialize(store.data,json);byte[] packet=null;WorldClientTextRequest request=null;
  manager.Notify_WorldTextPacket+=p=>packet=p;manager.Notify_WorldTextRequested+=r=>request=r;
  Check(manager.Request_SpaceMove()&&packet.SequenceEqual(NativeWorldTextPackets.SpaceMove())&&request.command=="/공간이동"&&request.character==service.WorldSnapshot().characterId&&request.session==service.WorldSnapshot().commanderSession,"authenticated local request has exact source bytes and current connection");
  packet[0]=0;Check(request.packet[0]==0x7a,"packet observer cannot corrupt request metadata");Check(JsonSerializer.Serialize(store.data,json)==before,"outbound command does not invent teleport, cost, reward or save changes");
  client.Logout();Check(!manager.Request_SpaceMove(),"post-logout requests rejected");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,mismatches=0,unityPlay=false}));
 }
}
