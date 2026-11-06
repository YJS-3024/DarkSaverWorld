using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class ShopChecks
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
        public Fixture(int count=1,bool money=true,ulong gold=20000)
        {
            definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Options);
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"상점검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,goldKnown=money,gold=money?gold:0,bankKnown=true,bank=400,
                inventory=Enumerable.Range(0,count).Select(i=>new WorldOwnedItem{id=Id(),item=1,slot=i}).ToArray(),
                storage=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0}},equipment=new[]{new WorldOwnedItem{id=Id(),item=1,slot=2}}};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world begins");
            var gate=definition.Map(1000).portals.First(p=>p.targetMap==2000);Walk(gate.cells[0].x,gate.cells[0].y);Check(server.EnterWorldPortal(gate.id,false).Accepted,"actual weapon shop portal");
            var npc=definition.Map(2000).npcs.First();staff=npc.id;Walk(npc.counter[0].x,npc.counter[0].y);Check(server.OpenWorldFacility(staff).Accepted,"shop employee counter");
        }
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"grid walk request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"grid arrival");}
        public ShopQuote Quote(ShopRequest r=null,ulong price=8000)=>new ShopQuote{id=Id(),request=(r??View.shopQuoteRequest).Copy(),approved=true,price=price};
        public void QueueQuote(bool buy=true,string owned=null,int item=1)
        {Check(server.RequestShopQuote(buy,item,owned,View.facility.token,Id()).Accepted,"quote requested");}
        public void Info(bool buy=true,string owned=null,ulong? price=null)
        {Check(server.ExecuteShopQuote(buy,1,owned,View.facility.token,Id(),(r,v)=>Quote(r,price??(buy?8000UL:4000UL))).Accepted,"explicit quote accepted");}
        public void QueueTrade()=>Check(server.RequestShopTrade(View.facility.shopQuote.id,View.facility.token,Id()).Accepted,"quoted price confirmed");
        public ShopNotice Notice(ShopRequest r=null)
        {
            r=r??View.shopTradeRequest;
            return new ShopNotice{id=Id(),request=r.Copy(),approved=true,price=r.price,gold=r.buy?View.record.gold-r.price.Value:View.record.gold+r.price.Value,
                acquired=r.buy?new WorldOwnedItem{id=Id(),item=r.item,slot=r.slot}:null};
        }
        public void Approve()=>Check(server.ExecuteShopTrade(View.facility.shopQuote.id,View.facility.token,Id(),(r,v)=>Notice(r)).Accepted,"approved trade commits");
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"profile reconnect");Check(server.OpenWorldFacility(staff).Accepted,"new shop token");}
        public void Funds(ulong gold)=>Check(server.ReceiveCurrencyState(new CurrencyNotice{approved=true,id=Id(),character=View.characterId,session=View.currencySession,expectedRevision=View.record.currency?.revision??0,gold=gold}).Accepted,"new absolute wallet");
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var start=State(f.View.record);var sold=f.View.record.inventory[0].id;
        Check(!f.definition.Item(1).sellKnown,"original sale price remains unconfirmed");
        Check(f.server.ExecuteShopQuote(true,1,null,f.View.facility.token,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.shopQuoteRequest==null,"null quote authority does not invent price/lock");
        Check(f.server.SellWorldItem(sold,f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"legacy unknown sale price stays held");
        Check(f.server.RequestShopQuote(false,1,f.View.record.Equipment[0].id,f.View.facility.token,Id()).Failure==WorldFailure.NotOwned,"equipped item cannot be sold as inventory");
        Check(f.server.RequestShopQuote(true,99999,null,f.View.facility.token,Id()).Failure==WorldFailure.InvalidInput,"unstocked product rejected");
        f.QueueQuote();var q=f.Quote();Check(State(f.View.record)==start&&f.View.facility.shopQuote==null,"quote request is not purchase");
        Check(f.server.RequestShopQuote(true,1,null,f.View.facility.token,Id()).Failure==WorldFailure.Busy,"one quote pending");
        Check(f.server.BuyWorldItem(1,f.View.facility.token,Id()).Failure==WorldFailure.Busy,"legacy cannot bypass pending quote");
        Check(f.server.ReceiveShopQuote(q).Accepted&&f.View.facility.shopQuote.price==8000&&State(f.View.record)==start,"original quoted price displayed without charge");
        q.request.item=9;q.price=0;Check(f.View.facility.shopQuote.price==8000&&f.View.facility.shopQuote.request.item==1,"incoming quote detached");
        var copy=f.View.facility.shopQuote;copy.request.slot=27;Check(f.View.facility.shopQuote.request.slot==1,"quote view detached");
        Check(f.server.RequestShopTrade(Id(),f.View.facility.token,Id()).Failure==WorldFailure.StaleRequest,"cannot confirm different displayed quote");
        f.QueueTrade();var n=f.Notice();Check(State(f.View.record)==start,"confirmation request does not charge/create");
        Check(f.server.RequestShopTrade(f.View.facility.shopQuote.id,f.View.facility.token,Id()).Failure==WorldFailure.Busy,"one confirmation pending");
        f.store.fail=true;Check(f.server.ReceiveShopTrade(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==start&&f.View.shopTradeRequest!=null,"save failure preserves item/money and pending retry");
        f.store.fail=false;Check(f.server.ReceiveShopTrade(n).Accepted,"exact approved purchase retries");
        Check(f.View.record.inventory.Length==2&&f.View.record.inventory.Any(i=>i.id==n.acquired.id&&i.slot==1&&i.item==1)&&f.View.record.gold==12000&&f.View.record.shop.revision==1&&f.View.record.currency.revision==1,"one acquired instance, price, receipt and currency saved together");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.bank==400&&f.View.record.Equipment.Length==1&&f.View.record.storage.Length==1,"purchase preserves unrelated state");
        n.acquired.item=99;Check(f.View.record.inventory.Last().item==1,"acquired notice instance detached");
        Check(!f.server.ReceiveShopTrade(n).Accepted&&f.View.record.gold==12000&&f.View.record.inventory.Length==2,"duplicate purchase cannot charge/grant twice");
        f.Info(false,sold);Check(f.View.facility.shopQuote.price==4000&&!f.definition.Item(1).sellKnown,"sale quote does not overwrite global unknown price table");f.QueueTrade();var sale=f.Notice();
        Check(f.server.ReceiveShopTrade(sale).Accepted&&f.View.record.inventory.Length==1&&f.View.record.inventory.All(i=>i.id!=sold)&&f.View.record.gold==16000&&f.View.record.shop.revision==2,"exact owned instance removed, explicit sale price credited");
        f.Reload();Check(f.View.record.inventory.Length==1&&f.View.record.gold==16000&&f.View.record.shop.owned==sold&&f.View.facility.shopQuote==null&&f.View.shopTradeRequest==null,"completed trade survives reconnect; price offer/pending do not");
        foreach(string field in new[]{"request","character","session","token","npc","map","item","owned","slot","buy","revision","currency","price","missing","id"})
        {
            var bad=new Fixture();bad.QueueQuote();var quote=bad.Quote();var before=State(bad.View.record);
            switch(field)
            {
                case "request":quote.request.id=Id();break;case "character":quote.request.character=Id();break;case "session":quote.request.session=Id();break;case "token":quote.request.token=Id();break;
                case "npc":quote.request.npc="other";break;case "map":quote.request.map++;break;case "item":quote.request.item++;break;case "owned":quote.request.owned=Id();break;
                case "slot":quote.request.slot++;break;case "buy":quote.request.buy=false;break;case "revision":quote.request.revision++;break;case "currency":quote.request.currencyRevision++;break;
                case "price":quote.price=7999;break;case "missing":quote.price=null;break;case "id":quote.id="bad";break;
            }
            Check(!bad.server.ReceiveShopQuote(quote).Accepted&&bad.View.facility.shopQuote==null&&State(bad.View.record)==before,"reject malformed price/context without purchase: "+field);
        }
        foreach(string field in new[]{"id","quote","context-price","item","slot","id-owned","id-stored","id-equipped","missing-item","gold","price","missing-gold","missing-price","extra-context","revision"})
        {
            var bad=new Fixture();bad.Info();bad.QueueTrade();var notice=bad.Notice();var before=State(bad.View.record);
            switch(field)
            {
                case "id":notice.id="bad";break;case "quote":notice.request.quote=Id();break;case "context-price":notice.request.price=0;break;
                case "item":notice.acquired.item=2;break;case "slot":notice.acquired.slot=27;break;case "id-owned":notice.acquired.id=bad.View.record.inventory[0].id;break;
                case "id-stored":notice.acquired.id=bad.View.record.storage[0].id;break;case "id-equipped":notice.acquired.id=bad.View.record.Equipment[0].id;break;
                case "missing-item":notice.acquired=null;break;case "gold":notice.gold++;break;case "price":notice.price=0;notice.gold=20000;break;
                case "missing-gold":notice.gold=null;break;case "missing-price":notice.price=null;break;case "extra-context":notice.request.owned=Id();break;case "revision":notice.request.revision++;break;
            }
            Check(!bad.server.ReceiveShopTrade(notice).Accepted&&State(bad.View.record)==before,"reject malformed trade without mutation: "+field);
        }
        var full=new Fixture(28);Check(full.server.RequestShopQuote(true,1,null,full.View.facility.token,Id()).Failure==WorldFailure.InventoryFull,"full inventory cannot buy");full.Info(false,full.View.record.inventory[0].id);full.Approve();Check(full.View.record.inventory.Length==27,"full inventory can still sell existing item");
        var poor=new Fixture(gold:7999);poor.Info();Check(poor.server.RequestShopTrade(poor.View.facility.shopQuote.id,poor.View.facility.token,Id()).Failure==WorldFailure.InsufficientFunds&&poor.View.record.gold==7999,"quoted price is not a grant when wallet insufficient");
        var unknown=new Fixture(money:false);unknown.Info(false,unknown.View.record.inventory[0].id);Check(unknown.server.RequestShopTrade(unknown.View.facility.shopQuote.id,unknown.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"price can be displayed but unknown wallet cannot trade");
        var known=new Fixture();known.definition.Item(1).sellKnown=true;known.definition.Item(1).sellPrice=7;known.QueueQuote(false,known.View.record.inventory[0].id);Check(known.server.ReceiveShopQuote(known.Quote(price:8)).Failure==WorldFailure.InvalidInput,"explicit known original sale price cannot be replaced by different quote");
        var extra=new Fixture();extra.Info(false,extra.View.record.inventory[0].id);extra.QueueTrade();var e=extra.Notice();e.acquired=new WorldOwnedItem{id=Id(),item=1,slot=2};Check(!extra.server.ReceiveShopTrade(e).Accepted&&extra.View.record.inventory.Length==1,"sale cannot create a bonus item");
        var overflow=new Fixture(gold:ulong.MaxValue);overflow.Info(false,overflow.View.record.inventory[0].id,1);overflow.QueueTrade();Check(!overflow.server.ReceiveShopTrade(overflow.Notice()).Accepted&&overflow.View.record.gold==ulong.MaxValue&&overflow.View.record.inventory.Length==1,"sale overflow rejected atomically");
        var maximum=new Fixture(gold:ulong.MaxValue);maximum.Info();maximum.Approve();Check(maximum.View.record.gold==ulong.MaxValue-8000,"large unsigned purchase exact");
        var zero=new Fixture();zero.Info(false,zero.View.record.inventory[0].id,0);zero.Approve();Check(zero.View.record.gold==20000&&zero.View.record.inventory.Length==0&&zero.View.record.shop.revision==1&&zero.View.record.currency==null,"explicit zero sale still persists ownership version");
        var currency=new Fixture();currency.Info();currency.QueueTrade();var old=currency.Notice();currency.Funds(19000);Check(currency.View.facility.shopQuote==null&&currency.View.shopTradeRequest==null&&!currency.server.ReceiveShopTrade(old).Accepted,"new wallet invalidates price/confirmation");currency.Info();currency.Approve();Check(currency.View.record.gold==11000,"fresh price charges latest wallet");
        var closed=new Fixture();closed.Info();closed.QueueTrade();old=closed.Notice();closed.server.CloseWorldFacility();closed.server.OpenWorldFacility(closed.staff);Check(!closed.server.ReceiveShopTrade(old).Accepted&&closed.View.record.inventory.Length==1,"old facility token cannot buy");
        var interrupted=new Fixture();interrupted.Info();interrupted.QueueTrade();old=interrupted.Notice();interrupted.Reload();Check(!interrupted.server.ReceiveShopTrade(old).Accepted&&interrupted.View.record.gold==20000&&interrupted.View.record.inventory.Length==1,"unapproved interrupted trade cannot persist or resume old session");
        var distant=new Fixture();distant.Info();distant.QueueTrade();old=distant.Notice();var npc=distant.definition.Map(2000).npcs.First();npc.x=18;npc.y=12;npc.counter=Array.Empty<WorldPoint>();Check(distant.server.ReceiveShopTrade(old).Failure==WorldFailure.TooFar,"NPC distance rechecked on approval");
        foreach(string mode in new[]{"null","throw","wrong-request","denied"})
        {
            var provider=new Fixture();var before=State(provider.View.record);
            var reply=provider.server.ExecuteShopQuote(true,1,null,provider.View.facility.token,Id(),(r,v)=>
            {if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var quote=provider.Quote(r);if(mode=="wrong-request")quote.request.id=Id();else quote.approved=false;return quote;});
            Check(!reply.Accepted&&provider.View.shopQuoteRequest==null&&State(provider.View.record)==before,"quote provider failure unlocks: "+mode);
            provider.Info();reply=provider.server.ExecuteShopTrade(provider.View.facility.shopQuote.id,provider.View.facility.token,Id(),(r,v)=>
            {if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var notice=provider.Notice(r);if(mode=="wrong-request")notice.request.id=Id();else notice.approved=false;return notice;});
            Check(!reply.Accepted&&provider.View.shopTradeRequest==null&&State(provider.View.record)==before,"trade provider failure unlocks: "+mode);
        }
        var concurrent=new Fixture();concurrent.Info(false,concurrent.View.record.inventory[0].id,0);concurrent.QueueTrade();var earlier=concurrent.Notice();
        var other=new EntryAccountService(concurrent.store);new EntryClient(other).OpenLocalProfile();Check(other.BeginWorld(concurrent.definition).Accepted&&other.OpenWorldFacility(concurrent.staff).Accepted,"second service same stock state");
        var otherView=other.WorldSnapshot();var instance=otherView.record.inventory[0].id;Check(other.ExecuteShopQuote(false,1,instance,otherView.facility.token,Id(),(r,v)=>new ShopQuote{id=Id(),request=r.Copy(),approved=true,price=0}).Accepted,"second service quotes zero sale");
        Check(other.ExecuteShopTrade(other.WorldSnapshot().facility.shopQuote.id,other.WorldSnapshot().facility.token,Id(),(r,v)=>new ShopNotice{id=Id(),request=r.Copy(),approved=true,price=0,gold=20000}).Accepted,"second service zero sale commits");
        Check(concurrent.server.ReceiveShopTrade(earlier).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.inventory.Length==0,"free sale cannot overwrite more recent ownership save");
        var unrelated=new Fixture();unrelated.Info();unrelated.QueueTrade();old=unrelated.Notice();unrelated.store.data.accounts[0].character.world.inventory[0].item=2;Check(unrelated.server.ReceiveShopTrade(old).Failure==WorldFailure.StaleRequest&&unrelated.store.data.accounts[0].character.world.inventory[0].item==2,"newer unrelated inventory preserved");
        var legacy=new Fixture();Check(legacy.server.BuyWorldItem(1,legacy.View.facility.token,Id()).Accepted&&legacy.View.record.shop.revision==1,"legacy original-price purchase records ownership revision");legacy.definition.Item(1).sellKnown=true;legacy.definition.Item(1).sellPrice=1;
        Check(legacy.server.SellWorldItem(legacy.View.record.inventory[0].id,legacy.View.facility.token,Id()).Accepted&&legacy.View.record.shop.revision==2,"explicit legacy sale records same ownership revision");
        var path=Path.Combine(Path.GetTempPath(),"ds-shop-"+Id()+".json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var doc)&&doc.accounts[0].character.world.shop.revision==2,"shop receipt actual DataContract serialization");
            var service=new EntryAccountService(disk);new EntryClient(service).OpenLocalProfile();Check(service.BeginWorld(f.definition).Accepted&&service.WorldSnapshot().record.inventory.Length==1&&service.WorldSnapshot().record.gold==16000&&service.WorldSnapshot().facility==null,"file-backed reconnect restores completed sale only");
        }
        finally{if(File.Exists(path))File.Delete(path);}
        var valid=f.View.record.Copy();valid.shop.owned="bad";Check(!WorldRecord.Valid(valid),"invalid saved shop receipt rejected");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual original-price shop quote/confirm/approved purchase-sale, explicit unknown sale quote, owned-item/currency and isolated persistence contracts; no recovered native opcode/sale formula claim."}));
    }
}
