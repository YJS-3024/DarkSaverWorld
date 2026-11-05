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
        Check(f.server.ReceiveBattlePacket(notice).Failure==WorldFailure.InvalidStage&&State(f.View.record)==before,"54 victory display cannot fabricate scenario clear");
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
        Check(!f.server.ReceiveBattlePacket(notice).Accepted&&State(f.View.record)==before,"54 display alone cannot end a live scenario "+outcome);
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
    internal static void RunNativeResults(string path)
    {root=path;CodecCases();VictoryResult();foreach(int outcome in new[]{2,3,4})ExitResult(outcome);InvalidResults();Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual54 service/manager ingress, explicit source phase/exit contracts, display versus rewards, codec bounds, save failure/retry/deep-copy/reconnect/origin return; no original server awards or real Unity Play."}));}
}
