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
internal static partial class ScenarioProgressChecks
{
    static string root;static int assertions,entries,endings,returns,events,mutations;
    static int recruitEndingCases,recruitEventCases,recruitAssertions;
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
        public Fixture(int map,string intro=null,string outro=null,string eventSource=null,WorldPoint entryPoint=null)
        {
            catalog=Catalog();var m=catalog.Map(map);if(intro!=null)m.intro=intro;if(outro!=null)m.outro=outro;if(eventSource!=null)m.events=eventSource;
            var entry=m.entries.FirstOrDefault(e=>e.unit==0)??new CampaignEntry{x=0,y=0};
            if(entryPoint!=null)entry=new CampaignEntry{x=entryPoint.x,y=entryPoint.y};
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
    static void NativeImmediateEvents()
    {
        const string script="EVENT 1\nDO 500\nMESSAGE 1 COM0 0 COM0 chest\nGOLD 100\nUSEITEM 398\nGETGENERAL 100\nCHANGESPRITE 5 15 0 1 0 22\nEND\nCHANGETILE2 0 0 0 255 0 3 22\nEVENT 2\nCHANGETILE2 0 0 0 255 0 3 22\nEND";
        var f=new Fixture(1,"END","END",script);f.Finish();
        NativeBattleNotice Input(params int[] events)=>new NativeBattleNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=0,
            packet=new byte[]{0x5c,0x15,(byte)events.Length,0}.Concat(events.SelectMany(n=>new byte[]{(byte)n,(byte)(n>>8)})).ToArray()};
        var notice=Input(1,2);string before=Snapshot(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattlePacket(notice).Failure==WorldFailure.StorageUnavailable&&Snapshot(f.View.record)==before,"native15 batch save failure leaves no partial map mutation or event");
        f.store.fail=false;Check(f.server.ReceiveBattlePacket(notice).Accepted,"native15 common receipt executes a synchronous map broadcast");
        Check(f.View.record.campaignProgress.events.Length==2&&f.View.record.campaignProgress.events.All(e=>e.completed)&&f.View.presentation.completed,"native15 completes both events without an interactive dialogue or delay");
        Check(f.server.PendingCampaignRequests().Length==0&&!f.View.record.goldKnown&&f.View.record.inventory.Length==0&&f.View.record.campaignPhase=="explore","native15 subject=-1 never sends owner grant/consume/clear requests");
        Check(f.View.record.campaignProgress.steps.Count(s=>s.command.StartsWith("CHANGESPRITE"))==1&&f.View.record.campaignProgress.steps.Count(s=>s.command.StartsWith("CHANGETILE2"))==1,"native15 respects END and applies only map mutations before it");
        var changed=f.catalog.ApplyChanges(1,f.View.record.campaignProgress.steps);Check(changed.attributes[15*f.catalog.Map(1).world.map.width+5]==22&&changed.attributes[0]==22,"source-backed native15 geometry changes are visible together");
        before=Snapshot(f.View.record);Check(f.server.ReceiveBattlePacket(notice).Failure==WorldFailure.StaleRequest&&Snapshot(f.View.record)==before,"native15 duplicate receipt cannot append/reapply mutations");
        f.Reload();Check(Snapshot(f.View.record)==before&&f.View.presentation==null,"native15 completed batch and map mutations survive actual service reconnect without starting its dialogue");
        var unknown=Input(99);Check(f.server.ReceiveBattlePacket(unknown).Accepted&&f.server.PendingCampaignRequests().Length==0,"native15 absent event flag is a safe no-op");
        foreach(string invalid in new[]{"owner","session","revision","approval","truncated","extra"})
        {
            var bad=Input(1);if(invalid=="owner")bad.character=Id();if(invalid=="session")bad.session=Id();if(invalid=="revision")bad.expectedRevision++;
            if(invalid=="approval")bad.approved=false;if(invalid=="truncated")bad.packet=bad.packet.Take(5).ToArray();if(invalid=="extra")bad.packet=bad.packet.Concat(new byte[]{0}).ToArray();
            before=Snapshot(f.View.record);Check(!f.server.ReceiveBattlePacket(bad).Accepted&&Snapshot(f.View.record)==before,"native15 bad envelope/length is atomic: "+invalid);
        }
        var blocked=new Fixture(1,"END","END","EVENT 1\nCHANGESPRITE 5 15 0 1 0 22\nLOADSCENE 2\nEND");blocked.Finish();
        var deferred=new NativeBattleNotice{approved=true,id=Id(),character=blocked.View.characterId,session=blocked.View.campaignSession,expectedRevision=0,packet=new byte[]{0x5c,0x15,1,0,1,0}};
        before=Snapshot(blocked.View.record);Check(blocked.server.ReceiveBattlePacket(deferred).Failure==WorldFailure.RulesUnavailable&&Snapshot(blocked.View.record)==before,"unproved pre-ownership transition defers the entire15 batch, including earlier map commands");
    }
    static void RecruitData(Fixture f)
    {
        f.definition.growthCatalog=Read<CharacterGrowthCatalog>("Assets/_Original/Data/Growth/CharacterGrowth.json");
        f.definition.combatCatalog=Read<OriginalCombatCatalog>("Assets/_Original/Data/Combat/OriginalCombat.json");
    }
    static Fixture RecruitFixture(string script="GETGENERAL 102\nEND")
    {var f=new Fixture(2,"END",script);RecruitData(f);f.Finish();f.Clear();f.Finish();return f;}
    static void LocalScenarioRecruits()
    {
        int previousAssertions=assertions;
        // Default manager, not a provider that merely returns true. Use each
        // actual ending body with an explicit approved-clear fixture.
        foreach(var map in Catalog().maps.Where(m=>m.kind=="scenario"&&CampaignCommand.Parse(m.outro).Any(c=>c.name=="GETGENERAL")))
        {
            var f=new Fixture(map.id);RecruitData(f);f.Finish();f.Clear();f.Finish();
            recruitEndingCases++;
            var pending=f.server.PendingCampaignRequests().Where(r=>r.packet[0]==0x3c).ToArray();
            Check(pending.Length>0,"actual ending emits a general request "+map.key);
            var manager=new LocalServerManager(f.server);int notices=0;manager.Notify_Commanders+=_=>notices++;
            manager.TestAdvance(0);
            foreach(var request in pending)
            {
                int job=request.packet[1]|request.packet[2]<<8;
                Check(f.View.record.campaignRecruits.Any(r=>r.map==map.id&&r.job==job),"source ending request receives a durable local receipt "+map.key);
                Check(job==100?f.View.record.roster==null:f.View.record.roster.rows.Any(r=>r.job==job),"source job is owned, with initial fighter excluded "+map.key);
            }
            Check(f.server.PendingCampaignRequests().All(r=>r.packet[0]!=0x3c)&&notices>0,"default manager acknowledges source general requests "+map.key);
        }
        foreach(var map in Catalog().maps.Where(m=>m.kind=="scenario"&&!CampaignCommand.Parse(m.outro).Any(c=>c.name=="GETGENERAL")))
        {
            var evt=OriginalCampaignScript.Events(map.events).FirstOrDefault(e=>e.Value.Any(c=>c.name=="GETGENERAL"));
            if(evt.Value==null)continue;
            var f=new Fixture(map.id);RecruitData(f);f.Finish();Check(f.server.ReceiveCampaignEvents(f.Event(evt.Key)).Accepted,"trusted typed event fixture "+map.key);f.Finish();
            var requests=f.server.PendingCampaignRequests().Where(r=>r.packet[0]==0x3c).ToArray();Check(requests.Length>0,"actual event-only reward emits GETGENERAL "+map.key);
            new LocalServerManager(f.server).TestAdvance(0);
            Check(requests.All(r=>f.View.record.campaignRecruits.Any(v=>v.map==map.id&&v.job==(r.packet[1]|r.packet[2]<<8))),"event-only requests receive durable rewards "+map.key);
            recruitEventCases++;
        }
        var earned=RecruitFixture();var req=earned.server.PendingCampaignRequests().Single();string before=Snapshot(earned.View.record);
        var malformed=req.Copy();malformed.packet[1]=103;Check(!earned.server.ProcessLocalScenarioRecruit(malformed).Accepted&&Snapshot(earned.View.record)==before,"modified packet cannot grant another job");
        var stale=req.Copy();stale.session=Id();Check(!earned.server.ProcessLocalScenarioRecruit(stale).Accepted&&Snapshot(earned.View.record)==before,"stale recruitment session has no mutation");
        earned.store.fail=true;Check(earned.server.ProcessLocalScenarioRecruit(req).Failure==WorldFailure.StorageUnavailable&&Snapshot(earned.View.record)==before,"failed save grants neither commander nor receipt nor acknowledgement");
        earned.store.fail=false;Check(earned.server.ProcessLocalScenarioRecruit(req).Accepted,"same request retries after storage recovery");
        var state=earned.View.record;var member=state.roster.rows[1];var source=earned.definition.growthCatalog.Row(102,1);var stats=earned.definition.combatCatalog.Job(102).baseStats;
        Check(state.roster.rows.Length==2&&member.job==102&&member.deployment==2&&member.profile.hp==stats.hp&&member.profile.mp==stats.mp,"new cleric uses source base stats and starts outside the deployment selection");
        Check(member.profile.growth.magic.SequenceEqual(source.magic.Distinct())&&member.profile.growth.skills.SequenceEqual(source.skills.Distinct()),"new commander receives the source first-level abilities");
        Check(earned.server.PendingCampaignRequests().Length==0&&state.campaignRecruits.Single().created,"ownership and request acknowledgement commit together");
        before=Snapshot(state);Check(!earned.server.ProcessLocalScenarioRecruit(req).Accepted&&Snapshot(earned.View.record)==before,"duplicate delivery cannot grant twice");
        var copy=earned.View;copy.record.campaignRecruits[0].job=103;Check(earned.View.record.campaignRecruits[0].job==102,"receipt snapshots cannot mutate service state");
        string temp=Path.Combine(Path.GetTempPath(),"ds-recruit-file-"+Id());
        try
        {
            var disk=new LocalAccountFileStore(Path.Combine(temp,"accounts.json"));
            Check(disk.TryWrite(earned.store.data)&&disk.TryRead(out var restored)&&Snapshot(restored.accounts[0].character.world)==Snapshot(earned.View.record),"DataContract file preserves receipt, secondary profile and abilities");
        }
        finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}
        earned.Reload();Check(earned.View.record.roster.rows.Length==2&&earned.View.record.campaignRecruits.Length==1,"reconnect preserves the earned commander");
        earned.Return();
        Check(earned.server.ExecuteCommanderOperation(CommanderOperation.Swap,1,0,Id(),null).Accepted&&earned.View.record.growth.job==102
            &&earned.View.record.hp==stats.hp&&earned.View.record.growth.magic.SequenceEqual(source.magic.Distinct()),"earned profile can become the playable commander through the existing swap");

