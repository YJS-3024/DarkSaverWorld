using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;
using DarkSaver.Original.Entry;

internal static class GatewayChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static void Check(bool ok,string text){assertions++;if(!ok)throw new Exception(text);}
    private static void Reject(Action action,string text){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}catch(IOException){rejected=true;}Check(rejected,text);}
    private static string Base=>Path.Combine(root,"Assets/_Original");
    private static GatewayMapCatalog Catalog()=>JsonSerializer.Deserialize<GatewayMapCatalog>(File.ReadAllText(Path.Combine(Base,"Data/Campaign/GatewayMaps.json")),Options);
    private static WorldDefinition World()
    {var w=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(Base,"Data/World/KaosiaWorld.json")),Options);var c=JsonSerializer.Deserialize<CampaignDefinition>(File.ReadAllText(Path.Combine(Base,"Data/Campaign/CampaignCatalog.json")),Options);return c.Merge(w);}
    private static ScenarioGatewaySettings Settings()
    {var c=Catalog();return new ScenarioGatewaySettings{applyToGame=true,gates=new[]{new ScenarioGateway{id=Guid.NewGuid().ToString("N"),fromMap=1000,targetMap=26,fromHash=c.maps.First(m=>m.id==1000).hash,targetHash=c.maps.First(m=>m.id==26).hash,name="테스트 입구",npcJob=807,x=20,y=21,spawnX=3,spawnY=37,enabled=true}}};}
    private sealed class Memory:IEntryAccountStore
    {public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}}
    private static (EntryAccountService server,Memory store) Service(WorldDefinition world)
    {var store=new Memory();var s=new EntryAccountService(store);var client=new EntryClient(s);client.OpenLocalProfile();client.Create(100,"입구검사");store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();Check(s.BeginWorld(world).Accepted,"completed fixture profile");return(s,store);}
    private static void Main(string[] args)
    {
        root=args[0];var catalog=Catalog();Check(catalog.maps.Length==151&&catalog.maps.First(m=>m.id==1000).width==43,"editor/runtime share legacy Kaosia43 rather than late61");
        var defaults=ScenarioGatewayData.Decode(File.ReadAllBytes(Path.Combine(Base,"Data/Campaign/UserGateways.json")));ScenarioGatewayData.Validate(defaults,catalog);
        Check(!defaults.applyToGame&&defaults.gates.Length==0,"no automatically invented entrances or NPCs");
        var settings=Settings();var world=World();int before=world.maps.Sum(m=>m.npcs.Length);
        ScenarioGatewayData.Apply(settings,catalog,world);Check(world.maps.Sum(m=>m.npcs.Length)==before+1&&world.Map(1000).npcs.Length==1,"one manually authored NPC appended");
        ScenarioGatewayData.Apply(settings,catalog,world);Check(world.Map(1000).npcs.Length==1,"reapplying does not duplicate NPC");
        settings.applyToGame=false;ScenarioGatewayData.Apply(settings,catalog,world);Check(world.Map(1000).npcs.Length==0,"global off removes authored NPC from cloned world");
        settings.applyToGame=true;settings.gates[0].enabled=false;ScenarioGatewayData.Apply(settings,catalog,world);Check(world.Map(1000).npcs.Length==0,"per-gate disabled remains absent");
        settings.gates[0].enabled=true;ScenarioGatewayData.Apply(settings,catalog,world);Check(World().Map(1000).npcs.Length==0,"native/source world never mutated");
        var blocked=Settings();blocked.gates[0].x=21;var blockedWorld=World();ScenarioGatewayData.Apply(blocked,catalog,blockedWorld);
        var blockedStore=new Memory();var blockedServer=new EntryAccountService(blockedStore);var blockedClient=new EntryClient(blockedServer);blockedClient.OpenLocalProfile();blockedClient.Create(100,"겹침검사");blockedStore.data.accounts[0].character.tutorialCompleted=true;blockedClient.OpenLocalProfile();
        Check(blockedServer.BeginWorld(blockedWorld).Failure==WorldFailure.InvalidStage&&blockedStore.data.accounts[0].character.world==null,"saved player overlap is rejected instead of teleporting or overwriting profile");
        var invalid=settings.Copy();invalid.gates=new[]{settings.gates[0].Copy(),settings.gates[0].Copy()};invalid.gates[1].id=Guid.NewGuid().ToString("N");
        Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"at most one NPC per target scenario");
        invalid=settings.Copy();invalid.gates[0].npcJob=401;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"monsters cannot impersonate entrance NPC artwork");
        invalid=settings.Copy();invalid.gates[0].x=43;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"legacy map bounds enforced");
        invalid=settings.Copy();invalid.gates[0].spawnX=-1;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"unselected spawn cannot save");
        invalid=settings.Copy();invalid.gates[0].fromHash="wrong";Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"changed map version rejected");
        invalid=settings.Copy();invalid.provenance="original";Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"manual authoring cannot be labelled original proof");
        invalid=settings.Copy();invalid.gates[0].targetMap=1010;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"scenario gateway cannot silently become field teleport");
        invalid=settings.Copy();invalid.gates[0].x=40;invalid.gates[0].y=35;Reject(()=>ScenarioGatewayData.Apply(invalid,catalog,World()),"existing door cannot be occupied by entrance NPC");
        invalid=settings.Copy();invalid.gates[0].spawnX=5;invalid.gates[0].spawnY=17;Reject(()=>ScenarioGatewayData.Apply(invalid,catalog,World()),"arrival cannot overlap native monster");
        var fixture=Service(world);var server=fixture.server;
        Check(server.MoveWorld(20,21).Failure==WorldFailure.InvalidInput&&server.WorldSnapshot().record.map==1000,"NPC tile does not act as walk-through portal");
        string npc=world.Map(1000).npcs.Single().id;Check(server.OpenWorldFacility(npc).Accepted,"adjacent NPC opens facility");
        var view=server.WorldSnapshot();Check(view.facility.kind==FacilityKind.ScenarioGateway&&view.facility.scenario==26&&!string.IsNullOrEmpty(view.facility.room),"room list targets chosen scenario");
        Check(server.MoveWorld(22,21).Failure==WorldFailure.Busy,"room list locks walking");
        Check(server.JoinScenarioRoom("wrong",view.facility.token,Guid.NewGuid().ToString("N")).Failure==WorldFailure.StaleRequest,"stale room rejected");
        string request=Guid.NewGuid().ToString("N");fixture.store.fail=true;
        Check(server.JoinScenarioRoom(view.facility.room,view.facility.token,request).Failure==WorldFailure.StorageUnavailable&&server.WorldSnapshot().record.map==1000&&server.WorldSnapshot().facility!=null,"failed join save keeps room/town state");
        fixture.store.fail=false;Check(server.JoinScenarioRoom(view.facility.room,view.facility.token,request).Accepted&&server.WorldSnapshot().record.map==26&&server.WorldSnapshot().record.x==3&&server.WorldSnapshot().record.y==37,"selection and participation enter explicit spawn");
        Check(server.WorldSnapshot().facility==null&&!server.WorldSnapshot().record.goldKnown&&server.WorldSnapshot().record.inventory.Length==0,"successful join releases UI without money/items");
        Check(server.JoinScenarioRoom(view.facility.room,view.facility.token,request).Failure==WorldFailure.StaleRequest,"duplicate participation cannot enter twice");
        fixture=Service(world);server=fixture.server;server.OpenWorldFacility(npc);Check(server.CloseWorldFacility().Accepted&&server.WorldSnapshot().facility==null&&server.WorldSnapshot().record.map==1000,"cancel stays in source world");
        server.MoveWorld(23,21);server.AdvanceWorld(10000);Check(server.OpenWorldFacility(npc).Failure==WorldFailure.TooFar,"NPC distance enforced");
        string folder=Path.Combine(Path.GetTempPath(),"gateway-tests-"+Guid.NewGuid().ToString("N")),path=Path.Combine(folder,"UserGateways.json");
        try
        {
            string hash=ScenarioGatewayData.Save(path,settings,catalog,null);Check(ScenarioGatewayData.Decode(File.ReadAllBytes(path)).gates.Single().spawnY==37,"actual authoring file roundtrip");
            var bytes=File.ReadAllBytes(path);settings.gates[0].name="변경";ScenarioGatewayData.Save(path,settings,catalog,hash);
            Reject(()=>ScenarioGatewayData.Save(path,settings,catalog,hash),"stale file snapshot cannot overwrite");
            settings.gates[0].x=-1;Reject(()=>ScenarioGatewayData.Save(path,settings,catalog,ScenarioGatewayData.Hash(File.ReadAllBytes(path))),"invalid edits do not overwrite valid file");
            Check(ScenarioGatewayData.Decode(File.ReadAllBytes(path)).gates[0].name=="변경"&&Directory.GetFiles(folder,"*.tmp").Length==0,"valid saved file retained, no temp files");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,defaultNpcCount=0,defaultApplyToGame=false,editorMaps=151,unityPlay=false,scope="Explicit synthetic NPC/room fixtures only; no production positions inserted."}));
    }
}
