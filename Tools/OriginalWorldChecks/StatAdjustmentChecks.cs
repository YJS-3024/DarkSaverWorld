using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class StatAdjustmentChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
    private static string State(object value)=>JsonSerializer.Serialize(value,Options);
    private static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",path)),Options);
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;
        public WorldView View=>server.WorldSnapshot();
        public Fixture()
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"능력검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,
                inventory=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0},new WorldOwnedItem{id=Id(),item=361,slot=1}}};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world");string id=Id();
            Check(server.ReceiveCharacterGrowth(new CharacterGrowthNotice{approved=true,id=id,character=View.characterId,session=View.growthSession,
                state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=id,experience=12345,points=7,magic=new[]{101},skills=new[]{10101},stats=new CharacterGrowthStats{attack=65,defense=20,magicAttack=8,magicDefense=6}}}).Accepted,"explicit growth fixture, not native attribute derivation");
        }
        public StatAdjustmentNotice Notice(StatAdjustmentRequest r=null)
        {
            r=r??View.statAdjustmentRequest;var record=View.record.statAllocation;
            return new StatAdjustmentNotice{id=Id(),request=r.Copy(),approved=true,
                values=r.operation==StatAdjustmentOperation.State?(record?.values??Enumerable.Range(0,6).Select(i=>new AttributeTriplet{before=10+i,after=12+i,limit=100}).ToArray()).Select(v=>v.Copy()).ToArray():r.values.Select(v=>v.Copy()).ToArray(),
                points=r.operation==StatAdjustmentOperation.State?(record?.points??10):r.points,costs=r.operation==StatAdjustmentOperation.State?new[]{1,2,3,4,5,6}:null};
        }
        public void Queue(StatAdjustmentOperation operation,int stat=-1,int direction=0)=>Check(server.RequestStatAdjustment(operation,stat,direction,Id()).Accepted,"stat request queued: "+operation);
        public void Info()=>Check(server.ExecuteStatAdjustment(StatAdjustmentOperation.State,-1,0,Id(),(r,v)=>Notice(r)).Accepted,"explicit raw values, points and costs");
        public void Step(int stat=2,int direction=1)=>Check(server.ExecuteStatAdjustment(StatAdjustmentOperation.Step,stat,direction,Id(),(r,v)=>Notice(r)).Accepted,"approved staged adjustment");
        public void Confirm()=>Check(server.ExecuteStatAdjustment(StatAdjustmentOperation.Confirm,-1,0,Id(),(r,v)=>Notice(r)).Accepted,"approved final allocation");
        public void Reload(){server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"profile reconnect");}
        public void Walk(){Check(server.MoveWorld(21,22).Accepted,"grid movement");server.AdvanceWorld(100000);Check(View.record.y==22,"grid arrival");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);
        Check(f.server.ExecuteStatAdjustment(StatAdjustmentOperation.State,-1,0,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.statAdjustmentRequest==null,"null authority does not invent costs/points or lock");
        Check(f.server.RequestStatAdjustment(StatAdjustmentOperation.Step,0,1,Id()).Failure==WorldFailure.RulesUnavailable,"no preview from growth points alone");
        f.Queue(StatAdjustmentOperation.State);var info=f.Notice();Check(State(f.View.record)==before,"query is not allocation");Check(f.server.ReceiveStatAdjustment(info).Accepted,"explicit state accepted");
        info.values[0].after=999;info.costs[0]=999;Check(f.View.statAdjustment.values[0].after==12&&f.View.statAdjustment.costs[0]==1,"received raw data deep copied");
        var copied=f.View.statAdjustment;copied.initial[0].after=0;Check(f.View.statAdjustment.initial[0].after==12,"view baseline detached");
        Check(f.View.statAdjustment.points==10&&f.View.record.growth.points==7&&!f.View.statAdjustment.Dirty,"native allocation points differ from generic growth points");
        Check(f.server.RequestStatAdjustment(StatAdjustmentOperation.Step,2,2,Id()).Failure==WorldFailure.InvalidInput,"cannot refund preexisting allocation");
        f.Queue(StatAdjustmentOperation.Step,2,1);var step=f.Notice();Check(State(f.View.record)==before&&f.View.statAdjustment.values[2].after==14&&f.View.statAdjustment.points==10,"arrow request alone makes no permanent or staged change");
        Check(f.server.RequestStatAdjustment(StatAdjustmentOperation.Step,0,1,Id()).Failure==WorldFailure.Busy&&f.server.RequestClassOptions(Id()).Failure==WorldFailure.Busy,"one pending operation and no concurrent class query");
        Check(f.server.ReceiveStatAdjustment(step).Accepted&&f.View.statAdjustment.values[2].after==15&&f.View.statAdjustment.values[2].before==12&&f.View.statAdjustment.points==7&&f.View.statAdjustment.draftRevision==1&&State(f.View.record)==before,"one approved point increase and exact explicit cost in draft only");
        Check(!f.server.ReceiveStatAdjustment(step).Accepted&&f.View.statAdjustment.points==7,"duplicate arrow cannot charge twice");
        Check(f.server.RequestClassOptions(Id()).Failure==WorldFailure.Busy&&f.server.EquipWorldItem(f.View.record.inventory[0].id,2,Id()).Failure==WorldFailure.Busy&&f.server.RequestItemUse(f.View.record.inventory[1].id,Id()).Failure==WorldFailure.Busy,"dirty draft excludes class/equipment/return use");
        Check(f.server.RequestStatAdjustment(StatAdjustmentOperation.State,-1,0,Id()).Failure==WorldFailure.Busy,"new query cannot discard staged allocation");
        f.Step(2,2);Check(f.View.statAdjustment.values[2].after==14&&f.View.statAdjustment.points==10&&!f.View.statAdjustment.Dirty,"undo only this session's addition and refund exact cost");
        Check(f.server.RequestStatAdjustment(StatAdjustmentOperation.Confirm,-1,0,Id()).Failure==WorldFailure.InvalidInput,"unchanged allocation is not a saved spend");
        f.Step();f.Queue(StatAdjustmentOperation.Confirm);var confirm=f.Notice();f.store.fail=true;
        Check(f.server.ReceiveStatAdjustment(confirm).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.statAdjustment.Dirty&&f.View.statAdjustmentRequest!=null,"failed final write preserves raw/permanent state and draft retry");
        f.store.fail=false;Check(f.server.ReceiveStatAdjustment(confirm).Accepted,"same final approval retries");
        Check(f.View.record.statAllocation.values[2].after==15&&f.View.record.statAllocation.points==7&&f.View.record.statAllocation.revision==1&&f.View.statAdjustment==null&&f.View.statAdjustmentRequest==null,"raw values, remaining points, receipt committed together");
        Check(f.View.record.growth.revision==1&&f.View.record.growth.points==7&&f.View.record.growth.stats.attack==65&&f.View.record.hp==17&&f.View.record.mp==3&&!f.View.record.goldKnown&&f.View.record.inventory.Length==2,"raw confirmation never infers effective stats, healing, growth-point spending or funds");
        before=State(f.View.record);Check(!f.server.ReceiveStatAdjustment(confirm).Accepted&&State(f.View.record)==before,"completed confirm cannot be replayed");f.Reload();
        Check(f.View.record.statAllocation.values[2].after==15&&f.View.record.statAllocation.points==7&&f.View.statAdjustment==null,"only confirmed allocation restored, no costs/draft inferred");f.Info();
        Check(f.server.RequestStatAdjustment(StatAdjustmentOperation.Step,2,2,Id()).Failure==WorldFailure.InvalidInput,"reopened baseline cannot refund confirmed earlier spend");
        f.Step(0);before=State(f.View.record);Check(f.server.CancelStatAdjustment().Accepted&&f.View.statAdjustment==null&&State(f.View.record)==before,"cancel discards staging without undoing previous confirmed allocation");
        var interrupted=new Fixture();interrupted.Info();interrupted.Step();interrupted.Reload();Check(interrupted.View.record.statAllocation==null&&interrupted.View.record.growth.stats.attack==65,"unconfirmed draft never persisted on interruption");
        var cancelled=new Fixture();cancelled.Info();cancelled.Queue(StatAdjustmentOperation.Step,2,1);var late=cancelled.Notice();cancelled.server.CancelStatAdjustment();Check(!cancelled.server.ReceiveStatAdjustment(late).Accepted&&cancelled.View.record.statAllocation==null,"cancel also invalidates in-flight arrow approval");
        foreach(string field in new[]{"id","context","character","session","map","growth","revision","missing-values","count","null-value","range","missing-points","negative-points","points-range","missing-costs","cost-count","negative-cost","cost-range","premature-effective"})
        {
            var bad=new Fixture();bad.Queue(StatAdjustmentOperation.State);var n=bad.Notice();var state=State(bad.View.record);
            switch(field)
            {
                case "id":n.id="bad";break;case "context":n.request.id=Id();break;case "character":n.request.character=Id();break;case "session":n.request.session=Id();break;case "map":n.request.map++;break;case "growth":n.request.growthRevision++;break;case "revision":n.request.revision++;break;
                case "missing-values":n.values=null;break;case "count":n.values=n.values.Take(5).ToArray();break;case "null-value":n.values[0]=null;break;case "range":n.values[0].after=32768;break;case "missing-points":n.points=null;break;case "negative-points":n.points=-1;break;case "points-range":n.points=32768;break;
                case "missing-costs":n.costs=null;break;case "cost-count":n.costs=new[]{1};break;case "negative-cost":n.costs[0]=-1;break;case "cost-range":n.costs[0]=32768;break;case "premature-effective":n.effectiveStats=new CharacterGrowthStats{attack=100};break;
            }
            Check(!bad.server.ReceiveStatAdjustment(n).Accepted&&bad.View.statAdjustment==null&&State(bad.View.record)==state,"invalid state leaves permanent state and draft unchanged: "+field);
        }
        foreach(string field in new[]{"terms","draft","direction","values","context-values","points","other-stat","costs","health","effective"})
        {
            var bad=new Fixture();bad.Info();bad.Queue(StatAdjustmentOperation.Step,2,1);var n=bad.Notice();var state=State(bad.View.record);var draft=State(bad.View.statAdjustment);
            switch(field)
            {
                case "terms":n.request.terms=Id();break;case "draft":n.request.draftRevision++;break;case "direction":n.request.direction=2;break;case "values":n.values[2].after++;break;case "context-values":n.request.values[2].after++;break;case "points":n.points++;break;case "other-stat":n.values[1].before++;break;case "costs":n.costs=new[]{9,9,9,9,9,9};break;
                case "health":n.health=new CharacterGrowthHealth{hp=200,maxHp=200,mp=45,maxMp=45};break;case "effective":n.effectiveStats=new CharacterGrowthStats{attack=100};break;
            }
            Check(!bad.server.ReceiveStatAdjustment(n).Accepted&&State(bad.View.record)==state&&State(bad.View.statAdjustment)==draft,"invalid arrow cannot mutate any other field: "+field);
        }
        var low=new Fixture();low.Queue(StatAdjustmentOperation.State);var l=low.Notice();l.points=0;l.costs[0]=0;Check(low.server.ReceiveStatAdjustment(l).Accepted,"explicit zero cost and zero points");low.Step(0);Check(low.View.statAdjustment.values[0].after==13&&low.View.statAdjustment.points==0,"explicit free increase not confused with missing cost");
        Check(low.server.RequestStatAdjustment(StatAdjustmentOperation.Step,1,1,Id()).Failure==WorldFailure.InvalidInput,"insufficient explicit allocation points held");low.Confirm();Check(low.View.record.statAllocation.revision==1&&low.View.record.growth.revision==1,"free raw allocation still has independent persisted revision");
        var boundary=new Fixture();boundary.Queue(StatAdjustmentOperation.State);var b=boundary.Notice();b.values[0].after=short.MaxValue;b.points=short.MaxValue;b.costs[1]=short.MaxValue;Check(boundary.server.ReceiveStatAdjustment(b).Accepted,"signed word boundary values received");
        Check(boundary.server.RequestStatAdjustment(StatAdjustmentOperation.Step,0,1,Id()).Failure==WorldFailure.InvalidInput,"reject signed word wraparound");boundary.Step(1);Check(boundary.View.statAdjustment.points==0,"max signed cost subtracted exactly");boundary.Step(1,2);Check(boundary.View.statAdjustment.points==short.MaxValue,"exact signed cost refund");
        var effective=new Fixture();effective.Info();effective.Step();effective.Queue(StatAdjustmentOperation.Confirm);var eff=effective.Notice();eff.effectiveStats=new CharacterGrowthStats{attack=90,defense=30,magicAttack=9,magicDefense=8};
        Check(effective.server.ReceiveStatAdjustment(eff).Accepted&&effective.View.record.growth.stats.attack==90&&effective.View.record.growth.revision==2&&effective.View.record.hp==17&&effective.View.record.growth.points==7,"only explicit effective stats update growth; no automatic health/points mapping");
        var health=new Fixture();health.Info();health.Step();health.Queue(StatAdjustmentOperation.Confirm);var h=health.Notice();h.health=new CharacterGrowthHealth{hp=18,maxHp=220,mp=4,maxMp=50};Check(health.server.ReceiveStatAdjustment(h).Accepted&&health.View.record.maxHp==220&&health.View.record.hp==18,"explicit validated health may accompany final approval");
        var ghost=new Fixture();ghost.store.data.accounts[0].character.world.ghost=true;ghost.store.data.accounts[0].character.world.hp=0;ghost.Reload();ghost.Info();ghost.Step();ghost.Queue(StatAdjustmentOperation.Confirm);var g=ghost.Notice();g.health=new CharacterGrowthHealth{hp=1,maxHp=200,mp=0,maxMp=45};Check(!ghost.server.ReceiveStatAdjustment(g).Accepted&&ghost.View.record.ghost&&ghost.View.record.hp==0,"allocation cannot resurrect ghost");
        foreach(string field in new[]{"values","points","effective"})
        {
            var bad=new Fixture();bad.Info();bad.Step();bad.Queue(StatAdjustmentOperation.Confirm);var n=bad.Notice();var state=State(bad.View.record);
            if(field=="values")n.values[0].after++;else if(field=="points")n.points++;else n.effectiveStats=new CharacterGrowthStats{attack=-1};
            Check(!bad.server.ReceiveStatAdjustment(n).Accepted&&State(bad.View.record)==state&&bad.View.statAdjustment.Dirty,"invalid final approval preserves draft and permanent state: "+field);
        }
        var moved=new Fixture();moved.Info();moved.Step();moved.Queue(StatAdjustmentOperation.Confirm);var old=moved.Notice();moved.Walk();Check(moved.View.statAdjustment==null&&!moved.server.ReceiveStatAdjustment(old).Accepted&&moved.View.record.statAllocation==null,"movement discards draft and old approval");
        var xp=new Fixture();xp.Info();xp.Step();Check(xp.server.ReceiveCharacterExperience(new CharacterExperienceNotice{id=Id(),approved=true,character=xp.View.characterId,session=xp.View.growthSession,expectedRevision=1,experience=555}).Accepted&&xp.View.statAdjustment==null,"new growth invalidates old raw allocation session");
        var money=new Fixture();money.Info();money.Step();Check(money.server.ReceiveCurrencyState(new CurrencyNotice{id=Id(),approved=true,character=money.View.characterId,session=money.View.currencySession,gold=123}).Accepted&&money.View.statAdjustment==null&&money.View.record.gold==123,"new money invalidates old full-state allocation snapshot without reverting funds");
        var equipment=new Fixture();equipment.Info();Check(equipment.server.EquipWorldItem(equipment.View.record.inventory[0].id,2,Id()).Accepted&&equipment.View.statAdjustment==null,"new equipment invalidates clean raw attribute display");
        foreach(string mode in new[]{"null","throw","wrong-request","denied"})
        {
            var provider=new Fixture();var state=State(provider.View.record);
            var reply=provider.server.ExecuteStatAdjustment(StatAdjustmentOperation.State,-1,0,Id(),(r,v)=>{if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var n=provider.Notice(r);if(mode=="wrong-request")n.request.id=Id();else n.approved=false;return n;});
            Check(!reply.Accepted&&provider.View.statAdjustmentRequest==null&&State(provider.View.record)==state,"provider failure unlocks with no mutation: "+mode);
        }
        var concurrent=new Fixture();concurrent.Info();concurrent.Step();concurrent.Queue(StatAdjustmentOperation.Confirm);var earlier=concurrent.Notice();
        var other=new EntryAccountService(concurrent.store);new EntryClient(other).OpenLocalProfile();Check(other.BeginWorld(concurrent.definition).Accepted,"second service sees same baseline");
        Check(other.ExecuteStatAdjustment(StatAdjustmentOperation.State,-1,0,Id(),(r,v)=>{var n=concurrent.Notice(r);n.request=r.Copy();return n;}).Accepted,"second service receives own explicit raw snapshot");
        Check(other.ExecuteStatAdjustment(StatAdjustmentOperation.Step,0,1,Id(),(r,v)=>new StatAdjustmentNotice{id=Id(),request=r.Copy(),approved=true,values=r.values.Select(a=>a.Copy()).ToArray(),points=r.points}).Accepted,"second service stages without growth change");
        Check(other.ExecuteStatAdjustment(StatAdjustmentOperation.Confirm,-1,0,Id(),(r,v)=>new StatAdjustmentNotice{id=Id(),request=r.Copy(),approved=true,values=r.values.Select(a=>a.Copy()).ToArray(),points=r.points}).Accepted,"second service commits raw-only allocation");
        Check(concurrent.server.ReceiveStatAdjustment(earlier).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.statAllocation.values[0].after==13,"independent allocation revision protects newer raw-only commit");
        var unrelated=new Fixture();unrelated.Info();unrelated.Step();unrelated.Queue(StatAdjustmentOperation.Confirm);old=unrelated.Notice();unrelated.store.data.accounts[0].character.world.inventory[0].item=2;Check(unrelated.server.ReceiveStatAdjustment(old).Failure==WorldFailure.StaleRequest&&unrelated.store.data.accounts[0].character.world.inventory[0].item==2,"newer unrelated inventory not overwritten");
        var path=Path.Combine(Path.GetTempPath(),"ds-stat-adjustment-"+Id()+".json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var doc)&&doc.accounts[0].character.world.statAllocation.revision==1,"allocation receipt actual DataContract round trip");
            var server=new EntryAccountService(disk);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(f.definition).Accepted&&server.WorldSnapshot().record.statAllocation.values[2].after==15&&server.WorldSnapshot().statAdjustment==null,"file-backed reconnect restores confirmed raw values only");
        }
        finally{if(File.Exists(path))File.Delete(path);}
        var invalid=f.View.record.Copy();invalid.statAllocation.values[0].after=32768;Check(!WorldRecord.Valid(invalid),"invalid saved raw values rejected");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual six native attribute triplets/costs, staged approvals/undo/cancel/final persistence and optional explicit effective stats/health; no default costs, growth-point alias or combat formula claim."}));
    }
}
