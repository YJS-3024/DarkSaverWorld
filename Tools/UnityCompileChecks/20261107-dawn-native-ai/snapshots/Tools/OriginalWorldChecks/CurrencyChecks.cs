using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class CurrencyChecks
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
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool ghost=false)
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"잔액검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{healthKnown=true,hp=ghost?0:17,maxHp=200,mp=3,maxMp=65,ghost=ghost,
                inventory=new[]{new WorldOwnedItem{id=Id(),item=200,slot=0}}};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated unknown-balance world begins");
        }
        public CurrencyNotice Notice(ulong? gold=null,ulong? bank=null)=>new CurrencyNotice{approved=true,id=Id(),character=View.characterId,session=View.currencySession,expectedRevision=View.record.currency?.revision??0,gold=gold,bank=bank};
        public void Funds(ulong? gold,ulong? bank=null)=>Check(server.ReceiveCurrencyState(Notice(gold,bank)).Accepted,"authoritative absolute balance applied");
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"real grid walk request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"real grid arrival");}
        public void Facility(int map)
        {
            var gate=definition.Map(View.record.map).portals.First(p=>p.targetMap==map);Walk(gate.cells[0].x,gate.cells[0].y);
            Check(server.EnterWorldPortal(gate.id,definition.Map(map).training).Accepted,"native local portal entered");
            if(definition.Map(map).training)return;
            var n=definition.Map(map).npcs.First();Walk(n.counter[0].x,n.counter[0].y);Check(server.OpenWorldFacility(n.id).Accepted,"real NPC counter interaction");
        }
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real profile reconnect");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);
        Check(!f.View.record.goldKnown&&!f.View.record.bankKnown&&f.View.record.currency==null,"legacy unknown wallet/bank preserved without a fabricated initial balance");
        Check(f.server.ExecuteCurrencyState(Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.currencyRequest==null&&State(f.View.record)==before,"missing authority does not fabricate balances or lock queries");
        Check(f.server.ReceiveCurrencyState(f.Notice()).Failure==WorldFailure.InvalidInput&&State(f.View.record)==before,"empty update cannot turn unknown fields into zero");
        var zero=f.Notice(0);Check(f.server.ReceiveCurrencyState(zero).Accepted&&f.View.record.goldKnown&&f.View.record.gold==0&&!f.View.record.bankKnown&&f.View.record.currency.revision==1,"explicit wallet zero becomes known, absent bank remains unknown");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.inventory.Length==1&&f.View.record.growth==null,"currency receipt cannot heal, grant items or level up");
        before=State(f.View.record);Check(f.server.ReceiveCurrencyState(zero).Failure==WorldFailure.StaleRequest&&State(f.View.record)==before,"duplicate absolute balance is not applied twice");
        f.Funds(16000,3000);Check(f.View.record.gold==16000&&f.View.record.bank==3000&&f.View.record.bankKnown,"independent wallet and bank values received");
        f.Funds(null,0);Check(f.View.record.bank==0&&f.View.record.gold==16000,"bank-only explicit zero does not touch wallet");
        f.Funds(7);Check(f.View.record.gold==7&&f.View.record.bank==0,"lower server balance replaces rather than adds to current money");
        f.Funds(ulong.MaxValue,ulong.MaxValue);Check(f.View.record.gold==ulong.MaxValue&&f.View.record.bank==ulong.MaxValue,"unsigned display/storage range is preserved without declaring a server limit");
        var stamp=f.View;stamp.record.currency.revision=999;Check(f.View.record.currency.revision==5,"receipt view cannot mutate live revision");
        var retry=f.Notice(42,99);f.store.fail=true;before=State(f.View.record);
        Check(f.server.ReceiveCurrencyState(retry).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed currency save preserves amounts/known flags/revision");
        f.store.fail=false;Check(f.server.ReceiveCurrencyState(retry).Accepted&&f.View.record.gold==42&&f.View.record.bank==99,"same receipt retries after storage failure");
        var oldSession=f.Notice(100);before=State(f.View.record);f.Reload();Check(State(f.View.record)==before,"amounts, flags and receipt persist after reconnect");
        Check(f.server.ReceiveCurrencyState(oldSession).Failure==WorldFailure.StaleRequest,"old currency session cannot replay after reconnect");
        foreach(string error in new[]{"owner","session","revision","id","approval","request"})
        {
            var n=f.Notice(100);switch(error)
            {case "owner":n.character=Id();break;case "session":n.session=Id();break;case "revision":n.expectedRevision++;break;case "id":n.id="bad";break;case "approval":n.approved=false;break;case "request":n.request=Id();break;}
            before=State(f.View.record);Check(!f.server.ReceiveCurrencyState(n).Accepted&&State(f.View.record)==before,"invalid notice preserves balances: "+error);
        }
        string query=Id();Check(f.server.RequestCurrencyState(query).Accepted,"query queued without balance mutation");var q=f.View.currencyRequest;
        Check(f.server.RequestCurrencyState(Id()).Failure==WorldFailure.Busy,"one unresolved query");
        var deny=f.Notice();deny.request=q.id;deny.approved=false;before=State(f.View.record);
        Check(!f.server.ReceiveCurrencyState(deny).Accepted&&f.View.currencyRequest==null&&State(f.View.record)==before,"query denial unlocks without monetary side effects");
        Check(f.server.RequestCurrencyState(query).Failure==WorldFailure.StaleRequest,"query identity cannot be reused");
        foreach(string failure in new[]{"null","throw","wrong-request"})
        {
            before=State(f.View.record);var result=f.server.ExecuteCurrencyState(Id(),(r,v)=>{if(failure=="null")return null;if(failure=="throw")throw new Exception("fixture");var n=f.Notice(100);n.request=Id();return n;});
            Check(!result.Accepted&&f.View.currencyRequest==null&&State(f.View.record)==before,"failed provider unlocks and preserves state: "+failure);
        }
        Check(f.server.ExecuteCurrencyState(Id(),(r,v)=>{v.record.gold=9999;var n=f.Notice(150);n.request=r.id;return n;}).Accepted&&f.View.record.gold==150,"configured query provider operates on copies and applies its approved final value");
        // Query context follows the character across maps, not the temporary bank/shop window.
        var across=new Fixture();across.server.RequestCurrencyState(Id());var pending=across.View.currencyRequest;var reply=across.Notice(1000,2000);reply.request=pending.id;
        var gate=across.definition.Map(1000).portals.First(p=>p.targetMap==2400);across.Walk(gate.cells[0].x,gate.cells[0].y);across.server.EnterWorldPortal(gate.id,false);
        Check(across.server.ReceiveCurrencyState(reply).Accepted&&across.View.record.map==2400&&across.View.record.gold==1000,"valid delayed scalar reply survives an unrelated map transition");
        // Normal money-changing transactions invalidate old scalar query revisions too.
        var shop=new Fixture();var start=shop.Notice(16000,4000);shop.server.ReceiveCurrencyState(start);shop.Facility(2000);var token=shop.View.facility.token;
        shop.server.RequestCurrencyState(Id());var delayed=shop.Notice(16000,4000);delayed.request=shop.View.currencyRequest.id;
        var dagger=shop.definition.items.First(i=>i.name=="대거");Check(dagger.price==8000&&shop.server.BuyWorldItem(dagger.id,token,Id()).Accepted,"received wallet funds buy the native dagger through real shop service");
        Check(shop.View.record.gold==8000&&shop.View.record.currency.revision==2&&shop.View.currencyRequest==null,"purchase debit advances currency revision and retires older query");
        before=State(shop.View.record);Check(shop.server.ReceiveCurrencyState(delayed).Failure==WorldFailure.StaleRequest&&State(shop.View.record)==before,"late pre-purchase balance cannot refund spent money");
        start.expectedRevision=shop.View.record.currency.revision;Check(shop.server.ReceiveCurrencyState(start).Failure==WorldFailure.StaleRequest,"old notice identity cannot be reused with a forged newer revision");
        // A fresh scalar update must not erase staged warehouse moves when confirmed later.
        var warehouse=new Fixture();warehouse.Funds(12000,2000);warehouse.definition.rules.storageRuleKnown=true;warehouse.definition.rules.storageSlots=48;warehouse.definition.rules.storageFee=10;
        warehouse.Facility(2510);token=warehouse.View.facility.token;var book=warehouse.View.record.inventory.Single().id;
        Check(warehouse.server.MoveWorldStorage(true,book,token,Id()).Accepted,"isolated known-fee storage draft stages actual owned book");
        warehouse.Funds(9000,777);Check(warehouse.View.facility.storage.Single().id==book&&warehouse.View.record.inventory.Single().id==book,"currency snapshot preserves staged transfer and committed inventory separately");
        Check(warehouse.server.ConfirmWorldStorage(token,Id()).Accepted&&warehouse.View.record.gold==8990&&warehouse.View.record.bank==777&&warehouse.View.record.storage.Single().id==book&&warehouse.View.record.currency.revision==3,"warehouse confirmation uses refreshed balances/version without undoing staged item move");
        // Checkpoint an intermediate grid tile, then fail the later path-end save.
        var moving=new Fixture();Check(moving.server.MoveWorld(23,21).Accepted,"multi-step world movement starts");moving.server.AdvanceWorld(160);
        Check(moving.View.moving&&moving.View.record.x==22,"intermediate confirmed tile reached while route continues");moving.Funds(909,808);
        Check(moving.View.moving&&moving.store.data.accounts[0].character.world.x==22,"scalar snapshot persists reached tile without stopping route");
        moving.store.fail=true;moving.server.AdvanceWorld(10000);
        Check(!moving.View.moving&&moving.View.record.x==22&&moving.View.record.gold==909&&moving.View.record.bank==808&&moving.View.record.currency.revision==1,"failed route-end save rolls back to currency checkpoint, never to old money");
        moving.store.fail=false;moving.Reload();Check(moving.View.record.x==22&&moving.View.record.gold==909,"checkpoint and latest wallet survive actual reconnect");
        // A temple quote made against an older balance is invalidated by the scalar update.
        var temple=new Fixture(true);temple.Funds(500);temple.Facility(2600);token=temple.View.facility.token;
        RevivalQuote Offer(RevivalQuoteRequest r)=>new RevivalQuote{approved=true,id=Id(),request=r.id,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,revision=r.revision,fee=100};
        temple.server.ExecuteRevivalQuote(token,Id(),(r,v)=>Offer(r));temple.server.RequestTempleRevival(token,Id());var revival=temple.View.revivalRequest;
        var staleRevive=new RevivalNotice{approved=true,id=Id(),request=revival.id,quote=revival.quote,character=revival.character,session=revival.session,token=revival.token,npc=revival.npc,map=revival.map,revision=revival.revision,gold=400,health=new CharacterGrowthHealth{hp=50,maxHp=200,mp=3,maxMp=65}};
        temple.Funds(400);Check(temple.View.facility.revivalQuote==null&&temple.View.revivalRequest==null&&temple.server.ReceiveTempleRevival(staleRevive).Failure==WorldFailure.StaleRequest,"received balance invalidates old revival price/request before it can heal or charge");
        temple.server.ExecuteRevivalQuote(token,Id(),(r,v)=>Offer(r));
        Check(temple.server.ExecuteTempleRevival(token,Id(),(r,v)=>new RevivalNotice{approved=true,id=Id(),request=r.id,quote=r.quote,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,revision=r.revision,gold=300,health=new CharacterGrowthHealth{hp=50,maxHp=200,mp=3,maxMp=65}}).Accepted
            &&temple.View.record.gold==300&&!temple.View.record.ghost&&temple.View.record.currency.revision==3,"fresh approved revival pays once and advances both recovery and currency versions");
        // Missing bank policy is still withheld after balances become known.
        var bank=new Fixture();bank.Funds(1000,2000);bank.Facility(2400);token=bank.View.facility.token;before=State(bank.View.record);
        Check(bank.server.BankWorld(true,100,token,Id()).Failure==WorldFailure.RulesUnavailable&&State(bank.View.record)==before,"known balances do not imply free/known bank fees");
        bank.definition.rules.bankFeeKnown=true;bank.definition.rules.bankFee=5;
        Check(bank.server.BankWorld(true,100,token,Id()).Accepted&&bank.View.record.gold==895&&bank.View.record.bank==2100&&bank.View.record.currency.revision==2,"explicit legacy deposit also advances shared version");
        Check(bank.server.BankWorld(false,100,token,Id()).Accepted&&bank.View.record.gold==990&&bank.View.record.bank==2000&&bank.View.record.currency.revision==3,"explicit legacy withdrawal preserves checked arithmetic and advances version");
        // Stale service snapshots and fresh unrelated item changes cannot be overwritten.
        var concurrent=new Fixture();var oldServer=concurrent.server;var oldNotice=concurrent.Notice(10);concurrent.Reload();concurrent.Funds(200,300);var saved=State(concurrent.store.data);
        Check(oldServer.ReceiveCurrencyState(oldNotice).Failure==WorldFailure.StaleRequest&&State(concurrent.store.data)==saved,"older service cannot overwrite a newer persisted currency revision");
        var freshItem=new Fixture();var gift=new WorldOwnedItem{id=Id(),item=201,slot=1};freshItem.store.data.accounts[0].character.world.inventory=freshItem.View.record.inventory.Concat(new[]{gift}).ToArray();saved=State(freshItem.store.data);
        Check(freshItem.server.ReceiveCurrencyState(freshItem.Notice(100)).Failure==WorldFailure.StaleRequest&&State(freshItem.store.data)==saved,"currency receipt cannot erase fresh unrelated inventory written by another service");
        string path=Path.Combine(Path.GetTempPath(),"ds-currency-"+Id(),"accounts.json");
        try
        {
            var large=new Fixture();large.Funds(ulong.MaxValue,ulong.MaxValue);var file=new LocalAccountFileStore(path);
            Check(file.TryWrite(large.store.data)&&file.TryRead(out var document)&&document.accounts[0].character.world.gold==ulong.MaxValue,"unsigned money and receipt survive real DataContract file write/read");
            var restored=new EntryAccountService(file);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(large.definition).Accepted&&restored.WorldSnapshot().record.bank==ulong.MaxValue&&restored.WorldSnapshot().record.currency.revision==1,"file-backed login restores independent known bank value without signed truncation");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Authoritative absolute wallet/bank receipts, persistence and legacy monetary transaction revisions with isolated values; no starting-fund/fee/reward policy or raw wire claim."}));
    }
}
