using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;

internal static class DirectedEventEntry { public static void Main(string[] args)=>ScenarioProgressChecks.RunDirectedChecks(args[0]); }
#if VICTORY_MANAGER
internal static class VictoryManagerEntry { public static void Main(string[] args)=>ScenarioProgressChecks.RunVictoryManagerChecks(args[0]); }
#endif
internal static partial class ScenarioProgressChecks
{
    static Fixture Directed(string script,int map=1)
    {
        var mapData=Catalog().Map(map).world;var combat=Read<OriginalCombatCatalog>("Assets/_Original/Data/Combat/OriginalCombat.json");
        int kind=OriginalMemberCommands.MovementType(new TrainingUnit{job=100},combat).Value;
        var tile=Enumerable.Range(0,mapData.map.width*mapData.map.height).Select(i=>new TutorialTile(i%mapData.map.width,i/mapData.map.width))
            .First(t=>mapData.map.Terrain(t,kind*3+2)>=0&&mapData.map.Terrain(t,kind*3+2)!=99
                &&!mapData.npcs.Any(n=>n.x==t.X&&n.y==t.Y)&&!mapData.portals.Any(p=>p.cells.Any(c=>c.Tile==t)));
        var f=new Fixture(map,"END","END",script,new WorldPoint{x=tile.X,y=tile.Y});RecruitData(f);f.Finish();
        DirectedUnits(f,0,false);return f;
    }
    static void DirectedUnits(Fixture f,int team,bool moving,long? life=null)
    {
        var u=f.View.battle?.Unit(0)?.Copy()??new TrainingUnit{id=0,job=100,x=f.View.record.x,y=f.View.record.y,direction=1,
            life=1,hp=200,maxHp=200,mp=45,maxMp=45,ap=20,maxAp=20,canRest=true,nativeActive=true,nativeEnabled=true};
        if(life.HasValue)u.life=life.Value;u.nativeState=moving?1u:0u;
        u.nativeInfo=new NativeUnitState{fields=new int[44],name=Array.Empty<byte>(),className=Array.Empty<byte>()};
        u.nativeInfo.fields[0]=u.id;u.nativeInfo.fields[1]=u.job;u.nativeInfo.fields[24]=team;
        var received=f.server.ReceiveBattleUnits(new TrainingUnitsNotice{id=Id(),approved=true,character=f.View.characterId,session=f.View.campaignSession,
            expectedRevision=f.View.battle?.revision??0,units=new[]{u}});
        Check(received.Accepted,"explicit source-state fixture for directed event: "+received.Failure+" phase="+f.View.record.campaignPhase+" at="+u.x+","+u.y);
    }
    static NativeBattleNotice DirectedNotice(Fixture f,int number,int subject=0)=>new NativeBattleNotice{approved=true,id=Id(),
        character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle?.revision??0,
        unitLife=subject<0?0:f.View.battle?.Unit(subject)?.life??0,localTeam=0,packet=OriginalDirectedEvents.Packet(number,subject)};
    static void RawDirectedQueue()
    {
        var f=Directed("EVENT 1\nALL\nGOLD 123\nEND\nEVENT 2\nALL\nGOLD 456\nEND");
        var notice=DirectedNotice(f,1);string before=Snapshot(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattlePacket(notice).Failure==WorldFailure.StorageUnavailable&&Snapshot(f.View.record)==before,"raw0D initial persistence fails atomically");
        f.store.fail=false;Check(f.server.ReceiveBattlePacket(notice).Accepted,"raw0D is routed through production ingress");
        Check(f.View.presentation.nativeEvent&&f.View.presentation.nativeLine==0&&f.server.PendingCampaignRequests().Length==0,"receipt starts the event without executing its first body line");
        before=Snapshot(f.View.record);Check(f.server.ReceiveBattlePacket(notice).Failure==WorldFailure.StaleRequest&&Snapshot(f.View.record)==before,"raw0D receipt replay cannot enqueue twice");
        Check(f.server.ReceiveCampaignEvents(f.Event(2)).Failure==WorldFailure.Busy,"typed compatibility queue cannot mix into native queue");
        Check(f.server.ReceiveBattlePacket(DirectedNotice(f,2)).Accepted,"second raw event queues behind active first event");
        f.server.AdvanceWorld(0);Check(f.View.presentation.nativeLine==2&&f.server.PendingCampaignRequests().Length==0,"ALL metadata consumes exactly one Update");
        f.server.AdvanceWorld(0);Check(f.server.PendingCampaignRequests().Single().packet.SequenceEqual(new byte[]{0x3a,123,0,0,0}),"GOLD request uses original bytes, no money mutation");
        f.server.AdvanceWorld(0);Check(f.View.presentation.nativeLine==0&&f.View.record.campaignProgress.events[0].completed,"END starts next event without executing it in same Update");
        f.server.AdvanceWorld(0);Check(f.View.presentation.nativeLine==2&&f.server.PendingCampaignRequests().Length==1,"next event ALL runs on following Update");
        f.server.AdvanceWorld(0);Check(f.server.PendingCampaignRequests().Length==2,"FIFO second request follows first request");
        f.server.AdvanceWorld(0);Check(f.View.record.campaignProgress.events.All(e=>e.completed)&&!f.View.record.goldKnown,"queue drains without inventing a gold award");
        before=Snapshot(f.View.record);f.Reload();Check(Snapshot(f.View.record)==before&&f.View.presentation==null,"completed queue receipts and requests survive service recreation");

        var missing=Directed("EVENT 1\nEND\nEVENT 2\nGOLD 789\nEND");
        foreach(int number in new[]{1,99,2})Check(missing.server.ReceiveBattlePacket(DirectedNotice(missing,number)).Accepted,"queue a known/missing/known sequence");
        missing.server.AdvanceWorld(0);Check(missing.View.record.campaignProgress.events.Take(2).All(e=>e.completed)&&missing.View.record.campaignProgress.nativeBlocked,"missing head is consumed and leaves next queued event idle");
        for(int i=0;i<3;i++)missing.server.AdvanceWorld(1000);
        Check(missing.server.PendingCampaignRequests().Length==0,"Update alone does not pump after an absent event flag");
        Check(missing.server.ReceiveBattlePacket(DirectedNotice(missing,-1)).Accepted&&missing.View.presentation.nativeLine==0,"event=-1 pumps idle queue without appending");
        missing.server.AdvanceWorld(0);Check(missing.server.PendingCampaignRequests().Single().packet.SequenceEqual(new byte[]{0x3a,21,3,0,0}),"explicit pump releases the remaining native request");

        var full=Directed("EVENT 1\nDO 500\nEND\nEVENT 2\nALL\nEND");
        Check(full.server.ReceiveBattlePacket(DirectedNotice(full,1)).Accepted,"full queue active event");
        for(int i=0;i<100;i++)Check(full.server.ReceiveBattlePacket(DirectedNotice(full,2)).Accepted,"fill each original waiting slot");
        Check(full.View.record.campaignProgress.events.Count(e=>!e.completed)==101,"active plus100 waiting slots");
        Check(full.server.ReceiveBattlePacket(DirectedNotice(full,2)).Accepted&&full.View.record.campaignProgress.events.Length==101,"full native FIFO drops appended event without rejecting or deleting older entries");
        full.catalog.Map(1).events+="\nEVENT 3\nLOADSCENE 2\nEND";
        Check(full.server.ReceiveBattlePacket(DirectedNotice(full,3,2999)).Accepted&&full.View.record.campaignProgress.events.Length==101,
            "full FIFO drops a new unsupported body/unavailable subject without trying to start or inspect the dropped entry");
    }
    static void DirectedOwnershipAndSafety()
    {
        foreach(string invalid in new[]{"owner","session","revision","approval","life","team","team-missing","length","subject","event"})
        {
            var f=Directed("EVENT 1\nGETGENERAL 102\nEND");var n=DirectedNotice(f,1);
            if(invalid=="owner")n.character=Id();if(invalid=="session")n.session=Id();if(invalid=="revision")n.expectedRevision++;
            if(invalid=="approval")n.approved=false;if(invalid=="life")n.unitLife++;if(invalid=="team")n.sourceTeam=7;
            if(invalid=="team-missing")n.localTeam=null;if(invalid=="length")n.packet=n.packet.Take(5).ToArray();
            if(invalid=="subject")n.packet=OriginalDirectedEvents.Packet(1,2999);if(invalid=="event")n.packet=new byte[]{0x5c,0x0d,100,0,0,0};
            string state=Snapshot(f.View.record);Check(!f.server.ReceiveBattlePacket(n).Accepted&&Snapshot(f.View.record)==state&&f.server.PendingCampaignRequests().Length==0,"invalid native envelope is atomic: "+invalid);
        }
        foreach(int team in new[]{0,7})
        {
            var f=Directed("EVENT 1\nGOLD 123\nEND");DirectedUnits(f,team,true);
            Check(f.server.ReceiveBattlePacket(DirectedNotice(f,1)).Accepted,"moving source can receive a queued event");
            f.server.AdvanceWorld(1000);Check(f.View.presentation.nativeLine==1&&f.server.PendingCampaignRequests().Length==0,"source movement gates execution even for foreign ownership");
            DirectedUnits(f,team,false);f.server.AdvanceWorld(0);
            Check(f.server.PendingCampaignRequests().Length==(team==0?1:0),"owner comparison uses received team, never unit-id division");
        }
        var queued=Directed("EVENT 1\nEND\nEVENT 2\nGOLD 12\nEND");
        Check(queued.server.ReceiveBattlePacket(DirectedNotice(queued,1)).Accepted&&queued.server.ReceiveBattlePacket(DirectedNotice(queued,2)).Accepted,"queue before team update");
        DirectedUnits(queued,7,false,2);queued.server.AdvanceWorld(0);
        Check(queued.View.record.campaignProgress.events[1].native.life==2,"dequeue resolves the latest received life by native unit ID");queued.server.AdvanceWorld(0);
        Check(queued.server.PendingCampaignRequests().Length==0&&queued.View.record.campaignProgress.events.All(e=>e.completed),"next event captures latest received team when it starts, not when it queued");
        var active=Directed("EVENT 1\nDO 500\nGOLD 12\nEND");Check(active.server.ReceiveBattlePacket(DirectedNotice(active,1)).Accepted,"active source lifetime fixture");
        active.server.AdvanceWorld(0);DirectedUnits(active,7,false,2);active.server.AdvanceWorld(500);
        Check(active.server.PendingCampaignRequests().Length==1,"active event retains its F710 owner team but reads the current subject record, like the native ID reference");
        var anonymous=Directed("EVENT 1\nMESSAGE 1 709 700 A notice\nGETGENERAL 102\nEND");
        Check(anonymous.server.ReceiveBattlePacket(DirectedNotice(anonymous,1,-1)).Accepted,"signed subject=-1 accepted");anonymous.server.AdvanceWorld(0);
        Check(anonymous.View.presentation.completed&&anonymous.server.PendingCampaignRequests().Length==0,"subject=-1 skips owner-only dialogue and reward requests in one Update");
        var unsupported=Directed("EVENT 1\nGOLD 1\nLOADSCENE 2\nEND");var bad=DirectedNotice(unsupported,1);string before=Snapshot(unsupported.View.record);
        Check(unsupported.server.ReceiveBattlePacket(bad).Failure==WorldFailure.RulesUnavailable&&Snapshot(unsupported.View.record)==before,"unsupported native command defers whole event before an earlier request can leak");
    }
    static void DirectedReceivedMovement()
    {
        var f=Directed("EVENT 1\nGOLD 123\nEND");var actor=f.View.battle.Unit(0).Copy();actor.movementBudget=20;
        var units=new[]{actor};var start=actor.Tile;TutorialTile? destination=null;
        foreach(var tile in new[]{new TutorialTile(start.X+1,start.Y),new TutorialTile(start.X,start.Y+1),new TutorialTile(start.X-1,start.Y),new TutorialTile(start.X,start.Y-1)})
        {
            var path=OriginalNativeMovement.Path(f.definition.Map(1).map,f.definition.combatCatalog,units,0,start,tile,0);
            if(path?.points.Length>0){destination=tile;break;}
        }
        Check(destination.HasValue,"source terrain supplies an adjacent native movement fixture");
        Check(f.server.ReceiveBattleUnits(new TrainingUnitsNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,
            expectedRevision=f.View.battle.revision,units=units}).Accepted,"received movement-budget snapshot");
        var target=destination.Value;
        var movement=DirectedNotice(f,1);movement.packet=OriginalNativeMovement.Packet(false,0,target.X,target.Y);
        Check(f.server.ReceiveBattlePacket(movement).Accepted,"real5C01 movement receipt before directed event");
        Check((f.View.battle.Unit(0).nativeState.Value&1)==0,"movement receipt persists the completed state rather than an unfinished animation bit");
        Check(f.server.ReceiveBattlePacket(DirectedNotice(f,1)).Accepted,"directed event arrives during received movement");
        f.server.AdvanceWorld(0);
        Check(f.server.PendingCampaignRequests().Length==0&&f.View.presentation.nativeLine==1,"live native movement blocks command despite cleared durable movement bit");
        f.server.AdvanceWorld(10000);f.server.AdvanceWorld(0);
        Check(f.server.PendingCampaignRequests().Length==1,"directed command resumes once received movement finishes");
        f.server.AdvanceWorld(0);Check(f.server.PendingCampaignRequests().Length==1,"resumed command is not emitted twice");
    }
    static void DirectedRecruitAndReconnect()
    {
        foreach(var selected in new[]{(map:5,number:99,job:103),(map:9,number:50,job:107)})
        {
            var catalog=Catalog();var f=Directed(catalog.Map(selected.map).events,selected.map);var manager=new LocalServerManager(f.server);
            Check(f.server.ReceiveBattlePacket(DirectedNotice(f,selected.number)).Accepted,"real source raw0D event receipt "+selected.map);
            for(int i=0;i<200&&!f.View.record.campaignProgress.events.All(e=>e.completed);i++)
            {if(f.View.presentation.waitingMessage)f.server.ContinueCampaignPresentation();manager.TestAdvance(200);}
            Check(f.View.record.campaignProgress.events.All(e=>e.completed)&&f.View.record.roster.rows.Any(r=>r.job==selected.job),"actual event body issues3C and default manager grants its source job");
            Check(f.View.record.campaignRecruits.Length==1&&f.View.record.campaignRecruits[0].job==selected.job,"raw-event grant persists one permanent receipt");
            int count=f.View.record.roster.rows.Length;string saved=Snapshot(f.View.record);f.Reload();Check(Snapshot(f.View.record)==saved,"completed real event grant roundtrips without a second grant");
            manager=new LocalServerManager(f.server);Check(f.server.ReceiveBattlePacket(DirectedNotice(f,selected.number)).Accepted,"same source event can be delivered with a fresh native receipt");
            for(int i=0;i<200&&!f.View.record.campaignProgress.events.All(e=>e.completed);i++)
            {if(f.View.presentation.waitingMessage)f.server.ContinueCampaignPresentation();manager.TestAdvance(200);}
            Check(f.View.record.roster.rows.Length==count&&f.View.record.campaignRecruits.Length==1,"fresh event receipt does not duplicate a commander reward");
        }
        var interrupted=Directed("EVENT 1\nGETGENERAL 102\nDO 500\nEND");var m=new LocalServerManager(interrupted.server);
        Check(interrupted.server.ReceiveBattlePacket(DirectedNotice(interrupted,1)).Accepted,"interrupted raw event starts");m.TestAdvance(0);
        Check(interrupted.View.record.roster.rows.Length==2,"reward committed before interruption");interrupted.Reload();m=new LocalServerManager(interrupted.server);
        for(int i=0;i<20&&!interrupted.View.record.campaignProgress.events.All(e=>e.completed);i++)m.TestAdvance(200);
        Check(interrupted.View.record.roster.rows.Length==2&&interrupted.View.record.campaignRecruits.Length==1&&interrupted.View.record.campaignProgress.events.All(e=>e.completed),"interrupted event replays presentation and reuses committed command receipt");
        string directory=Path.Combine(Path.GetTempPath(),"ds-directed-save-"+Id());
        try
        {var disk=new LocalAccountFileStore(Path.Combine(directory,"accounts.json"));Check(disk.TryWrite(interrupted.store.data)&&disk.TryRead(out var restored)&&Snapshot(restored.accounts[0].character.world)==Snapshot(interrupted.View.record),"real file serializer preserves native queue ownership/receipts");}
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    static readonly System.Collections.Generic.List<object> directedMagicRequests=new System.Collections.Generic.List<object>();
    static void DirectedMessages()
    {
        var f=Directed("EVENT 1\nMESSAGE 0 0 ME 0 %s\nMESSAGE 1 COM0 COM0 0 %s\nEND");
        f.definition.nativeText=bytes=>System.Text.Encoding.ASCII.GetString(bytes);
        NativeBattleNotice Team(string name)=>new NativeBattleNotice{id=Id(),approved=true,character=f.View.characterId,session=f.View.campaignSession,
            expectedRevision=f.View.battle.revision,expectedTeamRevision=f.View.battleTeamRevision,localTeam=0,
            packet=OriginalBattleTeamNames.Packet(new[]{new NativeBattleTeamName{team=0,name=System.Text.Encoding.ASCII.GetBytes(name)}})};
        Check(f.server.ReceiveBattlePacket(Team("OwnTeam")).Accepted&&f.server.ReceiveBattlePacket(DirectedNotice(f,1)).Accepted,"source team name precedes raw message");
        f.server.AdvanceWorld(0);Check(f.View.presentation.message=="OwnTeam"&&f.View.presentation.messageResolved,"ME resolves received current team name rather than event subject or commander name");
        Check(f.server.ReceiveBattlePacket(Team("ChangedTeam")).Accepted&&f.View.presentation.message=="OwnTeam","a displayed message freezes formatted arguments when native MESSAGE executes");
        Check(f.server.ContinueCampaignPresentation().Accepted,"acknowledge frozen message");f.server.AdvanceWorld(0);
        Check(f.View.presentation.message=="진행검사"&&f.View.presentation.face==100,"COM0 uses the commander slot name and job for the portrait");
        Check(f.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Accepted&&f.View.presentation.waitingMessage,
            "mode1 dialogue does not impose the mode2 global battle-action lock");
        var wide=Directed("EVENT 1\nMESSAGE 0 0 ME 0 NOTICE %S received\nGOLD 10\nEND");Check(wide.server.ReceiveBattlePacket(DirectedNotice(wide,1)).Accepted,"retain source uppercase format verbatim");wide.server.AdvanceWorld(0);
        Check(wide.View.presentation.heldCommand==""&&wide.View.presentation.message=="NOTICE  received"&&wide.server.PendingCampaignRequests().Length==0,"bounded C-locale %S result is not replaced with an invented character name");
        Check(wide.server.ContinueCampaignPresentation().Accepted,"source wide-format message remains acknowledgeable");wide.server.AdvanceWorld(0);
        Check(wide.server.PendingCampaignRequests().Length==1,"unsafe source alias representation does not deadlock the following event request");
    }
    static void DirectedMagicAndLifecycle()
    {
        foreach(var choice in new[]{(magic:101,source:-1),(magic:301,source:-1),(magic:703,source:-1),(magic:3001,source:-2)})
        {
            var f=Directed("EVENT 1\nEND");
            f.definition.magicMenu=Read<MagicMenuCatalog>("Assets/_Original/Data/Combat/MagicMenu.json");
            f.definition.magicProjectiles=Read<MagicProjectileCatalog>("Assets/_Original/Data/Combat/MagicProjectiles.json");
            var u=f.View.battle.Unit(0).Copy();u.magicRules=new[]{new BattleMagicRule{magic=choice.magic,enabled=true,targets=new[]{0}}};
            Check(f.server.ReceiveBattleUnits(new TrainingUnitsNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,
                expectedRevision=f.View.battle.revision,units=new[]{u}}).Accepted,"explicit target eligibility for native scripted magic");
            string command="MAGIC "+choice.magic+" "+choice.source+" "+u.x+" "+u.y;f.catalog.Map(1).events="EVENT 1\n"+command+"\nEND";
            Check(f.server.ReceiveBattlePacket(DirectedNotice(f,1)).Accepted,"scripted MAGIC event receipt");
            string before=Snapshot(f.View.record);long revision=f.View.battle.revision;f.store.fail=true;f.server.AdvanceWorld(0);
            Check(Snapshot(f.View.record)==before&&f.server.PendingCampaignRequests().Length==0&&f.View.battle.magicEffects.Length==0&&f.View.presentation.heldCommand==command,
                "failed command save leaks neither mode1 request nor magic visual");
            f.store.fail=false;Check(f.server.RetryCampaignPresentation().Accepted,"scripted magic retry");f.server.AdvanceWorld(0);
            var request=f.server.PendingCampaignRequests().Single();Check(OriginalNativeMagic.TryRequest(request.packet,out var cast)&&cast.mode==1&&cast.magic==choice.magic&&cast.targets.SequenceEqual(new[]{0}),"0D MAGIC emits exact mode1 target capsule");
            Check(f.View.record.hp==u.hp&&f.View.record.mp==u.mp&&f.View.battle.Unit(0).ap==u.ap&&f.View.battle.revision==revision,"scripted magic only adds a visual and request, no cost/damage/XP or revision");
            Check(f.View.battle.magicEffects.Length>0&&f.View.battle.magicEffects.All(e=>e.magic==choice.magic),"scripted magic uses native live effect slots");
            directedMagicRequests.Add(new{magic=choice.magic,source=choice.source,x=u.x,y=u.y,selectedSource=cast.source,selectedX=cast.x,selectedY=cast.y,targets=cast.targets,request=request.packet.Select(b=>(int)b).ToArray()});
            f.Reload();for(int i=0;i<5&&!f.View.record.campaignProgress.events.All(e=>e.completed);i++)f.server.AdvanceWorld(0);
            Check(f.server.PendingCampaignRequests().Length==1&&f.server.PendingCampaignRequests()[0].id==request.id&&f.View.battle.magicEffects.Length==0,"reconnect reuses durable scripted magic request and does not replay its visual or multiply requests");
        }
        foreach(var kind in new[]{(command:"KILLCHAR1",dead:false),(command:"KILLCHAR1",dead:true),(command:"KILLCHAR2",dead:false)})
        {
            var f=Directed("EVENT 1\n"+kind.command+" 800\nEND");var commander=f.View.battle.Unit(0).Copy();
            var map=f.definition.Map(1);var tile=Enumerable.Range(0,map.map.width*map.map.height).Select(i=>new TutorialTile(i%map.map.width,i/map.map.width))
                .First(t=>t!=commander.Tile&&map.map.Terrain(t,2)>=0&&map.map.Terrain(t,2)!=99&&!map.npcs.Any(n=>n.x==t.X&&n.y==t.Y)&&!map.portals.Any(p=>p.cells.Any(c=>c.Tile==t)));
            var target=new TrainingUnit{id=800,job=100,x=tile.X,y=tile.Y,direction=1,life=1,hp=17,maxHp=30,mp=3,maxMp=5,ap=7,maxAp=20,nativeActive=true,nativeEnabled=true,nativeState=kind.dead?0x80u:0u};
            Check(f.server.ReceiveBattleUnits(new TrainingUnitsNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,
                expectedRevision=f.View.battle.revision,units=new[]{commander,target}}).Accepted,"explicit script target snapshot");
            Check(f.server.ReceiveBattlePacket(DirectedNotice(f,1)).Accepted,"script lifecycle event receipt");string before=Snapshot(f.View.record);long revision=f.View.battle.revision;
            f.store.fail=true;f.server.AdvanceWorld(0);Check(Snapshot(f.View.record)==before&&f.View.battle.Unit(800).nativeState==target.nativeState&&f.View.battle.Unit(800).nativeActive==true,"failed KILL save preserves runtime and durable target");
            f.store.fail=false;Check(f.server.RetryCampaignPresentation().Accepted,"script lifecycle retry");f.server.AdvanceWorld(0);var after=f.View.battle.Unit(800);
            Check(after.hp==17&&after.mp==3&&after.ap==7&&after.life==1,"KILL export does not manufacture HP0, costs, replacement life or rewards");
            Check(kind.command=="KILLCHAR2"?(after.nativeState.Value&0x80)!=0:after.nativeActive==kind.dead,"KILL1 preserves a corpse, otherwise deactivates; KILL2 only sets80");
            Check(f.View.battle.revision==revision+1&&f.server.PendingCampaignRequests().Length==0,"script lifecycle mutation is durable, invalidates old battle decisions, sends no fabricated death packet");
            f.Reload();for(int i=0;i<5&&!f.View.record.campaignProgress.events.All(e=>e.completed);i++)f.server.AdvanceWorld(0);
            Check(f.View.battle.revision==revision+1&&f.View.battle.Unit(800).nativeActive==after.nativeActive&&f.View.battle.Unit(800).nativeState==after.nativeState,"reconnect restores script mutation and skips already committed command");
        }
    }
    static Fixture VictoryFixture(string script="EVENT 1\nEND")
    {
        var f=Directed(script);f.definition.nativeText=bytes=>System.Text.Encoding.ASCII.GetString(bytes);
        var n=DirectedNotice(f,1);n.expectedTeamRevision=f.View.battleTeamRevision;
        n.packet=OriginalBattleTeamNames.Packet(new[]{new NativeBattleTeamName{team=0,name=System.Text.Encoding.ASCII.GetBytes("Local")}});
        Check(f.server.ReceiveBattlePacket(n).Accepted,"received current team for victory fixture");return f;
    }
    static NativeBattleNotice VictoryNotice(Fixture f,string name="Other",byte ignored=0)
    {
        var n=DirectedNotice(f,1);n.expectedTeamRevision=f.View.battleTeamRevision;
        n.packet=OriginalVictoryAnnouncement.Packet(ignored,System.Text.Encoding.ASCII.GetBytes(name));return n;
    }
    static void VictoryAnnouncements()
    {
        foreach(byte ignored in new byte[]{0,1,255})
        {
            var f=VictoryFixture();string before=Snapshot(f.View.record);long revision=f.View.battle.revision;
            foreach(string own in new[]{"Local","Local\0tail"})
                Check(f.server.ReceiveBattlePacket(VictoryNotice(f,own,ignored)).Accepted&&!f.View.nativeMessage.visible,"same C-string name is suppressed");
            var n=VictoryNotice(f,"Other",ignored);f.store.fail=true;
            Check(f.server.ReceiveBattlePacket(n).Accepted&&f.View.nativeMessage.visible&&f.View.nativeMessage.text=="Other님이 승리하셨습니다."&&f.View.nativeMessage.mode==0,"foreign notice is transient and does not need a successful store");
            Check(Snapshot(f.View.record)==before&&f.View.battle.revision==revision&&f.server.PendingCampaignRequests().Length==0,"notice cannot mutate clear, HP, XP, reward, battle revision or script queue");
            f.server.AdvanceWorld(10000);Check(f.View.nativeMessage.visible,"plain notice survives exactly10000ms");
            Check(f.server.ReceiveBattlePacket(n).Failure==WorldFailure.StaleRequest,"replayed notice is refused");
            f.server.AdvanceWorld(1);Check(!f.View.nativeMessage.visible,"replay does not reset the strict10000ms expiry");
            f.store.fail=false;Check(f.server.ReceiveBattlePacket(VictoryNotice(f,"local",ignored)).Accepted&&f.View.nativeMessage.visible,"native name comparison is case-sensitive");
            Check(f.server.ContinueCampaignPresentation().Accepted&&!f.View.nativeMessage.visible,"standalone notice acknowledges without an event presentation");
            Check(f.server.ReceiveBattlePacket(VictoryNotice(f,"Other\0ignored",ignored)).Accepted&&f.View.nativeMessage.text=="Other님이 승리하셨습니다.","C-string NUL ends the displayed name");
            f.Reload();Check(!f.View.nativeMessage.visible&&Snapshot(f.View.record)==before,"reconnect removes transient announcement and preserves save");
        }
        foreach(string invalid in new[]{"owner","session","revision","team-revision","approval","id","decoder","length","outgoing","missing-team"})
        {
            var f=VictoryFixture();var n=VictoryNotice(f);string before=Snapshot(f.View.record);
            if(invalid=="owner")n.character=Id();if(invalid=="session")n.session=Id();if(invalid=="revision")n.expectedRevision++;
            if(invalid=="team-revision")n.expectedTeamRevision++;if(invalid=="approval")n.approved=false;if(invalid=="id")n.id="bad";
            if(invalid=="decoder")f.definition.nativeText=null;if(invalid=="length")n.packet=n.packet.Take(4).ToArray();
            if(invalid=="outgoing")n.packet=new byte[]{0x5c,0x20,1};
            if(invalid=="missing-team")
            {
                var t=VictoryNotice(f);t.localTeam=null;t.packet=OriginalBattleTeamNames.Packet(new[]{new NativeBattleTeamName{team=7,name=new byte[]{65}}});
                Check(f.server.ReceiveBattlePacket(t).Accepted,"unresolved current team fixture");n.localTeam=null;
            }
            Check(!f.server.ReceiveBattlePacket(n).Accepted&&!f.View.nativeMessage.visible&&Snapshot(f.View.record)==before,"invalid victory envelope remains atomic: "+invalid);
        }
        var first=VictoryFixture("EVENT 1\nGOLD 7\nEND");Check(first.server.ReceiveBattlePacket(DirectedNotice(first,1)).Accepted,"event before first message");
        Check(first.server.ReceiveBattlePacket(VictoryNotice(first)).Accepted,"notice before script MESSAGE");first.server.AdvanceWorld(0);
        Check(first.server.PendingCampaignRequests().Length==1&&first.View.nativeMessage.visible,"untracked shared notice does not pause event");
        first.server.AdvanceWorld(0);Check(first.View.presentation.completed&&first.View.nativeMessage.visible,"END cannot dismiss untracked shared notice");
        var active=VictoryFixture("EVENT 1\nMESSAGE 1 100 0 0 one|two|three|four\nGOLD 8\nEND");
        Check(active.server.ReceiveBattlePacket(DirectedNotice(active,1)).Accepted,"tracked event begins");active.server.AdvanceWorld(0);
        Check(active.View.nativeMessage.visible&&active.View.presentation.waitingMessage,"MESSAGE starts native tracking");
        active.server.ContinueCampaignPresentation();Check(active.View.nativeMessage.firstLine==3,"portrait advances by three rows");
        Check(active.server.ReceiveBattlePacket(VictoryNotice(active)).Accepted&&active.View.nativeMessage.firstLine==0&&active.View.nativeMessage.mode==0,"20 replaces portrait and resets page");
        active.server.AdvanceWorld(0);Check(active.server.PendingCampaignRequests().Length==0,"tracked replacement still gates next script command");
        active.server.ContinueCampaignPresentation();Check(active.server.PendingCampaignRequests().Length==0&&!active.View.nativeMessage.visible,"acknowledgement does not run script inline");
        active.server.AdvanceWorld(0);Check(active.server.PendingCampaignRequests().Length==1&&!active.View.nativeMessage.visible,"next Update resumes without restoring old portrait");
        var later=VictoryFixture("EVENT 1\nMESSAGE 1 100 0 0 old\nGOLD 9\nEND");
        Check(later.server.ReceiveBattlePacket(DirectedNotice(later,1)).Accepted,"closed-message tracking fixture");later.server.AdvanceWorld(0);later.server.ContinueCampaignPresentation();
        Check(later.server.ReceiveBattlePacket(VictoryNotice(later)).Accepted,"20 arrives after old portrait closed");later.server.AdvanceWorld(10000);
        Check(later.server.PendingCampaignRequests().Length==0&&later.View.nativeMessage.visible,"script+30 remains set after old message closes");
        later.server.AdvanceWorld(1);Check(later.server.PendingCampaignRequests().Length==1&&!later.View.nativeMessage.visible,"timeout releases tracked script on the following Update");
        var d=new NativeCampaignDialogue();d.Show("",0);d.Advance(10001);Check(d.Visible,"empty native plain text has no automatic expiry");d.Continue();Check(!d.Visible,"empty notice can be clicked closed");
        d.Show("a|b|c|d",1);d.Advance(100000);Check(d.Visible,"portrait has no plain notice timeout");d.Continue();Check(d.Snapshot().firstLine==3&&d.Visible,"three-row page");d.Continue();Check(!d.Visible,"last page closes");
        Check(OriginalCampaignInput.DirectedMessage(null,new NativeCampaignMessageView{visible=true}),"shared notice is actionable without script");
        Check(!OriginalVictoryAnnouncement.TryDecode(new byte[]{0x5c,0x20,0},out _,out _)&&OriginalVictoryAnnouncement.Packet(0,new byte[256])==null,"incoming20 shape cannot be outgoing SCENECLEAR");
    }
