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
        public Fixture(bool known=false,WorldRecord record=null,int job=100)
        {
            Def=Definition();if(known){Def.rules.bankFeeKnown=true;Def.rules.storageRuleKnown=true;Def.rules.storageSlots=48;Def.rules.reviveRuleKnown=true;Def.rules.reviveHp=200;Def.rules.reviveMp=45;Def.rules.reviveFee=100;Def.rules.hireRuleKnown=true;Def.rules.dismissalRuleKnown=true;}
            Server=new EntryAccountService(Store);Client=new EntryClient(Server);Require(Client.OpenLocalProfile()&&Client.Create(job,"월드검사"),"fixture identity");
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
        RoutesAndGuards();Transactions();RecoveryAndPreservation();Retargeting();Equipment();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,routes,actualUnityPlay=false,policy="Explicit synthetic values only in tests; production unknown policies remain disabled"}));
    }
    private static void Retargeting()
    {
        var f=new Fixture(true);f.Def.Map(1000).map.attributes=Enumerable.Repeat(0,43*43).ToArray();
        Check(f.Server.MoveWorld(25,21).Accepted,"first movement accepted");f.Server.AdvanceWorld(80);
        var moving=f.View;
        Check(f.Server.MoveWorld(21,24).Accepted&&f.View.moving,"a second map click retargets instead of failing Moving");
        Check(f.View.renderX==moving.renderX&&f.View.renderY==moving.renderY&&f.View.record.x==moving.record.x,
            "retarget preserves the current partial tile step without teleporting");
        Check(f.Server.MoveWorld(-1,24).Failure==WorldFailure.InvalidInput&&f.View.renderX==moving.renderX&&f.View.moving,
            "unreachable retarget preserves the active route and visual position");
        f.Server.AdvanceWorld(10000);Check(!f.View.moving&&f.View.record.x==21&&f.View.record.y==24,"retarget arrives at the newest valid destination");
        var restored=f.Store.Data.accounts[0].character.world;Check(restored.x==21&&restored.y==24,"new movement endpoint persists");
        var fail=new Fixture(true);fail.Def.Map(1000).map.attributes=Enumerable.Repeat(0,43*43).ToArray();string state=State(fail.View.record);
        fail.Server.MoveWorld(25,21);fail.Server.AdvanceWorld(160);
        Require(fail.Server.MoveWorld(21,24).Accepted,"failed-save retarget setup");fail.Store.Fail=true;fail.Server.AdvanceWorld(10000);
        Check(fail.View.failure==WorldFailure.StorageUnavailable&&State(fail.View.record)==state,"retarget save failure rolls back to the committed start, not an intermediate tile");
        var stop=new Fixture();stop.Def.Map(1000).map.attributes=Enumerable.Repeat(0,43*43).ToArray();
        stop.Server.MoveWorld(25,21);stop.Server.AdvanceWorld(80);Check(stop.Server.MoveWorld(22,21).Accepted,"clicking the in-flight next tile requests a grid stop");
        stop.Server.AdvanceWorld(10000);Check(!stop.View.moving&&stop.View.record.x==22&&stop.View.record.y==21,"grid stop completes the preserved step only");
    }
    private static void Equipment()
    {
        var ids=Enumerable.Range(0,6).Select(_=>Id()).ToArray();
        var record=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,goldKnown=true,gold=1234,
            inventory=new[]{1,762,777,100,2,101}.Select((item,slot)=>new WorldOwnedItem{id=ids[slot],item=item,slot=slot}).ToArray()};
        var f=new Fixture(false,record);string before=State(f.View.record);
        Check(f.Server.EquipWorldItem(ids[0],0,Id()).Failure==WorldFailure.InvalidInput&&State(f.View.record)==before,"weapon to head rejected without ownership/stat changes");
        Check(f.Server.EquipWorldItem(ids[2],1,Id()).Failure==WorldFailure.InvalidInput&&State(f.View.record)==before,"native class flags reject mage-only armor for fighter");
        Check(f.Server.EquipWorldItem(ids[5],3,Id()).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==before,"positive required level is held when the original server level is unknown");
        f.Store.Fail=true;Check(f.Server.EquipWorldItem(ids[0],2,Id()).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"equipment save failure preserves inventory and equipment");f.Store.Fail=false;
        string request=Id();Check(f.Server.EquipWorldItem(ids[0],2,request).Accepted&&f.View.record.Equipment.Single().id==ids[0]&&f.View.record.Equipment[0].slot==2,"owned dagger equips to native left-hand slot");
        string equipped=State(f.View.record);Check(f.Server.EquipWorldItem(ids[0],2,request).Failure==WorldFailure.StaleRequest&&State(f.View.record)==equipped,"duplicate equipment request cannot move or duplicate an item");
        Check(f.Server.EquipWorldItem(ids[4],3,Id()).Failure==WorldFailure.InvalidInput,"native hand rule rejects two one-hand weapons");
        Check(f.Server.EquipWorldItem(ids[3],3,Id()).Accepted,"native shield category fits the opposite hand");
        Check(f.Server.EquipWorldItem(ids[4],2,Id()).Accepted&&f.View.record.inventory.Any(i=>i.id==ids[0])&&f.View.record.Equipment.Any(i=>i.id==ids[4]),"equipment replacement returns the previous owned item atomically");
        Check(f.Server.EquipWorldItem(ids[1],1,Id()).Accepted,"native leather armor fits the body");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.gold==1234&&!f.View.record.level.HasValue,"equipment does not heal, debit money or invent a server level");
        string preDrop=State(f.View.record);
        Check(f.Server.EquipWorldItem(ids[4],-1,Id(),5).Failure==WorldFailure.InvalidInput&&State(f.View.record)==preDrop,"occupied inventory drop is rejected without losing equipment");
        Check(f.Server.EquipWorldItem(ids[4],-1,Id(),12).Accepted&&f.View.record.inventory.Any(i=>i.id==ids[4]&&i.slot==12),"unequip returns the same identity to the selected empty inventory cell");
        var ownership=f.View.record.inventory.Concat(f.View.record.Equipment).Select(i=>i.id).ToArray();Check(ownership.Length==6&&ownership.Distinct().Count()==6,"equip/replace/unequip preserve every item exactly once");
        var snapshot=f.View; snapshot.record.equipment[0].item=999;Check(f.View.record.Equipment.All(i=>i.item!=999),"equipment snapshots cannot mutate authoritative items");
        string saved=State(f.View.record);f.Client.Logout();Require(f.Client.OpenLocalProfile(),"equipment reconnect identity");Require(f.Server.BeginWorld(f.Def).Accepted,"equipment reconnect world");
        Check(State(f.View.record)==saved,"equipped identities, positions and unknown values restore on reconnect");
        string path=Path.Combine(Path.GetTempPath(),"world-equipment-"+Id()+".json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.Store.Data)&&disk.TryRead(out var read)&&read.accounts[0].character.world.Equipment.Length==2,"real DataContract disk roundtrip preserves optional equipment");
        }
        finally{if(File.Exists(path))File.Delete(path);}
        Check(WorldRecord.Valid(new WorldRecord()),"legacy saves without equipment or level remain valid");
        f.Server.MoveWorld(22,21);var moving=State(f.View.record);
        Check(f.Server.EquipWorldItem(ids[4],2,Id()).Failure==WorldFailure.Busy&&State(f.View.record)==moving,"movement blocks gear edits without interrupting the route");
        var worn=new WorldOwnedItem{id=Id(),item=1,slot=2};var staff=new WorldOwnedItem{id=Id(),item=7,slot=0};
        var mage=new Fixture(false,new WorldRecord{level=6,inventory=new[]{staff},equipment=new[]{worn}},103);
        Check(mage.Server.EquipWorldItem(staff.id,3,Id()).Accepted&&mage.View.record.Equipment.Single().id==staff.id
            &&mage.View.record.inventory.Single().id==worn.id,"known-level native mage staff equips to either hand and clears the opposite hand atomically");
        Check(mage.Server.EquipWorldItem(worn.id,2,Id()).Accepted&&mage.View.record.inventory.Single().id==staff.id,
            "one-hand weapon replaces an opposite two-hand weapon without losing either identity");
        var full=new Fixture(false,new WorldRecord{inventory=Enumerable.Range(0,28).Select(slot=>new WorldOwnedItem{id=Id(),item=1,slot=slot}).ToArray(),
            equipment=new[]{new WorldOwnedItem{id=Id(),item=762,slot=1}}});
        var fullState=State(full.View.record);Check(full.Server.EquipWorldItem(full.View.record.Equipment[0].id,-1,Id()).Failure==WorldFailure.InventoryFull
            &&State(full.View.record)==fullState,"full inventory blocks unequip without dropping or duplicating equipment");
        var herb=new WorldOwnedItem{id=Id(),item=324,slot=0};var usage=new Fixture(false,new WorldRecord{inventory=new[]{herb}});
        string unused=State(usage.View.record);
        Check(usage.Server.UseWorldItem(herb.id,Id()).Failure==WorldFailure.RulesUnavailable&&State(usage.View.record)==unused,
            "native head-drop consumable request is held without consuming items or inventing server effects");
    }
}
