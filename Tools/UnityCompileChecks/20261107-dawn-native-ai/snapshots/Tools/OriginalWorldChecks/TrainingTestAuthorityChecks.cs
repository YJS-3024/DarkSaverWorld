using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;
namespace UnityEngine {
 [AttributeUsage(AttributeTargets.Field)]public class SerializeField:Attribute{}
 public class ScriptableObject {}
 public static class Time {public static double realtimeSinceStartupAsDouble;}
 public class GameObject {public bool Active=true;public void SetActive(bool active){Active=active;}}
 public class TextAsset {public string text;}
 public static class Resources {public static string Root;public static Func<int,string> Override; public static T Load<T>(string key)where T:class {
  if(key!="TrainingTestLayoutCatalog")throw new Exception("Unexpected resource key");
  return new TrainingTestLayoutCatalog {maps=new[]{101,102},layouts=new[]{101,102}.Select(n=>new TextAsset{text=Override?.Invoke(n)??File.ReadAllText(Path.Combine(Root,"Assets/_Original/Resources/TrainingTestLayouts",n+".json"))}).ToArray()}as T;
 }}
 public static class JsonUtility {public static T FromJson<T>(string text){try{return JsonSerializer.Deserialize<T>(text,new JsonSerializerOptions{IncludeFields=true});}catch(JsonException e){throw new ArgumentException("Invalid JSON",e);}}}
 public static class Debug {public static void Log(object v){}public static bool FailWarnings=true;public static string LastWarning;public static void LogWarning(object v){LastWarning=v.ToString();if(FailWarnings)throw new Exception(v.ToString());}}
}
namespace DarkSaver.Original.Entry {
 public sealed partial class LocalServerManager {
  private EntryAccountService service;
  public event Action<WorldView> Notify_TrainingInitialRequested;
  public bool isActiveAndEnabled=true;
  public event Action<WorldReply> Notify_World,Notify_MapEntered,Notify_TrainingChanged,Notify_BattleChanged;
  public WorldReply RelayWorld(WorldReply reply)=>WorldRequest(_=>reply,true);
  public WorldReply RelayTraining(WorldReply reply)=>TrainingResult(_=>reply);
  public WorldReply RelayBattle(WorldReply reply)=>BattleResult(_=>reply);
  public WorldReply Request_EnterMap(int map)=>new WorldReply(WorldFailure.InvalidStage);
  public WorldReply Request_EnterInterior(string gate)=>service.EnterWorldPortal(gate,false);
  public Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> TrainingAuthority,BattleAuthority;
  public object NativeTrainingAuthority,NativeBattleAuthority;
  public WorldView WorldSnapshot=>service?.WorldSnapshot();
  public Action<WorldReply> AfterUnits;
  public WorldReply Notify_TrainingUnits(TrainingUnitsNotice n){var reply=TrainingResult(s=>s.ReceiveTrainingUnits(n));AfterUnits?.Invoke(reply);return reply;}
  public Action<WorldReply> AfterTicks;
  public WorldReply Notify_TrainingTicks(BattleTickNotice n){var reply=TrainingResult(s=>s.ReceiveTrainingTicks(n));AfterTicks?.Invoke(reply);return reply;}
  public LocalServerManager(EntryAccountService s){service=s;}
  public void Begin()=>BeginTrainingTest(WorldSnapshot);
  private Func<int,int> SoldierTargetChoice=count=>0;
  private Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> AutomationAuthority(bool training)=>training?TrainingAuthority??BattleAuthority:BattleAuthority;
  public void Automate(double seconds){UnityEngine.Time.realtimeSinceStartupAsDouble=seconds;PumpBattleRegroup();}
  public void DisableTest()=>OnDisable();
  public void Tick(double dt){service.AdvanceWorld(dt*1000);PumpTrainingTest(dt);}
 }
}
internal static class TrainingTestAuthorityChecks {
 private static int checks;
 private static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 private sealed class Memory:IEntryAccountStore {
 public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public int writes;
 public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}
 public bool TryWrite(LocalAccountDocument d){if(fail)return false;writes++;data=d.Copy();return true;}}
 public static void Main(string[] args){
 string root=args[0];UnityEngine.Resources.Root=root;var opt=new JsonSerializerOptions{IncludeFields=true};
 T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",path)),opt);
 var store=new Memory();var server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"수련테스트");
 var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=200,maxHp=200,mp=45,maxMp=45};client.OpenLocalProfile();
 var def=Read<WorldDefinition>("Data/World/KaosiaWorld.json");def.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");def.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
 Check(server.BeginWorld(def).Accepted,"begin world");var gate=def.Map(1000).portals.First(p=>p.targetMap==101);
 Check(server.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"walk to training");server.AdvanceWorld(100000);Check(server.EnterWorldPortal(gate.id,true).Accepted,"enter training");
 var manager=new LocalServerManager(server);manager.Begin();var v=manager.WorldSnapshot;
 Check(v.training?.units.Length==11,"ten authored monsters plus commander");
 foreach(var unit in v.training.units.Where(u=>u.id>=400))
 {
  var raw=def.combatCatalog.Job(unit.job).baseStats;
  Check(unit.hp==raw.hp&&unit.maxHp==raw.hp&&unit.mp==Math.Max(0,raw.mp)&&unit.effectiveStats.attack==raw.attack&&unit.effectiveStats.defense==raw.defense,"source monster and mercenary-job stats "+unit.id);
  Check(unit.baseStats.mp==raw.mp&&unit.effectiveStats.nativeValues==null,"preserve unused source sentinel without fabricating received native stats");
 }
 Check(v.training.Commander.hp==200&&v.training.Commander.mp==45,"preserve commander health");
 Check(v.training.Commander.effectiveStats.attack==65&&v.training.Commander.effectiveStats.defense==45&&v.training.Commander.effectiveStats.magicAttack==45&&v.training.Commander.effectiveStats.magicDefense==35,"unknown commander stats use original fighter base values");
 // New optional source metadata must survive the actual account serializer without breaking old records.
 {
  var raw=def.combatCatalog.jobs.First(j=>j.baseStats.magicAttack==-1||j.baseStats.mp==-1);
  var actor=server.TrainingTestUnit(0,raw.job,1,1);
  var record=new ScenarioBattleRecord {map=26,revision=1,lastNotice=Guid.NewGuid().ToString("N"),units=new[]{actor},lives=new[]{new BattleLifeRecord{id=0,life=1}}};
  var serializer=new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ScenarioBattleRecord));
  using(var stream=new MemoryStream()){
   serializer.WriteObject(stream,record);stream.Position=0;var restored=(ScenarioBattleRecord)serializer.ReadObject(stream);
   Check(ScenarioBattleRecord.Valid(restored)&&JsonSerializer.Serialize(restored.units[0].baseStats,opt)==JsonSerializer.Serialize(raw.baseStats,opt),"source base and sentinel fields survive actual DataContract roundtrip");
   Check(restored.units[0].effectiveStats.magicAttack>=0&&restored.units[0].mp>=0,"source sentinel remains separate from nonnegative combat values");
  }
  actor.baseStats=null;
  using(var stream=new MemoryStream()){
   serializer.WriteObject(stream,record);string json=System.Text.Encoding.UTF8.GetString(stream.ToArray());stream.Position=0;var restored=(ScenarioBattleRecord)serializer.ReadObject(stream);
   Check(!json.Contains("baseStats")&&restored.units[0].baseStats==null&&ScenarioBattleRecord.Valid(restored),"legacy record without optional source metadata remains readable");
  }
 }
 // AP uses the actual native pulse/menu ingress, not direct writes in the presentation.
 var apView=manager.WorldSnapshot;
 Check(manager.Notify_TrainingTicks(new BattleTickNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=apView.characterId,session=apView.campaignSession,expectedRevision=apView.training.revision,unitLife=apView.training.Commander.life,packet=OriginalBattleTicks.MenuPacket(0,16,89)}).Accepted,"receive native AP update");
 manager.Tick(.49);Check(manager.WorldSnapshot.training.Commander.ap==16,"AP waits for half-second boundary");
 manager.Tick(.01);Check(manager.WorldSnapshot.training.Commander.ap==17,"native pulse restores one at half second");
 manager.Tick(1.5);Check(manager.WorldSnapshot.training.Commander.ap==20,"AP two per second caps at twenty");
 Check(manager.WorldSnapshot.record.growth.experience==0&&manager.WorldSnapshot.training.Commander.hp==200,"AP regeneration does not pay XP or heal");
 // A periodic AP packet cannot consume or cancel a pending command request.
 apView=manager.WorldSnapshot;
 Check(manager.Notify_TrainingTicks(new BattleTickNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=apView.characterId,session=apView.campaignSession,expectedRevision=apView.training.revision,unitLife=apView.training.Commander.life,packet=OriginalBattleTicks.MenuPacket(0,16,89)}).Accepted,"deplete AP for pending request fixture");
 string queuedId=Guid.NewGuid().ToString("N");Check(server.RequestTrainingAction(TrainingAction.Rest,-1,0,0,queuedId).Accepted,"queue delayed command");
 manager.Tick(.5);Check(manager.WorldSnapshot.training.pending?.id==queuedId&&manager.WorldSnapshot.training.Commander.ap==16,"AP waits instead of cancelling pending command");
 Check(server.CancelTrainingAction(queuedId).Accepted,"cancel isolated pending command");manager.Tick(.5);Check(manager.WorldSnapshot.training.Commander.ap==17,"AP resumes after pending command clears");
 // Exercise production authority through real queued actions; approach the first pack.
 var monster=v.training.units.First(u=>u.id==405);
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,5,7,manager.TrainingAuthority).Accepted,"first move request");server.AdvanceWorld(100000);
 Check(manager.WorldSnapshot.training.Commander.direction==3,"completed leftward move retains final facing");
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,2,7,manager.TrainingAuthority).Accepted,"second move request");server.AdvanceWorld(100000);
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,monster.x,monster.y-1,manager.TrainingAuthority).Accepted,"real move request");server.AdvanceWorld(100000);
 Check(server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority).Accepted,"real basic attack");
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==76,"fighter65 against defense41 deals24 from source stats");
 Check(manager.WorldSnapshot.training.numberEffects.Any(e=>e.unit==monster.id&&e.delta==-24),"native damage number reflects stat-based hit");
 Check(manager.WorldSnapshot.training.attackEffects.Any(e=>e.unit==0)&&manager.WorldSnapshot.training.Unit(monster.id).direction==0,"attack animation and target final facing are connected");
 while(manager.WorldSnapshot.training.Unit(monster.id).hp>0)Check(server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority).Accepted,"repeated real basic attacks");
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==0,"source HP reaches zero");
 Check(manager.WorldSnapshot.training.deaths.Any(e=>e.unit==monster.id&&e.life==1&&e.held),"native death presentation waits for damage number");
 Check(manager.WorldSnapshot.record.growth.experience==23,"first death awards source job401 experience23");
 var xpBefore=manager.WorldSnapshot.record.growth.experience;
 Check(!server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority).Accepted&&manager.WorldSnapshot.record.growth.experience==xpBefore,"dead lifetime cannot pay twice");manager.Tick(0);manager.Tick(29.99);
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==0,"not before thirty seconds");manager.Tick(0.02);
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==monster.maxHp&&manager.WorldSnapshot.training.Unit(monster.id).life==2,"fresh lifetime respawns after thirty seconds");
 Check(manager.WorldSnapshot.record.growth.experience==23,"timer and respawn do not award experience");
 // A corpse tile occupied by the player must postpone respawn without moving the spawn.
 while(manager.WorldSnapshot.training.Unit(monster.id).hp>0)Check(server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority).Accepted,"kill fresh lifetime");manager.Tick(0);
 Check(manager.WorldSnapshot.record.growth.experience==46,"fresh lifetime pays once more");
 Check(!server.ExecuteTrainingAction(TrainingAction.Move,-1,monster.x,monster.y,manager.TrainingAuthority).Accepted,"corpse remains occupied until native retirement");server.AdvanceWorld(3000);
 Check(manager.WorldSnapshot.training.Unit(monster.id).nativeEnabled==false&&manager.WorldSnapshot.training.deaths.Single(e=>e.unit==monster.id).Expired,"original corpse retires before server respawn");
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,monster.x,monster.y,manager.TrainingAuthority).Accepted,"occupy dead monster tile");server.AdvanceWorld(100000);manager.Tick(30);
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==0,"occupied spawn remains dead after thirty seconds");
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,monster.x,monster.y-1,manager.TrainingAuthority).Accepted,"vacate spawn tile");server.AdvanceWorld(100000);manager.Tick(0);
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==monster.maxHp&&manager.WorldSnapshot.training.Unit(monster.id).life==3,"vacated spawn returns at exact authored coordinates with fresh life");
 int beforeAiHp=manager.WorldSnapshot.record.hp;manager.Tick(1);
 Check(manager.WorldSnapshot.record.hp==beforeAiHp-46&&manager.WorldSnapshot.training.numberEffects.Any(e=>e.unit==0&&e.delta==-46),"adjacent AI hit emits authoritative HP and native damage number");
 Check(manager.WorldSnapshot.training.attackEffects.Any(e=>e.unit==monster.id)&&manager.WorldSnapshot.record.growth.experience==46,"enemy attack animation does not pay XP");

 // A failed durable killing hit must leave both the live monster and XP unchanged.
 while(manager.WorldSnapshot.training.Unit(monster.id).hp>24)Check(server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority).Accepted,"approach final hit");
 int finalHitHp=manager.WorldSnapshot.training.Unit(monster.id).hp;store.fail=true;var failed=server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority);store.fail=false;
 Check(failed.Failure==WorldFailure.StorageUnavailable&&manager.WorldSnapshot.training.Unit(monster.id).hp==finalHitHp&&manager.WorldSnapshot.record.growth.experience==46,"failed save preserves HP and XP atomically");
 Check(server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority).Accepted&&manager.WorldSnapshot.record.growth.experience==69,"retry pays exactly one kill");
 Check(server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,manager.TrainingAuthority).Accepted,"rest request");
 Check(server.EscapeTraining().Accepted,"escape training");manager.Tick(0);Check(manager.TrainingAuthority==null,"training authority cleaned on escape");
 Check(store.data.accounts.Single().character.world.growth.experience==69,"XP stored durably after escape");
 var recoveryHost=new RecoveryCheckHost(manager,def.combatCatalog);var beforeRecovery=manager.WorldSnapshot.record;
 store.fail=true;recoveryHost.Run();store.fail=false;
 Check(manager.WorldSnapshot.record.hp==beforeRecovery.hp&&manager.WorldSnapshot.record.growth.revision==beforeRecovery.growth.revision,"editor recovery storage failure preserves health and growth revision");
 Check(recoveryHost.Run().Contains("회복하고 저장")&&manager.WorldSnapshot.record.hp==beforeRecovery.maxHp&&manager.WorldSnapshot.record.mp==beforeRecovery.maxMp,"actual editor recovery command restores health outside combat");
 Check(manager.WorldSnapshot.record.growth.experience==69&&manager.WorldSnapshot.record.gold==beforeRecovery.gold,"editor recovery preserves earned XP and money");
 long recoveredRevision=manager.WorldSnapshot.record.growth.revision;recoveryHost.Run();Check(manager.WorldSnapshot.record.growth.revision==recoveredRevision,"healthy recovery click does not write again");

 // Separate memory store reload: unknown health must no longer block combat initialization.
 var saved=store.data.accounts.Single().character;saved.world.healthKnown=false;saved.world.hp=saved.world.maxHp=saved.world.mp=saved.world.maxMp=0;
 client.OpenLocalProfile();Check(server.BeginWorld(def).Accepted,"reload unknown health fixture");
 Check(server.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"unknown health fixture doorway");server.AdvanceWorld(100000);
 Check(server.EnterWorldPortal(gate.id,true).Accepted,"unknown health fixture entry");manager.Begin();v=manager.WorldSnapshot;
 Check(v.training!=null&&v.training.Commander.hp==200&&v.training.Commander.maxHp==200&&v.training.Commander.mp==45&&v.training.Commander.maxMp==45,"unknown health initializes provisional commander values");
 var combatRecovery=manager.WorldSnapshot.record.growth.revision;recoveryHost.Run();Check(manager.WorldSnapshot.record.growth.revision==combatRecovery,"editor recovery rejects active battle without saving");

 // Independent class fixtures: use original values for missing health; preserve known injury/stat outcomes.
 foreach(var sample in new (int job,int hp,int mp,int attack,int defense,int ma,int md)[]{(100,200,45,65,45,45,35),(101,210,40,70,48,40,30),(103,180,65,60,40,60,45)})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(sample.job,"기본능력검사");
  var ch=memory.data.accounts.Single().character;ch.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"begin class fixture "+sample.job);
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"walk class fixture");svc.AdvanceWorld(100000);
  Check(svc.EnterWorldPortal(gate.id,true).Accepted,"enter class fixture");var mgr=new LocalServerManager(svc);mgr.Begin();var actor=mgr.WorldSnapshot.training.Commander;
  Check(actor.hp==sample.hp&&actor.maxHp==sample.hp&&actor.mp==sample.mp&&actor.maxMp==sample.mp,"class base HP/MP "+sample.job);
  Check(actor.effectiveStats.attack==sample.attack&&actor.effectiveStats.defense==sample.defense&&actor.effectiveStats.magicAttack==sample.ma&&actor.effectiveStats.magicDefense==sample.md,"class base combat stats "+sample.job);
  Check(actor.effectiveStats.nativeValues==null,"do not invent mapping to native sixteen received values");
  Check(svc.EscapeTraining().Accepted,"leave class fixture");mgr.Tick(0);
  // Known wounded/leveled server result takes precedence over base values.
  ch=memory.data.accounts.Single().character;ch.world.hp=17;ch.world.mp=3;ch.world.level=1;
  ch.world.growth=new CharacterGrowthRecord {job=sample.job,classLevel=1,totalLevel=1,revision=1,lastNotice=Guid.NewGuid().ToString("N"),stats=new CharacterGrowthStats {attack=321,defense=123,magicAttack=234,magicDefense=345,nativeValues=Enumerable.Range(1,16).Select(n=>(short)n).ToArray()}};
  def.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"restore known stats fixture");
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"known fixture doorway");svc.AdvanceWorld(100000);
  Check(svc.EnterWorldPortal(gate.id,true).Accepted,"known fixture training");mgr.Begin();actor=mgr.WorldSnapshot.training.Commander;
  Check(actor.hp==17&&actor.mp==3&&actor.effectiveStats.attack==321&&actor.effectiveStats.defense==123,"known HP MP and received stats preserved");
  Check(actor.effectiveStats.nativeValues.SequenceEqual(Enumerable.Range(1,16).Select(n=>(short)n)),"received sixteen native values preserved");
  actor.effectiveStats.nativeValues[0]=999;Check(mgr.WorldSnapshot.training.Commander.effectiveStats.nativeValues[0]==1,"native stat snapshots cannot mutate live or saved stats");
 }
 // Actual approved effective-stat changes drive damage; source metadata is not a forced constant.
 foreach(var sample in new[]{(attack:120,defense:41,hp:21),(attack:65,defense:61,hp:96),(attack:40,defense:41,hp:100),(attack:41,defense:41,hp:100),(attack:int.MaxValue,defense:0,hp:0)})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"피해수치검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"damage fixture world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"damage fixture doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"damage fixture entry");var mgr=new LocalServerManager(svc);mgr.Begin();
  foreach(var tile in new[]{(5,7),(2,7),(2,10)}){Check(svc.ExecuteTrainingAction(TrainingAction.Move,-1,tile.Item1,tile.Item2,mgr.TrainingAuthority).Accepted,"damage fixture approach");svc.AdvanceWorld(100000);}
  var current=mgr.WorldSnapshot;var units=current.training.units.Select(u=>u.Copy()).ToArray();units.First(u=>u.id==0).effectiveStats.attack=sample.attack;units.First(u=>u.id==405).effectiveStats.defense=sample.defense;
  Check(mgr.Notify_TrainingUnits(new TrainingUnitsNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=current.characterId,session=current.campaignSession,expectedRevision=current.training.revision,units=units}).Accepted,"receive explicit effective stats");
  Check(svc.ExecuteTrainingAction(TrainingAction.Attack,405,0,0,mgr.TrainingAuthority).Accepted&&mgr.WorldSnapshot.training.Unit(405).hp==sample.hp,"actual hit uses current attack and defense "+sample);
  Check(mgr.WorldSnapshot.training.Unit(0).baseStats.attack==65,"received effective values do not rewrite source table metadata");
  Check(mgr.WorldSnapshot.record.growth.experience==(sample.hp==0?23ul:0ul),"zero or nonlethal hits never pay kill XP");
 }
 Check(LocalServerManager.TrainingPhysicalDamage(new TrainingUnit(),new TrainingUnit())==null,"missing stats never fall back to fixed one");
 Check(LocalServerManager.TrainingPhysicalDamage(new TrainingUnit{effectiveStats=new CharacterGrowthStats{attack=-1}},new TrainingUnit{effectiveStats=new CharacterGrowthStats()})==null,"invalid stats do not produce a fabricated hit");
 // Each adjacent enemy resolves its own attack/defense and native single-target notice.
 foreach(string mode in new[]{"normal","disable","failed-save"})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"다중반격검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"enemy damage world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"enemy damage doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"enemy damage entry");var mgr=new LocalServerManager(svc);mgr.Begin();
  var current=mgr.WorldSnapshot;var units=current.training.units.Select(u=>u.Copy()).ToArray();var first=units.First(u=>u.id==401);first.x=10;first.y=6;var second=units.First(u=>u.id==405);second.x=11;second.y=7;
  Check(mgr.Notify_TrainingUnits(new TrainingUnitsNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=current.characterId,session=current.campaignSession,expectedRevision=current.training.revision,units=units}).Accepted,"two adjacent source monsters");
  int attacks=0;mgr.AfterUnits=r=>{if(r.Accepted){attacks++;if(mode=="disable")mgr.DisableTest();}};
  if(mode=="failed-save")memory.fail=true;mgr.Tick(1);memory.fail=false;
  if(mode=="normal")Check(mgr.WorldSnapshot.record.hp==93&&attacks==2&&mgr.WorldSnapshot.training.attackEffects.Count(e=>e.unit==401||e.unit==405)==2,"source attacks deal61 and46 independently and emit accepted feedback");
  if(mode=="normal"){mgr.Tick(1);Check(mgr.WorldSnapshot.record.hp==0&&!mgr.WorldSnapshot.record.ghost&&mgr.WorldSnapshot.record.growth.experience==0,"stat-based enemy attacks clamp at death without ghost or XP gifts in training");}
  if(mode=="disable")Check(mgr.WorldSnapshot==null&&attacks==1&&memory.data.accounts.Single().character.world.hp==139,"first damage callback teardown blocks remaining attackers");
  if(mode=="failed-save"){Check(mgr.WorldSnapshot.record.hp==200&&attacks==0,"failed enemy hit leaves health unchanged");mgr.Tick(1);Check(mgr.WorldSnapshot.record.hp==93&&attacks==2,"retry resolves each enemy once without partial damage");}
 }
 // Tier-two request selects authored102, rather than reusing the101 roster.
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"이단수련검사");
  var ch=memory.data.accounts.Single().character;ch.tutorialCompleted=true;ch.world=new WorldRecord {level=1,growth=new CharacterGrowthRecord {job=105,classLevel=1,totalLevel=1,revision=1,lastNotice=Guid.NewGuid().ToString("N")}};
  cl.OpenLocalProfile();Check(svc.BeginWorld(def).Accepted,"begin tier two fixture");
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"walk tier two doorway");svc.AdvanceWorld(100000);
  Check(svc.EnterWorldPortal(gate.id,true).Accepted&&svc.WorldSnapshot().record.map==102,"tier chooses102");
  var mgr=new LocalServerManager(svc);mgr.Begin();var battle=mgr.WorldSnapshot.training;
  Check(battle?.units.Length==11&&battle.Unit(410)?.job==403&&battle.Unit(406)==null,"102 authored sparse unit IDs preserved");
  Check(battle.Unit(404).job==303&&battle.Unit(404).x==17&&battle.Unit(404).y==3,"102 boss job and coordinate preserved");
 }
 // All source categories share the same stat factory, including soldier/mercenary jobs.
 foreach(var row in def.combatCatalog.jobs)
 {
  var actor=server.TrainingTestUnit(400,row.job,1,1);
  Check(actor!=null&&actor.maxHp==row.baseStats.hp&&actor.baseStats.movement==row.baseStats.movement&&actor.effectiveStats.attack==row.baseStats.attack,"shared source unit factory "+row.job);
 }
 // Invalid training exports must remain a recoverable initial-response wait, with no partial growth save.
 foreach(string failure in new[]{"json","map","size","unit","duplicate","overlap","empty","job"})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"배치검증검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"layout guard world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"layout guard doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"layout guard entry");
  var node=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(root,"Assets/_Original/Resources/TrainingTestLayouts/101.json")));var units=node["units"].AsArray();
  if(failure=="map")node["mapKey"]="102";
  if(failure=="size")node["width"]=21;
  if(failure=="unit")units[0]["unit"]=1000;
  if(failure=="duplicate")units[1]["unit"]=units[0]["unit"].GetValue<int>();
  if(failure=="overlap"){units[1]["x"]=units[0]["x"].GetValue<int>();units[1]["y"]=units[0]["y"].GetValue<int>();}
  if(failure=="empty")units.Clear();
  if(failure=="job")units[0]["job"]=99999;
  string payload=failure=="json"?"{not valid JSON":node.ToJsonString();
  int before=memory.writes;var mgr=new LocalServerManager(svc);
  UnityEngine.Debug.LastWarning=null;UnityEngine.Debug.FailWarnings=false;UnityEngine.Resources.Override=map=>map==101?payload:null;
  try{mgr.Begin();}finally{UnityEngine.Debug.FailWarnings=true;UnityEngine.Resources.Override=null;}
  Check(mgr.WorldSnapshot.training==null&&mgr.TrainingAuthority==null&&memory.writes==before&&mgr.WorldSnapshot.record.growth==null,"invalid export does not install combat or write growth "+failure);
  Check(!string.IsNullOrEmpty(UnityEngine.Debug.LastWarning),"invalid export produces actionable warning "+failure);
  mgr.Begin();Check(mgr.WorldSnapshot.training?.units.Length==11&&mgr.TrainingAuthority!=null,"corrected export can initialize same entry "+failure);
 }
 // Existing hired ownership becomes controlled slots, without granting extra mercenaries.
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"용병능력검사");
  var ch=memory.data.accounts.Single().character;ch.tutorialCompleted=true;ch.world=new WorldRecord {hiredJob=200,hiredCount=2};
  cl.OpenLocalProfile();Check(svc.BeginWorld(def).Accepted,"begin hired fixture");
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"hired fixture doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"hired fixture entry");
  var mgr=new LocalServerManager(svc);mgr.Begin();var battle=mgr.WorldSnapshot.training;
  Check(battle.units.Length==13&&battle.Unit(1).job==200&&battle.Unit(2).job==200,"owned count only in controlled slots");
  Check(battle.Unit(1).hp==140&&battle.Unit(1).effectiveStats.attack==80,"mercenary actual source base stats");
  Check(battle.Unit(1).Tile!=battle.Unit(2).Tile&&battle.units.Select(u=>u.Tile).Distinct().Count()==13,"owned soldier placements do not overlap");
  var snapshot=battle.Unit(1);snapshot.baseStats.hp=9999;Check(mgr.WorldSnapshot.training.Unit(1).baseStats.hp==140,"base stat snapshots do not alias live state");
  Check(svc.ExecuteTrainingMemberAction(1,TrainingAction.Rest,-1,0,0,mgr.TrainingAuthority).Accepted,"controlled soldier rest through production service");
  Check(svc.ExecuteTrainingMemberAction(1,TrainingAction.Move,-1,5,6,mgr.TrainingAuthority).Accepted,"soldier first approach");svc.AdvanceWorld(100000);
  Check(svc.ExecuteTrainingMemberAction(1,TrainingAction.Move,-1,2,6,mgr.TrainingAuthority).Accepted,"soldier second approach");svc.AdvanceWorld(100000);
  Check(svc.ExecuteTrainingMemberAction(1,TrainingAction.Move,-1,2,10,mgr.TrainingAuthority).Accepted,"soldier in attack range");svc.AdvanceWorld(100000);
  while(mgr.WorldSnapshot.training.Unit(405).hp>0)Check(svc.ExecuteTrainingMemberAction(1,TrainingAction.Attack,405,0,0,mgr.TrainingAuthority).Accepted,"actual soldier finishing attacks");
  Check(mgr.WorldSnapshot.record.growth.experience==23,"soldier kill credits current commander once");
  Check(memory.data.accounts.Single().character.world.hiredCount==2,"XP outcome preserves hired ownership");
  mgr.Tick(0);mgr.Tick(30);Check(mgr.WorldSnapshot.training.Unit(405).hp==100,"soldier target respawns for automation fixture");
  Check(svc.ExecuteTrainingAction(TrainingAction.Move,-1,5,7,mgr.TrainingAuthority).Accepted,"leader first approach");svc.AdvanceWorld(100000);
  Check(svc.ExecuteTrainingAction(TrainingAction.Move,-1,2,7,mgr.TrainingAuthority).Accepted,"leader second approach");svc.AdvanceWorld(100000);
  Check(svc.ExecuteTrainingAction(TrainingAction.Move,-1,2,9,mgr.TrainingAuthority).Accepted,"leader commands adjacent soldier");svc.AdvanceWorld(100000);
  Check(svc.ExecuteTrainingAction(TrainingAction.TroopOrder,-1,0,0,mgr.TrainingAuthority,order:2).Accepted,"native automatic attack order");
  svc.AdvanceBattleAutomation(mgr.TrainingAuthority,count=>0,5000);
  var automatic=svc.AdvanceBattleAutomation(mgr.TrainingAuthority,count=>0,5001);
  Check(automatic?.Accepted==true&&mgr.WorldSnapshot.training.Unit(405).hp==61&&mgr.WorldSnapshot.training.Unit(1).ap==0,"native soldier AI action resets AP and resolves attack");
  mgr.Tick(.5);Check(mgr.WorldSnapshot.training.Unit(1).ap==1,"native pulse restores soldier AP after automatic action");
  Check(svc.ExecuteTrainingAction(TrainingAction.Move,-1,5,7,mgr.TrainingAuthority).Accepted,"leader moves beyond range");svc.AdvanceWorld(100000);mgr.Tick(10);
  Check(svc.ExecuteTrainingAction(TrainingAction.TroopOrder,-1,0,0,mgr.TrainingAuthority,order:3).Accepted,"native automatic defense order accepted");
  Check(svc.ExecuteTrainingRegroup(1,mgr.TrainingAuthority).Accepted,"verified native regroup plan accepted by local authority");svc.AdvanceWorld(100000);
  Check(mgr.WorldSnapshot.record.growth.experience==23,"regroup does not award XP");

 }
 // Reentrant map callbacks must not install the old training authority after escape.
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"콜백수련검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"callback fixture world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"callback fixture doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"callback fixture entry");
  var mgr=new LocalServerManager(svc);mgr.AfterUnits=r=>{if(r.Accepted)svc.EscapeTraining();};mgr.Begin();
  Check(mgr.TrainingAuthority==null&&svc.WorldSnapshot().training==null,"escape callback prevents stale authority installation");
  mgr.AfterUnits=null;Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"reenter doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"reenter after callback");mgr.Begin();
  Check(mgr.TrainingAuthority!=null&&mgr.WorldSnapshot.training!=null,"fresh entry creates current authority");
  Check(svc.EscapeTraining().Accepted,"rapid escape");
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"rapid new doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"rapid new entry");
  // No manager Tick occurred between these requests; stale binding must not block new initialization.
  mgr.Begin();Check(mgr.TrainingAuthority!=null&&mgr.WorldSnapshot.training!=null,"rapid reentry replaces stale binding without a cleanup frame");
  Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> external=(r,b)=>null;mgr.BattleAuthority=external;mgr.Tick(0);
  Check(mgr.TrainingAuthority==null&&mgr.BattleAuthority==external,"external authority takes precedence without being cleared");
 }
 // Execute the unmodified production OnDisable body during the initial-units callback.
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"비활성수련검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"disable fixture world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"disable fixture doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"disable fixture entry");
  var mgr=new LocalServerManager(svc);mgr.AfterUnits=r=>{if(r.Accepted)mgr.DisableTest();};mgr.Begin();
  Check(mgr.TrainingAuthority==null&&mgr.WorldSnapshot==null,"actual OnDisable clears local authority and prevents callback reinstallation");
 }
 // Known zero-HP and ghost records are not replaced with healthy base values.
 foreach(bool ghost in new[]{false,true})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"사망상태보존");
  var ch=memory.data.accounts.Single().character;ch.tutorialCompleted=true;ch.world=new WorldRecord {healthKnown=true,hp=0,maxHp=200,mp=3,maxMp=45,ghost=ghost,level=1,growth=new CharacterGrowthRecord {job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=Guid.NewGuid().ToString("N"),experience=17}};
  cl.OpenLocalProfile();Check(svc.BeginWorld(def).Accepted,"known death fixture world");
  if(ghost)
  {
   var ghostManager=new LocalServerManager(svc);int beforeWrites=memory.writes;
   new RecoveryCheckHost(ghostManager,def.combatCatalog).Run();Check(memory.writes==beforeWrites&&svc.WorldSnapshot().record.ghost&&svc.WorldSnapshot().record.hp==0,"editor recovery rejects ghost without writes");
  }
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"known death fixture doorway");svc.AdvanceWorld(100000);
  var entered=svc.EnterWorldPortal(gate.id,true);var mgr=new LocalServerManager(svc);int writes=memory.writes;
  UnityEngine.Debug.FailWarnings=false;try{if(entered.Accepted)mgr.Begin();}finally{UnityEngine.Debug.FailWarnings=true;}
  Check(svc.WorldSnapshot().record.hp==0&&svc.WorldSnapshot().record.mp==3&&svc.WorldSnapshot().record.ghost==ghost,"dead profile health/ghost preserved");
  Check(mgr.TrainingAuthority==null&&svc.WorldSnapshot().training==null&&memory.writes==writes,"zero HP does not create healthy battle or write initialization");
  Check(svc.WorldSnapshot().record.growth.experience==17,"death rejection preserves XP");
 }
 // Execute the actual LateUpdate and entry request together: disable in the nested units callback.
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"화면진입콜백");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"world frame fixture");
  var mgr=new LocalServerManager(svc);var frame=new WorldFrameCheckHost(mgr,def);
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"frame fixture doorway");svc.AdvanceWorld(100000);
  int initialEvents=0;mgr.Notify_TrainingInitialRequested+=v=>initialEvents++;
  mgr.AfterUnits=r=>{if(r.Accepted)mgr.DisableTest();};frame.Frame();
  Check(!frame.Running&&!frame.Active&&frame.Paints==0,"production LateUpdate stops safely after nested disable");
  Check(initialEvents==0,"initial-unit event is not issued with null or obsolete session");
  cl.OpenLocalProfile();Check(svc.BeginWorld(def).Accepted,"normal frame new world");
  var normalManager=new LocalServerManager(svc);var normalFrame=new WorldFrameCheckHost(normalManager,def);int duplicateInitial=0;
  normalManager.Notify_TrainingInitialRequested+=v=>duplicateInitial++;
  Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"normal frame doorway");svc.AdvanceWorld(100000);normalFrame.Frame();
  Check(normalFrame.Running&&normalFrame.Active&&normalFrame.Paints==1&&normalFrame.View.training?.units.Length==11,"normal production frame paints the latest initial-unit snapshot");
  Check(duplicateInitial==0,"completed local initialization does not request duplicate initial units");

 }
 // An old entrance response cannot initialize a second visit to the same map.
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"동일지도진입응답검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"same-map entry world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"same-map entry doorway");svc.AdvanceWorld(100000);
  var mgr=new LocalServerManager(svc);string nestedSession=null;int requests=0;
  mgr.Notify_TrainingInitialRequested+=_=>requests++;
  mgr.Notify_MapEntered+=_=>{
   Check(svc.EscapeTraining().Accepted,"entry observer escapes");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"entry observer returns to doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"entry observer starts separate raw visit");nestedSession=svc.WorldSnapshot().campaignSession;
  };
  var result=mgr.Request_EnterTraining(gate.id);
  Check(result.Accepted&&result.View.campaignSession!=nestedSession,"caller retains original entrance approval");
  Check(mgr.WorldSnapshot.campaignSession==nestedSession&&mgr.WorldSnapshot.training==null&&mgr.TrainingAuthority==null&&requests==0,"obsolete same-map entrance cannot initialize or request new visit units");
  Check(mgr.WorldSnapshot.record.growth==null,"obsolete same-map entrance cannot create growth state");
  mgr.Begin();Check(mgr.WorldSnapshot.training?.units.Length==11,"current visit still accepts explicit initialization");
 }
 // Use the real nested manager notification methods: observers may leave the context.
 foreach(string route in new[]{"world","training","battle"})foreach(string change in new[]{"none","logout","escape","reenter","revision"})foreach(bool firstSpecial in new[]{false,true})
 {
  if(route=="world"&&firstSpecial)continue;
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"후속알림검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"notification world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"notification doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"notification entry");
  var mgr=new LocalServerManager(svc);mgr.Begin();var snapshot=mgr.WorldSnapshot;
  int worldEvents=0,trainingEvents=0,battleEvents=0,mapEvents=0;bool changed=false;
  void Change(){if(changed)return;changed=true;
   if(change=="logout")mgr.DisableTest();
   else if(change=="escape"||change=="reenter"){
    Check(svc.EscapeTraining().Accepted,"observer escape");
    if(change=="reenter"){Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"observer doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"observer reentry");mgr.Begin();}}
   else if(change=="revision")Check(svc.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,mgr.TrainingAuthority).Accepted,"observer newer battle revision");
  }
  var accepted=new WorldReply(WorldFailure.None,snapshot);
  mgr.Notify_World+=r=>{if(!ReferenceEquals(r,accepted))return;worldEvents++;if(!firstSpecial)Change();};
  mgr.Notify_TrainingChanged+=r=>{if(!ReferenceEquals(r,accepted))return;trainingEvents++;if(firstSpecial&&route=="training")Change();};
  mgr.Notify_BattleChanged+=r=>{if(!ReferenceEquals(r,accepted))return;battleEvents++;if(firstSpecial&&route=="battle")Change();};
  mgr.Notify_MapEntered+=r=>{if(ReferenceEquals(r,accepted))mapEvents++;};
  var response=route=="world"?mgr.RelayWorld(accepted):route=="training"?mgr.RelayTraining(accepted):mgr.RelayBattle(accepted);
  Check(ReferenceEquals(response,accepted)&&worldEvents>=1,"notification preserves accepted caller result");
  bool sameWorld=change=="none"||change=="revision",sameBattle=change=="none";
  if(route=="world")Check(mapEvents==(sameWorld?1:0),"map followup matches live context "+change);
  else if(!firstSpecial)Check(trainingEvents==(sameBattle?1:0)&&battleEvents==(sameBattle?1:0),"world observer cannot publish stale battle "+route+change);
  else if(route=="training")Check(trainingEvents==1&&battleEvents==(sameBattle?1:0),"training observer cannot publish stale battle "+change);
  else Check(battleEvents==1&&trainingEvents==(sameBattle?1:0),"battle observer cannot publish stale training "+change);
 }
 // Growth followups cannot replay an obsolete owner, visit, XP or battle snapshot.
 foreach(string change in new[]{"none","disable","escape","reenter","new-xp","new-level","ap"})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"성장알림수명검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"growth event world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"growth event doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"growth event entry");var mgr=new LocalServerManager(svc);mgr.Begin();
  var before=mgr.WorldSnapshot;string id=Guid.NewGuid().ToString("N");var state=before.record.growth.Copy();state.classLevel=2;state.totalLevel=2;state.revision++;state.lastNotice=id;int events=0;bool changed=false;
  mgr.Notify_GrowthChanged+=r=>{if(r.View?.record.growth?.lastNotice==id)events++;};
  mgr.Notify_World+=r=>{
   if(changed||r.View?.record.growth?.lastNotice!=id)return;changed=true;
   if(change=="disable")mgr.DisableTest();
   else if(change=="escape"||change=="reenter"){
    Check(svc.EscapeTraining().Accepted,"growth observer escape");
    if(change=="reenter"){Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"growth observer doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"growth observer separate entry");mgr.Begin();}
   }
   else if(change=="new-xp")Check(svc.ReceiveCharacterExperience(new CharacterExperienceNotice{approved=true,id=Guid.NewGuid().ToString("N"),character=r.View.characterId,session=r.View.growthSession,expectedRevision=state.revision,experience=100}).Accepted,"growth observer receives newer XP");
   else if(change=="new-level"){
    var newer=state.Copy();newer.classLevel=3;newer.totalLevel=3;newer.revision++;newer.lastNotice=Guid.NewGuid().ToString("N");
    Check(svc.ReceiveCharacterGrowth(new CharacterGrowthNotice{approved=true,id=newer.lastNotice,character=r.View.characterId,session=r.View.growthSession,expectedRevision=state.revision,operation=CharacterGrowthOperation.LevelChanged,state=newer}).Accepted,"growth observer receives newer level");
   }
   else if(change=="ap"){
    var current=svc.WorldSnapshot();Check(svc.ReceiveTrainingTicks(new BattleTickNotice{approved=true,id=Guid.NewGuid().ToString("N"),character=current.characterId,session=current.campaignSession,expectedRevision=current.training.revision,unitLife=current.training.Commander.life,packet=OriginalBattleTicks.MenuPacket(0,19,89)}).Accepted,"growth observer receives newer battle revision");
   }
  };
  var result=mgr.Notify_CharacterGrowth(new CharacterGrowthNotice{approved=true,id=id,character=before.characterId,session=before.growthSession,expectedRevision=before.record.growth.revision,operation=CharacterGrowthOperation.LevelChanged,state=state});
  Check(result.Accepted&&events==(change=="none"?1:0),"growth event only publishes the live response "+change);
 }
 // Actual automatic-soldier pump must apply the same notification lifetime boundary.
 foreach(bool firstSpecial in new[]{false,true})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"자동전투알림검사");
  var ch=memory.data.accounts.Single().character;ch.tutorialCompleted=true;ch.world=new WorldRecord {hiredJob=200,hiredCount=1};cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"automatic notification world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"automatic notification doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"automatic notification entry");
  var mgr=new LocalServerManager(svc);mgr.Begin();
  foreach(var tile in new[]{(5,6),(2,6),(2,10)}){Check(svc.ExecuteTrainingMemberAction(1,TrainingAction.Move,-1,tile.Item1,tile.Item2,mgr.TrainingAuthority).Accepted,"automatic soldier approach");svc.AdvanceWorld(100000);}
  foreach(var tile in new[]{(5,7),(2,7),(2,9)}){Check(svc.ExecuteTrainingAction(TrainingAction.Move,-1,tile.Item1,tile.Item2,mgr.TrainingAuthority).Accepted,"automatic commander approach");svc.AdvanceWorld(100000);}
  Check(svc.ExecuteTrainingAction(TrainingAction.TroopOrder,-1,0,0,mgr.TrainingAuthority,order:2).Accepted,"automatic notification order");
  int worldEvents=0,battleEvents=0,trainingEvents=0;
  mgr.Notify_World+=r=>{worldEvents++;if(!firstSpecial)mgr.DisableTest();};mgr.Notify_BattleChanged+=r=>{battleEvents++;if(firstSpecial)mgr.DisableTest();};mgr.Notify_TrainingChanged+=r=>trainingEvents++;
  mgr.Automate(5);mgr.Automate(5.001);
  Check(worldEvents==1&&battleEvents==(firstSpecial?1:0)&&trainingEvents==0,"production automatic pump suppresses obsolete followup events");
  Check(mgr.WorldSnapshot==null&&memory.data.accounts.Single().character.world.growth.experience==0,"automatic callback teardown preserves stored XP");
 }
 // AP batch callbacks may tear down the service on the first pulse.
 foreach(bool reenter in new[]{false,true})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"AP콜백보존");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"AP callback fixture");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"AP callback doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"AP callback entry");
  var mgr=new LocalServerManager(svc);mgr.Begin();var av=mgr.WorldSnapshot;
  Check(mgr.Notify_TrainingTicks(new BattleTickNotice {approved=true,id=Guid.NewGuid().ToString("N"),character=av.characterId,session=av.campaignSession,expectedRevision=av.training.revision,unitLife=av.training.Commander.life,packet=OriginalBattleTicks.MenuPacket(0,0,89)}).Accepted,"AP callback setup");
  string oldSession=av.campaignSession;int delivered=0;
  mgr.AfterTicks=r=>{if(r.Accepted){delivered++;if(!reenter){mgr.DisableTest();return;}
    Check(svc.EscapeTraining().Accepted,"AP callback escape");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"AP callback reentry doorway");svc.AdvanceWorld(100000);
    Check(svc.EnterWorldPortal(gate.id,true).Accepted,"AP callback reentry");mgr.Begin();}};mgr.Tick(10);
  if(!reenter)Check(delivered==1&&mgr.WorldSnapshot==null&&mgr.TrainingAuthority==null,"AP batch stops after actual OnDisable callback");
  else Check(delivered==1&&mgr.WorldSnapshot.campaignSession!=oldSession&&mgr.WorldSnapshot.training.Commander.ap==20&&mgr.TrainingAuthority!=null,"old AP batch does not mutate the new entry");
  Check(memory.data.accounts.Single().character.world.growth.experience==0,"AP teardown does not pay or reset XP");
 }
 // Job-specific user entry points are optional and do not rewrite shared defaults.
 {
  var catalog=Read<GatewayMapCatalog>("Data/Campaign/GatewayMaps.json");string hash=catalog.maps.First(m=>m.id==102).hash;
  var pointsWorld=Read<WorldDefinition>("Data/World/KaosiaWorld.json");pointsWorld.combatCatalog=def.combatCatalog;pointsWorld.growthCatalog=def.growthCatalog;
  Check(!ScenarioGatewayData.HasEntryTerrain(pointsWorld,102,104)&&ScenarioGatewayData.HasEntryTerrain(pointsWorld,104,124),"terrain diagnostic distinguishes unsupported captain102 from supported naval124 training104");
  var shared=new ScenarioEntryPoint{map=102,mapHash=hash,x=10,y=7,enabled=true};
  var specific=Enumerable.Range(0,300).Select(i=>new ScenarioEntryPoint {map=102,mapHash=hash,x=i%20,y=i/20,enabled=true,useJob=true,job=107})
      .First(p=>ScenarioGatewayData.EntryLocationAllowed(pointsWorld,p));
  var settings=new ScenarioGatewaySettings{entryPoints=new[]{shared,specific}};ScenarioGatewayData.Validate(settings,catalog);
  Check(ReferenceEquals(ScenarioGatewayData.SelectEntry(settings,102,107),specific),"job override takes priority over shared point");
  Check(ReferenceEquals(ScenarioGatewayData.SelectEntry(settings,102,105),shared),"other jobs retain shared point");
  specific.enabled=false;Check(ReferenceEquals(ScenarioGatewayData.SelectEntry(settings,102,107),shared),"disabled job override falls back to shared point");specific.enabled=true;
  var roundtrip=ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(settings));Check(roundtrip.entryPoints.Length==2&&ScenarioGatewayData.SelectEntry(roundtrip,102,107).useJob,"optional class entry roundtrip");
  var old=ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(new ScenarioGatewaySettings{entryPoints=new[]{shared}}));
  Check(!old.entryPoints[0].useJob&&old.entryPoints[0].job==0,"legacy shared entry remains unconditioned");
  bool duplicate=false;try{ScenarioGatewayData.Validate(new ScenarioGatewaySettings{entryPoints=new[]{specific,specific.Copy()}},catalog);}catch(InvalidDataException e){duplicate=e.Message.Contains("직업 107");}
  Check(duplicate,"duplicate class entry identifies exact job");
  var blocked=specific.Copy();blocked.x=0;blocked.y=0;
  Check(!ScenarioGatewayData.EntryLocationAllowed(pointsWorld,blocked),"job override rejects blocked native terrain");
  foreach(int job in new[]{107,105})
  {
   var world=Read<WorldDefinition>("Data/World/KaosiaWorld.json");world.combatCatalog=def.combatCatalog;world.growthCatalog=def.growthCatalog;
   ScenarioGatewayData.Apply(settings,catalog,world);
   var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"직업별진입");
   var ch=memory.data.accounts.Single().character;ch.tutorialCompleted=true;ch.world=new WorldRecord {level=1,growth=new CharacterGrowthRecord {job=job,classLevel=1,totalLevel=1,revision=1,lastNotice=Guid.NewGuid().ToString("N")}};
   cl.OpenLocalProfile();Check(svc.BeginWorld(world).Accepted,"job entry fixture world");
   var door=world.Map(1000).portals.First(g=>g.targetMap==101);
   Check(svc.MoveWorld(door.cells[0].x,door.cells[0].y).Accepted,"job entry fixture doorway");svc.AdvanceWorld(100000);
   Check(svc.EnterWorldPortal(door.id,true).Accepted,"job conditional entry request");
   var expected=job==107?specific:shared;Check(svc.WorldSnapshot().record.x==expected.x&&svc.WorldSnapshot().record.y==expected.y,"actual service selects matching job entry");
   var mgr=new LocalServerManager(svc);mgr.Begin();Check(mgr.WorldSnapshot.training?.Commander.job==job,"sniper and gladiator both initialize on compatible authored points");
   Check(svc.EscapeTraining().Accepted&&svc.WorldSnapshot().record.map==1000,"job entries preserve original town return");
  }
 }
 // Real file-store durability: isolated fixture only, never the user's account/profile path.
 string diskRoot=Path.Combine(Path.GetTempPath(),"ds-training-xp-disk-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(diskRoot);
 try
 {
  string path=Path.Combine(diskRoot,"profile.json");var fileStore=new LocalAccountFileStore(path);var svc=new EntryAccountService(fileStore);var cl=new EntryClient(svc);
  cl.OpenLocalProfile();cl.Create(100,"파일경험치검사");Check(fileStore.TryRead(out var setup),"read isolated file account");
  // Prepare only this fixture's tutorial gate. This does not execute or certify the tutorial UI.
  setup.accounts.Single().character.tutorialCompleted=true;Check(fileStore.TryWrite(setup),"prepare isolated file fixture");cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"file fixture begin world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"file fixture doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"file fixture entry");
  var mgr=new LocalServerManager(svc);mgr.Begin();
  foreach(var tile in new[]{(5,7),(2,7),(2,10)}){Check(svc.ExecuteTrainingAction(TrainingAction.Move,-1,tile.Item1,tile.Item2,mgr.TrainingAuthority).Accepted,"file fixture approaches target");svc.AdvanceWorld(100000);}
  while(mgr.WorldSnapshot.training.Unit(405).hp>0)Check(svc.ExecuteTrainingAction(TrainingAction.Attack,405,0,0,mgr.TrainingAuthority).Accepted,"file-backed actual killing attacks");
  Check(fileStore.TryRead(out var diskDocument),"read saved kill file");var record=diskDocument.accounts.Single().character.world;
  Check(record.growth.experience==23&&record.level==1&&record.map==1000&&record.x==gate.returnX&&record.y==gate.returnY,"kill file preserves source XP and interrupted-training return");
  Check(!record.goldKnown&&record.gold==0&&record.inventory.Length==0,"kill file does not invent money or items");
  cl.Logout();var restoredService=new EntryAccountService(new LocalAccountFileStore(path));var restoredClient=new EntryClient(restoredService);restoredClient.OpenLocalProfile();
  Check(restoredService.BeginWorld(def).Accepted,"fresh service reads physical saved account");var restored=restoredService.WorldSnapshot();
  Check(restored.record.growth.experience==23&&restored.record.map==1000&&restored.record.x==gate.returnX&&restored.record.y==gate.returnY,"fresh service restores physical XP and entrance position");
  Check(restored.record.healthKnown&&restored.record.hp==200&&restored.record.mp==45,"fresh service retains source health");
  Check(restoredService.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"file replay doorway");restoredService.AdvanceWorld(100000);Check(restoredService.EnterWorldPortal(gate.id,true).Accepted,"file replay training");
  var replay=new LocalServerManager(restoredService);replay.Begin();Check(replay.WorldSnapshot.training.Unit(405).hp==100&&replay.WorldSnapshot.record.growth.experience==23,"fresh initial roster does not replay a previous XP award");restoredClient.Logout();
 }
 finally {Directory.Delete(diskRoot,true);}
 var registry=new TrainingTestLayoutCatalog();var asset=new UnityEngine.TextAsset{text="first"};registry.Set(101,asset);
 Check(ReferenceEquals(registry.Find(101),asset)&&registry.Find(102)==null,"catalog resolves explicit registered map only");
 var replaced=new UnityEngine.TextAsset{text="replacement"};registry.Set(101,replaced);
 Check(registry.maps.Length==1&&ReferenceEquals(registry.Find(101),replaced),"catalog replacement does not append a duplicate map");
 registry.Set(102,asset);Check(registry.maps.Length==2&&ReferenceEquals(registry.Find(102),asset),"catalog adds new authored training map");
 var broken=new TrainingTestLayoutCatalog {maps=new[]{101},layouts=Array.Empty<UnityEngine.TextAsset>()};var oldMapArray=broken.maps;var oldLayoutArray=broken.layouts;bool rejected=false;
 try{broken.Set(102,asset);}catch(InvalidOperationException){rejected=true;}
 Check(rejected&&ReferenceEquals(broken.maps,oldMapArray)&&ReferenceEquals(broken.layouts,oldLayoutArray),"new map cannot append into mismatched reference arrays");
 broken=new TrainingTestLayoutCatalog {maps=new[]{101,101},layouts=new[]{asset,replaced}};oldMapArray=broken.maps;oldLayoutArray=broken.layouts;rejected=false;
 try{broken.Set(102,asset);}catch(InvalidOperationException){rejected=true;}
 Check(rejected&&ReferenceEquals(broken.maps,oldMapArray)&&ReferenceEquals(broken.layouts,oldLayoutArray),"ambiguous duplicate map catalog is rejected without mutating references");
 // Execute the production focus receiver and BattleMap predicate; only window destruction/painting are host boundaries.
 foreach(string change in new[]{"normal","disabled","stopped","destroyed","definition","revision","escape","session"})
 {
  var memory=new Memory();var svc=new EntryAccountService(memory);var cl=new EntryClient(svc);cl.OpenLocalProfile();cl.Create(100,"전투시점검사");memory.data.accounts.Single().character.tutorialCompleted=true;cl.OpenLocalProfile();
  Check(svc.BeginWorld(def).Accepted,"focus world");Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"focus doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"focus training entry");
  var mgr=new LocalServerManager(svc);mgr.Begin();var captured=NativeBattleFocus.Capture(mgr.WorldSnapshot,405);Check(captured.HasValue,"received source unit supplies focus context");
  var receiver=new BattleFocusCheckHost(mgr,def);int writes=memory.writes;
  if(change=="disabled")receiver.isActiveAndEnabled=false;if(change=="stopped")receiver.Stop();if(change=="destroyed")receiver.DestroyCanvas();if(change=="definition")receiver.DropDefinition();
  if(change=="revision")Check(svc.ReceiveBattleUnits(new TrainingUnitsNotice{approved=true,id=Guid.NewGuid().ToString("N"),character=mgr.WorldSnapshot.characterId,session=mgr.WorldSnapshot.campaignSession,expectedRevision=mgr.WorldSnapshot.battle.revision,units=mgr.WorldSnapshot.battle.units}).Accepted,"newer unit snapshot before delayed focus");
  if(change=="escape"||change=="session")
  {
   Check(svc.EscapeTraining().Accepted,"focus source context escapes");
   if(change=="session"){Check(svc.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"focus next doorway");svc.AdvanceWorld(100000);Check(svc.EnterWorldPortal(gate.id,true).Accepted,"focus next visit");mgr.Begin();}
  }
  writes=memory.writes;receiver.Accept(captured.Value);
  Check(memory.writes==writes,"camera focus never saves growth/resources");
  if(change=="normal")
  {
   var expected=NativeViewport.Centered(captured.Value.x,captured.Value.y,def.Map(101).map.width,def.Map(101).map.height);
   Check(receiver.Paints==1&&receiver.Resets==1&&receiver.Left==expected.left&&receiver.Top==expected.top,"actual focus receiver applies native-tested viewport with current battle session");
   receiver.Accept(captured.Value);Check(receiver.Paints==2&&receiver.Resets==1,"same battle focus retains existing dialog session instead of resetting its camera again");
  }
  else Check(receiver.Paints==0&&receiver.Resets==0&&!receiver.Left.HasValue&&!receiver.Top.HasValue,"inactive/destroyed/stale focus cannot touch windows "+change);
 }
 Console.WriteLine(JsonSerializer.Serialize(new{checks,isolatedDiskProfiles=1,unityPlay=false}));
 }
}