#if VICTORY_MANAGER
    public static void RunVictoryManagerChecks(string repository)
    {
        root=repository;
        foreach(bool unavailableStore in new[]{false,true})
        {
            var f=VictoryFixture();var manager=new LocalServerManager(f.server);int outbound=0,focus=0,map=0,exit=0;
            manager.Notify_BattlePacketRequested+=_=>outbound++;manager.Notify_BattleFocusRequested+=_=>focus++;
            manager.Notify_MapEntered+=_=>map++;manager.Notify_ScenarioExited+=_=>exit++;
            f.store.fail=unavailableStore;string saved=Snapshot(f.View.record);var input=VictoryNotice(f);
            var received=manager.Notify_BattlePacket(input);
            Check(received.Accepted&&received.View.nativeMessage.visible&&manager.WorldSnapshot.nativeMessage.text=="Other님이 승리하셨습니다.","actual manager common ingress publishes received20 view with shared notice");
            Check(Snapshot(f.View.record)==saved&&outbound==0&&focus==0&&map==0&&exit==0,"manager20 emits no fabricated packets, focus, leave or reward");
            Check(manager.Notify_BattlePacket(input).Failure==WorldFailure.StaleRequest,"manager preserves duplicate guard");
            Check(!manager.Notify_TrainingPacket(VictoryNotice(f)).Accepted&&Snapshot(f.View.record)==saved,"training-specific manager ingress cannot accept a scenario20");
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlay=false,scope="Actual LocalServerManager.NativeBattlePackets partial, current service/WorldSnapshot and supplied minimal non-Unity transport host. Foreign victory20 produces no followup/focus/map/exit callbacks; not Unity lifecycle or Notify_World delivery."}));
    }
#endif
    public static void RunDirectedChecks(string repository)
    {
        root=repository;RawDirectedQueue();DirectedOwnershipAndSafety();DirectedReceivedMovement();DirectedRecruitAndReconnect();DirectedMagicAndLifecycle();DirectedMessages();VictoryAnnouncements();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,realRawRewardEvents=2,directedMagicRequests,unityPlay=false,scope="Production raw5C0D/20 ingress, shared notice replacement, FIFO, script magic/lifecycle and default local-server source-issued recruit processing. Explicit supplied unit/team/eligibility fixtures; no invented server trigger conditions."}));
    }
}
