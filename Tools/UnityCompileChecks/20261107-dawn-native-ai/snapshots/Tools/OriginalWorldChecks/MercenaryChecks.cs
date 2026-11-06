using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class MercenaryChecks
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
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;public string staff;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool money=true,ulong gold=1000,int job=-1,int count=0)
        {
            definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Options);
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"용병검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,goldKnown=money,gold=money?gold:0,bankKnown=true,bank=400,hiredJob=job,hiredCount=count,
                inventory=new[]{new WorldOwnedItem{id=Id(),item=200,slot=1}}};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world begins");
            var gate=definition.Map(1000).portals.First(p=>p.targetMap==2900);Walk(gate.cells[0].x,gate.cells[0].y);Check(server.EnterWorldPortal(gate.id,false).Accepted,"actual mercenary-house portal");
            var npc=definition.Map(2900).npcs.Single();staff=npc.id;Walk(npc.counter[0].x,npc.counter[0].y);Check(server.OpenWorldFacility(staff).Accepted,"NPC counter opens original facility");
        }
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"grid walk request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"grid arrival");}
        public void Queue(bool hire=true,int job=200)
        {Check(server.RequestMercenaryTransfer(hire,job,View.facility.token,Id()).Accepted,"queue explicit hire/dismiss");}
        public MercenaryNotice Notice(MercenaryRequest r=null,ulong? amount=null)
        {
            r=r??View.mercenaryRequest;var price=amount??(r.hire?r.price.Value:0UL);
            return new MercenaryNotice{approved=true,id=Id(),request=r.id,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,commander=r.commander,
                hire=r.hire,job=r.job,count=r.count+(r.hire?1:-1),expectedRevision=r.revision,currencyRevision=r.currencyRevision,amount=price,
                gold=r.hire?View.record.gold-price:View.record.gold+price};
        }
        public void Approve(bool hire=true,int job=200,ulong? refund=null)
        {Check(server.ExecuteMercenaryTransfer(hire,job,View.facility.token,Id(),(r,v)=>Notice(r,refund)).Accepted,"explicit transfer approved");}
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"profile reconnect");Check(server.OpenWorldFacility(staff).Accepted,"new NPC token");}
        public void Funds(ulong gold)=>Check(server.ReceiveCurrencyState(new CurrencyNotice{approved=true,id=Id(),character=View.characterId,session=View.currencySession,expectedRevision=View.record.currency?.revision??0,gold=gold}).Accepted,"new absolute funds");
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var initial=State(f.View.record);
        Check(!f.definition.rules.hireRuleKnown&&!f.definition.rules.dismissalRuleKnown,"unknown operating policy remains withheld");
        Check(f.server.HireWorldMercenary(200,f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"default legacy hire remains held");
        Check(f.server.ExecuteMercenaryTransfer(true,200,f.View.facility.token,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.mercenaryRequest==null&&State(f.View.record)==initial,"absent authority cannot charge/hire/lock");
        Check(f.server.RequestMercenaryTransfer(false,-1,f.View.facility.token,Id()).Failure==WorldFailure.NotOwned,"cannot dismiss zero members");
        Check(f.server.RequestMercenaryTransfer(true,99999,f.View.facility.token,Id()).Failure==WorldFailure.InvalidInput,"job not in commander list rejected");
        f.Queue();var request=f.View.mercenaryRequest;var notice=f.Notice();
        Check(request.price==200&&request.limit==2&&request.count==0&&request.commander==100,"original offer metadata in request");
        Check(State(f.View.record)==initial&&f.store.data.accounts[0].character.world.hiredCount==0,"request has no speculative money/ownership change");
        request.job=999;Check(f.View.mercenaryRequest.job==200,"pending view is detached");
        Check(f.server.RequestMercenaryTransfer(true,200,f.View.facility.token,Id()).Failure==WorldFailure.Busy,"one unresolved transfer");
        f.definition.rules.hireRuleKnown=true;
        Check(f.server.HireWorldMercenary(200,f.View.facility.token,Id()).Failure==WorldFailure.Busy,"legacy cannot bypass pending approval");f.definition.rules.hireRuleKnown=false;
        f.store.fail=true;Check(f.server.ReceiveMercenaryTransfer(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==initial&&f.View.mercenaryRequest!=null,"failed write preserves old money/ownership and pending retry");
        f.store.fail=false;Check(f.server.ReceiveMercenaryTransfer(notice).Accepted,"same approval retries after failed save");
        Check(f.View.record.hiredJob==200&&f.View.record.hiredCount==1&&f.View.record.gold==800&&f.View.record.mercenary.revision==1&&f.View.record.currency.revision==1,"one member, price, receipt and currency committed together");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.bank==400&&f.View.record.inventory.Length==1&&!f.definition.rules.hireRuleKnown,"unrelated state and policy preserved");
        Check(!f.server.ReceiveMercenaryTransfer(notice).Accepted&&f.View.record.hiredCount==1,"same receipt cannot hire twice");
        Check(!f.server.RequestMercenaryTransfer(true,201,f.View.facility.token,Id()).Accepted,"cannot mix types while one is owned");
        f.Approve();Check(f.View.record.hiredCount==2&&f.View.record.gold==600,"second member uses original price");
        Check(!f.server.RequestMercenaryTransfer(true,200,f.View.facility.token,Id()).Accepted,"original count limit enforced");
        f.Queue(false,999);var dismissal=f.Notice();Check(f.View.mercenaryRequest.job==200&&f.View.mercenaryRequest.price==null,"dismiss refers to current type, no invented refund");
        dismissal.amount=null;Check(f.server.ReceiveMercenaryTransfer(dismissal).Failure==WorldFailure.RulesUnavailable&&f.View.record.hiredCount==2,"missing refund is not zero");
        dismissal.amount=0;dismissal.gold=600;Check(f.server.ReceiveMercenaryTransfer(dismissal).Accepted&&f.View.record.hiredCount==1&&f.View.record.gold==600&&f.View.record.mercenary.revision==3,"explicit zero refund is allowed without defaulting unknown to zero");
        f.Approve(false,999,50);Check(f.View.record.hiredCount==0&&f.View.record.hiredJob==-1&&f.View.record.gold==650,"last dismiss clears type and credits only explicit refund");
        f.Reload();Check(f.View.record.hiredCount==0&&f.View.record.hiredJob==-1&&f.View.record.mercenary.amount==50&&f.View.mercenaryRequest==null,"committed type/count/refund survive reconnect; pending does not");
        f.Approve(true,201);Check(f.View.record.hiredJob==201&&f.View.record.hiredCount==1&&f.View.record.gold==350,"after last dismiss another original type can be hired");
        foreach(string problem in new[]{"character","session","token","npc","map","request","id","commander","revision","currency","hire","job","count","missing-amount","missing-gold","price","gold"})
        {
            var bad=new Fixture();bad.Queue();var n=bad.Notice();var before=State(bad.View.record);
            switch(problem)
            {
                case "character":n.character=Id();break;case "session":n.session=Id();break;case "token":n.token=Id();break;case "npc":n.npc="other";break;
                case "map":n.map=2400;break;case "request":n.request=Id();break;case "id":n.id="bad";break;case "commander":n.commander=101;break;
                case "revision":n.expectedRevision++;break;case "currency":n.currencyRevision++;break;case "hire":n.hire=false;break;case "job":n.job=201;break;
                case "count":n.count=2;break;case "missing-amount":n.amount=null;break;case "missing-gold":n.gold=null;break;case "price":n.amount=199;n.gold=801;break;case "gold":n.gold++;break;
            }
            Check(!bad.server.ReceiveMercenaryTransfer(n).Accepted&&State(bad.View.record)==before,"reject malformed response without mutation: "+problem);
        }
        var unknown=new Fixture(false);Check(unknown.server.RequestMercenaryTransfer(true,200,unknown.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"unknown wallet held");
        var poor=new Fixture(gold:199);Check(poor.server.RequestMercenaryTransfer(true,200,poor.View.facility.token,Id()).Failure==WorldFailure.InsufficientFunds,"insufficient original price held before authority");
        var denied=new Fixture();denied.Queue();var no=denied.Notice();no.approved=false;Check(!denied.server.ReceiveMercenaryTransfer(no).Accepted&&denied.View.mercenaryRequest==null&&denied.View.record.hiredCount==0,"denial unlocks without hiring");
        foreach(string mode in new[]{"null","throw","wrong-request","invalid"})
        {
            var provider=new Fixture();var before=State(provider.View.record);
            var reply=provider.server.ExecuteMercenaryTransfer(true,200,provider.View.facility.token,Id(),(r,v)=>
            {if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var n=provider.Notice(r);if(mode=="wrong-request")n.request=Id();else n.count=4;return n;});
            Check(!reply.Accepted&&provider.View.mercenaryRequest==null&&State(provider.View.record)==before,"provider failure unlocks without side effects: "+mode);
        }
        var stale=new Fixture();stale.Queue();var old=stale.Notice();stale.server.CloseWorldFacility();stale.server.OpenWorldFacility(stale.staff);Check(!stale.server.ReceiveMercenaryTransfer(old).Accepted&&stale.View.record.hiredCount==0,"old facility token rejected");
        stale.Queue();old=stale.Notice();stale.Reload();Check(!stale.server.ReceiveMercenaryTransfer(old).Accepted,"old login session rejected");
        var currency=new Fixture();currency.Queue();old=currency.Notice();currency.Funds(900);Check(currency.View.mercenaryRequest==null&&!currency.server.ReceiveMercenaryTransfer(old).Accepted&&currency.View.record.gold==900,"new funds invalidate old pending hire");currency.Approve();Check(currency.View.record.gold==700,"new request charges latest wallet");
        var moved=new Fixture();moved.Queue();old=moved.Notice();moved.server.CloseWorldFacility();moved.Walk(10,13);Check(!moved.server.ReceiveMercenaryTransfer(old).Accepted&&moved.View.record.hiredCount==0,"late approval after close and walking away rejected");
        var distant=new Fixture();distant.Queue();old=distant.Notice();var staff=distant.definition.Map(2900).npcs.Single();staff.x=18;staff.y=12;staff.counter=Array.Empty<WorldPoint>();
        Check(distant.server.ReceiveMercenaryTransfer(old).Failure==WorldFailure.TooFar&&distant.View.record.hiredCount==0,"current NPC interaction distance rechecked at approval");
        var interrupted=new Fixture();interrupted.Queue();interrupted.Reload();Check(interrupted.View.record.gold==1000&&interrupted.View.record.hiredCount==0&&interrupted.View.mercenaryRequest==null,"unapproved interrupted hire cannot persist");
        var overflow=new Fixture(gold:ulong.MaxValue,job:200,count:1);overflow.Queue(false);var over=overflow.Notice(amount:1);Check(!overflow.server.ReceiveMercenaryTransfer(over).Accepted&&overflow.View.record.gold==ulong.MaxValue&&overflow.View.record.hiredCount==1,"refund overflow rejected");
        var maximum=new Fixture(gold:ulong.MaxValue);maximum.Approve();Check(maximum.View.record.gold==ulong.MaxValue-200,"large unsigned wallet charged exactly");
        // Free transfers still change ownership and require a separate persisted revision.
        var concurrent=new Fixture();concurrent.definition.hires.First(h=>h.commander==100&&h.job==200).price=0;
        var other=new EntryAccountService(concurrent.store);new EntryClient(other).OpenLocalProfile();Check(other.BeginWorld(concurrent.definition).Accepted&&other.OpenWorldFacility(concurrent.staff).Accepted,"second service reads same ownership");
        Check(other.RequestMercenaryTransfer(true,200,other.WorldSnapshot().facility.token,Id()).Accepted,"second service queues free hire");var r=other.WorldSnapshot().mercenaryRequest;
        var older=new MercenaryNotice{approved=true,id=Id(),request=r.id,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,commander=r.commander,hire=true,job=200,count=1,expectedRevision=0,currencyRevision=0,amount=0,gold=1000};
        concurrent.Approve();Check(other.ReceiveMercenaryTransfer(older).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.mercenary.revision==1,"free hire revision protects more recent service save");
        var unrelated=new Fixture();unrelated.Queue();var approval=unrelated.Notice();unrelated.store.data.accounts[0].character.world.inventory[0].item=201;
        Check(unrelated.server.ReceiveMercenaryTransfer(approval).Failure==WorldFailure.StaleRequest&&unrelated.store.data.accounts[0].character.world.inventory[0].item==201,"unrelated newer inventory not overwritten by hire");
        var legacy=new Fixture();legacy.definition.rules.hireRuleKnown=true;legacy.definition.rules.dismissalRuleKnown=true;legacy.definition.rules.dismissalRefund=5;
        Check(legacy.server.HireWorldMercenary(200,legacy.View.facility.token,Id()).Accepted&&legacy.View.record.mercenary.revision==1,"explicit legacy hire records ownership version");
        Check(legacy.server.DismissWorldMercenary(legacy.View.facility.token,Id()).Accepted&&legacy.View.record.mercenary.revision==2&&legacy.View.record.gold==805,"explicit legacy dismiss records refund and ownership version");
        var path=Path.Combine(Path.GetTempPath(),"ds-mercenary-"+Id()+".json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var doc)&&doc.accounts[0].character.world.mercenary.revision==5,"receipt survives actual DataContract file write/read");
            var service=new EntryAccountService(disk);new EntryClient(service).OpenLocalProfile();Check(service.BeginWorld(f.definition).Accepted&&service.WorldSnapshot().record.hiredJob==201&&service.WorldSnapshot().record.hiredCount==1&&service.WorldSnapshot().record.gold==350,"file-backed reconnect restores approved ownership and money");
        }
        finally{if(File.Exists(path))File.Delete(path);}
        var valid=f.View.record.Copy();valid.mercenary.revision=0;Check(!WorldRecord.Valid(valid),"invalid saved receipt rejected");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual original-list mercenary hire/dismiss approval, explicit refund, ownership/currency and isolated persistence contracts; not battle deployment or recovered server policy."}));
    }
}
