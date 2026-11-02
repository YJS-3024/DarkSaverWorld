using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;

internal static class ItemUseChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
    private static string State(object value)=>JsonSerializer.Serialize(value,Options);
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
        public string Stone=>View.record.inventory.First(i=>i.item==361).id;
        public Fixture()
        {
            definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Options);
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"귀환검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,
                inventory=new[]{361,324,1,300,326,362}.Select((item,slot)=>new WorldOwnedItem{id=Id(),item=item,slot=slot}).ToArray(),
                storage=new[]{new WorldOwnedItem{id=Id(),item=361,slot=0}},equipment=new[]{new WorldOwnedItem{id=Id(),item=1,slot=2}}};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world begins without invented funds");
        }
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"grid walk request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"grid arrival");}
        public void Enter(int map)
        {var gate=definition.Map(1000).portals.First(p=>p.targetMap==map);Walk(gate.cells[0].x,gate.cells[0].y);Check(server.EnterWorldPortal(gate.id,map==101).Accepted,"actual source portal");}
        public void Queue(string owned=null)=>Check(server.RequestItemUse(owned??Stone,Id()).Accepted,"owned return stone request queued");
        public ItemUseNotice Notice(ItemUseRequest r=null,bool? consumed=true)=>new ItemUseNotice{id=Id(),request=(r??View.itemUseRequest).Copy(),approved=true,consumed=consumed,
            destination=new ItemUseDestination{map=1000,x=21,y=21,direction=2}};
        public void Approve(string owned=null,bool consumed=true)=>Check(server.ExecuteItemUse(owned??Stone,Id(),(r,v)=>Notice(r,consumed)).Accepted,"explicit return approval committed");
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"profile reconnect");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);var stone=f.Stone;
        Check(f.definition.Item(361).equipType==10&&f.definition.Item(324).equipType==10&&f.definition.Item(300).equipType==7,"original return-stone vs healing-item categories retained");
        Check(f.server.ExecuteItemUse(stone,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.itemUseRequest==null&&State(f.View.record)==before,"absent authority never consumes or supplies destination");
        Check(f.server.UseWorldItem(stone,Id()).Failure==WorldFailure.RulesUnavailable,"legacy use still withholds unknown policy");
        Check(f.server.RequestItemUse(f.View.record.storage[0].id,Id()).Failure==WorldFailure.NotOwned,"stored item cannot be used as inventory");
        Check(f.server.RequestItemUse(f.View.record.Equipment[0].id,Id()).Failure==WorldFailure.NotOwned,"equipped item cannot be used as inventory");
        Check(f.server.RequestItemUse(f.View.record.inventory.First(i=>i.item==300).id,Id()).Failure==WorldFailure.InvalidInput,"healing potion is not type10 head-drop use");
        foreach(int item in new[]{326,362})Check(f.server.RequestItemUse(f.View.record.inventory.First(i=>i.item==item).id,Id()).Failure==WorldFailure.RulesUnavailable,"unsupported type10 effect not consumed/guessed: "+item);
        f.Enter(2000);before=State(f.View.record);f.Queue();var n=f.Notice();var copied=f.View.itemUseRequest;copied.owned=Id();Check(f.View.itemUseRequest.owned==stone,"pending view detached");
        Check(State(f.View.record)==before&&f.View.record.map==2000&&f.View.record.inventory.Any(i=>i.id==stone),"request alone neither moves nor consumes");
        Check(f.server.RequestItemUse(stone,Id()).Failure==WorldFailure.Busy,"one unresolved use");
        Check(f.server.EquipWorldItem(f.View.record.inventory.First(i=>i.item==1).id,2,Id()).Failure==WorldFailure.Busy,"equipment cannot mutate pending use snapshot");
        f.store.fail=true;Check(f.server.ReceiveItemUse(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.itemUseRequest!=null,"failed save preserves source map/item and pending retry");
        f.store.fail=false;Check(f.server.ReceiveItemUse(n).Accepted,"exact approval retries after failed save");
        Check(f.View.record.map==1000&&f.View.record.x==21&&f.View.record.y==21&&f.View.record.direction==2&&f.View.record.inventory.All(i=>i.id!=stone)&&f.View.record.inventory.Length==5,"explicit destination and one consumed instance committed together");
        Check(f.View.record.itemUse.revision==1&&f.View.record.itemUse.sourceMap==2000&&f.View.record.itemUse.consumed&&f.View.record.itemUse.owned==stone&&f.View.itemUseRequest==null,"receipt captures committed result only");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&!f.View.record.goldKnown&&!f.View.record.bankKnown&&f.View.record.storage.Length==1&&f.View.record.Equipment.Length==1,"return does not heal, fund, resurrect or alter unrelated holdings");
        n.destination.x=99;Check(f.View.record.x==21&&f.View.record.itemUse.x==21,"notice destination is not retained by reference");
        before=State(f.View.record);Check(!f.server.ReceiveItemUse(n).Accepted&&State(f.View.record)==before,"same approval cannot consume or move twice");
        f.Reload();Check(f.View.record.map==1000&&f.View.record.x==21&&f.View.record.itemUse.revision==1&&f.View.record.inventory.All(i=>i.id!=stone)&&f.View.itemUseRequest==null,"completed return restored without pending request");
        f.Approve(f.View.record.inventory.First(i=>i.item==324).id,false);Check(f.View.record.inventory.Any(i=>i.item==324)&&f.View.record.itemUse.item==324&&!f.View.record.itemUse.consumed&&f.View.record.itemUse.revision==2,"second original return-stone ID and explicit nonconsumption supported without default");
        foreach(string field in new[]{"request","character","session","owned","source","item","slot","revision","id","missing-consumed","missing-destination","unknown-map","negative-x","bounds-y","direction","npc","training","scenario"})
        {
            var bad=new Fixture();bad.Queue();var notice=bad.Notice();var state=State(bad.View.record);
            switch(field)
            {
                case "request":notice.request.id=Id();break;case "character":notice.request.character=Id();break;case "session":notice.request.session=Id();break;case "owned":notice.request.owned=Id();break;
                case "source":notice.request.map=2000;break;case "item":notice.request.item=324;break;case "slot":notice.request.slot++;break;case "revision":notice.request.revision++;break;case "id":notice.id="bad";break;
                case "missing-consumed":notice.consumed=null;break;case "missing-destination":notice.destination=null;break;case "unknown-map":notice.destination.map=999999;break;
                case "negative-x":notice.destination.x=-1;break;case "bounds-y":notice.destination.y=999;break;case "direction":notice.destination.direction=4;break;
                case "npc":var npc=bad.definition.Map(2000).npcs.First();notice.destination.map=2000;notice.destination.x=npc.x;notice.destination.y=npc.y;break;
                case "training":notice.destination.map=101;break;
                case "scenario":bad.definition.campaign=new CampaignDefinition{maps=new[]{new CampaignMap{id=2000,kind="scenario"}}};notice.destination.map=2000;break;
            }
            Check(!bad.server.ReceiveItemUse(notice).Accepted&&State(bad.View.record)==state,"malformed or unsupported approval leaves map/item unchanged: "+field);
        }
        var consumption=new Fixture();consumption.Queue();var missing=consumption.Notice(consumed:null);Check(consumption.server.ReceiveItemUse(missing).Failure==WorldFailure.RulesUnavailable&&consumption.View.record.inventory.Length==6,"missing consumption is not assumed true or false");
        var cancelled=new Fixture();cancelled.Queue();var old=cancelled.Notice();cancelled.Walk(21,22);Check(cancelled.View.itemUseRequest==null&&!cancelled.server.ReceiveItemUse(old).Accepted&&cancelled.View.record.y==22&&cancelled.View.record.inventory.Length==6,"accepted walking invalidates old use, no snap back or consumption");
        var invalidMove=new Fixture();invalidMove.Queue();old=invalidMove.Notice();Check(!invalidMove.server.MoveWorld(-1,-1).Accepted&&invalidMove.View.itemUseRequest!=null&&invalidMove.server.ReceiveItemUse(old).Accepted,"rejected movement does not destroy still-current approval");
        var money=new Fixture();money.Queue();old=money.Notice();Check(money.server.ReceiveCurrencyState(new CurrencyNotice{id=Id(),approved=true,character=money.View.characterId,session=money.View.currencySession,gold=123}).Accepted,"isolated explicit currency receipt");
        Check(money.View.itemUseRequest==null&&!money.server.ReceiveItemUse(old).Accepted&&money.View.record.gold==123,"new currency invalidates old full-state approval");money.Approve();Check(money.View.record.gold==123,"new use preserves latest money");
        var interrupted=new Fixture();interrupted.Queue();old=interrupted.Notice();interrupted.Reload();Check(!interrupted.server.ReceiveItemUse(old).Accepted&&interrupted.View.record.inventory.Length==6&&interrupted.View.record.itemUse==null,"interrupted unapproved use never persists");
        var training=new Fixture();training.Enter(101);Check(training.server.RequestItemUse(training.Stone,Id()).Failure==WorldFailure.RulesUnavailable&&training.View.record.inventory.Length==6,"training exit is not bypassed by return-stone adapter");
        var sourceScenario=new Fixture();sourceScenario.definition.campaign=new CampaignDefinition{maps=new[]{new CampaignMap{id=1000,kind="scenario"}}};Check(sourceScenario.server.RequestItemUse(sourceScenario.Stone,Id()).Failure==WorldFailure.RulesUnavailable,"scenario return lifecycle not bypassed");
        var facility=new Fixture();facility.Enter(2000);var staff=facility.definition.Map(2000).npcs.First();facility.Walk(staff.counter[0].x,staff.counter[0].y);Check(facility.server.OpenWorldFacility(staff.id).Accepted,"facility opens");Check(facility.server.RequestItemUse(facility.Stone,Id()).Failure==WorldFailure.Busy,"use blocked while facility UI owns input");
        foreach(string mode in new[]{"null","throw","wrong-request","denied"})
        {
            var provider=new Fixture();var state=State(provider.View.record);
            var reply=provider.server.ExecuteItemUse(provider.Stone,Id(),(r,v)=>
            {if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var notice=provider.Notice(r);if(mode=="wrong-request")notice.request.id=Id();else notice.approved=false;return notice;});
            Check(!reply.Accepted&&provider.View.itemUseRequest==null&&State(provider.View.record)==state,"provider failure unlocks with no speculative effects: "+mode);
        }
        var concurrent=new Fixture();concurrent.Queue();var earlier=concurrent.Notice(consumed:false);
        var other=new EntryAccountService(concurrent.store);new EntryClient(other).OpenLocalProfile();Check(other.BeginWorld(concurrent.definition).Accepted,"second service sees original state");
        Check(other.ExecuteItemUse(concurrent.Stone,Id(),(r,v)=>new ItemUseNotice{id=Id(),request=r.Copy(),approved=true,consumed=false,destination=new ItemUseDestination{map=1000,x=21,y=22,direction=1}}).Accepted,"nonconsuming return also changes persisted revision");
        Check(concurrent.server.ReceiveItemUse(earlier).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.y==22,"old nonconsuming approval cannot overwrite newer relocation");
        var unrelated=new Fixture();unrelated.Queue();old=unrelated.Notice();unrelated.store.data.accounts[0].character.world.inventory[2].item=2;Check(unrelated.server.ReceiveItemUse(old).Failure==WorldFailure.StaleRequest&&unrelated.store.data.accounts[0].character.world.inventory[2].item==2,"more recent unrelated inventory preserved");
        var receipt=f.View.record.itemUse.Copy();receipt.revision=0;var invalid=f.View.record.Copy();invalid.itemUse=receipt;Check(!WorldRecord.Valid(invalid),"invalid saved use receipt rejected");
        var path=Path.Combine(Path.GetTempPath(),"ds-item-use-"+Id()+".json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var doc)&&doc.accounts[0].character.world.itemUse.revision==2,"use receipt actual DataContract round trip");
            var server=new EntryAccountService(disk);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(f.definition).Accepted&&server.WorldSnapshot().record.inventory.Length==5&&server.WorldSnapshot().record.itemUse.item==324&&server.WorldSnapshot().itemUseRequest==null,"file-backed reconnect restores completed item/destination only");
        }
        finally{if(File.Exists(path))File.Delete(path);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual original type10 return-stone input, explicit relocation/consumption approval and isolated persistence; no default destination, consumption policy, healing/teleport/furniture effect or native wire claim."}));
    }
}
