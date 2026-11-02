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
            if(world.Map(next.map).training||world.Definition.campaign?.Map(next.map)?.kind!="scenario")
            {next.scenarioVisit=null;return WorldFailure.None;}
            // Nested scenario visits require a separate source-evidenced transition policy.
            if(world.Record.scenarioVisit!=null)return WorldFailure.RulesUnavailable;
            next.scenarioVisit=new CampaignVisitRecord{id=Guid.NewGuid().ToString("N"),map=next.map,sourceMap=world.Record.map,
                x=world.Record.x,y=world.Record.y,direction=world.Record.direction};
            return WorldFailure.None;
        }
        public WorldReply ReceiveScenarioResult(CampaignResultNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(notice?.result==null)return WorldResult(WorldFailure.InvalidInput);
            var result=notice.result;var visit=world.Record.scenarioVisit;
            if(!notice.approved||notice.character!=worldCharacter||notice.session!=world.CampaignSession||visit==null||result.visit!=visit.id
                ||result.map!=world.Record.map||visit.result!=null||world.CampaignNotices.Contains(result.id))return WorldResult(WorldFailure.StaleRequest);
            if(world.Definition.campaign?.Map(result.map)?.kind!="scenario"||world.Moving||world.Kind!=FacilityKind.None||world.PendingPortal!=null
                ||world.PhaseSavePending||world.Record.campaignPhase!="outro"&&world.Record.campaignPhase!="complete")return WorldResult(WorldFailure.InvalidStage);
            if(!result.victory||!CampaignResultRecord.Valid(result)||result.allies.Concat(result.enemies).Any(u=>world.Definition.campaign.jobs.All(j=>j.job!=u.job)
                ||u.classJob.HasValue&&world.Definition.campaign.jobs.All(j=>j.job!=u.classJob.Value)))return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.scenarioVisit.result=result.Copy();f=CommitWorld(next);
            if(f==WorldFailure.None)world.Record=next;
            return WorldResult(f);
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
