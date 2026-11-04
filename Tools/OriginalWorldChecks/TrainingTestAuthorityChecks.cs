using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
namespace UnityEngine {
 [AttributeUsage(AttributeTargets.Field)]public class SerializeField:Attribute{}
 public class TextAsset {public string text;}
 public static class Resources {public static string Root; public static T Load<T>(string key)where T:class=>new TextAsset{text=File.ReadAllText(Path.Combine(Root,"Assets/_Original/Resources",key+".json"))}as T;}
 public static class JsonUtility {public static T FromJson<T>(string text)=>JsonSerializer.Deserialize<T>(text,new JsonSerializerOptions{IncludeFields=true});}
 public static class Debug {public static void Log(object v){}public static void LogWarning(object v){throw new Exception(v.ToString());}}
}
namespace DarkSaver.Original.Entry {
 public sealed partial class LocalServerManager {
  private EntryAccountService service;
  public Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> TrainingAuthority,BattleAuthority;
  public object NativeTrainingAuthority,NativeBattleAuthority;
  public WorldView WorldSnapshot=>service.WorldSnapshot();
  public WorldReply Notify_TrainingUnits(TrainingUnitsNotice n)=>service.ReceiveTrainingUnits(n);
  public LocalServerManager(EntryAccountService s){service=s;}
  public void Begin()=>BeginTrainingTest(WorldSnapshot);
  public void Tick(double dt)=>PumpTrainingTest(dt);
 }
}
internal static class TrainingTestAuthorityChecks {
 private static int checks;
 private static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 private sealed class Memory:IEntryAccountStore {
 public LocalAccountDocument data=new LocalAccountDocument();
 public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}
 public bool TryWrite(LocalAccountDocument d){data=d.Copy();return true;}}
 public static void Main(string[] args){
 string root=args[0];UnityEngine.Resources.Root=root;var opt=new JsonSerializerOptions{IncludeFields=true};
 T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",path)),opt);
 var store=new Memory();var server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"수련테스트");
 var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=200,maxHp=200,mp=45,maxMp=45};client.OpenLocalProfile();
 var def=Read<WorldDefinition>("Data/World/KaosiaWorld.json");def.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");
 Check(server.BeginWorld(def).Accepted,"begin world");var gate=def.Map(1000).portals.First(p=>p.targetMap==101);
 Check(server.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"walk to training");server.AdvanceWorld(100000);Check(server.EnterWorldPortal(gate.id,true).Accepted,"enter training");
 var manager=new LocalServerManager(server);manager.Begin();var v=manager.WorldSnapshot;
 Check(v.training?.units.Length==11,"ten authored monsters plus commander");
 Check(v.training.units.Where(u=>u.id>=400).All(u=>u.hp==1&&u.maxHp==1&&u.mp==1&&u.maxMp==1&&u.ap==1&&u.maxAp==1&&u.effectiveStats.attack==1&&u.effectiveStats.nativeValues.All(n=>n==1)),"all monster numeric stats one");
 Check(v.training.Commander.hp==200&&v.training.Commander.mp==45,"preserve commander health");
 Check(v.training.Commander.effectiveStats.attack==10&&v.training.Commander.effectiveStats.defense==10,"unknown commander stats receive explicit test values");
 // Exercise production authority through real queued actions; approach the first pack.
 var monster=v.training.units.First(u=>u.id==405);
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,5,7,manager.TrainingAuthority).Accepted,"first move request");server.AdvanceWorld(100000);
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,2,7,manager.TrainingAuthority).Accepted,"second move request");server.AdvanceWorld(100000);
 Check(server.ExecuteTrainingAction(TrainingAction.Move,-1,monster.x,monster.y-1,manager.TrainingAuthority).Accepted,"real move request");server.AdvanceWorld(100000);
 Check(server.ExecuteTrainingAction(TrainingAction.Attack,monster.id,0,0,manager.TrainingAuthority).Accepted,"real basic attack");
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==0,"one damage kills one HP");manager.Tick(0);manager.Tick(29.99);
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==0,"not before thirty seconds");manager.Tick(0.02);
 Check(manager.WorldSnapshot.training.Unit(monster.id).hp==1&&manager.WorldSnapshot.training.Unit(monster.id).life==2,"fresh lifetime respawns after thirty seconds");
 Check(server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,manager.TrainingAuthority).Accepted,"rest request");
 Check(server.EscapeTraining().Accepted,"escape training");manager.Tick(0);Check(manager.TrainingAuthority==null,"training authority cleaned on escape");
 // Separate memory store reload: unknown health must no longer block combat initialization.
 var saved=store.data.accounts.Single().character;saved.world.healthKnown=false;saved.world.hp=saved.world.maxHp=saved.world.mp=saved.world.maxMp=0;
 client.OpenLocalProfile();Check(server.BeginWorld(def).Accepted,"reload unknown health fixture");
 Check(server.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"unknown health fixture doorway");server.AdvanceWorld(100000);
 Check(server.EnterWorldPortal(gate.id,true).Accepted,"unknown health fixture entry");manager.Begin();v=manager.WorldSnapshot;
 Check(v.training!=null&&v.training.Commander.hp==200&&v.training.Commander.maxHp==200&&v.training.Commander.mp==45&&v.training.Commander.maxMp==45,"unknown health initializes provisional commander values");
 Console.WriteLine(JsonSerializer.Serialize(new{checks,unityPlay=false}));
 }
}