        var replay=RecruitFixture("GETGENERAL 102\nGETGENERAL 102\nEND");var pump=new LocalServerManager(replay.server);pump.TestAdvance(0);
        Check(replay.View.record.roster.rows.Length==2&&replay.View.record.campaignRecruits.Length==1&&replay.server.PendingCampaignRequests().Length==0,"separate requests for the same map/job grant once");
        var changed=replay.View.record.roster.Copy();changed.rows[1].job=106;changed.rows[1].profile.growth.job=106;
        Check(replay.server.ReceiveCommanderRoster(new CommanderRosterPush{approved=true,id=Id(),character=replay.View.characterId,session=replay.View.commanderSession,expectedRevision=changed.revision,state=changed}).Accepted,"authoritative class change fixture retains the earned identity");
        replay.Return();Check(replay.server.EnterCampaignMap(2).Accepted,"reenter the same reward map");replay.Finish();replay.Clear();replay.Finish();
        pump=new LocalServerManager(replay.server);pump.TestAdvance(0);
        Check(replay.View.record.roster.rows.Length==2&&replay.View.record.roster.rows[1].job==106&&replay.View.record.campaignRecruits.Length==1,"persistent map/job receipt prevents farming after the earned class changes");

        var initial=RecruitFixture("GETGENERAL 100\nEND");new LocalServerManager(initial.server).TestAdvance(0);
        Check(initial.View.record.roster==null&&initial.View.record.campaignRecruits.Single().created==false&&initial.server.PendingCampaignRequests().Length==0,"initial character job is acknowledged without duplicating it");
        var advanced=RecruitFixture("GETGENERAL 100\nEND");var saved=advanced.store.data.accounts[0].character.world;
        saved.level=1;saved.growth=new CharacterGrowthRecord{job=104,classLevel=1,totalLevel=1,revision=1,lastNotice=Id()};advanced.Reload();
        new LocalServerManager(advanced.server).TestAdvance(0);
        Check(advanced.View.record.growth.job==104&&advanced.View.record.roster==null&&!advanced.View.record.campaignRecruits.Single().created,"initial job exclusion survives a later commander class change");
        var owned=RecruitFixture("GETGENERAL 102\nGETGENERAL 103\nGETGENERAL 102\nEND");new LocalServerManager(owned.server).TestAdvance(0);
        Check(owned.View.record.roster.rows.Length==3&&owned.View.record.roster.rows.Select(r=>r.job).SequenceEqual(new[]{100,102,103}),"different source jobs can be granted without resetting existing profiles");

