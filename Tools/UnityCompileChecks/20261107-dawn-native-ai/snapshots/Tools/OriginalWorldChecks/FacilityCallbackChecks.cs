using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

static class FacilityCallbackChecks
{
    static int checks;static string root;static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    static string Id()=>Guid.NewGuid().ToString("N");
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static string State(object value)=>JsonSerializer.Serialize(value,Options);
    sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){data=value.Copy();return true;}
    }
    sealed class Fixture
    {
        public readonly Memory store=new Memory();public readonly EntryAccountService server;public readonly EntryClient client;public readonly WorldDefinition definition;
        public readonly string operation,staff;public readonly int item,hire;public WorldView View=>server.WorldSnapshot();public string Token=>View.facility.token;
        public Fixture(string op)
        {
            operation=op;definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Options);
            FacilityKind kind=op.StartsWith("shop")?FacilityKind.Shop:op.StartsWith("revival")?FacilityKind.Temple:op=="mercenary"?FacilityKind.Mercenary:FacilityKind.Warehouse;
            var map=definition.maps.First(m=>m.npcs.Any(n=>n.kind==(int)kind));var npc=map.npcs.First(n=>n.kind==(int)kind);staff=npc.id;
            item=kind==FacilityKind.Shop?definition.shops.First(s=>s.map==map.id).items.First(id=>definition.Item(id).price>=0):200;
            hire=definition.hires.First(h=>h.commander==100&&h.limit>0).job;
            server=new EntryAccountService(store);client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"콜백검사");var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{map=map.id,x=npc.counter[0].x,y=npc.counter[0].y,healthKnown=true,hp=kind==FacilityKind.Temple?0:17,maxHp=200,mp=3,maxMp=45,ghost=kind==FacilityKind.Temple,
                goldKnown=true,gold=1000000,bankKnown=true,bank=2000,inventory=new[]{new WorldOwnedItem{id=Id(),item=200,slot=0}},storage=Array.Empty<WorldOwnedItem>()};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated interior fixture");Open();Prepare();
        }
        void Open(){Check(server.OpenWorldFacility(staff).Accepted,"explicit facility interaction");}
        RevivalQuote Quote(RevivalQuoteRequest r)=>new RevivalQuote{id=Id(),request=r.id,approved=true,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,revision=r.revision,fee=0};
        public void Prepare()
        {
            if(operation=="shop-trade")Check(server.ExecuteShopQuote(true,item,null,Token,Id(),(r,v)=>new ShopQuote{id=Id(),request=r.Copy(),approved=true,price=(ulong)definition.Item(item).price}).Accepted,"explicit shop quote");
            if(operation=="revival-confirm")Check(server.ExecuteRevivalQuote(Token,Id(),(r,v)=>Quote(r)).Accepted,"explicit revival quote");
            if(operation=="warehouse-move"||operation=="warehouse-confirm")
                Check(server.ExecuteWarehouseOperation(WarehouseOperation.State,false,null,Token,Id(),(r,v)=>new WarehouseNotice{id=Id(),request=r.id,approved=true,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,operation=r.operation,
                    expectedRevision=r.revision,draftRevision=r.draftRevision,currencyRevision=r.currencyRevision,capacity=30,fee=0,feeSource=BankFeeSource.Wallet,inventory=r.inventory,storage=r.storage}).Accepted,"explicit warehouse terms, no production defaults");
        }
        WarehouseOperation WarehouseOp=>operation=="warehouse-state"?WarehouseOperation.State:operation=="warehouse-move"?WarehouseOperation.Move:WarehouseOperation.Confirm;
        public WorldReply Queue(string id)
        {
            switch(operation)
            {
                case "shop-quote":return server.RequestShopQuote(true,item,null,Token,id);
                case "shop-trade":return server.RequestShopTrade(View.facility.shopQuote.id,Token,id);
                case "revival-quote":return server.RequestRevivalQuote(Token,id);
                case "revival-confirm":return server.RequestTempleRevival(Token,id);
                case "mercenary":return server.RequestMercenaryTransfer(true,hire,Token,id);
                default:return server.RequestWarehouseOperation(WarehouseOp,true,View.facility.inventory[0].id,Token,id);
            }
        }
        public WorldReply Execute(string id,Action callback)
        {
            switch(operation)
            {
                case "shop-quote":return server.ExecuteShopQuote(true,item,null,Token,id,(r,v)=>{callback();return null;});
                case "shop-trade":return server.ExecuteShopTrade(View.facility.shopQuote.id,Token,id,(r,v)=>{callback();return null;});
                case "revival-quote":return server.ExecuteRevivalQuote(Token,id,(r,v)=>{callback();return null;});
                case "revival-confirm":return server.ExecuteTempleRevival(Token,id,(r,v)=>{callback();return null;});
                case "mercenary":return server.ExecuteMercenaryTransfer(true,hire,Token,id,(r,v)=>{callback();return null;});
                default:return server.ExecuteWarehouseOperation(WarehouseOp,true,View.facility.inventory[0].id,Token,id,(r,v)=>{callback();return null;});
            }
        }
        public string Pending=>View?.shopQuoteRequest?.id??View?.shopTradeRequest?.id??View?.revivalQuoteRequest?.id??View?.revivalRequest?.id??View?.mercenaryRequest?.id??View?.warehouseRequest?.id;
        public void Reconnect(){client.Logout();client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"same service begins fresh world session");Open();Prepare();}
        public void Reopen(){Check(server.CloseWorldFacility().Accepted,"close old facility");Open();Prepare();}
    }
    static void Main(string[] args)
    {
        root=args[0];int scenarios=0;
        foreach(string op in new[]{"shop-quote","shop-trade","revival-quote","revival-confirm","mercenary","warehouse-state","warehouse-move","warehouse-confirm"})
        foreach(string mode in new[]{"logout","logout-throw","reconnect","reconnect-throw","reopen","reopen-throw","null","throw"})
        {
            var f=new Fixture(op);string id=Id(),next=null;string original=State(f.store.data);string session=f.View.currencySession;int calls=0;
            var result=f.Execute(id,()=>
            {
                calls++;Check(f.Pending==id,"callback receives queued request: "+op);
                if(mode.StartsWith("logout"))f.client.Logout();
                if(mode.StartsWith("reconnect")){f.Reconnect();next=id;Check(f.Queue(next).Accepted,"new session may independently reuse same local request ID");}
                if(mode.StartsWith("reopen")){f.Reopen();next=Id();Check(f.Queue(next).Accepted,"same session new facility request");}
                if(mode.EndsWith("throw"))throw new InvalidOperationException("isolated approval provider failure");
            });
            Check(calls==1&&!result.Accepted,"one rejected callback: "+op+" "+mode);
            Check(State(f.store.data)==original,"callback failure never changes saved funds/items/health: "+op+" "+mode);
            if(mode.StartsWith("reconnect"))Check(f.View.currencySession!=session&&f.Pending==next,"old callback cannot clear same-ID new session request: "+op);
            else if(mode.StartsWith("reopen"))Check(f.Pending==next,"old callback cannot clear new facility request: "+op);
            else if(mode.StartsWith("logout"))Check(f.View==null,"no null dereference after logout: "+op);
            else Check(f.Pending==null,"failed ordinary provider releases only its request: "+op);
            scenarios++;
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,scenarios,unityPlay=false,scope="Current production services, eight facility operations with logout/reconnect/window replacement/null/exception inside approval callbacks. Same-ID new-session requests must survive old callbacks. Fresh in-memory interior fixtures; no player saves, source fees/limits, native packet or physical doorway claims."}));
    }
}
