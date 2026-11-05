using System;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldFailure PrepareScenarioVisit(WorldRecord next)
        {
            next.scenarioBattle=null;next.campaignProgress=null;
            if(world.Record.ghost&&(world.Map(next.map).training||world.Definition.campaign?.Map(next.map)?.kind=="scenario"))return WorldFailure.InvalidStage;
            var entry=ScenarioGatewayData.SelectEntry(world.Definition.gateways,next.map,world.CommanderJob);
            if(entry!=null)
            {
                if(!ScenarioGatewayData.EntryLocationAllowed(world.Definition,entry))return WorldFailure.InvalidInput;
                next.x=entry.x;next.y=entry.y;next.direction=1;
            }
            if(world.Map(next.map).training||world.Definition.campaign?.Map(next.map)?.kind!="scenario")
            {next.scenarioVisit=null;return WorldFailure.None;}
            // Nested scenario visits require a separate source-evidenced transition policy.
            if(world.Record.scenarioVisit!=null)return WorldFailure.RulesUnavailable;
            next.scenarioVisit=new CampaignVisitRecord{id=Guid.NewGuid().ToString("N"),map=next.map,sourceMap=world.Record.map,
                x=world.Record.x,y=world.Record.y,direction=world.Record.direction};
            return WorldFailure.None;
        }
        public WorldReply ReceiveScenarioResult(CampaignResultNotice notice)
            =>ReceiveScenarioResult(notice,false);
        private WorldReply ReceiveNativeResultKills(NativeBattleNotice notice)
        {
            var ready=ReadyWorld();if(ready!=WorldFailure.None)return WorldResult(ready);var input=notice?.Copy();
            if(input?.approved!=true||!OriginalResultKillPackets.TryDecode(input.packet,out var report))return WorldResult(WorldFailure.InvalidInput);
            var visit=world.Record.scenarioVisit;
            if(visit==null||world.Definition.campaign?.Map(world.Record.map)?.kind!="scenario")return WorldResult(WorldFailure.InvalidStage);
            if(!input.expectedResultKillRevision.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            bool current=world.NativeKillSession==world.CampaignSession;
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=(world.Combat?.Revision??0)||input.expectedResultKillRevision!=world.CurrentNativeKillRevision
                ||world.CurrentNativeKillRevision==long.MaxValue||current&&world.NativeKillNotices.Contains(input.id))return WorldResult(WorldFailure.StaleRequest);
            if(report.jobs.Any(job=>world.Definition.campaign.jobs.All(j=>j.job!=job)))return WorldResult(WorldFailure.InvalidInput);
            report.revision=world.CurrentNativeKillRevision+1;
            if(visit.result!=null)
            {
                var next=world.Record.Copy();next.scenarioVisit.result.nativeKills=report.Copy();next.scenarioVisit.result.kills=report.jobs.Length;
                ready=CommitWorld(next);if(ready!=WorldFailure.None)return WorldResult(ready);world.Record=next;
            }
            // DEC0 replaces only the display list. No unit removal, victory,
            // HP/MP, ownership, XP, gold or item outcome is generated here.
            if(!current)world.NativeKillNotices.Clear();world.NativeKillSession=world.CampaignSession;
            world.NativeKills=report.Copy();world.NativeKillNotices.Add(input.id);return WorldResult(WorldFailure.None);
        }
        private WorldReply ReceiveScenarioResult(CampaignResultNotice notice,bool nativeReport)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(notice?.result==null)return WorldResult(WorldFailure.InvalidInput);
            var result=notice.result;var visit=world.Record.scenarioVisit;
            if(!notice.approved||notice.character!=worldCharacter||notice.session!=world.CampaignSession||visit==null||result.visit!=visit.id
                ||result.map!=world.Record.map||visit.result!=null||world.CampaignNotices.Contains(result.id))return WorldResult(WorldFailure.StaleRequest);
            if(world.Definition.campaign?.Map(result.map)?.kind!="scenario"||world.Moving||world.Kind!=FacilityKind.None||world.PendingPortal!=null
                ||world.PhaseSavePending||world.Record.campaignPhase!="outro"&&world.Record.campaignPhase!="complete")return WorldResult(WorldFailure.InvalidStage);
            if(result.victory&&world.Record.lastScenarioExit?.visit==visit.id)return WorldResult(WorldFailure.InvalidStage);
            if(!nativeReport&&!result.victory||!CampaignResultRecord.Valid(result)||result.allies.Concat(result.enemies).Any(u=>world.Definition.campaign.jobs.All(j=>j.job!=u.job)
                ||u.classJob.HasValue&&world.Definition.campaign.jobs.All(j=>j.job!=u.classJob.Value))
                ||result.nativeKills?.jobs.Any(job=>world.Definition.campaign.jobs.All(j=>j.job!=job))==true)return WorldResult(WorldFailure.InvalidInput);
            var received=result.Copy();var kills=world.NativeKillView();
            if(kills!=null){received.nativeKills=kills;received.kills=kills.jobs.Length;}
            var next=world.Record.Copy();next.scenarioVisit.result=received;f=CommitWorld(next);
            if(f==WorldFailure.None)world.Record=next;
            return WorldResult(f);
        }
        private WorldReply ReceiveNativeScenarioResult(NativeBattleNotice notice)
        {
            var ready=ReadyWorld();if(ready!=WorldFailure.None)return WorldResult(ready);var input=notice?.Copy();
            if(input?.approved!=true||!OriginalResultPackets.TryDecode(input.packet,out var native)||!native.SafeDisplay)return WorldResult(WorldFailure.InvalidInput);
            var visit=world.Record.scenarioVisit;
            if(visit==null||input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=(world.Combat?.Revision??0))return WorldResult(WorldFailure.StaleRequest);
            if(native.outcome<1||native.outcome>4)return WorldResult(WorldFailure.RulesUnavailable);
            var rows=new CampaignResultUnit[native.rows.Length];
            for(int i=0;i<rows.Length;i++)
            {
                var row=native.rows[i];var job=world.Definition.campaign?.jobs.FirstOrDefault(j=>j.job==row.Job);
                if(job==null||row.CompanionJob>0&&world.Definition.campaign.jobs.All(j=>j.job!=row.CompanionJob))return WorldResult(WorldFailure.InvalidInput);
                // The first wire WORD is retained verbatim in native; the display row
                // index is not inferred to be a live unit/party ownership identifier.
                rows[i]=new CampaignResultUnit{unit=i,job=row.Job,name=world.Definition.combatCatalog?.Job(row.Job)?.name??row.Job.ToString(),experience=row.experience};
            }
            var result=new CampaignResultRecord{id=input.id,visit=visit.id,map=world.Record.map,victory=native.outcome==1,
                outcome=native.outcome==1?"victory":native.outcome==2?"retreat":native.outcome==3?"defeat":"terminated",
                spoils=native.spoils,eventCompleted=native.eventCompleted,eventTotal=native.eventTotal,allies=rows,native=native};
            if(native.outcome!=1)
            {
                var exit=world.Record.lastScenarioExit;
                var expected=native.outcome==2?CampaignExitKind.Retreat:native.outcome==3?CampaignExitKind.Defeat:CampaignExitKind.Terminated;
                if(world.Record.campaignPhase!="complete"||exit==null||exit.visit!=visit.id||exit.kind!=expected)return WorldResult(WorldFailure.InvalidStage);
            }
            // D280 only fills the report table. It never declares victory/defeat,
            // ends a live battle, grants rewards or invents an exit response.
            return ReceiveScenarioResult(new CampaignResultNotice{approved=true,character=input.character,session=input.session,result=result},true);
        }
        public WorldReply ConfirmScenarioResult(string resultId,string request)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            var visit=world.Record.scenarioVisit;
            if(!Guid.TryParseExact(request,"N",out _)||visit?.result?.id!=resultId||resultId==null||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.Record.campaignPhase!="complete"||world.PhaseSavePending||world.Presentation!=null&&!world.Presentation.Snapshot().completed||world.Moving||world.Kind!=FacilityKind.None)
                return WorldResult(WorldFailure.InvalidStage);
            f=ScenarioReturnOrigin(visit);if(f!=WorldFailure.None)return WorldResult(f);
            var next=world.Record.Copy();next.lastScenarioResult=visit.result.Copy();SetScenarioReturn(next,visit);
            f=CommitWorld(next);if(f==WorldFailure.None){world.Applied.Add(request);world.Relocate(next);}
            return WorldResult(f);
        }
    }
}
