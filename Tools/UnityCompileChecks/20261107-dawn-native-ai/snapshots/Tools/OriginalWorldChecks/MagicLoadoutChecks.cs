using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class MagicLoadoutChecks
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
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");definition.magicMenu=Read<MagicMenuCatalog>("Data/Combat/MagicMenu.json");
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"마법편집검사");var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,inventory=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0},new WorldOwnedItem{id=Id(),item=361,slot=1}}};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world");string id=Id();
            Check(server.ReceiveCharacterGrowth(new CharacterGrowthNotice{id=id,approved=true,character=View.characterId,session=View.growthSession,
                state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=id,points=7,experience=12345,magic=definition.growthCatalog.magicIds.Take(20).ToArray(),skills=new[]{10101},stats=new CharacterGrowthStats{attack=65,defense=20}}}).Accepted,"explicit owned magic fixture, not a learned-spell grant from editor");
        }
        public void Queue(MagicLoadoutOperation op,int index=-1)=>Check(server.RequestMagicLoadout(op,index,Id()).Accepted,"loadout request queued: "+op);
        public MagicLoadoutNotice Notice(MagicLoadoutRequest r=null)
        {
            r=r??View.magicLoadoutRequest;
            return new MagicLoadoutNotice{id=Id(),request=r.Copy(),approved=true,learned=r.operation==MagicLoadoutOperation.State?View.record.growth.magic.Take(16).ToArray():null,
                selected=r.operation==MagicLoadoutOperation.State?(View.record.magicLoadout?.selected??View.record.growth.magic.Take(2).ToArray()).ToArray():(int[])r.selected.Clone(),
                attributes=r.attributes?.Select(v=>v.Copy()).ToArray(),statPoints=r.statPoints};
        }
        public void Info()=>Check(server.ExecuteMagicLoadout(MagicLoadoutOperation.State,-1,Id(),(r,v)=>Notice(r)).Accepted,"explicit learned/selected response");
        public void Edit(bool add,int index)=>Check(server.ExecuteMagicLoadout(add?MagicLoadoutOperation.Add:MagicLoadoutOperation.Remove,index,Id(),(r,v)=>Notice(r)).Accepted,"approved staged list edit");
        public void Confirm()=>Check(server.ExecuteMagicLoadout(MagicLoadoutOperation.Confirm,-1,Id(),(r,v)=>Notice(r)).Accepted,"approved loadout confirmation");
        public void Stats()
        {
            Check(server.ExecuteStatAdjustment(StatAdjustmentOperation.State,-1,0,Id(),(r,v)=>new StatAdjustmentNotice{id=Id(),request=r.Copy(),approved=true,points=10,costs=new[]{1,2,3,4,5,6},values=Enumerable.Range(0,6).Select(i=>new AttributeTriplet{before=10+i,after=12+i,limit=100}).ToArray()}).Accepted,"joint explicit attribute state");
            Check(server.ExecuteStatAdjustment(StatAdjustmentOperation.Step,2,1,Id(),(r,v)=>new StatAdjustmentNotice{id=Id(),request=r.Copy(),approved=true,points=r.points,values=r.values.Select(a=>a.Copy()).ToArray()}).Accepted,"joint staged attribute increase");
        }
        public void Reload(){server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"profile reconnect");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);var owned=(int[])f.View.record.growth.magic.Clone();
        Check(f.definition.magicMenu.entries.Length==440&&f.definition.magicMenu.Find(101).iconX==0&&f.definition.magicMenu.Find(101).iconY==32,"original magic metadata/coordinates retained");
        Check(f.View.battleMagic.SequenceEqual(owned)&&f.server.ExecuteMagicLoadout(MagicLoadoutOperation.State,-1,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.magicLoadoutRequest==null,"legacy query without authority preserves approved owned magic and creates no input lock");
        Check(f.server.RequestMagicLoadout(MagicLoadoutOperation.Add,0,Id()).Failure==WorldFailure.RulesUnavailable,"owned spell alone is not a battle-use selection");
        f.Queue(MagicLoadoutOperation.State);var info=f.Notice();Check(State(f.View.record)==before,"query alone preserves profile");Check(f.server.ReceiveMagicLoadout(info).Accepted,"received native-sized lists accepted");
        info.learned[0]=999;info.selected[0]=999;Check(f.View.magicLoadout.learned[0]==owned[0]&&f.View.magicLoadout.selected[0]==owned[0],"received arrays deeply copied");
        var copy=f.View.magicLoadout;copy.initial[0]=999;Check(f.View.magicLoadout.initial[0]==owned[0],"view arrays detached");
        Check(f.View.magicLoadout.learned.Length==16&&f.View.magicLoadout.selected.Length==2&&f.View.MagicLoadoutNeedsConfirm&&f.View.battleMagic.SequenceEqual(owned),"first approved query remains session data until confirmation");
        f.Confirm();Check(f.View.record.magicLoadout.revision==1&&f.View.battleMagic.SequenceEqual(owned)&&f.View.record.growth.magic.SequenceEqual(owned),"confirm received initial selection without granting/unlearning");
        f.Info();Check(!f.View.MagicLoadoutNeedsConfirm,"unchanged known selection needs no extra saved spend");
        Check(f.server.RequestMagicLoadout(MagicLoadoutOperation.Add,0,Id()).Failure==WorldFailure.InvalidInput,"duplicate battle selection rejected");
        f.Queue(MagicLoadoutOperation.Remove,0);var removal=f.Notice();before=State(f.View.record);
        Check(f.View.magicLoadout.selected.Length==2&&State(f.View.record)==before,"remove request alone is not list mutation");
        Check(f.server.RequestMagicLoadout(MagicLoadoutOperation.Add,2,Id()).Failure==WorldFailure.Busy&&f.server.RequestStatAdjustment(StatAdjustmentOperation.Step,0,1,Id()).Failure==WorldFailure.Busy,"one pending edit and no concurrent attribute request");
        Check(f.server.ReceiveMagicLoadout(removal).Accepted&&f.View.magicLoadout.selected.SequenceEqual(new[]{owned[1]})&&f.View.record.growth.magic.SequenceEqual(owned)&&State(f.View.record)==before,"remove compacts selected list and never unlearns");
        Check(!f.server.ReceiveMagicLoadout(removal).Accepted&&f.View.magicLoadout.selected.Length==1,"duplicate removal cannot remove next spell");
        f.Edit(true,2);Check(f.View.magicLoadout.selected.SequenceEqual(new[]{owned[1],owned[2]}),"append approved learned selection preserving order");
        Check(f.server.RequestClassOptions(Id()).Failure==WorldFailure.Busy&&f.server.EquipWorldItem(f.View.record.inventory[0].id,2,Id()).Failure==WorldFailure.Busy&&f.server.RequestItemUse(f.View.record.inventory[1].id,Id()).Failure==WorldFailure.Busy,"dirty loadout excludes conflicting class/equipment/item actions");
        Check(f.server.RequestMagicLoadout(MagicLoadoutOperation.State,-1,Id()).Failure==WorldFailure.Busy,"query cannot discard staged selection");
        f.Queue(MagicLoadoutOperation.Confirm);var confirmation=f.Notice();f.store.fail=true;
        Check(f.server.ReceiveMagicLoadout(confirmation).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.magicLoadout.Dirty&&f.View.magicLoadoutRequest!=null,"failed save retains previous selected list and draft retry");
        f.store.fail=false;Check(f.server.ReceiveMagicLoadout(confirmation).Accepted,"same approval retries");
        Check(f.View.battleMagic.SequenceEqual(owned)&&f.View.record.magicLoadout.revision==2&&f.View.record.growth.magic.SequenceEqual(owned)&&f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.growth.points==7&&!f.View.record.goldKnown,"only battle selection changes; no unlearning, healing or point/currency spending");
        before=State(f.View.record);Check(!f.server.ReceiveMagicLoadout(confirmation).Accepted&&State(f.View.record)==before,"completed confirmation cannot replay");f.Reload();Check(f.View.battleMagic.SequenceEqual(owned)&&f.View.magicLoadout==null,"confirmed selection reconnects without staged editor");
        var active=f.View.battleMagic;active[0]=999;Check(f.View.battleMagic[0]==owned[0],"active selected view is copied");
        var full=new Fixture();full.Queue(MagicLoadoutOperation.State);var all=full.Notice();all.selected=owned.Take(8).ToArray();Check(full.server.ReceiveMagicLoadout(all).Accepted,"native eight-slot capacity");Check(full.server.RequestMagicLoadout(MagicLoadoutOperation.Add,8,Id()).Failure==WorldFailure.InvalidInput,"ninth selected spell held");
        for(int i=0;i<8;i++)full.Edit(false,0);Check(full.View.magicLoadout.selected.Length==0&&full.View.record.growth.magic.Length==20,"remove every battle spell without forgetting learned magic");full.Confirm();Check(full.View.battleMagic.SequenceEqual(owned),"legacy empty selection cannot disable approved owned magic");
        foreach(string field in new[]{"id","context","character","session","map","revision","growth","missing-learned","missing-selected","learned-count","selected-count","unowned","unknown","duplicate-learned","duplicate-selected","outside-learned","premature-health"})
        {
            var bad=new Fixture();bad.Queue(MagicLoadoutOperation.State);var n=bad.Notice();var state=State(bad.View.record);
            switch(field)
            {
                case "id":n.id="bad";break;case "context":n.request.id=Id();break;case "character":n.request.character=Id();break;case "session":n.request.session=Id();break;case "map":n.request.map++;break;case "revision":n.request.revision++;break;case "growth":n.request.growthRevision++;break;
                case "missing-learned":n.learned=null;break;case "missing-selected":n.selected=null;break;case "learned-count":n.learned=owned.Take(17).ToArray();break;case "selected-count":n.selected=owned.Take(9).ToArray();break;case "unowned":n.learned[0]=bad.definition.growthCatalog.magicIds.Last();break;case "unknown":n.learned[0]=999999;break;
                case "duplicate-learned":n.learned[1]=n.learned[0];break;case "duplicate-selected":n.selected[1]=n.selected[0];break;case "outside-learned":n.selected[0]=owned[19];break;case "premature-health":n.health=new CharacterGrowthHealth{hp=200,maxHp=200,mp=45,maxMp=45};break;
            }
            Check(!bad.server.ReceiveMagicLoadout(n).Accepted&&bad.View.magicLoadout==null&&State(bad.View.record)==state,"invalid list state preserved: "+field);
        }
        foreach(string field in new[]{"terms","draft","operation","index","magic","context-list","list-order","other-spell","learned","attributes"})
        {
            var bad=new Fixture();bad.Info();bad.Queue(MagicLoadoutOperation.Add,2);var n=bad.Notice();var state=State(bad.View.record);var draft=State(bad.View.magicLoadout);
            switch(field)
            {
                case "terms":n.request.terms=Id();break;case "draft":n.request.draftRevision++;break;case "operation":n.request.operation=MagicLoadoutOperation.Remove;break;case "index":n.request.index++;break;case "magic":n.request.magic++;break;case "context-list":n.request.selected[0]=owned[4];break;
                case "list-order":Array.Reverse(n.selected);break;case "other-spell":n.selected[0]=owned[4];break;case "learned":n.learned=owned.Take(15).ToArray();break;case "attributes":n.attributes=new[]{new AttributeTriplet()};break;
            }
            Check(!bad.server.ReceiveMagicLoadout(n).Accepted&&State(bad.View.record)==state&&State(bad.View.magicLoadout)==draft,"malformed edit cannot change either list: "+field);
        }
        var cancelled=new Fixture();cancelled.Info();cancelled.Edit(true,2);cancelled.Queue(MagicLoadoutOperation.Confirm);var late=cancelled.Notice();cancelled.server.CancelMagicLoadout();Check(!cancelled.server.ReceiveMagicLoadout(late).Accepted&&cancelled.View.record.magicLoadout==null,"cancel discards staging and delayed approval");
        var interrupted=new Fixture();interrupted.Info();interrupted.Edit(true,2);interrupted.Reload();Check(interrupted.View.record.magicLoadout==null&&interrupted.View.record.growth.magic.Length==20,"unconfirmed editor never persists on interruption");
        var moved=new Fixture();moved.Info();moved.Edit(true,2);moved.Queue(MagicLoadoutOperation.Confirm);var oldMove=moved.Notice();Check(moved.server.MoveWorld(21,22).Accepted,"walking starts while editing");moved.server.AdvanceWorld(100000);Check(moved.View.magicLoadout==null&&!moved.server.ReceiveMagicLoadout(oldMove).Accepted&&moved.View.record.magicLoadout==null,"walking cancels old editor approval without saving selection");
        var money=new Fixture();money.Info();money.Edit(true,2);Check(money.server.ReceiveCurrencyState(new CurrencyNotice{id=Id(),approved=true,character=money.View.characterId,session=money.View.currencySession,gold=123}).Accepted&&money.View.magicLoadout==null&&money.View.record.gold==123,"new currency invalidates old full-state editor without reverting funds");
        var magicHeal=new Fixture();magicHeal.Info();magicHeal.Queue(MagicLoadoutOperation.Confirm);var illegalHeal=magicHeal.Notice();illegalHeal.health=new CharacterGrowthHealth{hp=200,maxHp=200,mp=45,maxMp=45};Check(!magicHeal.server.ReceiveMagicLoadout(illegalHeal).Accepted&&magicHeal.View.record.hp==17&&magicHeal.View.record.magicLoadout==null,"magic-only approval cannot secretly heal");
        var joint=new Fixture();joint.Info();joint.Stats();joint.Edit(true,2);var original=State(joint.View.record);
        Check(joint.server.RequestStatAdjustment(StatAdjustmentOperation.Confirm,-1,0,Id()).Failure==WorldFailure.Busy,"dirty magic cannot be discarded by stats-only confirm");
        joint.Queue(MagicLoadoutOperation.Confirm);var combined=joint.Notice();Check(combined.attributes.Length==6&&combined.statPoints==7,"one final request includes exact staged attributes and magic list");
        joint.store.fail=true;Check(joint.server.ReceiveMagicLoadout(combined).Failure==WorldFailure.StorageUnavailable&&State(joint.View.record)==original&&joint.View.magicLoadout.Dirty&&joint.View.statAdjustment.Dirty,"joint save failure commits neither side");
        joint.store.fail=false;Check(joint.server.ReceiveMagicLoadout(combined).Accepted&&joint.View.record.magicLoadout.revision==1&&joint.View.record.statAllocation.revision==1&&joint.View.record.statAllocation.points==7&&joint.View.record.statAllocation.values[2].after==15&&joint.View.battleMagic.SequenceEqual(owned),"single approval commits both drafts atomically");
        Check(joint.View.record.growth.stats.attack==65&&joint.View.record.hp==17&&joint.View.record.growth.magic.SequenceEqual(owned)&&joint.View.magicLoadout==null&&joint.View.statAdjustment==null,"joint raw approval does not infer effective stats or learned spells");
        foreach(string field in new[]{"missing-values","points","value","effective"})
        {
            var bad=new Fixture();bad.Info();bad.Stats();bad.Edit(true,2);bad.Queue(MagicLoadoutOperation.Confirm);var n=bad.Notice();var state=State(bad.View.record);
            if(field=="missing-values")n.attributes=null;else if(field=="points")n.statPoints++;else if(field=="value")n.attributes[0].after++;else n.effectiveStats=new CharacterGrowthStats{attack=-1};
            Check(!bad.server.ReceiveMagicLoadout(n).Accepted&&State(bad.View.record)==state&&bad.View.magicLoadout.Dirty&&bad.View.statAdjustment.Dirty,"invalid joint approval commits neither side: "+field);
        }
        var effective=new Fixture();effective.Info();effective.Stats();effective.Edit(true,2);effective.Queue(MagicLoadoutOperation.Confirm);var eff=effective.Notice();eff.effectiveStats=new CharacterGrowthStats{attack=90,defense=30};
        Check(effective.server.ReceiveMagicLoadout(eff).Accepted&&effective.View.record.growth.stats.attack==90&&effective.View.record.growth.revision==2&&effective.View.record.hp==17,"explicit effective stats only in joint attribute confirmation");
        var cancelStats=new Fixture();cancelStats.Info();cancelStats.Stats();cancelStats.Edit(true,2);cancelStats.Queue(MagicLoadoutOperation.Confirm);var oldJoint=cancelStats.Notice();cancelStats.server.CancelStatAdjustment();Check(!cancelStats.server.ReceiveMagicLoadout(oldJoint).Accepted&&cancelStats.View.record.magicLoadout==null&&cancelStats.View.record.statAllocation==null,"cancelled attribute half invalidates pending joint approval without partial commit");
        var xp=new Fixture();xp.Info();xp.Confirm();xp.Info();xp.Edit(true,2);Check(xp.server.ReceiveCharacterExperience(new CharacterExperienceNotice{id=Id(),approved=true,character=xp.View.characterId,session=xp.View.growthSession,expectedRevision=1,experience=555}).Accepted&&xp.View.magicLoadout==null&&xp.View.battleMagic.SequenceEqual(owned),"XP invalidates editor but preserves confirmed still-owned battle selection");
        var lose=new Fixture();lose.Info();lose.Confirm();var stateGrowth=lose.View.record.growth.Copy();stateGrowth.revision++;stateGrowth.lastNotice=Id();stateGrowth.magic=new[]{owned[3]};
        Check(lose.server.ReceiveCharacterGrowth(new CharacterGrowthNotice{id=stateGrowth.lastNotice,character=lose.View.characterId,session=lose.View.growthSession,approved=true,expectedRevision=1,state=stateGrowth}).Accepted&&lose.View.battleMagic.SequenceEqual(new[]{owned[3]})&&lose.View.record.magicLoadout.selected.Length==2,"new approved ownership replaces casting list without rewriting legacy selection history");
        foreach(string mode in new[]{"null","throw","wrong-request","denied"})
        {
            var provider=new Fixture();var state=State(provider.View.record);var reply=provider.server.ExecuteMagicLoadout(MagicLoadoutOperation.State,-1,Id(),(r,v)=>{if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var n=provider.Notice(r);if(mode=="wrong-request")n.request.id=Id();else n.approved=false;return n;});
            Check(!reply.Accepted&&provider.View.magicLoadoutRequest==null&&State(provider.View.record)==state,"provider failure unlocks without ownership changes: "+mode);
            provider.Info();provider.Edit(true,2);reply=provider.server.ExecuteMagicLoadout(MagicLoadoutOperation.Confirm,-1,Id(),(r,v)=>{if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var n=provider.Notice(r);if(mode=="wrong-request")n.request.id=Id();else n.approved=false;return n;});
            Check(!reply.Accepted&&provider.View.magicLoadoutRequest==null&&provider.View.magicLoadout.Dirty&&State(provider.View.record)==state,"failed confirmation provider preserves editable draft for retry: "+mode);
        }
        var concurrent=new Fixture();concurrent.Info();concurrent.Edit(true,2);concurrent.Queue(MagicLoadoutOperation.Confirm);var earlier=concurrent.Notice();
        var other=new EntryAccountService(concurrent.store);new EntryClient(other).OpenLocalProfile();Check(other.BeginWorld(concurrent.definition).Accepted,"second service sees baseline");
        Check(other.ExecuteMagicLoadout(MagicLoadoutOperation.State,-1,Id(),(r,v)=>new MagicLoadoutNotice{id=Id(),request=r.Copy(),approved=true,learned=owned.Take(16).ToArray(),selected=owned.Take(1).ToArray()}).Accepted,"second service explicit selection state");
        Check(other.ExecuteMagicLoadout(MagicLoadoutOperation.Confirm,-1,Id(),(r,v)=>new MagicLoadoutNotice{id=Id(),request=r.Copy(),approved=true,selected=(int[])r.selected.Clone()}).Accepted,"second service confirms without growth/currency change");
        Check(concurrent.server.ReceiveMagicLoadout(earlier).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.magicLoadout.selected.Length==1,"independent loadout revision protects newer no-cost save");
        var path=Path.Combine(Path.GetTempPath(),"ds-magic-loadout-"+Id()+".json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(joint.store.data)&&disk.TryRead(out var doc)&&doc.accounts[0].character.world.magicLoadout.selected.Length==3&&doc.accounts[0].character.world.statAllocation.values[2].after==15,"joint receipt actual DataContract roundtrip");
            var service=new EntryAccountService(disk);new EntryClient(service).OpenLocalProfile();Check(service.BeginWorld(joint.definition).Accepted&&service.WorldSnapshot().battleMagic.SequenceEqual(owned)&&service.WorldSnapshot().magicLoadout==null&&service.WorldSnapshot().statAdjustment==null,"file-backed reconnect restores both confirmed sides only");
        }
        finally{if(File.Exists(path))File.Delete(path);}
        var invalid=f.View.record.Copy();invalid.magicLoadout.selected=new[]{owned[0],owned[0]};Check(!WorldRecord.Valid(invalid),"invalid saved duplicate selection rejected");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Legacy selected-list adapter persistence compatibility and atomic joint stat persistence; approved growth.magic remains the casting list. This adapter is not original scene23 learning/ownership UI and emits no native learning packets."}));
    }
}
