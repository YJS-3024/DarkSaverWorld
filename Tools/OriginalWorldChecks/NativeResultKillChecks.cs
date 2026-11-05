using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeResultKillEntry{public static void Main(string[] args)=>ResultChecks.RunNativeKills(args[0]);}
internal static partial class ResultChecks
{
    private static NativeBattleNotice KillNotice(Fixture f,params int[] jobs)=>new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,
        approved=true,expectedRevision=f.View.battle?.revision??0,expectedResultKillRevision=f.View.resultKillRevision,packet=OriginalResultKillPackets.Packet(new NativeResultKills{jobs=jobs})};
    private static void KillCodec()
    {
        foreach(int count in new[]{0,1,2,11,1000})
        {
            var report=new NativeResultKills{jobs=Enumerable.Range(0,count).Select(i=>i%2==0?(int)short.MinValue:short.MaxValue).ToArray()};
            var packet=OriginalResultKillPackets.Packet(report);
            Check(OriginalResultKillPackets.TryDecode(packet,out var decoded)&&decoded.jobs.SequenceEqual(report.jobs)&&OriginalResultKillPackets.Packet(decoded).SequenceEqual(packet),"31 signed ability keys round-trip without conversion to unit IDs");
            foreach(int length in Enumerable.Range(0,packet.Length))Check(!OriginalResultKillPackets.TryDecode(packet.Take(length).ToArray(),out _),"31 truncated prefix rejected");
            Check(!OriginalResultKillPackets.TryDecode(packet.Concat(new byte[]{0}).ToArray(),out _),"31 trailing bytes rejected");
        }
        Check(!OriginalResultKillPackets.TryDecode(new byte[]{0x5c,0x31,255,255},out _)&&!OriginalResultKillPackets.TryDecode(new byte[]{0x5c,0x31,233,3},out _),"negative/over-adapter-limit count rejected");
    }
    private static void KillBeforeAndAfterResult()
    {
        var f=new Fixture();f.Enter();var n=KillNotice(f,100,101);string resources=Economy(f.View.record),record=State(f.View.record),disk=State(f.store.data);
        Check(f.View.resultKills==null&&f.View.resultKillRevision==0,"no unreceived kill table is fabricated");f.store.fail=true;
        Check(f.server.ReceiveBattlePacket(n).Accepted&&f.View.resultKillRevision==1&&f.View.resultKills.jobs.SequenceEqual(new[]{100,101})
            &&State(f.View.record)==record&&State(f.store.data)==disk,"31 before result is presentation-only session data and requires no account write");f.store.fail=false;
        Check(f.View.record.campaignPhase=="explore"&&f.View.record.scenarioVisit.result==null&&Economy(f.View.record)==resources,"31 cannot declare clear, remove actors, award XP/money/items or heal");
        n.expectedResultKillRevision=1;Check(!f.server.ReceiveBattlePacket(n).Accepted,"31 replay ID remains rejected even with a fresh revision");
        var exposed=f.View.resultKills;exposed.jobs[0]=101;Check(f.View.resultKills.jobs[0]==100,"pending31 table is copied out defensively");
        f.Clear();f.FinishEnding();var result=RawNotice(f,RawReport());record=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattlePacket(result).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==record&&f.View.resultKills.jobs.Length==2,"failed54 keeps received31 pending without a partial report");
        f.store.fail=false;Check(f.server.ReceiveBattlePacket(result).Accepted&&f.View.record.scenarioVisit.result.kills==2
            &&f.View.record.scenarioVisit.result.nativeKills.revision==1&&Economy(f.View.record)==resources,"54 stores the explicit31 list without a reward grant");
        f.Reload();Check(f.View.resultKillRevision==1&&f.View.resultKills.jobs.SequenceEqual(new[]{100,101}),"received result31 restores on a new service session");
        var late=KillNotice(f,101);record=State(f.View.record);disk=State(f.store.data);f.store.fail=true;
        Check(f.server.ReceiveBattlePacket(late).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==record&&State(f.store.data)==disk
            &&f.View.resultKillRevision==1,"late31 storage failure preserves durable result and session table");
        f.store.fail=false;Check(f.server.ReceiveBattlePacket(late).Accepted&&f.View.resultKillRevision==2&&f.View.record.scenarioVisit.result.kills==1
            &&Economy(f.View.record)==resources,"same late31 retries and replaces only result display");
        string path=Path.Combine(Path.GetTempPath(),"ds-result31-"+Id(),"accounts.json");
        try
        {
            var file=new LocalAccountFileStore(path);Check(file.TryWrite(f.store.data)&&file.TryRead(out var restored)
                &&restored.accounts[0].character.world.scenarioVisit.result.nativeKills.jobs.SequenceEqual(new[]{101})
                &&restored.accounts[0].character.world.scenarioVisit.result.nativeKills.revision==2,"31 jobs and local revision survive an actual DataContract file round-trip");
            var reopened=new EntryAccountService(file);new EntryClient(reopened).OpenLocalProfile();Check(reopened.BeginWorld(f.definition).Accepted
                &&reopened.WorldSnapshot().resultKillRevision==2&&reopened.WorldSnapshot().resultKills.jobs.Single()==101,"real file account reconnect restores explicit result list");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        var invalid=f.View.record.scenarioVisit.result.Copy();invalid.kills=99;Check(!CampaignResultRecord.Valid(invalid),"stored total cannot disagree with explicit job list");
        invalid=f.View.record.scenarioVisit.result.Copy();invalid.nativeKills.jobs[0]=-1;Check(!CampaignResultRecord.Valid(invalid),"negative ability keys cannot enter persisted result data");
        Check(f.server.ReceiveBattlePacket(KillNotice(f)).Accepted&&f.View.resultKillRevision==3&&f.View.resultKills.jobs.Length==0&&f.View.record.scenarioVisit.result.kills==0,"explicit empty31 is distinct from missing data");
        string resultId=f.View.record.scenarioVisit.result.id;Check(f.server.ConfirmScenarioResult(resultId,Id()).Accepted&&f.View.record.map==1000
            &&f.View.resultKills==null&&f.View.resultKillRevision==0&&f.View.record.lastScenarioResult.nativeKills.jobs.Length==0&&Economy(f.View.record)==resources,"confirm returns to origin, retains last report and expires active kill-session data");
    }
    private static void InvalidKillNotices()
    {
        foreach(string fault in new[]{"owner","session","id","approval","revision","missing-counter","counter","count","job","length","trailing","prefix"})
        {
            var f=new Fixture();f.Enter();var n=KillNotice(f,100);if(fault=="owner")n.character=Id();if(fault=="session")n.session=Id();if(fault=="id")n.id="bad";
            if(fault=="approval")n.approved=false;if(fault=="revision")n.expectedRevision++;if(fault=="missing-counter")n.expectedResultKillRevision=null;if(fault=="counter")n.expectedResultKillRevision=1;
            if(fault=="count")n.packet[2]=255;if(fault=="job")n.packet=OriginalResultKillPackets.Packet(new NativeResultKills{jobs=new[]{32767}});
            if(fault=="length")n.packet=n.packet.Take(5).ToArray();if(fault=="trailing")n.packet=n.packet.Concat(new byte[]{0}).ToArray();if(fault=="prefix")n.packet[0]=0;
            string before=State(f.View.record),disk=State(f.store.data);Check(!f.server.ReceiveBattlePacket(n).Accepted&&State(f.View.record)==before&&State(f.store.data)==disk
                &&f.View.resultKills==null&&f.View.resultKillRevision==0,"invalid31 is atomic "+fault);
        }
        var outside=new Fixture();var packet=KillNotice(outside,100);Check(!outside.server.ReceiveBattlePacket(packet).Accepted,"town31 cannot create a scenario result or monster placement");
        var pending=new Fixture();pending.Enter();Check(pending.server.ReceiveBattlePacket(KillNotice(pending,100)).Accepted,"pending31 accepted");var stale=KillNotice(pending,101);pending.Reload();
        Check(pending.View.resultKills==null&&!pending.server.ReceiveBattlePacket(stale).Accepted,"pre-result31 is ephemeral and old session notices cannot restore it after reconnect");
    }
    private static void KillConcurrentServices()
    {
        var f=new Fixture();f.Enter();f.Clear();f.FinishEnding();Check(f.server.ReceiveBattlePacket(RawNotice(f,RawReport())).Accepted,"concurrency fixture has explicit result");
        var old=f.server;var oldNotice=KillNotice(f,100);f.Reload();Check(f.server.ReceiveBattlePacket(KillNotice(f,101)).Accepted,"new service stores current kill result");
        string disk=State(f.store.data),before=State(old.WorldSnapshot().record);
        Check(old.ReceiveBattlePacket(oldNotice).Failure==WorldFailure.StaleRequest&&State(old.WorldSnapshot().record)==before&&State(f.store.data)==disk,"stale service cannot overwrite a newer kill-table revision of the same result ID");
    }
    private static void ConfirmDuringKillDisplay()
    {
        var f=new Fixture();f.Enter();f.Clear();f.FinishEnding();Check(f.server.ReceiveBattlePacket(RawNotice(f,RawReport())).Accepted,"confirm fixture receives explicit result");
        Check(f.server.ReceiveBattlePacket(KillNotice(f,Enumerable.Repeat(100,11).ToArray())).Accepted,"confirm fixture receives eleven pending kill sprites");
        Check(f.server.ConfirmScenarioResult(f.View.record.scenarioVisit.result.id,Id()).Accepted&&f.View.record.map==1000,"confirmed result does not invent a requirement to finish kill-list animation");
    }
    private static void KillManagerCallbacks()
    {
        foreach(string change in new[]{"replace","logout","disable"})
        {
            var f=new Fixture();f.Enter();var manager=new LocalServerManager(f.server);int deliveries=0;bool once=true;
            manager.Notify_BattleChanged+=_=>deliveries++;
            manager.Notify_World+=_=>
            {
                if(!once)return;once=false;
                if(change=="replace")Check(f.server.ReceiveBattlePacket(KillNotice(f,101)).Accepted,"first world callback receives a newer31 table");
                else if(change=="logout")new EntryClient(f.server).Logout();else manager.isActiveAndEnabled=false;
            };
            Check(manager.Notify_BattlePacket(KillNotice(f,100)).Accepted&&deliveries==0,"old battle callback suppressed after world callback "+change);
            if(change=="replace")Check(f.View.resultKills.jobs.Single()==101&&f.View.resultKillRevision==2,"new31 survives rejection of old callback payload");
        }
        var normal=new Fixture();normal.Enter();var active=new LocalServerManager(normal.server);int count=0;active.Notify_BattleChanged+=_=>count++;
        Check(active.Notify_BattlePacket(KillNotice(normal,100)).Accepted&&count==1,"normal actual manager ingress dispatches31 once");
    }
    internal static void RunNativeKills(string path)
    {root=path;KillCodec();KillBeforeAndAfterResult();InvalidKillNotices();KillConcurrentServices();ConfirmDuringKillDisplay();KillManagerCallbacks();Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual31 service and manager ingress with original WorldRequest/context guard, pre/post54 state, no rewards/clear, save rollback/retry, file reconnect, stale-service revision and reentrant callback checks. No actual Unity Play."}));}
}
