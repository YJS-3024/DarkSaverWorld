using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class WorldChecks
{
    private static int assertions, routes;
    private static void Check(bool value,string message){if(!value)throw new Exception(message);assertions++;}
    private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static WorldDefinition Definition()=>JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText("Assets/_Original/Data/World/KaosiaWorld.json"),new JsonSerializerOptions{IncludeFields=true});
    private sealed class Memory : IEntryAccountStore
    {
        public LocalAccountDocument Data=new LocalAccountDocument();public bool Fail;
        public bool TryRead(out LocalAccountDocument value){value=Data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(Fail)return false;Data=value.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory Store=new Memory();public EntryAccountService Server;public EntryClient Client;public WorldDefinition Def;
        public Fixture(bool known=false,WorldRecord record=null)
        {
            Def=Definition();if(known){Def.rules.bankFeeKnown=true;Def.rules.storageRuleKnown=true;Def.rules.storageSlots=48;Def.rules.reviveRuleKnown=true;Def.rules.reviveHp=200;Def.rules.reviveMp=45;Def.rules.reviveFee=100;Def.rules.hireRuleKnown=true;Def.rules.dismissalRuleKnown=true;}
            Server=new EntryAccountService(Store);Client=new EntryClient(Server);Require(Client.OpenLocalProfile()&&Client.Create(100,"월드검사"),"fixture identity");
            // Synthetic world fixture only. This is not a played/completed tutorial.
            Store.Data.accounts[0].character.tutorialCompleted=true;
            if(record!=null)Store.Data.accounts[0].character.world=record.Copy();
            else if(known)Store.Data.accounts[0].character.world=new WorldRecord{goldKnown=true,gold=100000,bankKnown=true,bank=1000};
            Require(Client.OpenLocalProfile(),"fixture reload");Require(Server.BeginWorld(Def).Accepted,"world begin");
        }
        public WorldView View=>Server.WorldSnapshot();
        public void Walk(int x,int y)
        {
            if(View.record.x==x&&View.record.y==y)return;
            Require(Server.MoveWorld(x,y).Accepted,"move request "+View.record.map+" to"+x+","+y);
            for(int i=0;i<10000&&View.moving;i++)Server.AdvanceWorld(80);
            Require(!View.moving&&View.failure==WorldFailure.None&&View.record.x==x&&View.record.y==y,"arrival "+View.record.map);
        }
        public void Enter(int target)
        {
            var gate=Def.Map(1000).portals.First(g=>g.targetMap==target);Walk(gate.cells[0].x,gate.cells[0].y);
            Require(Server.EnterWorldPortal(gate.id,target==101).Accepted,"enter"+target);
        }
        public void Leave()
        {var m=Def.Map(View.record.map);if(m.training){Require(Server.EscapeTraining().Accepted,"flee");return;}var gate=m.portals.Single();Walk(gate.cells[0].x,gate.cells[0].y);Require(Server.EnterWorldPortal(gate.id,false).Accepted,"exit");}
        public string Open(int map,int staff=0)
        {Enter(map);var n=Def.Map(map).npcs[staff];Walk(n.counter[1].x,n.counter[1].y);Require(Server.OpenWorldFacility(n.id).Accepted,"open "+n.id);return View.facility.token;}
    }
    private static string State(WorldRecord record)=>JsonSerializer.Serialize(record,new JsonSerializerOptions{IncludeFields=true});
    private static void RoutesAndGuards()
    {
        var f=new Fixture();Check(f.View.record.map==1000&&f.View.record.x==21&&f.View.record.y==21,"tutorial boundary spawns at requested Kaosia21,21");
        Check(!f.View.record.goldKnown&&!f.View.record.bankKnown&&!f.Def.rules.bankFeeKnown&&!f.Def.rules.storageRuleKnown&&!f.Def.rules.reviveRuleKnown,"no invented starting funds/free services");
        Check(f.Def.Map(1000).map.width==43&&f.Def.Map(1000).decorations.Length==225,"user-confirmed43x43 native town");
        Check(f.Server.EnterWorldPortal("enter-2000",false).Failure==WorldFailure.InvalidInput,"entry requires actual door arrival");
        var ids=f.Def.maps.Where(m=>m.id!=1000).Select(m=>m.id).ToArray();
        foreach(int id in ids)
        {
            f.Enter(id);routes++;var map=f.Def.Map(id);
            Check(f.View.record.map==id&&f.View.record.x==10&&f.View.record.y==(id==101?7:map.map.height-2),"entry at interior door/training center "+id);
            if(id==101)
            {
                Check(f.Store.Data.accounts[0].character.world.map==1000,"training stores town return instead of battle");f.Walk(10,8);var before=State(f.View.record);f.Leave();Check(f.View.record.map==1000&&f.View.record.x==8&&f.View.record.y==21,"flee returns to training entrance");continue;
            }
            foreach(var npc in map.npcs)
            {
                Check(f.Server.OpenWorldFacility(npc.id).Failure==WorldFailure.TooFar,"far NPC rejected "+npc.id);
                f.Walk(npc.counter[1].x,npc.counter[1].y);Check(f.Server.OpenWorldFacility(npc.id).Accepted,"counter interaction "+npc.id);
                Check(f.Server.MoveWorld(10,10).Failure==WorldFailure.Busy,"facility owns input until close");
                if((FacilityKind)npc.kind==FacilityKind.Shop)
                {
                    int expected=id==2000?22:id==2100?20:id==2300?23:id==2800?6:0;
                    Check(f.View.facility.offers.Length==expected,"guide-confirmed stock "+id);
                    if(expected>0){string before=State(f.View.record);Check(f.Server.BuyWorldItem(f.View.facility.offers[0],f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==before,"unknown wallet holds trade without mutation");}
                }
                if((FacilityKind)npc.kind==FacilityKind.Bank)Check(f.Server.BankWorld(true,1,f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"unknown bank costs remain blocked");
                if((FacilityKind)npc.kind==FacilityKind.Warehouse)Check(f.Server.MoveWorldStorage(true,Id(),f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"unknown storage policy remains blocked");
                if((FacilityKind)npc.kind==FacilityKind.Temple)Check(f.Server.ReviveWorld(f.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable,"unknown revival remains blocked");
                Check(f.Server.CloseWorldFacility().Accepted&&f.View.facility==null,"close releases input");
                f.Walk(10,map.map.height-2);
            }
            f.Leave();Check(f.View.record.map==1000,"actual door returns to town "+id);
        }
        f.Enter(2000);f.Server.MoveWorld(4,6);Check(f.View.moving,"NPC interrupt fixture is moving");
        Check(f.Server.OpenWorldFacility("2000-807").Accepted,"moving click queues grid stop");
        for(int i=0;i<10&&f.View.moving;i++)f.Server.AdvanceWorld(80);
        Check(!f.View.moving&&f.View.renderX==f.View.record.x&&f.View.renderY==f.View.record.y,"stop at complete grid tile");
        Check(f.View.facility==null&&f.View.failure==WorldFailure.TooFar,"stop does not bypass counter distance");
    }
    private static void Transactions()
    {
        var f=new Fixture(true);string token=f.Open(2000);string request=Id();ulong gold=f.View.record.gold;
        Check(f.Server.BuyWorldItem(1,token,request).Accepted&&f.View.record.gold==gold-8000&&f.View.record.inventory.Length==1,"known synthetic funding buys native8000 dagger");
        var item=f.View.record.inventory[0];string state=State(f.View.record);
        Check(f.Server.BuyWorldItem(1,token,request).Failure==WorldFailure.StaleRequest&&State(f.View.record)==state,"duplicate success does not debit twice");
        Check(f.Server.BuyWorldItem(0,token,Id()).Failure==WorldFailure.InvalidInput,"client cannot buy arbitrary catalog item");
        f.Store.Fail=true;Check(f.Server.BuyWorldItem(1,token,Id()).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state,"failed buy write preserves wallet/inventory");f.Store.Fail=false;
        Check(f.Server.SellWorldItem(item.id,token,Id()).Failure==WorldFailure.RulesUnavailable,"resale is not assumed half price");
        var dagger=f.Def.Item(1);dagger.sellKnown=true;dagger.sellPrice=4000;
        Check(f.Server.SellWorldItem(item.id,token,Id()).Accepted&&f.View.record.inventory.Length==0&&f.View.record.gold==gold-4000,"explicit fixture resale commits atomically");
        Check(f.Server.SellWorldItem(item.id,token,Id()).Failure==WorldFailure.NotOwned,"sold item cannot be sold twice");
        f.Server.CloseWorldFacility();f.Leave();string oldToken=token;token=f.Open(2400);
        Check(f.Server.BankWorld(true,0,token,Id()).Failure==WorldFailure.InvalidInput,"bank rejects zero");
        Check(f.Server.BankWorld(true,ulong.MaxValue,token,Id()).Failure==WorldFailure.InsufficientFunds,"bank never wraps excessive amount");
        ulong wallet=f.View.record.gold,bank=f.View.record.bank;
        Check(f.Server.BankWorld(true,100,token,Id()).Accepted&&f.View.record.gold==wallet-100&&f.View.record.bank==bank+100,"known deposit ledger");
        Check(f.Server.BankWorld(false,100,token,Id()).Accepted&&f.View.record.gold==wallet&&f.View.record.bank==bank,"known withdrawal ledger");
        state=State(f.View.record);f.Store.Fail=true;Check(f.Server.BankWorld(true,10,token,Id()).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state,"failed bank write preserves both balances");f.Store.Fail=false;
        Check(f.Server.BuyWorldItem(1,oldToken,Id()).Failure==WorldFailure.StaleRequest,"old shop quote rejected across facility switch");
        f.Server.CloseWorldFacility();f.Leave();
        token=f.Open(2000);f.Server.BuyWorldItem(1,token,Id());f.Server.CloseWorldFacility();f.Leave();
        token=f.Open(2510);item=f.View.record.inventory.Single();state=State(f.View.record);
        Check(f.Server.MoveWorldStorage(true,item.id,token,Id()).Accepted&&State(f.View.record)==state&&f.View.facility.storage.Length==1,"storage stages without mutating committed ownership");
        f.Server.CloseWorldFacility();Check(State(f.View.record)==state,"cancel discards storage draft");
        f.Server.OpenWorldFacility("2510-807");token=f.View.facility.token;f.Server.MoveWorldStorage(true,item.id,token,Id());
        f.Store.Fail=true;Check(f.Server.ConfirmWorldStorage(token,Id()).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state,"failed storage commit preserves original ledger");f.Store.Fail=false;
        Check(f.Server.ConfirmWorldStorage(token,Id()).Accepted&&f.View.record.storage.Single().id==item.id&&f.View.record.inventory.Length==0,"storage confirm preserves unique instance");
        f.Server.OpenWorldFacility("2510-807");token=f.View.facility.token;
        Check(f.Server.MoveWorldStorage(false,item.id,token,Id()).Accepted&&f.Server.ConfirmWorldStorage(token,Id()).Accepted&&f.View.record.inventory.Single().id==item.id,"withdraw retains item identity");
        f.Leave();token=f.Open(2900);wallet=f.View.record.gold;
        Check(f.Server.HireWorldMercenary(200,token,Id()).Accepted&&f.View.record.hiredCount==1,"native class hire list accepts eligible soldier");
        Check(f.Server.HireWorldMercenary(201,token,Id()).Failure==WorldFailure.InvalidInput,"no mixed hired troop type");
        Check(f.Server.HireWorldMercenary(200,token,Id()).Accepted&&f.Server.HireWorldMercenary(200,token,Id()).Failure==WorldFailure.InvalidInput,"native limit2 not summed across skills");
        Check(f.View.record.gold==wallet-400,"native200 price per hired soldier");
        Check(f.Server.DismissWorldMercenary(token,Id()).Accepted&&f.Server.DismissWorldMercenary(token,Id()).Accepted&&f.View.record.hiredJob==-1,"last dismissal clears troop type");
        var persisted=f.Store.Data.accounts[0].character.world.Copy();f.Client.Logout();f.Client.OpenLocalProfile();
        Check(f.Server.BeginWorld(f.Def).Accepted&&State(f.View.record)==State(persisted),"world inventory/bank/location/hiring restore after reconnect");
    }
    private static void RecoveryAndPreservation()
    {
        var f=new Fixture(true,new WorldRecord{goldKnown=true,gold=500,bankKnown=true,healthKnown=true,hp=0,maxHp=200,mp=0,maxMp=45,ghost=true});
        string token=f.Open(2600);f.Store.Fail=true;var state=State(f.View.record);
        Check(f.Server.ReviveWorld(token,Id()).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state,"failed revival does not remove ghost/debit");f.Store.Fail=false;
        Check(f.Server.ReviveWorld(token,Id()).Accepted&&!f.View.record.ghost&&f.View.record.hp==200&&f.View.record.mp==45&&f.View.record.gold==400,"only explicit synthetic revival policy changes health");
        Check(f.Server.ReviveWorld(token,Id()).Failure==WorldFailure.InvalidStage&&f.View.record.gold==400,"repeat revival not charged");
        var training=new Fixture();training.Enter(101);training.Walk(10,8);training.Client.Logout();training.Client.OpenLocalProfile();
        Check(training.Server.BeginWorld(training.Def).Accepted&&training.View.record.map==1000&&training.View.record.x==8&&training.View.record.y==21,"interrupted training reconnects at town entrance");
        var movement=new Fixture();state=State(movement.View.record);movement.Server.MoveWorld(22,21);movement.Store.Fail=true;
        for(int i=0;i<10&&movement.View.moving;i++)movement.Server.AdvanceWorld(80);
        Check(!movement.View.moving&&movement.View.failure==WorldFailure.StorageUnavailable&&State(movement.View.record)==state,"failed movement persistence rolls back to last grid state");
        var unauth=new EntryAccountService(new Memory());Check(unauth.BeginWorld(Definition()).Failure==WorldFailure.Unauthenticated,"world cannot bypass identity");
        var store=new Memory();var server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"미완료");
        Check(server.BeginWorld(Definition()).Failure==WorldFailure.InvalidStage,"world cannot bypass incomplete tutorial");
        var copy=f.View;copy.record.gold=0;Check(f.View.record.gold==400,"world replies cannot mutate authoritative wallet");
        var large=new Fixture(true,new WorldRecord{goldKnown=true,gold=ulong.MaxValue-1,bankKnown=true,bank=10});token=large.Open(2400);large.Def.rules.bankFee=1;
        Check(large.Server.BankWorld(false,2,token,Id()).Accepted&&large.View.record.gold==ulong.MaxValue&&large.View.record.bank==8,"unsigned balance arithmetic subtracts quoted fee before addition");
        var crossing=new Fixture();crossing.Enter(101);
        crossing.Def.Map(101).portals=new[]{new WorldPortal{id="fixture-crossed-door",targetMap=1000,spawnX=8,spawnY=21,cells=new[]{new WorldPoint{x=10,y=8}}}};
        Require(crossing.Server.MoveWorld(10,9).Accepted,"route beyond fixture door");crossing.Server.AdvanceWorld(2000);
        Check(!crossing.View.moving&&crossing.View.record.y==9&&crossing.View.pendingPortal==null,"unselected door is avoided in transit instead of entering a different facility");
        Require(crossing.Server.MoveWorld(10,8).Accepted,"explicit fixture door target");crossing.Server.AdvanceWorld(2000);
        Check(!crossing.View.moving&&crossing.View.record.y==8&&crossing.View.pendingPortal=="fixture-crossed-door","explicit door target stops on the entry grid cell");
        string path=Path.Combine(Path.GetTempPath(),"world-schema-"+Id()+".json");
        try{var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.Store.Data)&&disk.TryRead(out var read)&&read.accounts[0].character.world.hp==200,"real file roundtrip preserves optional world schema");}
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private static void Main()
    {
        RoutesAndGuards();Transactions();RecoveryAndPreservation();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,routes,actualUnityPlay=false,policy="Explicit synthetic values only in tests; production unknown policies remain disabled"}));
    }
}