        var full=RecruitFixture();var view=full.View;var rows=new[]{100,101,103,104,105,106,107,108,109,110}.Select((job,i)=>new OwnedCommander{id=i==0?view.characterId:Id(),job=job,nativeRecord=i}).ToArray();
        Check(full.server.ReceiveCommanderRoster(new CommanderRosterPush{approved=true,id=Id(),character=view.characterId,session=view.commanderSession,expectedRevision=0,state=new CommanderRoster{rows=rows}}).Accepted,"explicit full ten-slot roster fixture");
        req=full.server.PendingCampaignRequests().Single();before=Snapshot(full.View.record);
        Check(full.server.ProcessLocalScenarioRecruit(req).Failure==WorldFailure.CommanderRosterFull&&Snapshot(full.View.record)==before&&full.server.PendingCampaignRequests().Length==1,"full slots retain an unacknowledged reward without overwriting a commander");
        var fullManager=new LocalServerManager(full.server);fullManager.TestAdvance(0);Check(full.server.PendingCampaignRequests().Length==1,"manager retains a blocked full-slot reward");
        var reduced=full.View.record.roster.Copy();reduced.rows=reduced.rows.Take(9).ToArray();
        Check(full.server.ReceiveCommanderRoster(new CommanderRosterPush{approved=true,id=Id(),character=full.View.characterId,session=full.View.commanderSession,expectedRevision=reduced.revision,state=reduced}).Accepted,"free one slot using a trusted roster update");
        fullManager.TestAdvance(0);Check(full.View.record.roster.rows.Length==10&&full.View.record.roster.rows.Last().job==102&&full.server.PendingCampaignRequests().Length==0,"manager automatically retries the pending reward when the roster changes");

