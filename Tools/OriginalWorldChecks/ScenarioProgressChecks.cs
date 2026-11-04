using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;
using DarkSaver.Original.Maps;
using DarkSaver.Original.Tutorial;
internal static class ScenarioProgressChecks
{
    static string root;static int assertions,entries,endings,returns,events,mutations;
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
    static string Id()=>Guid.NewGuid().ToString("N");
    static T Read<T>(string relative)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,relative)),Json);
    static string Snapshot(WorldRecord record)=>JsonSerializer.Serialize(record,Json);
    sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    static CampaignDefinition Catalog()
    {
        var c=Read<CampaignDefinition>("Assets/_Original/Data/Campaign/CampaignCatalog.json");
        using var report=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json")));
        var files=report.RootElement.GetProperty("scripts").EnumerateArray().Select(e=>e.GetString()).ToArray();
        string Text(int i)=>i<0?"":File.ReadAllText(Path.Combine(root,files[i]));
        foreach(var m in c.maps){m.intro=Text(m.introIndex);m.outro=Text(m.outroIndex);m.events=Text(m.eventIndex);}
        c.ConfigureNativeMaps((id,steps)=>
        {
            var map=c.Map(id);string folder=Path.Combine(root,"Assets/_Original/SourceData",map.world.sourceVersion=="Saver_1.2"?"KaosiaLegacy":"Native/map");
            var changes=steps.Where(s=>s.scene==id).Select(s=>new CampaignCommand(s.command,0)).Where(x=>x.name=="CHANGESPRITE"||x.name=="CHANGETILE2")
                .Select(x=>new NativeMapDecoder.MapChange{name=x.name,args=x.args.Select((v,i)=>x.Int(i)).ToArray()}).ToArray();
            var r=NativeMapDecoder.ApplyChanges(File.ReadAllBytes(Path.Combine(folder,map.key+".mrc.bytes")),rid=>File.ReadAllBytes(Path.Combine(folder,"mdsr"+rid+".grs.bytes")),changes,map.world.map.attributes);
            return new CampaignChangedMap{bytes=r.bytes,attributes=r.attributes,renderAttributes=r.renderAttributes};
        });return c;
    }
    sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public EntryClient client;public WorldDefinition definition;public CampaignDefinition catalog;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(int map,string intro=null,string outro=null,string eventSource=null)
        {
            catalog=Catalog();var m=catalog.Map(map);if(intro!=null)m.intro=intro;if(outro!=null)m.outro=outro;if(eventSource!=null)m.events=eventSource;
            var entry=m.entries.FirstOrDefault(e=>e.unit==0)??new CampaignEntry{x=0,y=0};
            catalog.connections=new[]{new CampaignConnection{fromMap=1000,toMap=map,x=entry.x,y=entry.y,evidence="ISOLATED TEST ROUTE",cells=new[]{new WorldPoint{x=21,y=21}}}};
            definition=catalog.Merge(Read<WorldDefinition>("Assets/_Original/Data/World/KaosiaWorld.json"));
            server=new EntryAccountService(store);client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"진행검사");
            store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"world begins");
            Check(server.EnterCampaignMap(map).Accepted,"fixture entry "+map);entries++;
        }
        public void Reload(){client.Logout();server=new EntryAccountService(store);client=new EntryClient(server);client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"reload");}
        public string Finish()
        {
            for(int i=0;i<20000;i++)
            {
                server.AdvanceWorld(200);var p=View.presentation;
                if(p==null||p.completed)return null;
                if(!string.IsNullOrEmpty(p.heldCommand))return p.heldCommand;
                if(p.waitingMessage)Check(server.ContinueCampaignPresentation().Accepted,"dialogue continue");
            }
            throw new Exception("presentation did not finish");
        }
        public void Clear(){Check(server.ReceiveCampaignClear(new CampaignClearNotice{approved=true,map=View.record.map,session=View.campaignSession,id=Id()}).Accepted,"approved clear");}
        public CampaignEventsNotice Event(int n)=>new CampaignEventsNotice{approved=true,id=Id(),character=View.characterId,session=View.campaignSession,visit=View.record.scenarioVisit.id,map=View.record.map,packet=new byte[]{0x5c,0x15,1,0,(byte)n,0}};
        public void Return()
        {
            var v=View;var id=Id();Check(server.ReceiveScenarioResult(new CampaignResultNotice{approved=true,character=v.characterId,session=v.campaignSession,result=new CampaignResultRecord{id=id,visit=v.record.scenarioVisit.id,map=v.record.map,victory=true,allies=Array.Empty<CampaignResultUnit>(),enemies=Array.Empty<CampaignResultUnit>()}}).Accepted,"approved display report");
            Check(server.ConfirmScenarioResult(id,Id()).Accepted,"result confirm");returns++;
            Check(View.record.map==1000&&View.record.x==21&&View.record.y==21&&View.record.scenarioVisit==null&&View.record.campaignProgress==null,"exact origin return and visit cleanup");
            Check(!View.record.goldKnown&&View.record.inventory.Length==0,"no invented grants");Reload();Check(View.record.map==1000&&View.record.lastScenarioResult.id==id,"result and origin reconnect");
        }
    }
    static void Main(string[] args)
    {
        root=args[0];Check(OriginalCampaignScript.Packet(new CampaignCommand("GETGENERAL 101",1)).SequenceEqual(new byte[]{0x3c,101,0}),"native general bytes");
        Check(OriginalCampaignScript.Packet(new CampaignCommand("GOLD -2",1)).SequenceEqual(new byte[]{0x3a,254,255,255,255}),"native DWORD bytes");
        Check(OriginalCampaignScript.Packet(new CampaignCommand("USEITEM 398",1)).SequenceEqual(new byte[]{0x49,142,1}),"native item bytes");
        Check(OriginalCampaignScript.Packet(new CampaignCommand("SCENECLEAR ALONE",1)).SequenceEqual(new byte[]{0x5c,0x20,2}),"native clear bytes");
        foreach(int threshold in new[]{-1,0,1,50,100,101})foreach(uint seed in new uint[]{0,1,7,2147483647,uint.MaxValue})
        {uint state=unchecked(seed*214013+2531011);Check(OriginalCampaignScript.Random(threshold,seed)==(((state>>16&32767)%101)<threshold?1:0),"native CRT threshold");}
        var bodies=OriginalCampaignScript.Events("EVENT 0\nCOMMANDER\nONCE\n1 CHARPOS 6 15\n1. skip\nMESSAGE 0 0 ME 0 test\nEND\nEVENT 100\nGOLD 999\nEVENT 99\nEND");
        Check(bodies.Count==2&&bodies[0].Length==4&&bodies[0].All(c=>!c.raw.StartsWith("1")),"native client body excludes positive conditions");
        Check(!OriginalCampaignScript.DecodeEvents(new byte[]{0x5c,0x15,1,0,0},out _)&&!OriginalCampaignScript.DecodeEvents(new byte[]{0x5c,0x15,1,0,100,0},out _),"truncated/out-of-range events denied");
        var branch=new CampaignPresentation("TEST\n(\nSETPOSITION 2 3\n)\nRANDOM 50\nTEST\n(\nSETPOSITION 4 5\n)\nTESTNOT\n(\nSETPOSITION 6 7\n)\nEND",100,1,null,c=>0);
        branch.Advance(0);Check(branch.Snapshot().completed&&branch.Snapshot().cameraX==6,"native initial result1 and branch flags");
        var catalog=Catalog();var held=new List<object>();
        foreach(var map in catalog.maps.Where(m=>m.kind=="scenario"))
        {
            events+=OriginalCampaignScript.Events(map.events).Count;
            var f=new Fixture(map.id);Check(f.Finish()==null&&f.View.record.campaignPhase=="explore","source intro reaches explore "+map.key);
            f.Clear();var pending=f.Finish();
            if(pending!=null){held.Add(new{map=map.id,command=pending});continue;}
            endings++;Check(f.View.record.campaignPhase=="complete","source outro reaches complete "+map.key);f.Return();
        }
        Check(held.Count==0&&endings==97,"all native source outros finish");
        var combine=new CampaignCombineAnimation(new CampaignCommand("COMBINE_CLASS 1 320 96 100 50",1),new[]{new CampaignCommand("155 320 384",2)});
        combine.Advance(50);Check(combine.Snapshot().counter==0,"combine strict interval boundary");combine.Advance(1);Check(combine.Snapshot().counter==1&&combine.Snapshot().bodies[0].renderY==382,"combine native integer interpolation");
        for(int i=0;i<100;i++)combine.Advance(51);Check(combine.Snapshot().phase==1,"combine101 ticks then hold");
        combine.Advance(2999);Check(combine.Snapshot().phase==1,"combine 2999 tick hold");combine.Advance(1);Check(combine.Snapshot().phase==2&&combine.Snapshot().alpha==0,"combine 3000 transition");
        for(int i=0;i<31;i++)combine.Advance(1);Check(combine.Snapshot().alpha==248&&!combine.Completed,"native flash248 last draw");combine.Advance(1);Check(combine.Completed,"combine unlocks interpreter after final flash");
        var journal=new Fixture(3,"END","FIRSTCLEAR\nTEST\n(\nGETGENERAL 100\nGOLD 399\nUSEITEM 398\nSAVECLEAR\n)\nTESTNOT\n(\nGOLD 1\n)\nMESSAGE 1 COM0 0 COM0 pause\nEND");
        journal.Finish();journal.Clear();Check(journal.View.presentation.waitingMessage&&journal.server.PendingCampaignRequests().Length==3,"one-way requests do not hold ending");
        var requests=journal.server.PendingCampaignRequests();string visit=journal.View.record.scenarioVisit.id;
        Check(journal.View.record.clearedScenarios.SequenceEqual(new[]{3})&&journal.View.record.campaignProgress.steps.First().result==1,"first clear is local durable flag keyed to logical scenario");
        journal.Reload();Check(journal.server.PendingCampaignRequests().Select(r=>r.id).SequenceEqual(requests.Select(r=>r.id))&&journal.View.presentation.waitingMessage,"reconnect reuses frozen branch and request IDs");
        Check(journal.server.AcknowledgeCampaignRequest(requests[0]).Failure==WorldFailure.StaleRequest,"old session ack rejected");
        var current=journal.server.PendingCampaignRequests()[0];journal.store.fail=true;
        Check(journal.server.AcknowledgeCampaignRequest(current).Failure==WorldFailure.StorageUnavailable&&journal.server.PendingCampaignRequests().Length==3,"failed ack durable pending");journal.store.fail=false;
        Check(journal.server.AcknowledgeCampaignRequest(current).Accepted&&journal.server.PendingCampaignRequests().Length==2,"valid ack");
        Check(journal.server.AcknowledgeCampaignRequest(current).Failure==WorldFailure.StaleRequest,"duplicate ack");
        journal.Finish();journal.Return();Check(journal.server.PendingCampaignRequests().Length==2,"unacknowledged requests survive return and reconnect");
        foreach(var delivery in journal.server.PendingCampaignRequests())Check(journal.server.AcknowledgeCampaignRequest(delivery).Accepted,"origin-context delivery can ack from town");
        Check(journal.server.EnterCampaignMap(3).Accepted,"repeat entry");journal.Finish();journal.Clear();
        Check(journal.server.PendingCampaignRequests().Single().command=="GOLD 1"&&!journal.server.PendingCampaignRequests().Single().id.StartsWith(visit+":"),"repeat clear uses alternate branch and new visit context");
        var failure=new Fixture(3,"END","MESSAGE 1 COM0 0 COM0 wait\nFIRSTCLEAR\nSAVECLEAR\nGETGENERAL 100\nEND");failure.Finish();failure.Clear();failure.store.fail=true;failure.server.ContinueCampaignPresentation();
        Check(failure.View.presentation.heldCommand=="FIRSTCLEAR"&&failure.View.record.campaignProgress==null,"failed branch save blocks before side effect");failure.store.fail=false;
        Check(failure.server.RetryCampaignPresentation().Accepted&&failure.View.presentation.completed&&failure.server.PendingCampaignRequests().Length==1,"retry resumes exact held instruction");
        var ev=new Fixture(1,"END","END","EVENT 1\nONCE\n1 CHARPOS 99 99\nMESSAGE 1 COM0 0 COM0 chest\nRANDOM 100\nTEST\n(\nGOLD 100\n)\nCHANGESPRITE 5 15 0 1 0 22\nEND\nEVENT 2\nMESSAGE 1 COM0 0 COM0 second\nEND");
        ev.Finish();var notice=ev.Event(1);Check(ev.server.ReceiveCampaignEvents(notice).Accepted,"approved 5C15 queues event");
        Check(ev.server.MoveWorld(ev.View.record.x+1,ev.View.record.y).Failure==WorldFailure.Busy,"queued event locks movement before first tick");
        string before=Snapshot(ev.View.record);Check(ev.server.ReceiveCampaignEvents(notice).Failure==WorldFailure.StaleRequest&&Snapshot(ev.View.record)==before,"duplicate event refused atomically");
        ev.server.AdvanceWorld(0);Check(ev.View.presentation.message=="chest","native event starts with body not conditions");
        ev.Reload();ev.server.AdvanceWorld(0);Check(ev.View.presentation.message=="chest","unfinished event restarts source body");
        ev.Finish();ev.server.AdvanceWorld(0);Check(ev.View.record.campaignProgress.events.Single().completed&&ev.server.PendingCampaignRequests().Length==1,"event completes with one pending request");
        Check(ev.View.record.campaignProgress.steps.Any(s=>s.command.StartsWith("CHANGESPRITE")),"map change journal");
        var changed=ev.catalog.ApplyChanges(1,ev.View.record.campaignProgress.steps);Check(changed.attributes[15*ev.catalog.Map(1).world.map.width+5]==22,"sprite replacement changes passability");mutations++;
        ev.Reload();Check(ev.catalog.ApplyChanges(1,ev.View.record.campaignProgress.steps).bytes.SequenceEqual(changed.bytes),"map changes reconnect exactly");
        var next=ev.Event(2);ev.store.fail=true;before=Snapshot(ev.View.record);Check(ev.server.ReceiveCampaignEvents(next).Failure==WorldFailure.StorageUnavailable&&Snapshot(ev.View.record)==before,"event receive failure no partial queue");ev.store.fail=false;
        Check(ev.server.ReceiveCampaignEvents(next).Accepted,"event retry after storage recovers");
        var oldServer=ev.server;ev.server=new EntryAccountService(ev.store);ev.client=new EntryClient(ev.server);ev.client.OpenLocalProfile();Check(ev.server.BeginWorld(ev.definition).Accepted,"concurrent fresh service");var newEvent=ev.Event(2);Check(ev.server.ReceiveCampaignEvents(newEvent).Accepted,"fresh session event saved");
        var stale=next;stale.id=Id();Check(oldServer.ReceiveCampaignEvents(stale).Failure==WorldFailure.StaleRequest,"stale service cannot overwrite progress");
        // Mutation operates on private bytes; invalid target coordinates are native no-ops.
        var m=catalog.Map(2);string path=Path.Combine(root,"Assets/_Original/SourceData/Native/map/02.mrc.bytes");var raw=File.ReadAllBytes(path);var copy=(byte[])raw.Clone();
        var tile=NativeMapDecoder.ApplyChanges(raw,id=>File.ReadAllBytes(Path.Combine(root,"Assets/_Original/SourceData/Native/map/mdsr"+id+".grs.bytes")),new[]{new NativeMapDecoder.MapChange{name="CHANGETILE2",args=new[]{0,0,0,255,0,3,22}}},m.world.map.attributes);
        Check(tile.attributes[0]==22&&tile.renderAttributes[0]==22&&raw.SequenceEqual(copy),"native last arguments swapped and immutable source");mutations++;
        var noOp=NativeMapDecoder.ApplyChanges(raw,id=>null,new[]{new NativeMapDecoder.MapChange{name="CHANGESPRITE",args=new[]{-1,-1,0,0,0,0}}},m.world.map.attributes);Check(noOp.bytes.SequenceEqual(raw),"unmatched sprite origin never adds a sprite");
        var fx=new CampaignPresentation("CHARPOS 401 400 2 3\nCHARPOS COM0 0 2 4\nATTACK 400 0 1\nMAGIC 101 0 -1 -1 400\nKILLCHAR1 400\nEND",100,1);
        var captured=new List<CampaignCommand>();fx.Effect+=c=>captured.Add(c);fx.Advance(0);Check(captured.Count==2&&captured.All(c=>c.actors.Any(a=>a.unit==400))&&!fx.Snapshot().actors.Any(a=>a.unit==400),"effect endpoints survive immediate actor removal");
        var playback=new CampaignVisualPlayback();Check(playback.Accept(captured[0],null,null,20,20)&&playback.attacks[0].attacker&&playback.attacks[400].partner==0,"script attack target/source order");
        playback.Advance(2000);Check(playback.attacks.Count==0,"transient attack expires without HP mutation");
        var nav=new Fixture(1,"END","END");nav.Finish();var navMap=nav.catalog.Map(1).world.map;var position=nav.View.record;
        int targetX=position.x+1,targetY=position.y,index=targetY*navMap.width+targetX;
        navMap.attributes=Enumerable.Repeat(3,navMap.width*navMap.height).ToArray();navMap.attributes[position.y*navMap.width+position.x]=0;
        nav.catalog.Map(1).events="EVENT 1\nCHANGETILE2 "+targetX+" "+targetY+" 0 255 0 0 0\nEND";
        Check(nav.server.MoveWorld(targetX,targetY).Failure==WorldFailure.InvalidInput,"isolated wall blocks before map change");
        Check(nav.server.ReceiveCampaignEvents(nav.Event(1)).Accepted,"open fixture wall through approved event");nav.server.AdvanceWorld(0);nav.server.AdvanceWorld(0);
        Check(nav.server.MoveWorld(targetX,targetY).Accepted&&navMap.attributes[index]==3,"effective navigation opens changed cell and immutable shared definition remains blocked");nav.server.AdvanceWorld(200);
        Check(nav.View.record.x==targetX&&nav.View.record.y==targetY,"changed collision leads to durable arrival");mutations++;
        var diskPath=Path.Combine(Path.GetTempPath(),"ds-scenario-progress-checks","isolated-profile.json");var disk=new LocalAccountFileStore(diskPath);
        Check(disk.TryWrite(ev.store.data)&&disk.TryRead(out var restored),"real DataContract file roundtrip");
        disk.TryRead(out var readBack);Check(Snapshot(readBack.accounts[0].character.world)==Snapshot(ev.store.data.accounts[0].character.world),"new progress/delivery fields roundtrip through production serializer");
        var caps=new Fixture(1,"END","END","EVENT 1\nMESSAGE 1 COM0 0 COM0 wait\nEND");caps.Finish();var cap=caps.Event(1);
        cap.packet=new byte[204];cap.packet[0]=0x5c;cap.packet[1]=0x15;cap.packet[2]=100;for(int i=0;i<100;i++)cap.packet[4+i*2]=1;
        Check(caps.server.ReceiveCampaignEvents(cap).Accepted,"native100 pending events accepted");Check(caps.server.ReceiveCampaignEvents(caps.Event(1)).Failure==WorldFailure.Busy,"101st pending event refused before activation");
        caps.server.AdvanceWorld(0);Check(caps.server.ReceiveCampaignEvents(caps.Event(1)).Accepted,"active event has separate slot plus100 pending");
        Check(caps.server.ReceiveCampaignEvents(caps.Event(1)).Failure==WorldFailure.Busy,"native queue remains bounded with active event");
        var menu=Read<MagicMenuCatalog>("Assets/_Original/Data/Combat/MagicMenu.json");var patterns=Read<MagicProjectileCatalog>("Assets/_Original/Data/Combat/MagicProjectiles.json");
        Check(playback.Accept(captured[1],menu,patterns,20,20)&&playback.magic.Count>0&&playback.magic.All(e=>e.targetX==2&&e.targetY==3),"script magic captures endpoints independently of killed actor");
        Check(playback.magic.All(e=>e.sourceX==2&&e.sourceY==4),"script caster location");
        var managerFixture=new Fixture(3,"END","GETGENERAL 100\nEND","EVENT 1\nMESSAGE 1 COM0 0 COM0 clear request\nSCENECLEAR ALONE\nEND");managerFixture.Finish();
        var manager=new LocalServerManager(managerFixture.server);manager.ConfigureCampaign(new NativeCampaignArt{Definition=managerFixture.catalog});
        int initials=0,dispatches=0,deliveries=0,results=0,clears=0;
        manager.ScenarioInitialAuthority=v=>{initials++;return null;};manager.Notify_ScenarioScriptRequest+=r=>dispatches++;
        manager.ScenarioScriptAuthority=(r,v)=>{deliveries++;return true;};
        manager.ScenarioClearAuthority=v=>{clears++;return new CampaignClearNotice{approved=true,id=Id(),map=v.record.map,session=v.campaignSession};};
        manager.ScenarioResultAuthority=v=>{results++;return new CampaignResultNotice{approved=true,character=v.characterId,session=v.campaignSession,result=new CampaignResultRecord{id=Id(),visit=v.record.scenarioVisit.id,map=v.record.map,victory=true,allies=Array.Empty<CampaignResultUnit>(),enemies=Array.Empty<CampaignResultUnit>()}};};
        manager.TestAdvance(0);manager.TestAdvance(0);Check(initials==1,"manager initial provider runs once without fabricated fallback");
        Check(manager.Notify_ScenarioEvents(managerFixture.Event(1)).Accepted,"manager event notification");manager.TestAdvance(0);
        Check(clears==0&&managerFixture.View.presentation.waitingMessage,"clear authority waits for event dialogue");
        manager.Request_ContinueScenario();manager.TestAdvance(0);Check(clears==1&&managerFixture.View.record.campaignPhase=="outro","queued SCENECLEAR requests explicit approval after event completion");
        manager.TestAdvance(0);manager.TestAdvance(0);Check(results==1&&deliveries==1&&managerFixture.View.record.scenarioVisit.result!=null,"manager result/grant delivery providers connect once");
        Check(managerFixture.server.PendingCampaignRequests().Length==0&&dispatches==2,"manager acknowledges both native clear and general requests");
        Check(manager.Request_ConfirmScenarioResult(managerFixture.View.record.scenarioVisit.result.id,Id()).Accepted,"manager result confirm returns");
        manager.TestAdvance(0);Check(managerFixture.View.record.map==1000&&results==1&&clears==1,"town update cannot repeat scenario authority");
        var interrupted=new Fixture(3,"END","END");interrupted.Finish();
        var interruptedManager=new LocalServerManager(interrupted.server);interruptedManager.ConfigureCampaign(new NativeCampaignArt{Definition=interrupted.catalog});
        int staleInitial=0;interruptedManager.ScenarioInitialAuthority=v=>{interruptedManager.isActiveAndEnabled=false;return null;};
        interruptedManager.Notify_ScenarioInitialRequested+=v=>staleInitial++;interruptedManager.TestAdvance(0);
        Check(staleInitial==0,"initial provider disabling manager stops downstream initial notification");
        var interruptedScript=new Fixture(3,"GETGENERAL 100\nEND","END");interruptedScript.Finish();
        var scriptManager=new LocalServerManager(interruptedScript.server);scriptManager.ConfigureCampaign(new NativeCampaignArt{Definition=interruptedScript.catalog});
        int staleAuthority=0;scriptManager.Notify_ScenarioScriptRequest+=r=>scriptManager.isActiveAndEnabled=false;
        scriptManager.ScenarioScriptAuthority=(r,v)=>{staleAuthority++;return true;};scriptManager.TestAdvance(0);
        Check(staleAuthority==0&&interruptedScript.server.PendingCampaignRequests().Length==1,"disabled script callback cannot run authority or acknowledge pending command");
        interruptedManager.isActiveAndEnabled=true;interruptedManager.ScenarioInitialAuthority=v=>null;interruptedManager.TestAdvance(0);
        Check(staleInitial==1,"re-enabled initial request can retry after interrupted dispatch");

        var interruptedEffects=new Fixture(3,"CHARPOS 401 400 2 3\nCHARPOS COM0 0 2 4\nATTACK 400 0 1\nMAGIC 101 0 -1 -1 400\nEND","END");interruptedEffects.Finish();
        var effectManager=new LocalServerManager(interruptedEffects.server);effectManager.ConfigureCampaign(new NativeCampaignArt{Definition=interruptedEffects.catalog});
        int staleEffects=0;effectManager.Notify_ScenarioEffect+=c=>{staleEffects++;effectManager.isActiveAndEnabled=false;};effectManager.TestAdvance(0);
        Check(staleEffects==1,"effect callback disabling manager discards remaining old-context audiovisual commands");
        effectManager.isActiveAndEnabled=true;effectManager.TestAdvance(0);Check(staleEffects==1,"discarded old effects do not replay after re-enable");
        var manualEntry=new Fixture(3,"END","END");manualEntry.Finish();manualEntry.Clear();manualEntry.Finish();manualEntry.Return();
        var entryMap=manualEntry.catalog.Map(3);var originalArrival=manualEntry.catalog.connections.First(c=>c.toMap==3);var sourceEntry=new CampaignEntry{x=originalArrival.x,y=originalArrival.y};
        var authored=new ScenarioEntryPoint{map=3,mapHash=entryMap.hash,enabled=true};
        var free=Enumerable.Range(0,entryMap.world.map.width*entryMap.world.map.height).Select(n=>new ScenarioEntryPoint{map=3,mapHash=entryMap.hash,x=n%entryMap.world.map.width,y=n/entryMap.world.map.width,enabled=true})
            .First(p=>(p.x!=sourceEntry.x||p.y!=sourceEntry.y)&&ScenarioGatewayData.EntryLocationAllowed(manualEntry.definition,p));authored.x=free.x;authored.y=free.y;
        var userEntries=new ScenarioGatewaySettings{entryPoints=new[]{authored}};var editorMaps=Read<GatewayMapCatalog>("Assets/_Original/Data/Campaign/GatewayMaps.json");
        ScenarioGatewayData.Apply(userEntries,editorMaps,manualEntry.definition);
        authored.x=-10;Check(manualEntry.definition.gateways.entryPoints[0].x==free.x,"entry point config is copied on publication");
        var encodedEntries=ScenarioGatewayData.Encode(manualEntry.definition.gateways);var decodedEntries=ScenarioGatewayData.Decode(encodedEntries);
        Check(decodedEntries.entryPoints.Length==1&&decodedEntries.entryPoints[0].x==free.x,"manual entry survives actual settings serialization");
        var badEntry=decodedEntries.Copy();badEntry.entryPoints[0].mapHash="old-version";
        bool badVersion=false;try{ScenarioGatewayData.Validate(badEntry,editorMaps);}catch(InvalidDataException){badVersion=true;}Check(badVersion,"manual point stale map version rejected");
        badEntry=decodedEntries.Copy();badEntry.entryPoints[0].x=entryMap.world.map.width;
        bool badBounds=false;try{ScenarioGatewayData.Validate(badEntry,editorMaps);}catch(InvalidDataException){badBounds=true;}Check(badBounds,"manual point out of bounds rejected");
        badEntry=decodedEntries.Copy();badEntry.entryPoints=new[]{badEntry.entryPoints[0],badEntry.entryPoints[0].Copy()};
        bool badDuplicate=false;try{ScenarioGatewayData.Validate(badEntry,editorMaps);}catch(InvalidDataException){badDuplicate=true;}Check(badDuplicate,"duplicate manual map entries rejected");
        int cellIndex=free.y*entryMap.world.map.width+free.x,oldAttribute=entryMap.world.map.attributes[cellIndex];entryMap.world.map.attributes[cellIndex]=27;
        Check(!ScenarioGatewayData.EntryLocationAllowed(manualEntry.definition,free),"blocked manual entry location rejected");entryMap.world.map.attributes[cellIndex]=oldAttribute;
        var beforeEntry=Snapshot(manualEntry.View.record);manualEntry.store.fail=true;
        Check(manualEntry.server.EnterCampaignMap(3).Failure==WorldFailure.StorageUnavailable&&Snapshot(manualEntry.View.record)==beforeEntry,"manual point entry save failure preserves source record");manualEntry.store.fail=false;
        Check(manualEntry.server.EnterCampaignMap(3).Accepted&&manualEntry.View.record.x==free.x&&manualEntry.View.record.y==free.y,"actual scenario entry uses manual point before presentation");
        Check(manualEntry.View.record.scenarioVisit.x==21&&manualEntry.View.record.scenarioVisit.y==21,"manual destination never changes return origin");
        manualEntry.Finish();manualEntry.Clear();manualEntry.Finish();manualEntry.Return();
        decodedEntries.entryPoints[0].enabled=false;ScenarioGatewayData.Apply(decodedEntries,editorMaps,manualEntry.definition);
        Check(manualEntry.server.EnterCampaignMap(3).Accepted&&manualEntry.View.record.x==sourceEntry.x&&manualEntry.View.record.y==sourceEntry.y,"disabled manual point restores original connection arrival");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,scenarioEntries=entries,sourceOutroCompletions=endings,approvedResultReturns=returns,eventBodies=events,mapMutationFixtures=mutations,held,scope="Approved-clear fixtures only; no actual combat policy or Unity playback."}));
    }
}
