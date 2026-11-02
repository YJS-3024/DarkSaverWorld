using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
using DarkSaver.Original.Entry;
internal static class CommanderChecks
{
    static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string Id()=>Guid.NewGuid().ToString("N");static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
    static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",path)),Json);
    static string State(object value)=>JsonSerializer.Serialize(value,Json);
    sealed class Store:IEntryAccountStore
    {public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}}
    sealed class Fixture
    {
        public Store store=new Store();public EntryAccountService service;public EntryClient client;public WorldDefinition definition;
        public WorldView View=>service.WorldSnapshot();
        public Fixture()
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
            service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"캐릭터검사");
            store.data.accounts[0].character.tutorialCompleted=true;store.data.accounts[0].character.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,goldKnown=true,gold=123,bankKnown=true,bank=456,
                inventory=new[]{new WorldOwnedItem{id="00000000000000000000000000000001",item=1,slot=0}},storage=new[]{new WorldOwnedItem{id="00000000000000000000000000000003",item=2,slot=0}}};
            client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"world starts with isolated wounded state");
        }
        public void Reload(){client.Logout();service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted,"reload");}
        public CommanderNotice Notice()
        {
            var r=View.commanderRequest?.Copy();return new CommanderNotice{id=Id(),approved=true,request=r,state=new CommanderRoster{alternateGate=true,rows=new[]{
                new OwnedCommander{id="first",job=100,nativeRecord=0,deployment=1},
                new OwnedCommander{id="second",job=101,nativeRecord=1,deployment=2,profile=new CommanderProfile{healthKnown=true,hp=33,maxHp=210,mp=13,maxMp=40,inventory=new[]{new WorldOwnedItem{id="00000000000000000000000000000002",item=3,slot=2}}}},
                new OwnedCommander{id="third",job=103,nativeRecord=2,deployment=1,profile=new CommanderProfile{inventory=Array.Empty<WorldOwnedItem>()}}}}};
        }
        public void Roster(){Check(service.RequestCommanderOperation(CommanderOperation.State,0,0,Id()).Accepted,"request roster");Check(service.ReceiveCommanderState(Notice()).Accepted,"approved roster");}
    }
    static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();Check(f.View.record.roster==null&&f.View.commanders==null,"legacy profile does not fabricate other characters");
        Check(f.service.ExecuteCommanderOperation(CommanderOperation.State,0,0,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.commanderRequest==null,"missing snapshot provider leaves no pending request");
        Check(f.service.InspectCommander(0).Accepted&&f.service.InspectCommander(1).Failure==WorldFailure.NotOwned,"legacy inspection is primary only");
        f.Roster();Check(f.View.commanders.rows.Length==3&&f.View.commanders.Extended&&f.View.record.roster.rows[0].profile==null,"three-row alternate layout and canonical live primary");
        var snapshot=f.View.commanders;snapshot.rows[1].profile.hp=999;Check(f.View.commanders.rows[1].profile.hp==33,"view cannot mutate storage");
        string original=State(f.View.record);Check(f.service.InspectCommander(1).Accepted&&f.View.inspectedCommander==1&&State(f.View.record)==original,"inspection does not change avatar, HP, wallet or inventory");
        f.store.fail=true;Check(f.service.ExecuteCommanderOperation(CommanderOperation.ToggleDeployment,1,0,Id(),null).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==original,"toggle save failure preserves previous flags");
        var pending=f.View.commanderRequest;Check(pending!=null&&pending.NativePacket.SequenceEqual(new byte[]{0x42,1,0,1,0}),"toggle native packet uses record word and value1");
        f.store.fail=false;var retry=new CommanderNotice{id=Id(),approved=true,request=pending,state=OriginalCommanderPackets.Edit(pending)};Check(f.service.ReceiveCommanderState(retry).Accepted&&f.View.commanders.rows[1].deployment==1,"same pending toggle can retry storage");
        Check(f.service.ReceiveCommanderState(retry).Failure==WorldFailure.StaleRequest,"duplicate response rejected");
        var before=f.View.record.Copy();string growthSession=f.View.growthSession,currencySession=f.View.currencySession,campaignSession=f.View.campaignSession;
        Check(f.service.ExecuteCommanderOperation(CommanderOperation.Swap,1,0,Id(),null).Accepted,"source local full-record exchange");
        Check(f.View.commanders.rows[0].id=="second"&&f.View.commanders.rows[0].job==101&&f.View.record.hp==33&&f.View.record.mp==13,"secondary profile becomes primary without healing");
        Check(f.View.commanders.rows[1].profile.hp==17&&f.View.commanders.rows[1].profile.mp==3&&f.View.commanders.rows[1].profile.inventory.Single().id=="00000000000000000000000000000001","live former primary captured completely");
        Check(f.View.record.inventory.Single().id=="00000000000000000000000000000002"&&f.View.record.gold==123&&f.View.record.bank==456&&f.View.record.storage.Single().id=="00000000000000000000000000000003","personal items exchange but shared balances/storage preserved");
        Check(f.View.commanders.rows[0].nativeRecord==0&&f.View.commanders.rows[1].nativeRecord==1,"native info indices rewritten after exchange");
        Check(f.View.growthSession!=growthSession&&f.View.currencySession!=currencySession&&f.View.campaignSession!=campaignSession&&f.View.inspectedCommander==1,"old personal action sessions invalidated; selected index preserved");
        Check(f.View.record.map==before.map&&f.View.record.x==before.x&&f.View.record.y==before.y,"exchange preserves position");
        f.Reload();Check(f.View.commanders.rows[0].id=="second"&&f.View.record.hp==33&&f.View.record.inventory.Single().id=="00000000000000000000000000000002","primary selection and personal data reconnect");
        Check(f.service.ExecuteCommanderOperation(CommanderOperation.Swap,1,0,Id(),null).Accepted&&f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.inventory.Single().id=="00000000000000000000000000000001","roundtrip exchange does not erase wounds or items");
        Check(f.service.ExecuteCommanderOperation(CommanderOperation.ToggleDeployment,0,0,Id(),null).Failure==WorldFailure.RulesUnavailable,"primary has no native deployment checkbox");
        foreach(var test in new[]{"owner","session","revision","baseline","map","id","slot","profile","job","duplicate"})
        {
            var v=new Fixture();Check(v.service.RequestCommanderOperation(CommanderOperation.State,0,0,Id()).Accepted,"validation request");var n=v.Notice();
            switch(test)
            {case "owner":n.request.character=Id();break;case "session":n.request.session=Id();break;case "revision":n.request.revision++;break;case "baseline":n.request.baseline="bad";break;case "map":n.request.map++;break;case "id":n.id="bad";break;case "slot":n.state.rows=new OwnedCommander[11];break;case "profile":n.state.rows[1].profile.hp=999;break;case "job":n.state.rows[1].job=999999;break;case "duplicate":n.state.rows[1].id=n.state.rows[0].id;break;}
            string b=State(v.View.record);Check(!v.service.ReceiveCommanderState(n).Accepted&&State(v.View.record)==b,"invalid input preserves all data: "+test);
        }
        var stale=new Fixture();stale.Roster();stale.service.RequestCommanderOperation(CommanderOperation.ToggleDeployment,1,0,Id());var late=stale.View.commanderRequest.Copy();
        var current=new EntryAccountService(stale.store);var currentClient=new EntryClient(current);currentClient.OpenLocalProfile();current.BeginWorld(stale.definition);
        Check(current.ExecuteCommanderOperation(CommanderOperation.ToggleDeployment,1,0,Id(),null).Accepted,"new service commits roster");
        Check(stale.service.ReceiveCommanderState(new CommanderNotice{id=Id(),approved=true,request=late,state=OriginalCommanderPackets.Edit(late)}).Failure==WorldFailure.StaleRequest,"stale service cannot overwrite later roster");
        var pushed=new Fixture();var roster=pushed.Notice().state;
        var push=new CommanderRosterPush{id=Id(),approved=true,character=pushed.View.characterId,session=pushed.View.commanderSession,expectedRevision=0,state=roster};
        string pushBefore=State(pushed.View.record);pushed.store.fail=true;
        Check(pushed.service.ReceiveCommanderRoster(push).Failure==WorldFailure.StorageUnavailable&&State(pushed.View.record)==pushBefore,"unsolicited grant save failure is atomic");
        pushed.store.fail=false;Check(pushed.service.ReceiveCommanderRoster(push).Accepted&&pushed.View.commanders.rows.Length==3,"approved push adds real owned records without opening F2");
        Check(pushed.service.ReceiveCommanderRoster(push).Failure==WorldFailure.StaleRequest,"grant push replay rejected");
        var revision=pushed.View.commanders.revision;var change=push.state.Copy();change.rows[0].job=103;
        Check(pushed.service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=pushed.View.characterId,session=pushed.View.commanderSession,expectedRevision=revision,state=change}).Failure==WorldFailure.InvalidInput,"grant push cannot secretly replace active primary");
        change=push.state.Copy();change.rows[1].profile.inventory=null;
        Check(pushed.service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=pushed.View.characterId,session=pushed.View.commanderSession,expectedRevision=revision,state=change}).Accepted,"unknown secondary bag remains unknown");
        Check(pushed.service.ExecuteCommanderOperation(CommanderOperation.Swap,1,0,Id(),null).Failure==WorldFailure.RulesUnavailable,"unknown bag cannot erase live primary items through swap");
        var isolated=new Fixture();isolated.Roster();isolated.service.RequestCommanderOperation(CommanderOperation.ToggleDeployment,1,0,Id());var tampered=isolated.View.commanderRequest;
        var injected=OriginalCommanderPackets.Edit(tampered);injected.rows[1].profile.hp++;
        string preserved=State(isolated.View.record);Check(isolated.service.ReceiveCommanderState(new CommanderNotice{id=Id(),approved=true,request=tampered,state=injected}).Failure==WorldFailure.InvalidInput&&State(isolated.View.record)==preserved,"toggle response cannot inject HP or other profile changes");
        var outfitting=new Fixture();outfitting.Roster();var o=outfitting.View;string primary=CommanderProfile.Capture(o.record).Fingerprint();string equipId=Id();
        Check(outfitting.service.EquipCommanderItem(1,"wrong",o.commanders.revision,"00000000000000000000000000000002",2,equipId).Failure==WorldFailure.StaleRequest,"other owner's slot identity is denied");
        outfitting.store.fail=true;string allBefore=State(outfitting.View.record);
        Check(outfitting.service.EquipCommanderItem(1,"second",o.commanders.revision,"00000000000000000000000000000002",2,equipId).Failure==WorldFailure.StorageUnavailable&&State(outfitting.View.record)==allBefore,"secondary equipment save failure preserves both inventories");
        outfitting.store.fail=false;Check(outfitting.service.EquipCommanderItem(1,"second",o.commanders.revision,"00000000000000000000000000000002",2,equipId).Accepted,"selected secondary can equip without becoming avatar");
        Check(CommanderProfile.Capture(outfitting.View.record).Fingerprint()==primary&&outfitting.View.commanders.rows[1].profile.equipment.Single().slot==2&&outfitting.View.commanders.rows[1].profile.inventory.Length==0,"secondary equipment touches only selected character");
        string afterEquip=State(outfitting.View.record);Check(outfitting.service.EquipCommanderItem(1,"second",o.commanders.revision,"00000000000000000000000000000002",-1,Id(),2).Failure==WorldFailure.StaleRequest&&State(outfitting.View.record)==afterEquip,"stale F4 drag revision cannot edit changed roster");
        outfitting.Reload();Check(outfitting.View.commanders.rows[1].profile.hp==33&&outfitting.View.commanders.rows[1].profile.equipment.Single().item==3,"secondary wounds and equipment persist");
        Check(outfitting.service.ExecuteCommanderOperation(CommanderOperation.Swap,1,0,Id(),null).Accepted&&outfitting.View.record.Equipment.Single().item==3&&outfitting.View.record.hp==33,"equipped secondary becomes primary without item duplication or healing");
        var collision=new Fixture();collision.service.RequestCommanderOperation(CommanderOperation.State,0,0,Id());var duplicate=collision.Notice();duplicate.state.rows[1].profile.inventory[0].id="00000000000000000000000000000001";
        Check(collision.service.ReceiveCommanderState(duplicate).Failure==WorldFailure.InvalidInput&&collision.View.record.roster==null,"same local item identity cannot be granted to two owned characters");
        var partial=new Fixture();partial.Roster();var known=partial.View;Check(known.commanders.rows[0].name=="캐릭터검사","known creation name is retained in first primary record");
        var onlyList=new CommanderRoster{rows=known.commanders.rows.Select(r=>new OwnedCommander{id=r.id,job=r.job,nativeRecord=r.nativeRecord,deployment=r.deployment}).ToArray()};
        Check(partial.service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=known.characterId,session=known.commanderSession,expectedRevision=known.commanders.revision,state=onlyList}).Accepted&&partial.View.commanders.rows[1].profile.hp==33&&partial.View.commanders.rows[1].profile.inventory.Single().item==3,"list-only notification preserves received wounds and items");
        onlyList.rows[1].job=100;known=partial.View;string previousList=State(known.record);
        Check(partial.service.ReceiveCommanderRoster(new CommanderRosterPush{id=Id(),approved=true,character=known.characterId,session=known.commanderSession,expectedRevision=known.commanders.revision,state=onlyList}).Failure==WorldFailure.RulesUnavailable&&State(partial.View.record)==previousList,"job change without replacement personal state cannot erase cached character data");
        var disk=new LocalAccountFileStore(Path.Combine(Path.GetTempPath(),"ds-commander-checks","profile.json"));Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var data),"DataContract disk roundtrip");disk.TryRead(out data);Check(State(data.accounts[0].character.world)==State(f.store.data.accounts[0].character.world),"roster/profile fields roundtrip exactly");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Approved isolated roster snapshots; proven local inspect/toggle/exchange, durable storage, no operating defaults or real account saves."}));
    }
}