        var waiting=RecruitFixture();waiting.Return();req=waiting.server.PendingCampaignRequests().Single();
        Check(waiting.server.RequestCommanderOperation(CommanderOperation.State,0,0,Id()).Accepted&&waiting.View.commanderRequest!=null,"outstanding roster query before a delayed reward fixture");
        Check(waiting.server.ProcessLocalScenarioRecruit(req).Accepted&&waiting.View.commanderRequest==null,"new roster ownership invalidates the outstanding old roster query");
        var observed=RecruitFixture();var observerManager=new LocalServerManager(observed.server);int refused=0;
        observerManager.Notify_World+=r=>{if(!r.Accepted)refused++;};observerManager.Notify_ScenarioScriptRequest+=r=>observerManager.Request_ProcessScenarioRecruit(r);
        observerManager.TestAdvance(0);Check(observed.View.record.roster.rows.Length==2&&observed.server.PendingCampaignRequests().Length==0&&refused==0,"an observer-handled grant is not processed twice or reported as a stale error");

        var interrupted=RecruitFixture();var manager2=new LocalServerManager(interrupted.server);int late=0;
        manager2.Notify_World+=_=>interrupted.client.Logout();manager2.Notify_Commanders+=_=>late++;
        Check(manager2.Request_ProcessScenarioRecruit(interrupted.server.PendingCampaignRequests().Single()).Accepted&&late==0,"logout during world callback suppresses the stale commander notification");
        interrupted.Reload();Check(interrupted.View.record.roster.rows.Length==2,"callback interruption cannot roll back a committed recruit");

