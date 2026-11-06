using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class PlayerIdentityChecks
{
 static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};static string Id()=>Guid.NewGuid().ToString("N");static string State(object value)=>JsonSerializer.Serialize(value,Json);
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 sealed class Memory:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}public bool TryWrite(LocalAccountDocument d){if(fail)return false;data=d.Copy();return true;}}
 sealed class Fixture
 {
  public Memory store=new Memory();public EntryAccountService service;public EntryClient client;public WorldDefinition definition;public LocalServerManager manager;
  public WorldView View=>service.WorldSnapshot();
  public Fixture()
  {
   definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Json);definition.growthCatalog=JsonSerializer.Deserialize<CharacterGrowthCatalog>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/Growth/CharacterGrowth.json")),Json);service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"정보검사");var c=store.data.accounts[0].character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,level=3,inventory=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0}}};
   client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"isolated known wounds, no identity defaults");manager=new LocalServerManager(service);manager.ConfigureClientText(JsonSerializer.Deserialize<NativeCodePage949>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Resources/OriginalCodePage949.json")),Json));
  }
  public PlayerIdentityNotice Notice()=>new PlayerIdentityNotice{id=Id(),approved=true,character=View.characterId,session=View.commanderSession,expectedRevision=View.record.identity?.revision??0,age=24,gender=1,spouse="영웅"};
  public void Reload(){service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"identity reconnect");}
 }
 static void Main(string[] args)
 {
  root=args[0];var f=new Fixture();Check(f.View.record.identity==null,"legacy profile has no guessed age, gender or marriage");var before=State(f.View.record);var notice=f.Notice();f.store.fail=true;
  Check(f.manager.Notify_PlayerIdentity(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"identity save failure is atomic");f.store.fail=false;Check(f.manager.Notify_PlayerIdentity(notice).Accepted&&f.View.record.identity.revision==1,"same approved header retries");
  Check(f.View.record.identity.age==24&&f.View.record.identity.gender==1&&f.View.record.identity.spouse=="영웅","explicit shared identity values applied");Check(knownPersonal(f.View.record)==knownPersonalFromJson(before),"identity does not change wounds, money, inventory or map");
  var detached=f.View.record.identity;detached.age=999;Check(f.View.record.identity.age==24,"identity snapshot detached");Check(f.manager.Notify_PlayerIdentity(notice).Failure==WorldFailure.StaleRequest,"same notice cannot replay");
  var partial=f.Notice();partial.age=null;partial.gender=2;partial.spouse=null;Check(f.manager.Notify_PlayerIdentity(partial).Accepted&&f.View.record.identity.age==24&&f.View.record.identity.spouse=="영웅"&&f.View.record.identity.gender==2,"partial header preserves unprovided known fields");
  var clear=f.Notice();clear.age=null;clear.gender=null;clear.spouse="";Check(f.manager.Notify_PlayerIdentity(clear).Accepted&&f.View.record.identity.spouse=="","explicit empty spouse remains empty, no invented text");
  before=State(f.View.record);foreach(string field in new[]{"owner","session","id","revision","denied","missing","gender-negative","gender-unknown","control","codec"})
  {
   var bad=f.Notice();switch(field){case "owner":bad.character=Id();break;case "session":bad.session=Id();break;case "id":bad.id="bad";break;case "revision":bad.expectedRevision--;break;case "denied":bad.approved=false;break;case "missing":bad.age=bad.gender=null;bad.spouse=null;break;case "gender-negative":bad.gender=-1;break;case "gender-unknown":bad.gender=3;break;case "control":bad.spouse="bad\nname";break;case "codec":bad.spouse="😀";break;}
   Check(!f.manager.Notify_PlayerIdentity(bad).Accepted&&State(f.View.record)==before,"invalid identity preserves all state: "+field);
  }
  f.Reload();Check(f.View.record.identity.age==24&&f.View.record.identity.gender==2&&f.View.record.identity.spouse=="","approved shared header persists");
  var moved=new Fixture();notice=moved.Notice();Check(moved.service.MoveWorld(21,22).Accepted,"start native grid move");before=State(moved.View.record);Check(moved.service.ReceivePlayerIdentity(notice).Failure==WorldFailure.Busy&&State(moved.View.record)==before,"identity cannot commit over moving player state");moved.service.AdvanceWorld(100000);Check(moved.service.ReceivePlayerIdentity(notice).Accepted&&moved.View.record.y==22,"deferred header applies after movement without undoing position");
  var concurrent=new Fixture();var other=new EntryAccountService(concurrent.store);new EntryClient(other).OpenLocalProfile();other.BeginWorld(concurrent.definition);var w=other.WorldSnapshot();
  Check(other.ReceivePlayerIdentity(new PlayerIdentityNotice{id=Id(),approved=true,character=w.characterId,session=w.commanderSession,expectedRevision=0,age=31,gender=0}).Accepted,"second service commits newer shared header");
  Check(concurrent.service.ReceivePlayerIdentity(concurrent.Notice()).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.identity.age==31,"older client cannot overwrite newer shared identity");
  Check(concurrent.service.EquipWorldItem(concurrent.View.record.inventory[0].id,2,Id()).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.identity.age==31,"unrelated old item commit cannot erase newer header");
  var inventory=new Fixture();notice=inventory.Notice();inventory.store.data.accounts[0].character.world.inventory[0].item=3;Check(inventory.service.ReceivePlayerIdentity(notice).Failure==WorldFailure.StaleRequest&&inventory.store.data.accounts[0].character.world.inventory[0].item==3,"new metadata cannot erase concurrently changed unversioned inventory");
  var owner=new Fixture();notice=owner.Notice();owner.client.Logout();Check(!owner.manager.Notify_PlayerIdentity(notice).Accepted,"logout rejects old header");owner.client.OpenLocalProfile();owner.service.BeginWorld(owner.definition);Check(!owner.manager.Notify_PlayerIdentity(notice).Accepted,"old header cannot cross reconnect");
  var path=Path.Combine(Path.GetTempPath(),"ds-identity-"+Id()+".json");try{var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var document)&&document.accounts[0].character.world.identity.age==24&&document.accounts[0].character.world.identity.spouse=="","actual DataContract identity roundtrip");}finally{if(File.Exists(path))File.Delete(path);}
  var record=f.View.record.Copy();record.identity.gender=4;Check(!WorldRecord.Valid(record),"malformed persisted native gender rejected");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Approved partial shared info-manager header; source gender table used by UI. Production account/service and manager partial with isolated component dispatcher; session/revision/replay, concurrency/save failure and actual temporary file checks. No age/marriage defaults or native wire layout."}));
 }
 static string knownPersonal(WorldRecord record){var copy=record.Copy();copy.identity=null;return State(copy);}
 static string knownPersonalFromJson(string json)=>knownPersonal(JsonSerializer.Deserialize<WorldRecord>(json,Json));
}
