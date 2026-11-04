using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;

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
        var authored=ScenarioGatewayData.Decode(File.ReadAllBytes(Path.Combine(Base,"Data/Campaign/UserGateways.json")));ScenarioGatewayData.Validate(authored,catalog);
        var defaults=ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(new ScenarioGatewaySettings()));
        Check(!defaults.applyToGame&&defaults.gates.Length==0,"empty contract does not invent entrances or NPCs; authored file may legitimately be enabled");
        var authoredWorld=World();ScenarioGatewayData.Apply(authored,catalog,authoredWorld);
        foreach(var gate in authored.gates.Where(g=>authored.applyToGame&&g.enabled&&g.IsWalk))
        {
            var portal=authoredWorld.Map(gate.fromMap).portals.Single(p=>p.gateway==gate.id);
            Check(portal.targetMap==gate.targetMap&&portal.cells.Select(c=>(c.x,c.y)).SequenceEqual(gate.Cells().Select(c=>(c.x,c.y))),"actual authored portal shape/target applied "+gate.name);
        }
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
        Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"overlapping NPC cells rejected");
        invalid.gates[1].y=22;ScenarioGatewayData.Validate(invalid,catalog);Check(invalid.gates.Length==2,"separate entrances may share a target scenario");
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
        CheckAreas(catalog);CheckArrivalAreas(catalog);CheckGatewayOrder(catalog);
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,defaultNpcCount=0,defaultApplyToGame=false,editorMaps=151,areas=new[]{"3x1","6x1","1x3","2x3"},arrivalAreas=new[]{"2x1","1x2","2x2"},unityPlay=false,scope="Explicit synthetic NPC/room and multi-cell map transition fixtures; no production positions inserted."}));
    }
    private static WorldDefinition FlatWorld()
    {
        // Synthetic passable fixtures only. Native source maps/settings/accounts remain untouched.
        var w=World();var catalog=Catalog();
        w.maps=new[]{1000,1010,26,101,2000}.Select(id=>{
            var info=catalog.maps.First(m=>m.id==id);var terrain=new int[27*30];terrain[2]=1;
            return new WorldMapDefinition{id=id,title="fixture",training=id==101,map=new TutorialMap{width=info.width,height=info.height,attributes=new int[info.width*info.height],terrain=terrain,abilities=Array.Empty<TutorialAbility>()},portals=Array.Empty<WorldPortal>(),npcs=Array.Empty<WorldNpc>(),decorations=Array.Empty<WorldDecoration>()};
        }).ToArray();
        w.campaign=new CampaignDefinition{maps=w.maps.Where(m=>m.id!=1000&&m.id!=2000).Select(m=>new CampaignMap{id=m.id,kind=m.id==1010?"world":"scenario",world=m,units=Array.Empty<CampaignUnit>(),intro="",outro=""}).ToArray(),connections=Array.Empty<CampaignConnection>(),jobs=Array.Empty<CampaignJob>()};
        return w;
    }
    private static ScenarioGateway Walk(int from,int to,int x,int y,int width=3,int height=1,int sx=10,int sy=10)
    {
        var c=Catalog();return new ScenarioGateway{id=Guid.NewGuid().ToString("N"),trigger="walk",name="fixture area",enabled=true,
            fromMap=from,targetMap=to,fromHash=c.maps.First(m=>m.id==from).hash,targetHash=c.maps.First(m=>m.id==to).hash,
            x=x,y=y,width=width,height=height,spawnX=sx,spawnY=sy,returnX=21,returnY=22};
    }
    private static ScenarioGatewaySettings Areas(params ScenarioGateway[] gates)=>new ScenarioGatewaySettings{applyToGame=true,gates=gates};
    private static void CheckAreas(GatewayMapCatalog catalog)
    {
        var legacy=ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(Settings())).gates[0];
        Check(!legacy.IsWalk&&legacy.Width==1&&legacy.Height==1,"version1 missing fields retain single-tile NPC behavior");
        foreach(var size in new[]{(width:3,height:1),(width:6,height:1),(width:1,height:3),(width:2,height:3)})for(int row=0;row<size.height;row++)for(int column=0;column<size.width;column++)
        {
            var gate=Walk(1000,1010,22,22,size.width,size.height);var settings=Areas(gate);var world=FlatWorld();ScenarioGatewayData.Apply(settings,catalog,world);
            Check(world.Map(1000).portals.Single().cells.Length==size.width*size.height,"area exports all trigger cells");
            var service=Service(world).server;Check(service.EnterCampaignMap(1010).Failure==WorldFailure.RulesUnavailable,"remote transition denied without reaching entrance");
            Check(service.MoveWorld(22+column,22+row).Accepted,"each entrance cell reachable");service.AdvanceWorld(10000);
            Check(service.WorldSnapshot().pendingPortal=="scenario-gateway-"+gate.id,"each entrance column activates same portal");
            Check(service.EnterCampaignMap(1010).Accepted,"authored route enters campaign field through server");
            var view=service.WorldSnapshot();Check(view.record.map==1010&&view.record.x==10&&view.record.y==10&&view.pendingPortal==null&&view.record.campaignPhase==null,"different-map destination and phase applied exactly");
        }
        var useToggle=Areas(Walk(1000,1010,22,22,1,1,10,10));var staleRegistry=FlatWorld();
        staleRegistry.maps=staleRegistry.maps.Where(m=>m.id!=1010).ToArray();useToggle.applyToGame=false;
        ScenarioGatewayData.Apply(useToggle,catalog,staleRegistry);
        Check(staleRegistry.Map(1000).portals.Length==0,"unchecked drafts do not resolve destination runtime maps");
        useToggle.applyToGame=true;string toggleError="";
        try{ScenarioGatewayData.Apply(useToggle,catalog,staleRegistry);}catch(InvalidDataException e){toggleError=e.Message;}
        Check(toggleError.Contains("도착 맵 1010"),"enabling local application exposes stale registry destination exactly");
        var freshRegistry=FlatWorld();ScenarioGatewayData.Apply(useToggle,catalog,freshRegistry);
        Check(freshRegistry.Map(1000).portals.Single().targetMap==1010,"same enabled1000-to1010 connection works against fresh full registry");
        var duplicateNameGood=Walk(1000,1010,22,24,1,1,10,10);var duplicateNameBad=Walk(1000,201,22,22,1,1,10,10);
        duplicateNameGood.name=duplicateNameBad.name="같은 입구 이름";string identified="";
        try{ScenarioGatewayData.Apply(Areas(duplicateNameGood,duplicateNameBad),catalog,FlatWorld());}catch(InvalidDataException e){identified=e.Message;}
        Check(identified.Contains(duplicateNameBad.id)&&!identified.Contains(duplicateNameGood.id)&&identified.Contains("출발 1000")&&identified.Contains("도착 201"),"error identifies the failing second gateway even when names match");
        var invalidPoint=new ScenarioEntryPoint{map=3,mapHash=catalog.maps.First(m=>m.id==3).hash,x=-1,y=7,enabled=true};identified="";
        try{ScenarioGatewayData.Validate(new ScenarioGatewaySettings{entryPoints=new[]{invalidPoint}},catalog);}catch(InvalidDataException e){identified=e.Message;}
        Check(identified.Contains("진입점")&&identified.Contains("맵 3")&&identified.Contains("(-1,7)"),"manual point validation reports exact map and coordinates");
        string unregistered="";
        try{ScenarioGatewayData.Apply(Areas(Walk(1000,201,22,22,1,1,10,10)),catalog,FlatWorld());}catch(InvalidDataException e){unregistered=e.Message;}
        Check(unregistered.Contains("도착 맵 201"),"editor-only destination reports its exact unregistered ID");
        unregistered="";
        try{ScenarioGatewayData.Apply(Areas(Walk(201,1000,10,10,1,1,21,21)),catalog,FlatWorld());}catch(InvalidDataException e){unregistered=e.Message;}
        Check(unregistered.Contains("출발 맵 201"),"editor-only source reports its exact unregistered ID");
        var a=Walk(1000,1010,22,22,3);var b=Walk(1000,1010,22,24,6,1,11,11);
        var back=Walk(1010,1000,10,12,6,1,21,22);var document=Areas(a,b,back);var multi=FlatWorld();ScenarioGatewayData.Apply(document,catalog,multi);
        Check(multi.Map(1000).portals.Length==2&&multi.Map(1010).portals.Length==1,"multiple entrances and explicit reverse direction applied");
        var pair=Service(multi);var server=pair.server;server.MoveWorld(22,22);server.AdvanceWorld(10000);pair.store.fail=true;
        Check(server.EnterCampaignMap(1010).Failure==WorldFailure.StorageUnavailable&&server.WorldSnapshot().record.map==1000&&server.WorldSnapshot().pendingPortal!=null,"failed transition save preserves source and pending entry for retry");
        pair.store.fail=false;Check(server.EnterCampaignMap(1010).Accepted,"same entry retry succeeds after storage recovery");
        server.MoveWorld(10,12);server.AdvanceWorld(10000);Check(server.EnterWorldPortal("scenario-gateway-"+back.id,false).Accepted&&server.WorldSnapshot().record.map==1000&&server.WorldSnapshot().record.x==21&&server.WorldSnapshot().record.y==22,"reverse area returns to configured source tile");
        var roundtrip=ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(document));ScenarioGatewayData.Validate(roundtrip,catalog);
        Check(roundtrip.gates[1].width==6&&roundtrip.gates[2].fromMap==1010&&roundtrip.gates[2].targetMap==1000,"area dimensions/direction survive JSON roundtrip");
        var invalid=document.Copy();invalid.gates[1].y=22;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"partially overlapping rectangles rejected");
        invalid=Areas(a.Copy());invalid.gates[0].width=int.MaxValue;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"oversized area rejected before allocation");
        invalid=Areas(a.Copy());invalid.gates[0].width=0;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"zero width is invalid for a new walk area");
        invalid=Areas(a.Copy());invalid.gates[0].height=-1;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"negative height rejected");
        invalid=Areas(a.Copy());invalid.gates[0].x=42;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"whole area must fit source map");
        invalid=Areas(a.Copy());invalid.gates[0].trigger="unknown";Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"unknown activation type rejected");
        var blocked=FlatWorld();blocked.Map(1000).map.attributes[22*blocked.Map(1000).map.width+23]=1;blocked.Map(1000).map.terrain[32]=99;
        Reject(()=>ScenarioGatewayData.Apply(Areas(a),catalog,blocked),"one blocked cell invalidates entire source area");
        string terrainError="";try{ScenarioGatewayData.Apply(Areas(a),catalog,blocked);}catch(InvalidDataException e){terrainError=e.Message;}
        Check(terrainError.Contains(a.name)&&terrainError.Contains("1000")&&terrainError.Contains("(23,22)"),"blocked entrance diagnostic identifies exact gate/map/cell");
        var atomic=FlatWorld();atomic.Map(1010).map.attributes[11*atomic.Map(1010).map.width+11]=1;atomic.Map(1010).map.terrain[32]=99;
        var originalMaps=atomic.maps;Reject(()=>ScenarioGatewayData.Apply(Areas(a,b),catalog,atomic),"later invalid entry rejected");
        Check(ReferenceEquals(atomic.maps,originalMaps)&&atomic.Map(1000).portals.Length==0&&atomic.gateways==null,"failed apply does not publish partial edits");
        invalid=Areas(a.Copy(),Walk(1010,1000,10,10,3,1,21,22));var exitArrival=FlatWorld();ScenarioGatewayData.Apply(invalid,catalog,exitArrival);
        Check(exitArrival.Map(1010).portals.Length==1,"arrival on another exit is permitted with reentry guard");
        var native=new WorldPortal{id="native-fixture",targetMap=2000,spawnX=10,spawnY=10,cells=a.Cells()};var overridden=FlatWorld();overridden.Map(1000).portals=new[]{native};
        var edit=Walk(1000,1010,23,22,6);edit.replacesPortal=native.id;var edits=Areas(edit);
        ScenarioGatewayData.Apply(edits,catalog,overridden);Check(overridden.Map(1000).portals.Single().targetMap==1010&&overridden.Map(1000).portals.Single().cells.Length==6,"explicit original-door override replaces rather than overlaps");
        ScenarioGatewayData.Apply(edits,catalog,overridden);Check(overridden.Map(1000).portals.Length==1,"reapplying override is idempotent");
        edits.gates[0].enabled=false;ScenarioGatewayData.Apply(edits,catalog,overridden);Check(overridden.Map(1000).portals.Single()==native&&native.cells.Length==3&&native.targetMap==2000,"disabling override restores unmodified original door");
        edits.gates[0].enabled=true;edits.applyToGame=false;ScenarioGatewayData.Apply(edits,catalog,overridden);Check(overridden.Map(1000).portals.Single()==native,"global off restores native door");
        invalid=Areas(edit.Copy());invalid.gates[0].replacesPortal="missing";Reject(()=>ScenarioGatewayData.Apply(invalid,catalog,FlatWorld()),"unknown replacement id rejected");
        var scenario=FlatWorld();var toScenario=Walk(1000,26,22,22);ScenarioGatewayData.Apply(Areas(toScenario),catalog,scenario);server=Service(scenario).server;
        server.MoveWorld(22,22);server.AdvanceWorld(10000);Check(server.EnterCampaignMap(26).Accepted&&server.WorldSnapshot().record.campaignPhase=="intro","scenario entrance starts intro phase");
        var training=FlatWorld();training.Map(1000).portals=new[]{new WorldPortal{id="other-training-door",targetMap=101,spawnX=10,spawnY=10,returnX=29,returnY=30,cells=new[]{new WorldPoint{x=30,y=30}}}};
        var toTraining=Walk(1000,101,22,22);ScenarioGatewayData.Apply(Areas(toTraining),catalog,training);pair=Service(training);server=pair.server;server.MoveWorld(22,22);server.AdvanceWorld(10000);
        Check(server.EnterCampaignMap(101).Accepted&&server.WorldSnapshot().record.map==101,"second training entrance enters normally");
        Check(pair.store.data.accounts[0].character.world.map==1000&&pair.store.data.accounts[0].character.world.x==21&&pair.store.data.accounts[0].character.world.y==22,"training interrupted save uses actual entered door return tile");
        Check(server.EscapeTraining().Accepted&&server.WorldSnapshot().record.x==21&&server.WorldSnapshot().record.y==22,"training escape uses actual door rather than first matching map");
        string folder=Path.Combine(Path.GetTempPath(),"area-tests-"+Guid.NewGuid().ToString("N")),path=Path.Combine(folder,"UserGateways.json");
        try{ScenarioGatewayData.Save(path,document,catalog,null);var restored=ScenarioGatewayData.Decode(File.ReadAllBytes(path));Check(restored.gates.Length==3&&restored.gates[1].Width==6,"multiple areas saved to isolated real file");}
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
    private static void CheckArrivalAreas(GatewayMapCatalog catalog)
    {
        var legacy=ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(Settings())).gates[0];
        Check(legacy.SpawnWidth==1&&legacy.SpawnHeight==1,"old settings retain one arrival cell");
        var forward=Walk(1000,1010,22,22,6);forward.spawnWidth=2;forward.spawnHeight=1;
        var back=Walk(1010,1000,10,10,2,1,22,22);back.spawnWidth=6;back.spawnHeight=1;
        var settings=Areas(forward,back);var copied=ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(settings));
        Check(copied.gates[0].SpawnWidth==2&&copied.gates[1].SpawnWidth==6,"arrival areas persist across serialization");
        int[] expected={10,10,10,11,11,11};
        for(int i=0;i<6;i++)
        {
            var world=FlatWorld();ScenarioGatewayData.Apply(settings,catalog,world);var fixture=Service(world);var server=fixture.server;
            server.MoveWorld(22+i,22);server.AdvanceWorld(10000);
            if(i==5)
            {
                fixture.store.fail=true;Check(server.EnterCampaignMap(1010).Failure==WorldFailure.StorageUnavailable&&server.WorldSnapshot().record.map==1000&&server.WorldSnapshot().pendingPortal!=null,"multi-cell arrival save failure retains entry for same-cell retry");fixture.store.fail=false;
            }
            Check(server.EnterCampaignMap(1010).Accepted&&server.WorldSnapshot().record.x==expected[i]&&server.WorldSnapshot().record.y==10,"6-to-2 source lane maps to expected destination");
            Check(server.WorldSnapshot().pendingPortal==null,"arrival does not immediately trigger return exit");
            if(i==0)
            {
                server=new EntryAccountService(fixture.store);var client=new EntryClient(server);client.OpenLocalProfile();
                Check(server.BeginWorld(world).Accepted&&server.WorldSnapshot().record.map==1010,"saved arrival on exit restores same map");
            }
            Check(server.MoveWorld(expected[i]==10?11:10,10).Accepted,"can walk between cells inside arrival exit");server.AdvanceWorld(10000);
            Check(server.WorldSnapshot().pendingPortal==null&&server.WorldSnapshot().record.map==1010,"walking inside arrival exit cannot bounce back");
            Check(server.EnterWorldPortal("scenario-gateway-"+back.id,false).Failure==WorldFailure.InvalidInput,"arrival exit cannot be requested before leaving and reentering");
            Check(server.MoveWorld(10,11).Accepted,"can leave arrival exit");server.AdvanceWorld(10000);
            int lane=i%2;Check(server.MoveWorld(10+lane,10).Accepted,"can deliberately reenter either return lane");server.AdvanceWorld(10000);
            Check(server.WorldSnapshot().pendingPortal=="scenario-gateway-"+back.id,"exit rearms after leaving its whole area");
            Check(server.EnterWorldPortal("scenario-gateway-"+back.id,false).Accepted&&server.WorldSnapshot().record.map==1000&&server.WorldSnapshot().record.x==(lane==0?22:27),"two-lane return maps to ends of six-cell arrival area");
        }
        for(int row=0;row<2;row++)
        {
            var gate=Walk(1000,1010,22,22,1,2);gate.spawnWidth=2;gate.spawnHeight=1;var world=FlatWorld();ScenarioGatewayData.Apply(Areas(gate),catalog,world);var server=Service(world).server;
            server.MoveWorld(22,22+row);server.AdvanceWorld(10000);Check(server.EnterCampaignMap(1010).Accepted&&server.WorldSnapshot().record.x==10+row&&server.WorldSnapshot().record.y==10,"vertical entrance maps to horizontal arrival");
        }
        for(int column=0;column<4;column++)
        {
            var gate=Walk(1000,1010,22,22,4);gate.spawnWidth=2;gate.spawnHeight=2;var world=FlatWorld();ScenarioGatewayData.Apply(Areas(gate),catalog,world);var server=Service(world).server;
            server.MoveWorld(22+column,22);server.AdvanceWorld(10000);Check(server.EnterCampaignMap(1010).Accepted&&server.WorldSnapshot().record.x==10+column%2&&server.WorldSnapshot().record.y==10+column/2,"rectangular arrival preserves documented cell order");
        }
        var vertical=forward.Copy();vertical.spawnWidth=1;vertical.spawnHeight=2;var verticalWorld=FlatWorld();ScenarioGatewayData.Apply(Areas(vertical),catalog,verticalWorld);var verticalServer=Service(verticalWorld).server;
        verticalServer.MoveWorld(27,22);verticalServer.AdvanceWorld(10000);Check(verticalServer.EnterCampaignMap(1010).Accepted&&verticalServer.WorldSnapshot().record.x==10&&verticalServer.WorldSnapshot().record.y==11,"horizontal entrance maps to vertical arrival end");
        var npc=Settings();npc.gates[0].spawnWidth=2;var npcWorld=FlatWorld();ScenarioGatewayData.Apply(npc,catalog,npcWorld);var n=Service(npcWorld).server;n.OpenWorldFacility(npcWorld.Map(1000).npcs.Single().id);var facility=n.WorldSnapshot().facility;
        Check(n.JoinScenarioRoom(facility.room,facility.token,Guid.NewGuid().ToString("N")).Accepted&&n.WorldSnapshot().record.x==3&&n.WorldSnapshot().record.y==37,"single NPC source uses first configured arrival cell");
        var invalid=Areas(forward.Copy());invalid.gates[0].spawnX=159;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"whole arrival area must fit map bounds");
        invalid=Areas(forward.Copy());invalid.gates[0].spawnHeight=-1;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"negative arrival extent rejected");
        invalid=Areas(forward.Copy());invalid.gates[0].spawnWidth=int.MaxValue;Reject(()=>ScenarioGatewayData.Validate(invalid,catalog),"oversized arrival rejected before allocation");
        var blocked=FlatWorld();blocked.Map(1010).map.attributes[10*blocked.Map(1010).map.width+11]=1;blocked.Map(1010).map.terrain[32]=99;
        Reject(()=>ScenarioGatewayData.Apply(Areas(forward),catalog,blocked),"second arrival tile cannot be blocked");
        var occupied=FlatWorld();occupied.Map(1010).npcs=new[]{new WorldNpc{id="fixture",x=11,y=10}};
        Reject(()=>ScenarioGatewayData.Apply(Areas(forward),catalog,occupied),"second arrival tile cannot contain NPC");
        var monster=FlatWorld();monster.campaign.Map(1010).units=new[]{new CampaignUnit{unit=400,job=401,x=11,y=10}};
        Reject(()=>ScenarioGatewayData.Apply(Areas(forward),catalog,monster),"second arrival tile cannot contain original monster");
    }
    private static void CheckGatewayOrder(GatewayMapCatalog catalog)
    {
        var a=Walk(1000,1010,22,22);var b=Walk(1000,1010,22,24);var c=Walk(1000,1010,22,26);
        b.group="카오시아 시설";c.group="필드 연결";var settings=Areas(a,b,c);
        var original=settings.gates.ToDictionary(g=>g.id,g=>Convert.ToBase64String(ScenarioGatewayData.Encode(Areas(g))));
        Check(a.GroupName==""&&ScenarioGatewayData.Decode(ScenarioGatewayData.Encode(Settings())).gates[0].GroupName=="","old settings have no custom group");
        var next=settings.Copy();Check(next.gates[1].group==b.group&&next.gates[1]!=b,"group metadata copied independently");
        Check(ScenarioGatewayData.Move(next,c.id,a.id,false)&&next.gates.Select(g=>g.id).SequenceEqual(new[]{c.id,a.id,b.id}),"arbitrary move before anchor keeps all gateways");
        Check(ScenarioGatewayData.Move(next,c.id,b.id,true)&&next.gates.Select(g=>g.id).SequenceEqual(new[]{a.id,b.id,c.id}),"move after anchor restores expected order");
        byte[] before=ScenarioGatewayData.Encode(next);
        Check(!ScenarioGatewayData.Move(next,a.id,a.id,false)&&!ScenarioGatewayData.Move(next,"missing",b.id,false)&&!ScenarioGatewayData.Move(next,a.id,"missing",false)&&before.SequenceEqual(ScenarioGatewayData.Encode(next)),"invalid moves preserve complete document");
        foreach(var gate in next.gates)Check(original[gate.id]==Convert.ToBase64String(ScenarioGatewayData.Encode(Areas(gate))),"reordering preserves route dimensions, hashes, id and group");
        next.gates[0].group="새 그룹";Check(settings.gates[0].group==null,"editing copied group leaves original untouched");
        ScenarioGatewayData.Move(next,c.id,a.id,false);var world=FlatWorld();ScenarioGatewayData.Apply(next,catalog,world);
        foreach(var gate in next.gates)
        {
            var portal=world.Map(1000).portals.Single(p=>p.gateway==gate.id);
            Check(portal.targetMap==gate.targetMap&&portal.spawnX==gate.spawnX&&portal.spawnY==gate.spawnY&&portal.cells[0].x==gate.x&&portal.cells[0].y==gate.y,"group/order metadata does not alter runtime connection");
        }
        string folder=Path.Combine(Path.GetTempPath(),"gateway-order-tests-"+Guid.NewGuid().ToString("N")),path=Path.Combine(folder,"UserGateways.json");
        try
        {
            ScenarioGatewayData.Save(path,next,catalog,null);var loaded=ScenarioGatewayData.Decode(File.ReadAllBytes(path));
            Check(loaded.gates.Select(g=>g.id).SequenceEqual(next.gates.Select(g=>g.id))&&loaded.gates.Select(g=>g.group).SequenceEqual(next.gates.Select(g=>g.group)),"custom Unicode groups and arbitrary order survive actual file save/reload");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
}
