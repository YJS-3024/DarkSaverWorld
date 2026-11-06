using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeResultEntry{public static void Main(string[] args)=>ResultChecks.RunNativeResults(args[0]);}
internal static partial class ResultChecks
{
    private static NativeResultReport RawReport(int outcome=1)=>new NativeResultReport{outcome=outcome,eventTotal=4,eventCompleted=2,spoils=uint.MaxValue,
        rows=new[]{new NativeResultRow{words=new short[]{-123,100,2,2,200,3,1,-1,101,-1,102,-1,-1,103},experience=uint.MaxValue}}};
    private static NativeBattleNotice RawNotice(Fixture f,NativeResultReport report)=>new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,approved=true,
        expectedRevision=f.View.battle?.revision??0,packet=OriginalResultPackets.Packet(report)};
    private static string Economy(WorldRecord r)=>State(new{r.gold,r.goldKnown,r.growth,r.hp,r.maxHp,r.mp,r.maxMp,r.healthKnown,r.ghost,r.inventory,r.equipment,r.roster});
    private static void CodecCases()
    {
        foreach(int count in new[]{0,1,5,10})foreach(int outcome in new[]{-32768,0,1,2,3,4,32767})
        {
            var report=RawReport(outcome);report.rows=Enumerable.Range(0,count).Select(_=>report.rows[0].Copy()).ToArray();var packet=OriginalResultPackets.Packet(report);
            Check(OriginalResultPackets.TryDecode(packet,out var decoded)&&OriginalResultPackets.Packet(decoded).SequenceEqual(packet),"54 roundtrip preserves opaque/signed WORDs and unsigned DWORDs");
            foreach(int length in Enumerable.Range(0,packet.Length))Check(!OriginalResultPackets.TryDecode(packet.Take(length).ToArray(),out _),"54 rejects every truncated prefix");
            Check(!OriginalResultPackets.TryDecode(packet.Concat(new byte[]{0}).ToArray(),out _),"54 rejects trailing data");
        }
        Check(!OriginalResultPackets.TryDecode(new byte[]{0x5c,0x54,0,0,0,0,0,0,0,0,0,0,11,0},out _),"54 fixed10-row destination is not overrun");
        Check(OriginalResultPackets.Growth(0,uint.MaxValue)=="경험치 획득 : -1"&&OriginalResultPackets.Growth(1,uint.MaxValue)=="레벨업"&&OriginalResultPackets.Growth(2,uint.MaxValue)=="클레스체인지","54 row experience and growth captions match original signed formatter");
    }
    private static void VictoryResult()
    {
        var f=new Fixture();f.Enter();var notice=RawNotice(f,RawReport());string before=State(f.View.record);
        Check(f.server.ReceiveBattlePacket(notice).Accepted&&f.View.record.scenarioVisit.result==null&&f.View.record.campaignPhase=="explore"&&f.View.record.scenarioVisit.nativeEnd.stage==NativeScenarioEndRecord.Prepared,"54 prepares display data without fabricating scenario clear");
        f.Clear();f.FinishEnding();notice=RawNotice(f,RawReport());string economy=Economy(f.View.record);f.store.fail=true;before=State(f.View.record);
        Check(f.server.ReceiveBattlePacket(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"54 display save failure leaves no partial result");
        f.store.fail=false;var manager=new LocalServerManager(f.server);int delivered=0;manager.Notify_BattleChanged+=_=>delivered++;
        Check(manager.Notify_BattlePacket(notice).Accepted&&delivered==1,"54 passes actual manager common ingress");
        var r=f.View.record.scenarioVisit.result;
        Check(r.native.rows[0].words[0]==-123&&r.allies[0].unit==0&&r.native.rows[0].experience==uint.MaxValue&&r.spoils==uint.MaxValue,"54 preserves opaque first WORD and unsigned display amounts without inventing live identity");
        Check(Economy(f.View.record)==economy&&!r.allies[0].level.HasValue&&!r.allies[0].classJob.HasValue&&!r.kills.HasValue&&!r.experience.HasValue,"54 display does not grant money/items/XP/class or invent absent kill/total-XP fields");
        string path=Path.Combine(Path.GetTempPath(),"ds-native-result-"+Id(),"accounts.json");
        try
        {
            var file=new LocalAccountFileStore(path);var document=f.store.data.Copy();
            Check(file.TryWrite(document)&&file.TryRead(out var restored)&&State(restored.accounts[0].character.world.scenarioVisit.result.native)==State(r.native)
                &&Economy(restored.accounts[0].character.world)==economy,"real DataContract disk round-trip preserves signed native rows, uint32 amounts and unrelated account state");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        Check(!f.server.ReceiveBattlePacket(notice).Accepted,"54 duplicate cannot apply twice");
        r.native.rows[0].words[1]=101;Check(f.View.record.scenarioVisit.result.native.rows[0].Job==100,"54 exposed result is a deep copy");
        f.Reload();r=f.View.record.scenarioVisit.result;Check(r.native.rows[0].Job==100&&r.native.spoils==uint.MaxValue&&r.native.rows[0].experience==uint.MaxValue&&Economy(f.View.record)==economy,"54 received report survives reconnect without a reward grant");
        var forged=r.Copy();forged.native.rows[0].words[1]=101;Check(!CampaignResultRecord.Valid(forged),"native and projected result rows cannot disagree");
        Check(f.server.ConfirmScenarioResult(r.id,Id()).Accepted&&f.View.record.map==1000&&f.View.record.x==21&&f.View.record.y==21&&Economy(f.View.record)==economy,"54 confirm returns to stored origin without altering unrelated account state");
    }
    private static void ExitResult(int outcome)
    {
        var f=new Fixture();f.Enter();var notice=RawNotice(f,RawReport(outcome));string before=State(f.View.record),economy=Economy(f.View.record),progress=State(f.View.record.campaignProgress);
        Check(f.server.ReceiveBattlePacket(notice).Accepted&&f.View.record.campaignPhase=="explore"&&f.View.record.scenarioVisit.result==null,"54 display alone cannot end a live scenario "+outcome);
        string request=null;
        if(outcome==2)
        {
            request=Id();Check(f.server.RequestScenarioRetreat(f.View.record.scenarioVisit.id,request).Accepted,"explicit retreat request precedes native retreat result");
        }
        var exit=new CampaignExitNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,visit=f.View.record.scenarioVisit.id,
            expectedBattleRevision=f.View.record.scenarioBattle?.revision??0,request=request,kind=outcome==2?CampaignExitKind.Retreat:outcome==3?CampaignExitKind.Defeat:CampaignExitKind.Terminated,awaitResult=true};
        var invalidExit=new CampaignExitNotice{approved=true,id=Id(),character=exit.character,session=exit.session,visit=exit.visit,request=request,kind=exit.kind,
            expectedBattleRevision=exit.expectedBattleRevision,awaitResult=true,result=f.Notice().result};
        before=State(f.View.record);Check(!f.server.ReceiveScenarioExit(invalidExit).Accepted&&State(f.View.record)==before,"end notice cannot both await and supply a result");
        f.store.fail=true;Check(f.server.ReceiveScenarioExit(exit).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed explicit end keeps live battle and retreat request for retry");f.store.fail=false;
        Check(f.server.ReceiveScenarioExit(exit).Accepted&&f.View.record.campaignPhase=="complete"&&f.View.record.scenarioVisit.result==null,"explicit end notice waits for display data without fabricating a report");
        Check(!f.server.ReceiveScenarioExit(exit).Accepted,"explicit end replay rejected while waiting for report");
        before=State(f.View.record);
        foreach(int other in Enumerable.Range(1,4).Where(v=>v!=outcome))Check(!f.server.ReceiveBattlePacket(RawNotice(f,RawReport(other))).Accepted&&State(f.View.record)==before,"54 display cannot replace explicit exit outcome "+other);
        Check(!f.server.ReceiveScenarioResult(f.Notice()).Accepted&&State(f.View.record)==before,"typed victory report cannot replace an explicit non-victory exit");
        f.Reload();Check(f.View.record.campaignPhase=="complete"&&f.View.record.scenarioVisit.result==null,"explicit end with pending display survives reconnect");
        notice=RawNotice(f,RawReport(outcome));
        f.store.fail=true;before=State(f.View.record);Check(f.server.ReceiveBattlePacket(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"native non-victory save rollback "+outcome);
        f.store.fail=false;Check(f.server.ReceiveBattlePacket(notice).Accepted&&f.View.record.campaignPhase=="complete"&&f.View.record.scenarioVisit.result.native.outcome==outcome,"native retreat/defeat/termination opens its explicit result "+outcome);
        Check(State(f.View.record.campaignProgress)==progress&&Economy(f.View.record)==economy,"native non-victory never fabricates a clear/reward/heal/ghost outcome");
        string id=f.View.record.scenarioVisit.result.id;Check(!f.server.ReceiveBattlePacket(notice).Accepted,"native non-victory replay rejected");f.Reload();
        Check(f.View.record.scenarioVisit.result.id==id&&f.View.record.scenarioVisit.result.native.outcome==outcome,"non-victory native display restores");
        Check(f.server.ConfirmScenarioResult(id,Id()).Accepted&&f.View.record.map==1000&&Economy(f.View.record)==economy,"native non-victory confirm returns to source "+outcome);
    }
    private static void InvalidResults()
    {
        foreach(string invalid in new[]{"owner","session","revision","approval","outcome","event","job","count","length","companion-count"})
        {
            var f=new Fixture();f.Enter();f.Clear();f.FinishEnding();var report=RawReport();var notice=RawNotice(f,report);
            if(invalid=="owner")notice.character=Id();if(invalid=="session")notice.session=Id();if(invalid=="revision")notice.expectedRevision++;if(invalid=="approval")notice.approved=false;
            if(invalid=="outcome"){report.outcome=0;notice.packet=OriginalResultPackets.Packet(report);}if(invalid=="event"){report.eventCompleted=5;notice.packet=OriginalResultPackets.Packet(report);}
            if(invalid=="job"){report.rows[0].words[1]=9999;notice.packet=OriginalResultPackets.Packet(report);}if(invalid=="companion-count"){report.rows[0].words[5]=32767;notice.packet=OriginalResultPackets.Packet(report);}
            if(invalid=="count")notice.packet[12]=11;if(invalid=="length")notice.packet=notice.packet.Take(15).ToArray();
            string state=State(f.View.record);Check(!f.server.ReceiveBattlePacket(notice).Accepted&&State(f.View.record)==state,"bad native result is atomic "+invalid);
        }
    }
    private static NativeBattleNotice EndNotice(Fixture f,byte flag=0)=>new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,
        approved=true,expectedRevision=f.View.battle?.revision??0,packet=OriginalScenarioEnd.Packet(flag)};
    private static void FinishNativeEnding(Fixture f)
    {
        for(int i=0;i<10000&&f.View.record.scenarioVisit!=null;i++)
        {if(f.View.presentation?.waitingMessage==true)f.server.ContinueCampaignPresentation();f.server.AdvanceWorld(1000);}
        Check(f.View.record.map==1000&&f.View.record.scenarioVisit==null,"native DEE finishes and returns automatically without a second result confirmation");
    }
    private static void RoundTripNativeEnd(Fixture f)
    {
        string directory=Path.Combine(Path.GetTempPath(),"ds-end-save-"+Id());
        try
        {
            var disk=new LocalAccountFileStore(Path.Combine(directory,"accounts.json"));
            Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var restored)&&State(restored.accounts[0].character.world)==State(f.View.record),"real DataContract file roundtrip preserves native end journal and report unsigned/signed fields");
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    private static void NativeEndLatePackets()
    {
        var f=new Fixture();f.Enter();f.definition.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");
        var commander=new TrainingUnit{id=0,job=100,x=f.View.record.x,y=f.View.record.y,direction=1,life=1,hp=17,maxHp=200,mp=3,maxMp=65,ap=20,maxAp=20,canRest=true,nativeActive=true,nativeEnabled=true};
        Check(f.server.ReceiveBattleUnits(new TrainingUnitsNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=0,units=new[]{commander}}).Accepted,"explicit isolated battle state precedes native end");
        Check(f.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Accepted,"pending command exists before authoritative end");
        Check(f.server.ReceiveBattlePacket(RawNotice(f,RawReport())).Accepted&&f.server.ReceiveBattlePacket(EndNotice(f)).Accepted,"54 then61 can arrive with an unfulfilled player request");
        Check(f.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Failure==WorldFailure.Busy,"native end blocks new battle commands");
        var late=f.View.battle.units.Select(u=>u.Copy()).ToArray();late[0].hp=12;
        Check(f.server.ReceiveBattleUnits(new TrainingUnitsNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,units=late}).Accepted,"late approved unit stats still apply during native end notice");
        Check(f.View.record.hp==12&&f.View.record.scenarioBattle.units[0].hp==12,"late stats are durable without a fabricated victory award");
        var kills=new NativeResultKills{jobs=new[]{100,101}};
        Check(f.server.ReceiveBattlePacket(new NativeBattleNotice{approved=true,id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,expectedResultKillRevision=f.View.resultKillRevision,packet=OriginalResultKillPackets.Packet(kills)}).Accepted,"31 updates the native pending report after61");
        f.Reload();Check(f.View.battle.Commander.hp==12&&f.View.resultKills.jobs.SequenceEqual(kills.jobs)&&f.View.record.scenarioVisit.nativeEnd.stage==NativeScenarioEndRecord.Notice,"late stats/kill list and notice restore together");
        Check(f.server.ContinueCampaignPresentation().Accepted,"late receipt notice can be acknowledged");
        Check(f.server.ConfirmScenarioResult(f.View.record.scenarioVisit.nativeEnd.table.id,Id()).Accepted&&f.View.battle==null&&f.View.record.scenarioBattle==null,"native DEE clears live/saved battle only after result confirmation");
    }
    private static void NativeEndSuffixSelection()
    {
        foreach(byte flag in new byte[]{0,1})
        {
            var f=new Fixture();f.Enter();var map=f.definition.campaign.Map(26);
            map.outroVariants=new[]{"a","b"}.Select(v=>new CampaignOutroVariant{suffix=v,script="MESSAGE 1 100 0 0 selected-"+v+"\nDO 1000\nEND"}).ToArray();
            NativeBattleNotice Suffix(byte v)=>new NativeBattleNotice{id=Id(),approved=true,character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle?.revision??0,packet=OriginalScenarioScriptSuffix.Packet(v)};
            var manager=new LocalServerManager(f.server);int outbound=0;manager.Notify_BattlePacketRequested+=_=>outbound++;
            string economy=Economy(f.View.record);Check(manager.Notify_BattlePacket(Suffix(1)).Accepted,"manager accepts native66 before54/61");
            Check(manager.Notify_BattlePacket(RawNotice(f,RawReport())).Accepted,"54 does not freeze the66 suffix");
            if(flag==0)
            {
                Check(manager.Notify_BattlePacket(EndNotice(f,0)).Accepted&&f.server.ContinueCampaignPresentation().Accepted,"normal61 reaches result confirmation");
                string id=f.View.record.scenarioVisit.nativeEnd.table.id,request=Id(),before=State(f.View.record);f.store.fail=true;
                Check(f.server.ConfirmScenarioResult(id,request).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed result confirmation does not freeze or start DEE");f.store.fail=false;
                Check(manager.Notify_BattlePacket(Suffix(2)).Accepted&&f.server.ConfirmScenarioResult(id,request).Accepted,"DEE chooses newest66 at successful confirm");
            }
            else
            {
                var end=EndNotice(f,1);string before=State(f.View.record);f.store.fail=true;
                Check(manager.Notify_BattlePacket(end).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed fast61 does not freeze or start DEE");f.store.fail=false;
                Check(manager.Notify_BattlePacket(Suffix(2)).Accepted&&manager.Notify_BattlePacket(end).Accepted,"fast61 chooses newest66 on successful retry");
            }
            string context=f.View.presentation.context;
            Check(f.View.record.scenarioVisit.nativeOutroSuffix=="b"&&context.EndsWith(OriginalCampaignScript.Fingerprint(map.Outro("b"))),"both native54/61 paths play selected B instead of base");
            Check(Economy(f.View.record)==economy&&outbound==0,"suffix/end selection emits no fabricated reward or leave");
            RoundTripNativeEnd(f);f.Reload();Check(f.View.presentation.context==context,"native54/61 reconnect uses frozen B source");
            Check(f.server.ReceiveBattlePacket(Suffix(1)).Accepted&&f.View.presentation.context==context,"late66 does not retarget running B");
        }
    }
    private static void NativeEndLifecycle()
    {
        foreach(int outcome in new[]{1,2,3,4})foreach(byte flag in new byte[]{0,1,2,255})
        {
            var f=new Fixture();f.Enter();string economy=Economy(f.View.record),before=State(f.View.record);
            var end=EndNotice(f,flag);
            Check(f.server.ReceiveBattlePacket(end).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==before,"61 does not invent missing54 table");
            var table=RawNotice(f,RawReport(outcome));f.store.fail=true;
            Check(f.server.ReceiveBattlePacket(table).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"54 pre-end table save is atomic");
            f.store.fail=false;Check(f.server.ReceiveBattlePacket(table).Accepted,"54 table received before61");
            Check(f.View.record.campaignPhase=="explore"&&f.View.record.scenarioVisit.result==null&&Economy(f.View.record)==economy,"54 alone does not clear, exit, heal or grant display amounts");
            Check(!f.server.ReceiveBattlePacket(table).Accepted,"54 pre-end duplicate rejected");
            end=EndNotice(f,flag);f.store.fail=true;before=State(f.View.record);
            Check(f.server.ReceiveBattlePacket(end).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"61 save failure has no partial UI stage or exit");
            f.store.fail=false;var manager=new LocalServerManager(f.server);int immediateMaps=0,immediateExits=0,immediateLeaves=0;manager.Notify_BattlePacketRequested+=p=>{Check(p.SequenceEqual(new byte[]{0x5b}),"native cleanup emits exact root5B host leave bytes");immediateLeaves++;};manager.Notify_MapEntered+=_=>immediateMaps++;manager.Notify_ScenarioExited+=_=>immediateExits++;
            Check(manager.Notify_BattlePacket(end).Accepted,"61 common manager ingress accepts same notice after save recovery");
            Check(immediateMaps==(flag==1&&outcome!=1?1:0)&&immediateExits==immediateMaps&&immediateLeaves==immediateMaps,"only immediate native cleanup publishes map/exit events at receipt");
            Check(Economy(f.View.record)==economy,"61 has no reward or resource outcome");
            if(flag==1&&outcome!=1)
            {Check(f.View.record.map==1000&&f.View.record.lastScenarioResult.native.outcome==outcome,"flag1 nonvictory cleans up directly");continue;}
            Check(!f.server.ReceiveBattlePacket(end).Accepted,"61 duplicate cannot restart notice/result/DEE");
            var received=f.View.record.scenarioVisit.nativeEnd;
            if(flag==1)
            {Check(received.stage==NativeScenarioEndRecord.Outro&&f.View.record.campaignPhase=="outro","flag1 victory skips notice and result and begins DEE");FinishNativeEnding(f);continue;}
            Check(received.stage==OriginalScenarioEnd.Stage(outcome,flag)&&f.View.record.campaignPhase=="explore","normal61 stage depends on original table outcome; only exact flag1 skips");
            Check(f.server.MoveWorld(f.View.record.x+1,f.View.record.y).Failure==WorldFailure.Busy,"end notice/result locks world input");
            var exposure=f.View;exposure.record.scenarioVisit.nativeEnd.table.native.rows[0].words[1]=101;
            Check(f.View.record.scenarioVisit.nativeEnd.table.native.rows[0].Job==100,"native end snapshot deep copies table rows");
            before=State(f.View.record);RoundTripNativeEnd(f);f.Reload();Check(State(f.View.record)==before,"notice/result stage persists across service reconnect");
            if(outcome==1||outcome==3)
            {
                f.server.AdvanceWorld(10000);Check(f.View.record.scenarioVisit.nativeEnd.stage==NativeScenarioEndRecord.Notice,"plain61 notice is still active at exactly10000ms");
                f.store.fail=true;before=State(f.View.record);f.server.AdvanceWorld(1);
                Check(State(f.View.record)==before&&f.View.failure==WorldFailure.StorageUnavailable,"notice expiry save failure retains notice for retry");
                f.store.fail=false;f.server.AdvanceWorld(0);Check(f.View.record.scenarioVisit.nativeEnd.stage==NativeScenarioEndRecord.Result,"strictly greater10000ms expiry retries and opens result");
            }
            var result=f.View.record.scenarioVisit.nativeEnd.table;
            Check(f.View.record.campaignPhase=="explore"&&f.View.record.scenarioVisit.result==null,"normal result is displayed before DEE starts");
            before=State(f.View.record);string request=Id();f.store.fail=true;
            Check(f.server.ConfirmScenarioResult(result.id,request).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"result confirm persistence failure keeps result for retry");
            f.store.fail=false;Check(f.server.ConfirmScenarioResult(result.id,request).Accepted,"result confirm retries without replaying a failed transition");
            if(outcome==1)
            {
                Check(f.View.record.campaignPhase=="outro"&&f.View.record.scenarioVisit.nativeEnd.stage==NativeScenarioEndRecord.Outro,"victory confirm now starts original DEE");
                RoundTripNativeEnd(f);f.Reload();Check(f.View.record.campaignPhase=="outro","native DEE journal can reconnect");FinishNativeEnding(f);
            }
            else Check(f.View.record.map==1000&&f.View.record.scenarioVisit==null,"nonvictory confirm returns without DEE");
            Check(f.View.record.lastScenarioResult.native.outcome==outcome&&Economy(f.View.record)==economy,"completed native result stays historical and does not award spoils/XP");
            Check(f.View.record.x==21&&f.View.record.y==21,"native cleanup uses actual stored source tile");f.Reload();
            Check(f.View.record.scenarioVisit==null&&f.View.record.lastScenarioResult.native.outcome==outcome,"confirmed native end remains closed after reconnect");
        }
    }
    private static void NativeEndManagerCases()
    {
        foreach(string disable in new[]{"none","world","packet","map","replace-and-forge"})
        {
            var f=new Fixture();f.Enter();var manager=new LocalServerManager(f.server);
            Check(manager.Notify_BattlePacket(RawNotice(f,RawReport())).Accepted&&manager.Notify_BattlePacket(EndNotice(f)).Accepted,"manager native victory setup");
            Check(f.server.ContinueCampaignPresentation().Accepted,"manager native notice acknowledge");int maps=0,exits=0,worlds=0,leaves=0;
            manager.Notify_BattlePacketRequested+=p=>{Check(p.SequenceEqual(new byte[]{0x5b}),"native return host notification has no invented fields");leaves++;if(disable=="packet")manager.isActiveAndEnabled=false;};
            manager.Notify_MapEntered+=_=>maps++;manager.Notify_ScenarioExited+=_=>exits++;
            Check(manager.Request_ConfirmScenarioResult(f.View.record.scenarioVisit.nativeEnd.table.id,Id()).Accepted&&maps==0&&exits==0&&leaves==0,"result confirm starts DEE without a false map-entry or exit event");
            manager.Notify_World+=r=>
            {
                worlds++;if(disable=="world")manager.isActiveAndEnabled=false;
                if(disable=="replace-and-forge")
                {
                    Check(f.server.EnterCampaignMap(26).Accepted,"first return callback immediately enters a new battle");var now=f.View;
                    r.View.characterId=now.characterId;r.View.commanderSession=now.commanderSession;r.View.campaignSession=now.campaignSession;r.View.record.map=now.record.map;
                }
            };
            if(disable=="map")manager.Notify_MapEntered+=_=>manager.isActiveAndEnabled=false;
            for(int n=0;n<10000&&worlds==0;n++)
            {if(f.View.presentation?.waitingMessage==true)f.server.ContinueCampaignPresentation();manager.TestAdvanceWorld(1000);}
            Check(f.View.record.map==(disable=="replace-and-forge"?26:1000)&&worlds==1,"automatic native cleanup publishes its accepted world transition once");
            Check(leaves==(disable=="world"||disable=="replace-and-forge"?0:1)&&maps==(disable=="world"||disable=="packet"||disable=="replace-and-forge"?0:1)&&exits==(disable=="none"?1:0),"return event delivery rechecks lifetime after each callback: "+disable);
        }
    }
    private static void NativeLeaveSaveFailure()
    {
        var f=new Fixture();f.Enter();var manager=new LocalServerManager(f.server);int packets=0,maps=0;
        manager.Notify_BattlePacketRequested+=p=>{Check(p.SequenceEqual(new byte[]{0x5b}),"only the recovered one-byte leave notification is emitted");packets++;};
        manager.Notify_MapEntered+=_=>maps++;
        Check(manager.Notify_BattlePacket(RawNotice(f,RawReport(2))).Accepted&&manager.Notify_BattlePacket(EndNotice(f)).Accepted&&packets==0,"native nonvictory result awaits confirm without sending leave early");
        string id=f.View.record.scenarioVisit.nativeEnd.table.id,request=Id(),before=State(f.View.record);f.store.fail=true;
        Check(manager.Request_ConfirmScenarioResult(id,request).Failure==WorldFailure.StorageUnavailable&&packets==0&&maps==0&&State(f.View.record)==before,"failed return emits no5B and leaves the report retryable");
        f.store.fail=false;Check(manager.Request_ConfirmScenarioResult(id,request).Accepted&&packets==1&&maps==1,"same request emits one host leave notification after successful durable return");
        Check(!manager.Request_ConfirmScenarioResult(id,request).Accepted&&packets==1&&maps==1,"duplicate result confirmation cannot send another5B");
    }
    private static void NativeEndConcurrentServices()
    {
        var f=new Fixture();f.Enter();Check(f.server.ReceiveBattlePacket(RawNotice(f,RawReport())).Accepted,"concurrency fixture54");
        var old=f.server;var oldEnd=EndNotice(f);f.Reload();
        Check(f.server.ReceiveBattlePacket(EndNotice(f)).Accepted,"new service advances prepared table to notice");string durable=State(f.store.data);
        Check(old.ReceiveBattlePacket(oldEnd).Failure==WorldFailure.StaleRequest&&State(f.store.data)==durable,"old prepared service cannot overwrite a newer61 stage with the same visit/map/phase");
        old=f.server;f.Reload();Check(f.server.ContinueCampaignPresentation().Accepted,"new service advances notice to result");durable=State(f.store.data);
        old.AdvanceWorld(10001);Check(old.WorldSnapshot().failure==WorldFailure.StaleRequest&&State(f.store.data)==durable,"old notice expiry cannot overwrite an already acknowledged result");
        var earlier=f.server;var delayed=RawNotice(f,RawReport());f.Reload();var newer=RawNotice(f,RawReport());
        Check(f.server.ReceiveBattlePacket(newer).Accepted,"current54 can replace display table after61 without changing end outcome");durable=State(f.store.data);
        Check(earlier.ReceiveBattlePacket(delayed).Failure==WorldFailure.StaleRequest&&State(f.store.data)==durable,"old service cannot overwrite newer result content while phase remains explore");
    }
    private static void NativeEndGuardsAndTeams()
    {
        foreach(string invalid in new[]{"owner","session","revision","approval","id","short","extra"})
        {
            var f=new Fixture();f.Enter();Check(f.server.ReceiveBattlePacket(RawNotice(f,RawReport())).Accepted,"guard fixture table received");
            var notice=EndNotice(f);if(invalid=="owner")notice.character=Id();if(invalid=="session")notice.session=Id();if(invalid=="revision")notice.expectedRevision++;
            if(invalid=="approval")notice.approved=false;if(invalid=="id")notice.id="bad";if(invalid=="short")notice.packet=new byte[]{0x5c,0x61};if(invalid=="extra")notice.packet=notice.packet.Concat(new byte[]{0}).ToArray();
            string state=State(f.View.record);Check(!f.server.ReceiveBattlePacket(notice).Accepted&&State(f.View.record)==state,"61 malformed/stale envelope preserves whole record: "+invalid);
        }
        var noRoster=new Fixture();noRoster.Enter();string uninitialized=State(noRoster.View.record);
        var premature=new NativeBattleNotice{approved=true,id=Id(),character=noRoster.View.characterId,session=noRoster.View.campaignSession,expectedRevision=0,unitLife=1,packet=OriginalMagicRedirect.Packet(101,0,0,0)};
        Check(noRoster.server.ReceiveBattlePacket(premature,out var absent).Failure==WorldFailure.RulesUnavailable&&absent==null&&State(noRoster.View.record)==uninitialized,"68 before the first roster safely defers without a NullReference or fake unit creation");
        var shape=new Fixture();shape.Enter();Check(shape.server.ReceiveBattlePacket(RawNotice(shape,RawReport())).Accepted,"journal shape table");
        var invalidJournal=shape.View.record;invalidJournal.scenarioVisit.nativeEnd.stage=NativeScenarioEndRecord.Notice;invalidJournal.scenarioVisit.nativeEnd.receipt=Id();invalidJournal.campaignPhase="intro";
        Check(!WorldRecord.Valid(invalidJournal),"a received end notice cannot be restored as an active intro");
        invalidJournal.campaignPhase="explore";invalidJournal.scenarioVisit.nativeEnd.flag=1;
        Check(!WorldRecord.Valid(invalidJournal),"exact flag1 cannot persist an impossible notice/result stage");
        var g=new Fixture();g.Enter();Check(g.server.ReceiveBattlePacket(RawNotice(g,RawReport())).Accepted,"team fixture table");
        var names=Enumerable.Range(0,21).Select(i=>new NativeBattleTeamName{team=i==20?0:i,name=System.Text.Encoding.ASCII.GetBytes(i==20?"last":"first")}).ToArray();
        var team=new NativeBattleNotice{id=Id(),character=g.View.characterId,session=g.View.campaignSession,approved=true,expectedRevision=0,expectedTeamRevision=0,localTeam=0,packet=OriginalBattleTeamNames.Packet(names)};
        Check(g.server.ReceiveBattlePacket(team).Accepted,"actual60 supplies current-team context");Check(g.server.ReceiveBattlePacket(EndNotice(g)).Accepted,"61 uses current native global name");
        Check(System.Text.Encoding.ASCII.GetString(g.View.record.scenarioVisit.nativeEnd.winner)=="last","winner uses last matching row over21, unlike status first9/firstmatch");
        g.Reload();Check(System.Text.Encoding.ASCII.GetString(g.View.record.scenarioVisit.nativeEnd.winner)=="last","winner notice name survives reconnect as received bytes");
        Check(g.server.ContinueCampaignPresentation().Accepted&&g.View.record.scenarioVisit.nativeEnd.stage==NativeScenarioEndRecord.Result,"notice click opens result immediately");
        var h=new Fixture();h.Enter();Check(h.server.ReceiveBattlePacket(RawNotice(h,RawReport())).Accepted,"unknown team fixture table");
        team.id=Id();team.character=h.View.characterId;team.session=h.View.campaignSession;team.localTeam=null;Check(h.server.ReceiveBattlePacket(team).Accepted,"team table can arrive without resolved local team");
        var unresolved=EndNotice(h);string before=State(h.View.record);
        Check(h.server.ReceiveBattlePacket(unresolved).Failure==WorldFailure.RulesUnavailable&&State(h.View.record)==before,"unknown global winner name is not guessed from player profile");
        unresolved.localTeam=0;unresolved.expectedTeamRevision=0;
        Check(h.server.ReceiveBattlePacket(unresolved).Failure==WorldFailure.StaleRequest&&State(h.View.record)==before,"explicit stale winner-table context rejected before notice mutation");
        unresolved.expectedTeamRevision=null;
        Check(h.server.ReceiveBattlePacket(unresolved).Failure==WorldFailure.RulesUnavailable&&State(h.View.record)==before,"explicit local team without its table revision is not guessed");
        unresolved.expectedTeamRevision=h.View.battleTeamRevision;
        Check(h.server.ReceiveBattlePacket(unresolved).Accepted&&System.Text.Encoding.ASCII.GetString(h.View.record.scenarioVisit.nativeEnd.winner)=="last","same61 succeeds once explicit current-team context is supplied");
    }
    internal static void RunNativeResults(string path)
    {root=path;CodecCases();NativeEndSuffixSelection();NativeEndLifecycle();NativeEndGuardsAndTeams();NativeEndLatePackets();NativeEndManagerCases();NativeEndConcurrentServices();NativeLeaveSaveFailure();VictoryResult();foreach(int outcome in new[]{2,3,4})ExitResult(outcome);InvalidResults();Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual54 table and61 service/manager ingress, notice/result/DEE/return stages, strict notice expiry, save retry/reconnect, global winner names, legacy typed phase/exit contracts, display versus rewards, codec bounds, save failure/retry/deep-copy/reconnect/origin return; no original server awards or real Unity Play."}));}
}
