using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class BankChecks
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
        public Fixture(bool funds=true)
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"은행검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=65,inventory=new[]{new WorldOwnedItem{id=Id(),item=200,slot=0}}};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world begins");
            if(funds)Funds(1000,2000);
            var gate=definition.Map(1000).portals.First(p=>p.targetMap==2400);Walk(gate.cells[0].x,gate.cells[0].y);Check(server.EnterWorldPortal(gate.id,false).Accepted,"real bank entrance");
            var npc=definition.Map(2400).npcs.Single();staff=npc.id;Walk(npc.counter[0].x,npc.counter[0].y);Check(server.OpenWorldFacility(npc.id).Accepted,"actual bank counter interaction");
        }
        public void Funds(ulong wallet,ulong savings)=>Check(server.ReceiveCurrencyState(new CurrencyNotice{approved=true,id=Id(),character=View.characterId,session=View.currencySession,expectedRevision=View.record.currency?.revision??0,gold=wallet,bank=savings}).Accepted,"isolated approved balances received");
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"native world walk request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"native world walk arrival");}
        public void Queue(bool deposit=true,ulong amount=100)=>Check(server.RequestBankTransfer(deposit,amount,View.facility.token,Id()).Accepted,"bank transfer queued");
        public BankTransferNotice Notice(ulong? fee=5,BankFeeSource? source=BankFeeSource.Wallet,BankTransferRequest r=null)
        {
            r=r??View.bankTransferRequest;
            BigInteger wallet=View.record.gold,savings=View.record.bank;
            if(r.deposit){wallet-=r.amount;savings+=r.amount;}else{wallet+=r.amount;savings-=r.amount;}
            if(source==BankFeeSource.Wallet)wallet-=fee??0;else savings-=fee??0;
            return new BankTransferNotice{approved=true,id=Id(),request=r.id,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,deposit=r.deposit,amount=r.amount,
                expectedRevision=r.currencyRevision,fee=fee,feeSource=source,gold=wallet>=0&&wallet<=ulong.MaxValue?(ulong)wallet:0,bank=savings>=0&&savings<=ulong.MaxValue?(ulong)savings:0};
        }
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real profile reconnect");Check(server.OpenWorldFacility(staff).Accepted,"bank window reopened with a fresh token");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);string token=f.View.facility.token;
        Check(!f.definition.rules.bankFeeKnown&&f.server.BankWorld(true,100,token,Id()).Failure==WorldFailure.RulesUnavailable,"default fee policy remains held");
        Check(f.server.ExecuteBankTransfer(true,100,token,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.bankTransferRequest==null&&State(f.View.record)==before,"missing authority cannot create free transfers or pending input locks");
        Check(f.server.RequestBankTransfer(true,0,token,Id()).Failure==WorldFailure.InvalidInput,"zero transfer refused");
        Check(f.server.RequestBankTransfer(true,1001,token,Id()).Failure==WorldFailure.InsufficientFunds&&f.server.RequestBankTransfer(false,2001,token,Id()).Failure==WorldFailure.InsufficientFunds,"source principal cannot exceed known funds");
        f.Queue();var request=f.View.bankTransferRequest;var notice=f.Notice();
        Check(State(f.View.record)==before&&f.View.record.gold==1000&&f.View.record.bank==2000,"request alone neither debits nor credits");
        var exposed=f.View;exposed.bankTransferRequest.amount=999;Check(f.View.bankTransferRequest.amount==100,"request view is detached");
        Check(f.server.RequestBankTransfer(false,100,token,Id()).Failure==WorldFailure.Busy&&f.server.BankWorld(true,100,token,Id()).Failure==WorldFailure.Busy,"modern and compatibility paths cannot overlap pending transfer");
        f.store.fail=true;Check(f.server.ReceiveBankTransfer(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.bankTransferRequest!=null,"failed save preserves both balances, receipt and pending request");
        f.store.fail=false;Check(f.server.ReceiveBankTransfer(notice).Accepted,"same approved transfer retries after storage recovery");
        Check(f.View.record.gold==895&&f.View.record.bank==2100&&f.View.record.currency.revision==2&&f.View.record.lastBankTransfer.id==notice.id&&f.View.record.lastBankTransfer.fee==5,"deposit and explicit wallet fee persist together with shared currency revision");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.inventory.Length==1&&f.View.record.growth==null&&!f.definition.rules.bankFeeKnown,"transfer changes no items, vitals, growth or operating policy");
        before=State(f.View.record);Check(f.server.ReceiveBankTransfer(notice).Failure==WorldFailure.StaleRequest&&State(f.View.record)==before,"duplicate bank approval cannot pay twice");
        Check(f.server.RequestBankTransfer(true,100,token,request.id).Failure==WorldFailure.StaleRequest,"retired request nonce cannot be reused");
        notice.fee=0;var view=f.View;view.record.lastBankTransfer.fee=0;Check(f.View.record.lastBankTransfer.fee==5,"receipt view/provider payload do not alias persisted audit");
        f.Reload();Check(f.View.record.gold==895&&f.View.record.bank==2100&&f.View.record.lastBankTransfer.fee==5,"balances and explicit fee survive login");
        Check(f.server.ExecuteBankTransfer(false,100,f.View.facility.token,Id(),(r,v)=>f.Notice(7,BankFeeSource.Savings,r)).Accepted&&f.View.record.gold==995&&f.View.record.bank==1993&&f.View.record.lastBankTransfer.feeSource==BankFeeSource.Savings,"approved withdrawal may explicitly charge savings rather than guessing wallet fees");
        var unknown=new Fixture(false);Check(unknown.server.RequestBankTransfer(true,100,unknown.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable&&!unknown.View.record.goldKnown&&!unknown.View.record.bankKnown,"missing balances are not fabricated as zero");
        var denied=new Fixture();denied.Queue();var rejection=denied.Notice();rejection.approved=false;rejection.gold=null;rejection.bank=null;rejection.fee=null;rejection.feeSource=null;
        before=State(denied.View.record);Check(!denied.server.ReceiveBankTransfer(rejection).Accepted&&denied.View.bankTransferRequest==null&&State(denied.View.record)==before,"denial unlocks without needing invented result money/fees");
        foreach(string problem in new[]{"owner","session","token","npc","map","request","id","revision","direction","amount","wallet","savings","fee","fee-source","fee-code"})
        {
            var bad=new Fixture();bad.Queue();var n=bad.Notice();switch(problem)
            {
                case "owner":n.character=Id();break;case "session":n.session=Id();break;case "token":n.token=Id();break;case "npc":n.npc="bad";break;case "map":n.map=1000;break;
                case "request":n.request=Id();break;case "id":n.id="bad";break;case "revision":n.expectedRevision++;break;case "direction":n.deposit=false;break;case "amount":n.amount=101;break;
                case "wallet":n.gold=896;break;case "savings":n.bank=2101;break;case "fee":n.fee=null;break;case "fee-source":n.feeSource=null;break;case "fee-code":n.feeSource=(BankFeeSource)99;break;
            }
            before=State(bad.View.record);Check(!bad.server.ReceiveBankTransfer(n).Accepted&&State(bad.View.record)==before,"invalid bank approval preserves complete state: "+problem);
        }
        // Independent BigInteger reference covers final-value validation and intermediate UInt64 overflow.
        var matrix=new Fixture();var max=ulong.MaxValue;
        var cases=new (ulong gold,ulong bank,ulong amount,ulong fee,BankFeeSource source,bool deposit)[]{
            (1000,2000,100,5,BankFeeSource.Wallet,true),(1000,2000,100,5,BankFeeSource.Savings,true),
            (1000,2000,100,5,BankFeeSource.Wallet,false),(1000,2000,100,5,BankFeeSource.Savings,false),
            (max,0,max,0,BankFeeSource.Wallet,true),(0,max,max,0,BankFeeSource.Savings,false),
            (1,max,1,1,BankFeeSource.Savings,true),(max,1,1,1,BankFeeSource.Wallet,false),
            (max,max,1,0,BankFeeSource.Wallet,true),(max,max,1,0,BankFeeSource.Savings,false),
            (100,0,100,1,BankFeeSource.Wallet,true),(0,100,100,1,BankFeeSource.Savings,false),
            (0,100,1,2,BankFeeSource.Wallet,false),(100,0,1,2,BankFeeSource.Savings,true),
            (max,max,max,max,BankFeeSource.Savings,true),(max,max,max,max,BankFeeSource.Wallet,false)};
        foreach(var c in cases)
        {
            matrix.Funds(c.gold,c.bank);matrix.Queue(c.deposit,c.amount);var n=matrix.Notice(c.fee,c.source);
            BigInteger total=(BigInteger)c.gold+c.bank-c.fee;BigInteger gold=c.gold,bank=c.bank;
            if(c.deposit){gold-=c.amount;bank+=c.amount;}else{gold+=c.amount;bank-=c.amount;}
            if(c.source==BankFeeSource.Wallet)gold-=c.fee;else bank-=c.fee;
            bool valid=gold>=0&&gold<=max&&bank>=0&&bank<=max;before=State(matrix.View.record);var reply=matrix.server.ReceiveBankTransfer(n);
            Check(reply.Accepted==valid,"exact independent arithmetic acceptance");
            if(valid)Check(matrix.View.record.gold==(ulong)gold&&matrix.View.record.bank==(ulong)bank&&(BigInteger)matrix.View.record.gold+matrix.View.record.bank==total,"exact bank value/fee conservation");
            else{Check(State(matrix.View.record)==before,"invalid arithmetic cannot mutate balances");matrix.server.CloseWorldFacility();matrix.server.OpenWorldFacility(matrix.staff);}
        }
        var closed=new Fixture();closed.Queue();var old=closed.Notice();closed.server.CloseWorldFacility();closed.server.OpenWorldFacility(closed.staff);
        Check(closed.server.ReceiveBankTransfer(old).Failure==WorldFailure.StaleRequest&&closed.View.record.gold==1000,"closing and reopening invalidates old approval token");
        closed.Queue();old=closed.Notice();closed.Reload();Check(!closed.server.ReceiveBankTransfer(old).Accepted&&closed.View.bankTransferRequest==null&&closed.View.record.gold==1000,"pending transfer is not saved and old login-session reply is refused");
        var scalar=new Fixture();scalar.Queue();old=scalar.Notice();scalar.Funds(900,2200);
        Check(scalar.View.bankTransferRequest==null&&scalar.server.ReceiveBankTransfer(old).Failure==WorldFailure.StaleRequest,"new balance snapshot invalidates outstanding bank decision");
        var distance=new Fixture();distance.Queue();var far=distance.Notice();var npc=distance.definition.Map(2400).npcs.Single();npc.x=18;npc.y=1;npc.counter=Array.Empty<WorldPoint>();
        before=State(distance.View.record);Check(distance.server.ReceiveBankTransfer(far).Failure==WorldFailure.TooFar&&State(distance.View.record)==before,"receipt cannot bypass current NPC/counter distance");
        foreach(string changed in new[]{"balance","items","receipt"})
        {
            var stale=new Fixture();stale.Queue();var n=stale.Notice();var stored=stale.store.data.accounts[0].character.world;
            if(changed=="balance")stored.gold=999;
            if(changed=="items")stored.inventory=stored.inventory.Concat(new[]{new WorldOwnedItem{id=Id(),item=201,slot=1}}).ToArray();
            if(changed=="receipt")stored.currency=new CurrencyReceipt{id=Id(),revision=2};
            var saved=State(stale.store.data);before=State(stale.View.record);
            Check(stale.server.ReceiveBankTransfer(n).Failure==WorldFailure.StaleRequest&&State(stale.store.data)==saved&&State(stale.View.record)==before,"concurrent stored state cannot be overwritten: "+changed);
        }
        foreach(string failure in new[]{"null","throw","wrong-request"})
        {
            var failed=new Fixture();before=State(failed.View.record);
            Check(!failed.server.ExecuteBankTransfer(true,100,failed.View.facility.token,Id(),(r,v)=>{if(failure=="null")return null;if(failure=="throw")throw new Exception("fixture");var n=failed.Notice(r:r);n.request=Id();return n;}).Accepted
                &&failed.View.bankTransferRequest==null&&State(failed.View.record)==before,"provider failure unlocks without money movement: "+failure);
        }
        var live=new Fixture();Check(live.server.ExecuteBankTransfer(true,100,live.View.facility.token,Id(),(r,v)=>{v.record.gold=0;return live.Notice(0,BankFeeSource.Wallet,r);}).Accepted&&live.View.record.gold==900&&live.View.record.bank==2100&&!live.definition.rules.bankFeeKnown,"configured provider can explicitly approve zero fee without setting a default free policy or aliasing record");
        // Receipt identity also participates in cross-protocol currency replay prevention.
        var replay=live.View.record.lastBankTransfer;Check(live.server.ReceiveCurrencyState(new CurrencyNotice{approved=true,id=replay.id,character=live.View.characterId,session=live.View.currencySession,expectedRevision=live.View.record.currency.revision,gold=1000,bank=2000}).Failure==WorldFailure.StaleRequest,"bank receipt cannot be replayed as a scalar balance refund");
        string path=Path.Combine(Path.GetTempPath(),"ds-bank-"+Id(),"accounts.json");
        try
        {
            var file=new LocalAccountFileStore(path);Check(file.TryWrite(live.store.data)&&file.TryRead(out var document)&&document.accounts[0].character.world.lastBankTransfer.amount==100,"real file format retains final balances and bank audit");
            var service=new EntryAccountService(file);new EntryClient(service).OpenLocalProfile();Check(service.BeginWorld(live.definition).Accepted&&service.WorldSnapshot().record.gold==900&&service.WorldSnapshot().record.bank==2100&&service.WorldSnapshot().bankTransferRequest==null,"file-backed login restores completed bank transfer without pending replay");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual bank request/approval/audit/persistence with isolated explicit fee sources and BigInteger boundary reference. No recovered bank pricing/limit/shared-account or raw wire claim."}));
    }
}
