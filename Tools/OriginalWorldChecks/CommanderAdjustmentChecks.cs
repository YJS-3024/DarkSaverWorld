using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class CommanderAdjustmentChecks
{
 static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
 static string Id()=>Guid.NewGuid().ToString("N");static string State(object value)=>JsonSerializer.Serialize(value,Json);
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 sealed class Memory:IEntryAccountStore
 {public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}}
 sealed class Fixture
 {
  public Memory store=new Memory();public EntryAccountService service;public EntryClient client;public WorldDefinition definition;
  public WorldView View=>service.WorldSnapshot();
  public CommanderProfile Target=>View.commanders.rows[View.adjustmentTarget?.index??0].profile;
  static CharacterGrowthRecord Growth(int job,int level,int attack)=>new CharacterGrowthRecord{job=job,classLevel=level,totalLevel=level,revision=1,lastNotice=Id(),experience=(ulong)(100+job),points=7,magic=new[]{101,301},skills=new[]{10101},stats=new CharacterGrowthStats{attack=attack,defense=10,magicAttack=5,magicDefense=4}};
  public Fixture()
  {
   definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Json);definition.growthCatalog=JsonSerializer.Deserialize<CharacterGrowthCatalog>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/Growth/CharacterGrowth.json")),Json);
   service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"대상검사");var c=store.data.accounts[0].character;c.tutorialCompleted=true;
   c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,level=1,growth=Growth(100,1,65),goldKnown=true,gold=1234,bankKnown=true,bank=5678,inventory=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0}}};
   client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"isolated explicit primary growth");
   var r=new CommanderRoster{rows=new[]{new OwnedCommander{id="first",job=100,nativeRecord=0},new OwnedCommander{id="second",job=101,nativeRecord=1,profile=new CommanderProfile{level=2,healthKnown=true,hp=23,maxHp=210,mp=11,maxMp=40,growth=Growth(101,2,91),inventory=new[]{new WorldOwnedItem{id=Id(),item=3,slot=0}}}},new OwnedCommander{id="third",job=103,nativeRecord=2,profile=new CommanderProfile{level=3,healthKnown=true,hp=31,maxHp=180,mp=19,maxMp=65,growth=Growth(103,3,47),inventory=Array.Empty<WorldOwnedItem>()}}}};
   Check(service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=View.characterId,session=View.commanderSession,expectedRevision=0,state=r}).Accepted,"approved full secondary personal records");
  }
  public void Bind(int index)=>Check(service.SelectAdjustmentCommander(index,View.commanders.rows[index].id,View.commanders.revision).Accepted,"bind owned adjustment target");
  public void Options()
  {Check(service.ExecuteClassOptions(Id(),(r,v)=>new ClassOptionsNotice{id=Id(),approved=true,request=r.Copy(),classPoints=1,right=new ClassCandidate{job=120},options=Array.Empty<ClassCandidate>()}).Accepted,"explicit target candidates");}
  public void QueueClass()=>Check(service.RequestClassChange(1,0,View.classOptions.id,Id()).Accepted,"queue selected target class");
  public ClassChangeNotice ClassNotice()
  {var r=View.classChangeRequest;var g=Target.growth.Copy();string id=Id();g.job=r.job;g.revision++;g.lastNotice=id;return new ClassChangeNotice{id=id,approved=true,request=r.Copy(),state=g};}
  public StatAdjustmentNotice StatNotice(StatAdjustmentRequest r)
  {return new StatAdjustmentNotice{id=Id(),approved=true,request=r.Copy(),points=r.operation==StatAdjustmentOperation.State?10:r.points,costs=r.operation==StatAdjustmentOperation.State?new[]{1,1,1,1,1,1}:null,values=r.operation==StatAdjustmentOperation.State?Enumerable.Range(0,6).Select(i=>new AttributeTriplet{before=10+i,after=10+i,limit=100}).ToArray():r.values.Select(v=>v.Copy()).ToArray()};}
  public void Stats(StatAdjustmentOperation op)=>Check(service.ExecuteStatAdjustment(op,0,1,Id(),(r,v)=>StatNotice(r)).Accepted,"approved owned stat operation");
  public MagicLoadoutNotice MagicNotice(MagicLoadoutRequest r)
  {return new MagicLoadoutNotice{id=Id(),approved=true,request=r.Copy(),learned=r.operation==MagicLoadoutOperation.State?(int[])Target.growth.magic.Clone():null,selected=r.operation==MagicLoadoutOperation.State?new[]{301}:(int[])r.selected.Clone(),attributes=r.attributes?.Select(v=>v.Copy()).ToArray(),statPoints=r.statPoints};}
  public void Magic(MagicLoadoutOperation op,int index=-1)=>Check(service.ExecuteMagicLoadout(op,index,Id(),(r,v)=>MagicNotice(r)).Accepted,"approved owned magic operation");
  public void Reload(){service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"reconnected approved personal records");}
 }
 static void Main(string[] args)
 {
  root=args[0];var f=new Fixture();string primary=CommanderProfile.Capture(f.View.record).Fingerprint(),third=f.View.commanders.rows[2].profile.Fingerprint();
  var shared=f.View.record.Copy();f.Bind(1);Check(f.View.adjustmentTarget.index==1&&f.View.adjustmentProfile.growth.job==101&&f.View.record.growth.job==100,"separate inspected record and world avatar");
  var copy=f.View.adjustmentTarget;copy.identity="changed";Check(f.View.adjustmentTarget.identity=="second","view target detached");
  f.Options();Check(f.View.classOptions.request.target.identity=="second"&&f.View.classOptions.request.revision==1,"class candidates carry owned identity and own growth revision");f.QueueClass();var notice=f.ClassNotice();
  var altered=f.ClassNotice();altered.request.target.index=2;Check(!f.service.ReceiveClassChange(altered).Accepted&&f.Target.growth.job==101,"another target cannot approve the request");
  f.store.fail=true;Check(f.service.ReceiveClassChange(notice).Failure==WorldFailure.StorageUnavailable&&f.Target.growth.job==101&&f.View.classChangeRequest!=null,"failed owned class save preserves pending retry");f.store.fail=false;
  Check(f.service.ReceiveClassChange(notice).Accepted&&f.Target.growth.job==120&&f.View.commanders.rows[1].job==120&&f.Target.classChange.job==120,"owned class/receipt commit to selected row");
  Check(CommanderProfile.Capture(f.View.record).Fingerprint()==primary&&f.View.commanders.rows[2].profile.Fingerprint()==third,"owned class does not mutate primary or other character");
  Check(f.View.record.gold==shared.gold&&f.View.record.bank==shared.bank&&State(f.View.record.storage)==State(shared.storage)&&f.View.record.map==shared.map&&f.View.record.x==shared.x&&f.View.record.y==shared.y,"shared wallet/storage/map are preserved");
  Check(f.View.adjustmentTarget.rosterRevision==f.View.commanders.revision&&!f.service.ReceiveClassChange(notice).Accepted,"target follows own successful roster revision and result cannot replay");
  f.Stats(StatAdjustmentOperation.State);Check(f.View.statAdjustmentRequest==null&&f.View.statAdjustment.points==10,"owned raw stat state is transient");f.Stats(StatAdjustmentOperation.Step);
  Check(f.service.SelectAdjustmentCommander(2,"third",f.View.commanders.revision).Failure==WorldFailure.Busy,"dirty owned draft cannot silently move to another character");
  Check(f.service.RequestStatAdjustment(StatAdjustmentOperation.Confirm,-1,0,Id()).Accepted,"owned stat confirmation queued");var stat=f.StatNotice(f.View.statAdjustmentRequest);stat.effectiveStats=new CharacterGrowthStats{attack=99,defense=11,magicAttack=6,magicDefense=5};
  stat.health=new CharacterGrowthHealth{hp=21,maxHp=210,mp=9,maxMp=40};Check(f.service.ReceiveStatAdjustment(stat).Accepted&&f.Target.growth.stats.attack==99&&f.Target.hp==21&&f.Target.mp==9,"explicit owned stats and health apply to correct character");
  Check(CommanderProfile.Capture(f.View.record).Fingerprint()==primary&&f.Target.statAllocation.values[0].after==11&&f.Target.statAllocation.points==9,"primary wounds/gear untouched and owned allocation saved");
  f.Magic(MagicLoadoutOperation.State);Check(f.View.MagicLoadoutNeedsConfirm&&f.View.battleMagic==null,"secondary first selection does not become primary active magic");f.Magic(MagicLoadoutOperation.Confirm);
  Check(f.Target.magicLoadout.selected.SequenceEqual(new[]{301})&&f.Target.growth.magic.SequenceEqual(new[]{101,301})&&f.View.record.magicLoadout==null,"owned selection never unlearns or changes primary spells");
  f.Stats(StatAdjustmentOperation.State);f.Stats(StatAdjustmentOperation.Step);f.Magic(MagicLoadoutOperation.State);f.Magic(MagicLoadoutOperation.Add,0);f.Magic(MagicLoadoutOperation.Confirm);
  Check(f.Target.magicLoadout.selected.SequenceEqual(new[]{301,101})&&f.Target.statAllocation.points==9&&f.View.statAdjustment==null&&f.View.magicLoadout==null,"joint owned stat/magic confirmation saves atomically");
  Check(CommanderProfile.Capture(f.View.record).Fingerprint()==primary&&f.View.record.gold==1234&&f.View.record.bank==5678,"all secondary edits preserve primary and money");
  f.Reload();Check(f.View.adjustmentTarget==null&&f.View.commanders.rows[1].profile.magicLoadout.selected.SequenceEqual(new[]{301,101})&&f.View.commanders.rows[1].profile.hp==21,"reconnect restores personal changes without stale editor binding");
  Check(f.service.SelectAdjustmentCommander(1,"third",f.View.commanders.revision).Failure==WorldFailure.StaleRequest&&f.service.SelectAdjustmentCommander(1,"second",f.View.commanders.revision-1).Failure==WorldFailure.StaleRequest,"wrong owner/revision cannot bind");
  var unknown=f.View.commanders.Copy();unknown.rows[2].profile.growth=null;
  Check(f.service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=f.View.characterId,session=f.View.commanderSession,expectedRevision=f.View.commanders.revision,state=unknown}).Accepted,"explicit incomplete secondary profile remains incomplete");
  Check(f.service.SelectAdjustmentCommander(2,"third",f.View.commanders.revision).Failure==WorldFailure.RulesUnavailable,"no guessed growth for incomplete personal record");
  var changed=new Fixture();changed.Bind(1);changed.Options();changed.QueueClass();var old=changed.ClassNotice();var pushed=changed.View.commanders.Copy();pushed.rows[0].profile=null;
  Check(changed.service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=changed.View.characterId,session=changed.View.commanderSession,expectedRevision=changed.View.commanders.revision,state=pushed}).Accepted,"new authoritative roster revision");
  Check(!changed.service.ReceiveClassChange(old).Accepted&&changed.View.commanders.rows[1].job==101,"old secondary approval cannot cross roster replacement");
  var concurrent=new Fixture();concurrent.Bind(1);concurrent.Options();concurrent.QueueClass();old=concurrent.ClassNotice();concurrent.store.data.accounts[0].character.world.roster.revision++;
  Check(concurrent.service.ReceiveClassChange(old).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.roster.rows[1].job==101,"concurrent stored roster cannot be overwritten");
  var switcher=new Fixture();switcher.Bind(1);switcher.service.RequestStatAdjustment(StatAdjustmentOperation.State,-1,0,Id());var previous=switcher.StatNotice(switcher.View.statAdjustmentRequest);switcher.service.CancelStatAdjustment();switcher.Bind(2);
  Check(!switcher.service.ReceiveStatAdjustment(previous).Accepted&&switcher.View.adjustmentTarget.identity=="third","late response rejected after explicit target switch");
  var swap=new Fixture();swap.Bind(1);swap.Options();swap.QueueClass();old=swap.ClassNotice();Check(swap.service.ExecuteCommanderOperation(CommanderOperation.Swap,1,0,Id(),null).Accepted,"source-proven primary exchange");
  Check(swap.View.adjustmentTarget==null&&!swap.service.ReceiveClassChange(old).Accepted&&swap.View.record.growth.job==101,"exchange clears owned adjustment binding and old replies");
  var primaryEdit=new Fixture();primaryEdit.Options();primaryEdit.QueueClass();notice=primaryEdit.ClassNotice();Check(primaryEdit.service.ReceiveClassChange(notice).Accepted&&primaryEdit.View.record.roster.rows[0].job==120,"primary class keeps roster job cache consistent");
  var path=Path.Combine(Path.GetTempPath(),"ds-owned-adjustment-"+Id()+".json");try
  {var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var saved)&&saved.accounts[0].character.world.roster.rows[1].profile.magicLoadout.selected.Length==2,"actual DataContract owned profile roundtrip");}finally{if(File.Exists(path))File.Delete(path);}
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Production received-state class/stat/magic flows for explicitly owned secondary characters, guarded target/revision, save retry, primary isolation, reconnect and file serialization. No native raw wire/eligibility/cost derivation."}));
 }
}