        var native=new Fixture(2,"END","GETGENERAL 102\nEND");RecruitData(native);native.Finish();var nativeView=native.View;
        Check(native.server.ReceiveBattlePacket(new NativeBattleNotice{approved=true,id=Id(),character=nativeView.characterId,session=nativeView.campaignSession,expectedRevision=0,
            packet=OriginalResultPackets.Packet(new NativeResultReport{outcome=1,rows=Array.Empty<NativeResultRow>()})}).Accepted,"native victory display table fixture");
        Check(native.server.ReceiveBattlePacket(new NativeBattleNotice{approved=true,id=Id(),character=nativeView.characterId,session=nativeView.campaignSession,expectedRevision=0,packet=OriginalScenarioEnd.Packet(1)}).Accepted,"native fast end starts the source outro");
        var nativeManager=new LocalServerManager(native.server);nativeManager.TestNativeAdvance(0);
        Check(native.View.record.map==1000&&native.View.record.roster.rows[1].job==102&&native.server.PendingCampaignRequests().Length==0,
            "native outro return before the manager pump still grants and acknowledges the persisted source request");
        Check(native.View.record.campaignRecruits.Single().map==2,"post-return grant records the source scenario, not the town");
        var raced=RecruitFixture();var secondService=new EntryAccountService(raced.store);var secondClient=new EntryClient(secondService);secondClient.OpenLocalProfile();
        Check(secondService.BeginWorld(raced.definition).Accepted,"concurrent service reads the same pending reward");
        var secondRequest=secondService.PendingCampaignRequests().Single();
        Check(raced.server.ProcessLocalScenarioRecruit(raced.server.PendingCampaignRequests().Single()).Accepted,"first service commits the reward");
        Check(secondService.ProcessLocalScenarioRecruit(secondRequest).Failure==WorldFailure.StaleRequest
            &&raced.store.data.accounts[0].character.world.roster.rows.Length==2&&raced.store.data.accounts[0].character.world.campaignRecruits.Length==1,
            "stale concurrent service cannot duplicate or overwrite the winning receipt");
        recruitAssertions=assertions-previousAssertions;
    }
    static void Main(string[] args)
    {
        root=args[0];NativeImmediateEvents();LocalScenarioRecruits();Check(OriginalCampaignScript.Packet(new CampaignCommand("GETGENERAL 101",1)).SequenceEqual(new byte[]{0x3c,101,0}),"native general bytes");
        Check(OriginalCampaignScript.Packet(new CampaignCommand("GOLD -2",1)).SequenceEqual(new byte[]{0x3a,254,255,255,255}),"native DWORD bytes");
        Check(OriginalCampaignScript.Packet(new CampaignCommand("USEITEM 398",1)).SequenceEqual(new byte[]{0x49,142,1}),"native item bytes");
        Check(OriginalCampaignScript.Packet(new CampaignCommand("SCENECLEAR ALONE",1)).SequenceEqual(new byte[]{0x5c,0x20,2}),"native clear bytes");
        foreach(int threshold in new[]{-1,0,1,50,100,101})foreach(uint seed in new uint[]{0,1,7,2147483647,uint.MaxValue})
        {uint state=unchecked(seed*214013+2531011);Check(OriginalCampaignScript.Random(threshold,seed)==(((state>>16&32767)%101)<threshold?1:0),"native CRT threshold");}
        var bodies=OriginalCampaignScript.Events("EVENT 1\nCOMMANDER\nONCE\n1 CHARPOS 6 15\n1. skip\nMESSAGE 0 0 ME 0 test\nEND\nEVENT 100\nGOLD 999\nEVENT 99\nEND");
        Check(bodies.Count==2&&bodies[1].Length==5&&bodies[1].Any(c=>c.raw=="1. skip")&&bodies[1].All(c=>!c.raw.StartsWith("1 CHARPOS")),"native loader excludes full decimal conditions but keeps numeric-prefix unknown lines");
        Check(OriginalCampaignScript.Events("EVENT 0\nGOLD 999\nEND")[0].Single().name=="END","native event0 does not collect action lines");
        Check(!OriginalCampaignScript.Events("EVENT 1\nGOLD 1\n\nEND").ContainsKey(1)&&!OriginalCampaignScript.Events("EVENT 1\nGOLD 1\n999\nEND").ContainsKey(1),"native loader disables unfinished events at blank/999");
        Check(OriginalCampaignScript.Events("EVENT 1\n"+string.Join("\n",Enumerable.Repeat("ALL",105))+"\nEND")[1].Length==100,"native loader has99 action slots plus END");
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
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,recruitEndingCases,recruitEventCases,recruitAssertions,scenarioEntries=entries,sourceOutroCompletions=endings,approvedResultReturns=returns,eventBodies=events,mapMutationFixtures=mutations,held,scope="Approved-clear and typed-event local recruit fixtures; no actual combat policy or Unity playback."}));
    }
}
