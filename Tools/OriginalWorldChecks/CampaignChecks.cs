using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Maps;

internal static class CampaignChecks
{
    private static int checks;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
    private sealed class Store:IEntryAccountStore
    {
        public LocalAccountDocument data;public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data?.Copy()??new LocalAccountDocument();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    private static string root;
    private static CampaignDefinition Catalog()
    {
        var definition=JsonSerializer.Deserialize<CampaignDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/Campaign/CampaignCatalog.json")),Options);
        using var report=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json")));
        var scripts=report.RootElement.GetProperty("scripts").EnumerateArray().Select(x=>x.GetString()).ToArray();
        foreach(var map in definition.maps)
        {map.intro=map.introIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.introIndex]));map.outro=map.outroIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.outroIndex]));map.events=map.eventIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.eventIndex]));}
        return definition;
    }
    private static WorldDefinition World()=>JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Options);
    private static (EntryAccountService server,Store store,EntryClient client) Identity()
    {
        var store=new Store();var server=new EntryAccountService(store);var client=new EntryClient(server);
        client.OpenLocalProfile();client.Create(100,"캠페인검사");store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();return(server,store,client);
    }
    private static void Main(string[] args)
    {
        root=args[0];var catalog=Catalog();var world=catalog.Merge(World());
        Check(catalog.maps.Length==126&&catalog.maps.Count(m=>m.kind=="scenario")==97&&catalog.maps.Count(m=>m.kind=="world")==29,"all original campaign/world resources");
        Check(world.IsValid&&world.maps.Length==137,"town/interiors plus source maps share valid navigation");
        Check(catalog.connections.Length==0&&world.Map(1000).portals.Length==10,"no invented field/scenario entrances");
        Check(catalog.maps.Sum(m=>m.units.Length)==610&&catalog.maps.Count(m=>m.units.Length>0)==16,"no fallback rosters");
        var identity=Identity();Check(identity.server.BeginWorld(world).Accepted,"authenticated completed profile enters merged world");
        string before=JsonSerializer.Serialize(identity.server.WorldSnapshot().record,Options);
        foreach(var map in catalog.maps)
        {
            Check(identity.server.EnterCampaignMap(map.id).Failure==WorldFailure.RulesUnavailable,"missing original connection denied "+map.key);
            Check(JsonSerializer.Serialize(identity.server.WorldSnapshot().record,Options)==before,"denied entry preserves profile "+map.key);
        }
        // Only this isolated fixture adds a connection. Production connections stay empty.
        var testCatalog=Catalog();var target=testCatalog.Map(26);var spawn=target.entries.First(e=>e.unit==0);
        testCatalog.connections=new[]{new CampaignConnection{fromMap=1000,toMap=26,x=spawn.x,y=spawn.y,evidence="isolated synthetic route; not original evidence",cells=new[]{new WorldPoint{x=21,y=21}}}};
        var test=Identity();var testWorld=testCatalog.Merge(World());test.server.BeginWorld(testWorld);test.store.fail=true;
        Check(test.server.EnterCampaignMap(26).Failure==WorldFailure.StorageUnavailable&&test.server.WorldSnapshot().record.map==1000,"failed entry save retains source map");
        test.store.fail=false;Check(test.server.EnterCampaignMap(26).Accepted&&test.server.WorldSnapshot().units.Length==65,"source SET/POS map entry populates exact roster");
        Check(test.server.WorldSnapshot().record.x==3&&test.server.WorldSnapshot().record.y==37,"source POS entry remains literal");
        var snapshot=test.server.WorldSnapshot();snapshot.units[0].x=999;Check(test.server.WorldSnapshot().units[0].x==5,"roster snapshot cannot mutate source");
        Check(test.server.CampaignUnitAction(400).Failure==WorldFailure.RulesUnavailable&&test.server.CampaignUnitAction(999999).Failure==WorldFailure.InvalidInput,"known target request held; invalid target rejected");
        Check(test.server.RequestCampaignClear().Failure==WorldFailure.RulesUnavailable,"no invented clear condition or reward");
        Check(test.server.MoveWorld(5,17).Failure==WorldFailure.InvalidInput,"native roster tiles block player movement");
        Check(!test.server.WorldSnapshot().record.goldKnown&&test.server.WorldSnapshot().record.inventory.Length==0,"no economic grants");
        test.client.Logout();test.client.OpenLocalProfile();Check(test.server.BeginWorld(testWorld).Accepted&&test.server.WorldSnapshot().record.map==26,"native world position reconnects without arbitrary relocation");
        var clear=new CampaignClearNotice{map=26,id=Guid.NewGuid().ToString("N"),session="stale",approved=true};
        Check(test.server.ReceiveCampaignClear(clear).Failure==WorldFailure.StaleRequest,"old map/session clear notification rejected");
        clear.session=test.server.WorldSnapshot().campaignSession;test.store.fail=true;
        Check(test.server.ReceiveCampaignClear(clear).Failure==WorldFailure.StorageUnavailable,"failed authoritative ending save is not acknowledged");
        test.store.fail=false;Check(test.server.ReceiveCampaignClear(clear).Accepted,"explicit trusted server clear begins source ending without granting items");
        Check(test.server.ReceiveCampaignClear(clear).Failure==WorldFailure.StaleRequest,"duplicate authoritative clear rejected");
        test.store.fail=true;test.server.AdvanceWorld(0);Check(test.server.MoveWorld(4,37).Failure==WorldFailure.Busy,"failed ending phase save holds movement instead of allowing partial transition");
        test.store.fail=false;test.server.AdvanceWorld(0);Check(test.server.WorldSnapshot().record.campaignPhase=="complete"&&test.server.WorldSnapshot().record.inventory.Length==0,"empty source ending completes presentation only, not fabricated rewards");
        clear.id=Guid.NewGuid().ToString("N");Check(test.server.ReceiveCampaignClear(clear).Failure==WorldFailure.InvalidStage,"completed phase cannot replay ending with a new notice id");
        var script=new CampaignPresentation("LOADSCENE 1\nCHARPOS COM0 0 2 3\nCHARMOVE 0 4 3\nDO2 0\nMESSAGE 1 COM0 0 COM0 대사|둘째줄\nGETGENERAL 101\nEND",100,1);
        script.Advance(0);Check(script.Snapshot().actors.Single().job==100&&!script.Snapshot().waitingMessage,"scene actor resolves only own commander");
        script.Advance(80);Check(script.Snapshot().actors.Single().renderX==2.5&&script.Snapshot().actors.Single().direction==2,"quantized motion and right-facing row");
        script.Advance(1000);Check(script.Snapshot().waitingMessage&&script.Snapshot().message=="대사|둘째줄"&&script.Snapshot().face==100,"DO2 waits for move before source dialogue");
        script.Continue();Check(!script.Snapshot().completed&&script.Snapshot().heldCommand=="GETGENERAL 101","server reward command is not skipped/auto-granted");
        script.Continue();Check(!script.Snapshot().completed,"continue cannot bypass held authority command");
        var effects=new List<string>();script=new CampaignPresentation("PLAYMIDI bgm/test.mid 1\nCHARPOS 401 400 2 3\nATTACK 400 401 1\nMAGIC 1 400 -1 -1 401\nKILLCHAR2 400\nEND",100,1);script.Effect+=c=>effects.Add(c.name);script.Advance(0);
        Check(effects.SequenceEqual(new[]{"PLAYMIDI","ATTACK","MAGIC"})&&script.Snapshot().completed,"initial presentation calls are delivered in order");
        Check(script.Snapshot().actors.Single().dead,"native death state separate from clear/reward");
        script=new CampaignPresentation("LOADSCENE 2\nEND",100,1);script.Advance(0);Check(script.Snapshot().heldCommand=="LOADSCENE 2","unapproved map transition stays held");
        script=new CampaignPresentation("CHARPOS COM1 1 2 3\nEND",100,1);script.Advance(0);Check(script.Snapshot().actors.Single().job==-1,"other commanders not fabricated from local job");
        script=new CampaignPresentation("CHARPOS 401 400 2 3\nCHARDIR 400 RIGHT\nCHARMOVE 400 2 3\nDO2 400\nEND",100,1);script.Advance(0);Check(script.Snapshot().completed&&script.Snapshot().actors.Single().direction==2,"zero-distance movement neither stalls DO2 nor changes facing");
        var sourceBase=Path.Combine(root,"Assets/_Original/SourceData/Native/map");int rendered=0;var intro=new List<object>();
        foreach(var map in catalog.maps)
        {
            var bytes=File.ReadAllBytes(Path.Combine(sourceBase,map.key+".mrc.bytes"));
            var layers=NativeMapDecoder.RenderLayers(bytes,id=>File.ReadAllBytes(Path.Combine(sourceBase,"mdsr"+id+".grs.bytes")));
            Check(layers.width==map.world.map.width*32&&layers.height==map.world.map.height*32,"native render layers "+map.key);rendered++;
            if(map.key=="01"||map.key=="26"||map.key=="1010")
            {
                var composite=(byte[])layers.rgba.Clone();
                foreach(var layer in layers.foreground)for(int y=0;y<layer.height;y++)for(int x=0;x<layer.width;x++)
                {int tx=layer.x+x,ty=layer.y+y,at=(y*layer.width+x)*4;if(tx<0||ty<0||tx>=layers.width||ty>=layers.height||layer.rgba[at+3]==0)continue;Buffer.BlockCopy(layer.rgba,at,composite,(ty*layers.width+tx)*4,4);}
                var flat=NativeMapDecoder.Render(bytes,id=>File.ReadAllBytes(Path.Combine(sourceBase,"mdsr"+id+".grs.bytes")));
                Check(composite.SequenceEqual(flat.rgba),"body insertion preserves foreground file order "+map.key);
            }
            if(!string.IsNullOrWhiteSpace(map.intro))
            {
                var run=new CampaignPresentation(map.intro,100,map.id,id=>world.Map(id)!=null);int ticks=0;
                do{run.Advance(100);if(run.Snapshot().waitingMessage)run.Continue();ticks++;}while(!run.Snapshot().completed&&string.IsNullOrEmpty(run.Snapshot().heldCommand)&&ticks<20000);
                Check(ticks<20000,"intro is finite or explicitly held "+map.key);
                intro.Add(new{map.key,run.Snapshot().completed,run.Snapshot().heldCommand});
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,maps=126,worldMaps=29,scenarios=97,originalUnits=610,rendered,productionConnections=0,intro,unityPlay=false,scope="Source resource, managed state/held-authority, script and layer rendering checks. Test route exists only in isolated fixture."}));
    }
}
