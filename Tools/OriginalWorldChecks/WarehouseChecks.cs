using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class WarehouseChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
    private static T Read<T>(string file)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",file)),Options);
    private static string State(object value)=>JsonSerializer.Serialize(value,Options);
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;public string staff;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(int owned=3,int stored=0,bool money=true,bool equipped=false)
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"창고검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=65,goldKnown=money,gold=money?1000UL:0,bankKnown=money,bank=money?2000UL:0,
                inventory=Enumerable.Range(0,owned).Select(i=>new WorldOwnedItem{id=Id(),item=200,slot=i}).ToArray(),storage=Enumerable.Range(0,stored).Select(i=>new WorldOwnedItem{id=Id(),item=201,slot=i}).ToArray(),
                equipment=equipped?new[]{new WorldOwnedItem{id=Id(),item=200,slot=2}}:null};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated profile begins");
            var gate=definition.Map(1000).portals.First(p=>p.targetMap==2510);Walk(gate.cells[0].x,gate.cells[0].y);Check(server.EnterWorldPortal(gate.id,false).Accepted,"actual warehouse entrance");
            var npc=definition.Map(2510).npcs.Single();staff=npc.id;Walk(npc.counter[0].x,npc.counter[0].y);Check(server.OpenWorldFacility(staff).Accepted,"actual counter/NPC opens warehouse draft");
        }
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"native world walk request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"native grid arrival");}
        public WarehouseNotice Notice(WarehouseRequest r=null,int? capacity=24,ulong? fee=7,BankFeeSource? source=BankFeeSource.Wallet)
        {
            r=r??View.warehouseRequest;
            var n=new WarehouseNotice{approved=true,id=Id(),request=r.id,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,operation=r.operation,
                expectedRevision=r.revision,draftRevision=r.draftRevision,currencyRevision=r.currencyRevision,capacity=capacity,fee=fee,feeSource=source,
                inventory=r.inventory.Select(i=>i.Copy()).ToArray(),storage=r.storage.Select(i=>i.Copy()).ToArray()};
            if(r.operation==WarehouseOperation.Move)
            {
                var from=r.deposit?n.inventory:n.storage;var to=r.deposit?n.storage:n.inventory;var item=from.First(i=>i.id==r.owned).Copy();item.slot=r.slot;
                from=from.Where(i=>i.id!=r.owned).ToArray();to=to.Concat(new[]{item}).ToArray();n.inventory=r.deposit?from:to;n.storage=r.deposit?to:from;
            }
            if(r.operation==WarehouseOperation.Confirm&&fee.HasValue)
            {if(source==BankFeeSource.Wallet)n.gold=View.record.gold-fee.Value;else n.bank=View.record.bank-fee.Value;}
            return n;
        }
        public void Info(int? capacity=24,ulong? fee=7,BankFeeSource? source=BankFeeSource.Wallet)
        {Check(server.ExecuteWarehouseOperation(WarehouseOperation.State,false,null,View.facility.token,Id(),(r,v)=>Notice(r,capacity,fee,source)).Accepted,"explicit warehouse capacity/fee accepted");}
        public void Move(bool deposit,string item,ulong fee=7,BankFeeSource source=BankFeeSource.Wallet)
        {Check(server.ExecuteWarehouseOperation(WarehouseOperation.Move,deposit,item,View.facility.token,Id(),(r,v)=>Notice(r,View.facility.warehouseTerms.capacity,fee,source)).Accepted,"approved draft move");}
        public void Queue(WarehouseOperation op,bool deposit=false,string item=null)
        {Check(server.RequestWarehouseOperation(op,deposit,item,View.facility.token,Id()).Accepted,"warehouse operation queued: "+op);}
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real profile reconnect");Check(server.OpenWorldFacility(staff).Accepted,"new warehouse facility token");}
        public void Funds(ulong amount)=>Check(server.ReceiveCurrencyState(new CurrencyNotice{approved=true,id=Id(),character=View.characterId,session=View.currencySession,expectedRevision=View.record.currency?.revision??0,gold=amount}).Accepted,"authoritative wallet update");
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture(equipped:true);var before=State(f.View.record);var item=f.View.record.inventory[0].id;
        Check(!f.definition.rules.storageRuleKnown&&f.server.MoveWorldStorage(true,item,f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"default warehouse policy remains unknown");
        Check(f.server.ExecuteWarehouseOperation(WarehouseOperation.State,false,null,f.View.facility.token,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.warehouseRequest==null&&State(f.View.record)==before,"absent authority does not assume 12/48 slots or lock input");
        Check(f.server.RequestWarehouseOperation(WarehouseOperation.Move,true,item,f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"no draft move before authoritative capacity");
        f.Info();Check(f.View.facility.warehouseTerms.capacity==24&&f.View.facility.storageFeeKnown&&f.View.facility.storageFee==7&&State(f.View.record)==before,"terms are display/session data only");
        var view=f.View;view.facility.warehouseTerms.capacity=1;Check(f.View.facility.warehouseTerms.capacity==24,"terms view is detached");
        Check(f.server.RequestWarehouseOperation(WarehouseOperation.Move,true,f.View.record.Equipment[0].id,f.View.facility.token,Id()).Failure==WorldFailure.NotOwned,"equipped item cannot be deposited through inventory move");
        f.Queue(WarehouseOperation.Move,true,item);var move=f.Notice();
        Check(State(f.View.record)==before&&f.View.facility.inventory.Any(i=>i.id==item)&&f.View.facility.storage.Length==0,"request alone changes neither committed nor draft item positions");
        Check(f.server.RequestWarehouseOperation(WarehouseOperation.Confirm,false,null,f.View.facility.token,Id()).Failure==WorldFailure.Busy,"one unresolved operation");
        Check(f.server.ReceiveWarehouseOperation(move).Accepted&&f.View.facility.storage.Single().id==item&&f.View.record.inventory.Any(i=>i.id==item)&&f.View.record.storage.Length==0&&f.View.facility.storageDraftRevision==1,"move approval changes draft only");
        move.storage[0].item=999;Check(f.View.facility.storage[0].item==200,"server payload does not alias draft");
        Check(f.server.ReceiveWarehouseOperation(move).Failure==WorldFailure.StaleRequest,"duplicate stage does not duplicate item");
        f.Queue(WarehouseOperation.Confirm);var confirm=f.Notice();f.store.fail=true;
        Check(f.server.ReceiveWarehouseOperation(confirm).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.facility.storage.Single().id==item&&f.View.warehouseRequest!=null,"failed final save preserves original inventory/money and staged proposal for retry");
        f.store.fail=false;Check(f.server.ReceiveWarehouseOperation(confirm).Accepted,"same final approval retries");
        Check(f.View.record.storage.Single().id==item&&f.View.record.inventory.Length==2&&f.View.record.gold==993&&f.View.record.bank==2000&&f.View.record.warehouse.revision==1&&f.View.record.warehouse.fee==7&&f.View.facility==null,"items, explicit fee and receipt commit together; successful confirm closes warehouse");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.Equipment.Length==1&&!f.definition.rules.storageRuleKnown,"commit preserves unrelated stats/equipment and operating policy");
        before=State(f.View.record);Check(!f.server.ReceiveWarehouseOperation(confirm).Accepted&&State(f.View.record)==before,"completed receipt cannot be replayed");f.Reload();
        Check(f.View.record.storage.Single().id==item&&f.View.record.gold==993&&f.View.record.warehouse.id==confirm.id&&f.View.facility.warehouseTerms==null,"completed storage survives actual login; terms are not persisted");
        f.Info();f.Move(false,item);Check(f.View.facility.inventory.Any(i=>i.id==item)&&f.View.record.storage.Any(i=>i.id==item),"retrieval also remains staged");f.server.CloseWorldFacility();
        Check(f.View.record.storage.Any(i=>i.id==item)&&f.View.record.gold==993,"cancel discards retrieval without losing item or charging fee");
        var large=new Fixture(14);large.Info(24,0);var ids=large.View.record.inventory.Select(i=>i.id).ToArray();
        for(int i=0;i<ids.Length;i++)large.Move(true,ids[i],(ulong)i);
        Check(large.View.facility.storage.Length==14&&large.View.facility.storage.Any(i=>i.slot==13)&&large.View.record.storage.Length==0,"original 12 visible cells are not invented as a storage capacity");
        Check(large.View.facility.inventory.Concat(large.View.facility.storage).Select(i=>i.id).OrderBy(i=>i).SequenceEqual(ids.OrderBy(i=>i)),"all 14 owned identities conserved across stages");
        large.Reload();Check(large.View.record.inventory.Length==14&&large.View.record.storage.Length==0&&large.View.facility.storage.Length==0,"interrupted unconfirmed draft is not silently committed");
        var zero=new Fixture();zero.Info(0,0);Check(zero.server.RequestWarehouseOperation(WarehouseOperation.Move,true,zero.View.record.inventory[0].id,zero.View.facility.token,Id()).Failure==WorldFailure.InventoryFull,"explicit capacity zero blocks deposits without becoming a guessed default");
        var full=new Fixture(28,1);full.Info(1,0);Check(full.server.RequestWarehouseOperation(WarehouseOperation.Move,false,full.View.record.storage[0].id,full.View.facility.token,Id()).Failure==WorldFailure.InventoryFull,"retrieval cannot exceed the existing 28-slot inventory representation");
        var unknownFee=new Fixture();unknownFee.Info(24,null,null);Check(!unknownFee.View.facility.storageFeeKnown&&unknownFee.server.RequestWarehouseOperation(WarehouseOperation.Confirm,false,null,unknownFee.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"missing fee is displayed unknown, never free");
        unknownFee.Move(true,unknownFee.View.record.inventory[0].id,0);Check(unknownFee.View.facility.storageFeeKnown&&unknownFee.View.facility.storageFee==0,"explicit new stage fee zero is known zero");
        var savings=new Fixture();savings.Info(24,10,BankFeeSource.Savings);savings.Move(true,savings.View.record.inventory[0].id,10,BankFeeSource.Savings);
        Check(savings.server.ExecuteWarehouseOperation(WarehouseOperation.Confirm,false,null,savings.View.facility.token,Id(),(r,v)=>savings.Notice(r,24,10,BankFeeSource.Savings)).Accepted&&savings.View.record.bank==1990&&savings.View.record.gold==1000,"explicit fee source may be savings without guessing wallet debit");
        foreach(string problem in new[]{"owner","session","token","npc","map","request","id","revision","draft","currency","operation","capacity","fee-source","gift","type","slot","duplicate","other-slot"})
        {
            var bad=new Fixture();bad.Info();bad.Queue(WarehouseOperation.Move,true,bad.View.record.inventory[0].id);var n=bad.Notice();
            switch(problem)
            {
                case "owner":n.character=Id();break;case "session":n.session=Id();break;case "token":n.token=Id();break;case "npc":n.npc="bad";break;case "map":n.map=1000;break;
                case "request":n.request=Id();break;case "id":n.id="bad";break;case "revision":n.expectedRevision++;break;case "draft":n.draftRevision++;break;case "currency":n.currencyRevision++;break;
                case "operation":n.operation=WarehouseOperation.Confirm;break;case "capacity":n.capacity=25;break;case "fee-source":n.feeSource=null;break;
                case "gift":n.storage=n.storage.Concat(new[]{new WorldOwnedItem{id=Id(),item=200,slot=1}}).ToArray();break;
                case "type":n.storage[0].item=201;break;case "slot":n.storage[0].slot=2;break;case "duplicate":n.inventory=n.inventory.Concat(new[]{n.storage[0].Copy()}).ToArray();break;case "other-slot":n.inventory[0].slot=10;break;
            }
            before=State(bad.View.record);var draft=State(bad.View.facility.inventory)+State(bad.View.facility.storage);
            Check(!bad.server.ReceiveWarehouseOperation(n).Accepted&&State(bad.View.record)==before&&State(bad.View.facility.inventory)+State(bad.View.facility.storage)==draft,"invalid stage cannot mint, change or relocate unrelated owned items: "+problem);
        }
        var closed=new Fixture();closed.Info();closed.Queue(WarehouseOperation.Move,true,closed.View.record.inventory[0].id);var old=closed.Notice();closed.server.CloseWorldFacility();closed.server.OpenWorldFacility(closed.staff);
        Check(!closed.server.ReceiveWarehouseOperation(old).Accepted&&closed.View.record.storage.Length==0,"reopened NPC rejects old move approval");
        var changed=new Fixture();changed.Info();changed.Move(true,changed.View.record.inventory[0].id);changed.Queue(WarehouseOperation.Confirm);old=changed.Notice();changed.Funds(900);
        Check(changed.View.warehouseRequest==null&&changed.View.facility.storage.Length==1&&changed.server.ReceiveWarehouseOperation(old).Failure==WorldFailure.StaleRequest,"new funds invalidate old confirmation while preserving item draft");
        Check(changed.server.ExecuteWarehouseOperation(WarehouseOperation.Confirm,false,null,changed.View.facility.token,Id(),(r,v)=>changed.Notice(r)).Accepted&&changed.View.record.gold==893&&changed.View.record.storage.Length==1,"new confirmation uses updated wallet and same staged item");
        var noMoney=new Fixture(money:false);noMoney.Info();noMoney.Move(true,noMoney.View.record.inventory[0].id);Check(noMoney.server.RequestWarehouseOperation(WarehouseOperation.Confirm,false,null,noMoney.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"unknown fee payer balance cannot be treated as zero");
        var denied=new Fixture();denied.Info();denied.Queue(WarehouseOperation.Move,true,denied.View.record.inventory[0].id);var denial=denied.Notice();denial.approved=false;
        before=State(denied.View.record);Check(!denied.server.ReceiveWarehouseOperation(denial).Accepted&&denied.View.warehouseRequest==null&&State(denied.View.record)==before&&denied.View.facility.storage.Length==0,"denied move unlocks and preserves original draft");
        foreach(string failure in new[]{"null","throw","wrong-request"})
        {
            var failed=new Fixture();before=State(failed.View.record);
            Check(!failed.server.ExecuteWarehouseOperation(WarehouseOperation.State,false,null,failed.View.facility.token,Id(),(r,v)=>{if(failure=="null")return null;if(failure=="throw")throw new Exception("fixture");var n=failed.Notice(r);n.request=Id();return n;}).Accepted
                &&failed.View.warehouseRequest==null&&State(failed.View.record)==before,"provider failure cannot lock warehouse or change inventory: "+failure);
        }
        var distance=new Fixture();distance.Info();distance.Queue(WarehouseOperation.Move,true,distance.View.record.inventory[0].id);var far=distance.Notice();var npc=distance.definition.Map(2510).npcs.Single();npc.x=18;npc.y=1;npc.counter=Array.Empty<WorldPoint>();
        before=State(distance.View.record);Check(distance.server.ReceiveWarehouseOperation(far).Failure==WorldFailure.TooFar&&State(distance.View.record)==before,"warehouse response cannot bypass current NPC distance");
        // Free confirmations still need their own durable version even though currency does not change.
        var concurrent=new Fixture(1);concurrent.Info(24,0);concurrent.Move(true,concurrent.View.record.inventory[0].id,0);concurrent.Queue(WarehouseOperation.Confirm);var stale=concurrent.Notice(fee:0);var previous=concurrent.server;
        concurrent.Reload();concurrent.Info(24,0);concurrent.Move(true,concurrent.View.record.inventory[0].id,0);
        Check(concurrent.server.ExecuteWarehouseOperation(WarehouseOperation.Confirm,false,null,concurrent.View.facility.token,Id(),(r,v)=>concurrent.Notice(r,24,0)).Accepted&&concurrent.View.record.currency==null,"explicit free storage does not fabricate a money change");
        var saved=State(concurrent.store.data);Check(previous.ReceiveWarehouseOperation(stale).Failure==WorldFailure.StaleRequest&&State(concurrent.store.data)==saved,"independent warehouse revision rejects stale free confirmation without relying on money version");
        var fresh=new Fixture();fresh.Info();fresh.Move(true,fresh.View.record.inventory[0].id);fresh.Queue(WarehouseOperation.Confirm);var response=fresh.Notice();fresh.store.data.accounts[0].character.world.inventory=fresh.View.record.inventory.Concat(new[]{new WorldOwnedItem{id=Id(),item=201,slot=10}}).ToArray();
        saved=State(fresh.store.data);Check(fresh.server.ReceiveWarehouseOperation(response).Failure==WorldFailure.StaleRequest&&State(fresh.store.data)==saved,"unrelated fresh owned item cannot be overwritten by stale confirmation");
        string path=Path.Combine(Path.GetTempPath(),"ds-warehouse-"+Id(),"accounts.json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(concurrent.store.data)&&disk.TryRead(out var doc)&&doc.accounts[0].character.world.warehouse.revision==1,"warehouse audit survives actual file serialization");
            var restored=new EntryAccountService(disk);var reconnect=new EntryClient(restored);
            Check(reconnect.OpenLocalProfile(),"file-backed warehouse profile login: "+reconnect.Failure+" store="+disk.LastFailure+" code="+disk.LastErrorCode);
            var begin=restored.BeginWorld(concurrent.definition);
            Check(begin.Accepted,"file-backed warehouse BeginWorld: "+begin.Failure+" store="+disk.LastFailure+" code="+disk.LastErrorCode);
            var restoredView=restored.WorldSnapshot();
            Check(restoredView!=null&&restoredView.record.storage.Length==1&&restoredView.facility==null,"file-backed login restores confirmed items only and no pending draft");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        // Moving an owned item cannot manufacture or clear its approved remaining-use count.
        foreach(var operation in new[]{WarehouseOperation.State,WarehouseOperation.Move,WarehouseOperation.Confirm})
        foreach(int? uses in new int?[]{null,0,5})
        foreach(int? changedUses in new int?[]{null,0,5,-1})
        {
            if(uses==changedUses)continue;
            var usesCase=new Fixture(1);usesCase.store.data.accounts[0].character.world.inventory[0].remainingUses=uses;usesCase.Reload();
            string owned=usesCase.View.record.inventory[0].id;
            if(operation!=WarehouseOperation.State)usesCase.Info();
            if(operation==WarehouseOperation.Confirm)usesCase.Move(true,owned);
            usesCase.Queue(operation,true,operation==WarehouseOperation.Move?owned:null);
            var tampered=usesCase.Notice();var row=tampered.inventory.Concat(tampered.storage).Single(i=>i.id==owned);row.remainingUses=changedUses;
            string unchanged=State(usesCase.View.record),draftBefore=State(usesCase.View.facility.inventory)+State(usesCase.View.facility.storage);
            Check(!usesCase.server.ReceiveWarehouseOperation(tampered).Accepted&&State(usesCase.View.record)==unchanged
                &&State(usesCase.View.facility.inventory)+State(usesCase.View.facility.storage)==draftBefore,"warehouse preserves unknown/zero/positive uses, rejects changed count: "+operation+" "+uses+" -> "+changedUses);
            row.remainingUses=uses;Check(usesCase.server.ReceiveWarehouseOperation(tampered).Accepted,"unchanged owned-use receipt still retries after rejected mutation");
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual warehouse terms/staged move/final approval, ownership conservation, fee and persistence contracts with isolated limits/prices; no original wire/order/capacity policy claim."}));
    }
}
