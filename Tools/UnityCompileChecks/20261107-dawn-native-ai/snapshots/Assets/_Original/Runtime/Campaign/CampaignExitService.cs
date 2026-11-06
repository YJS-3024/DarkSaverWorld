using System;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldFailure ScenarioExitReady()
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Map(world.Record.map).training||world.Definition.campaign?.Map(world.Record.map)?.kind!="scenario"
                ||world.Record.scenarioVisit==null||world.Record.campaignPhase!="explore")return WorldFailure.InvalidStage;
            if(world.Kind!=FacilityKind.None||world.PendingPortal!=null||world.SceneLocked)return WorldFailure.Busy;
            return WorldFailure.None;
        }
        public WorldReply RequestScenarioRetreat(string visit,string request)
        {
            var f=ScenarioExitReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Record.scenarioVisit.id!=visit||!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.Moving||world.PendingRetreat!=null||world.Combat?.Pending!=null)return WorldResult(WorldFailure.Busy);
            world.PendingRetreat=new CampaignRetreatRequest{character=worldCharacter,session=world.CampaignSession,visit=visit,id=request,battleRevision=world.Record.scenarioBattle?.revision??0};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteScenarioRetreat(string visit,Func<CampaignRetreatRequest,WorldView,CampaignExitNotice> authority)
        {
            var f=ScenarioExitReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var id=Guid.NewGuid().ToString("N");var queued=RequestScenarioRetreat(visit,id);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(world.PendingRetreat.Copy(),queued.View);
                if(notice!=null)
                {
                    if(notice.request!=id||notice.kind!=CampaignExitKind.Retreat)
                    {if(world.PendingRetreat?.id==id)world.PendingRetreat=null;return WorldResult(WorldFailure.StaleRequest);}
                    var result=ReceiveScenarioExit(notice);
                    if(!result.Accepted&&world.PendingRetreat?.id==id)world.PendingRetreat=null;
                    return WorldResult(result.Failure);
                }
            }
            catch(Exception){/* Failed local authority must leave the user able to retry. */}
            if(world.PendingRetreat?.id==id)world.PendingRetreat=null;
            return WorldResult(WorldFailure.RulesUnavailable);
        }
        private WorldFailure ScenarioReturnOrigin(CampaignVisitRecord visit)
        {
            var source=world.Map(visit.sourceMap);var tile=new TutorialTile(visit.x,visit.y);
            return source==null||!source.map.Contains(tile)||source.map.Terrain(tile,2)<=0||source.map.Terrain(tile,2)==99
                ||source.npcs.Any(n=>n.x==tile.X&&n.y==tile.Y)||world.Definition.campaign?.Map(source.id)?.units.Any(u=>u.x==tile.X&&u.y==tile.Y)==true
                ?WorldFailure.InvalidInput:WorldFailure.None;
        }
        private void SetScenarioReturn(WorldRecord next,CampaignVisitRecord visit)
        {
            next.map=visit.sourceMap;next.x=visit.x;next.y=visit.y;next.direction=visit.direction;
            // User-confirmed rule, not an independently recovered original server formula.
            next.ghost=next.ghost||next.healthKnown&&next.hp==0;
            next.scenarioVisit=null;next.scenarioBattle=null;next.campaignPhase=null;next.campaignProgress=null;
        }
        public WorldReply ReceiveScenarioExit(CampaignExitNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ScenarioExitReady();if(f!=WorldFailure.None)return WorldResult(f);
            var visit=world.Record.scenarioVisit;var pending=world.PendingRetreat;
            if(notice.character!=worldCharacter||notice.session!=world.CampaignSession||notice.visit!=visit.id
                ||!Guid.TryParseExact(notice.id,"N",out _)||notice.expectedBattleRevision!=(world.Record.scenarioBattle?.revision??0)
                ||world.CampaignNotices.Contains(notice.id)||world.Record.lastScenarioExit?.id==notice.id)return WorldResult(WorldFailure.StaleRequest);
            if(!Enum.IsDefined(typeof(CampaignExitKind),notice.kind))return WorldResult(WorldFailure.InvalidInput);
            if(notice.awaitResult&&notice.result!=null)return WorldResult(WorldFailure.InvalidInput);
            if(notice.kind==CampaignExitKind.Retreat)
            {
                if(pending==null||pending.id!=notice.request||pending.battleRevision!=notice.expectedBattleRevision)return WorldResult(WorldFailure.StaleRequest);
                if(!notice.approved){world.PendingRetreat=null;return WorldResult(WorldFailure.InvalidInput);}
                if(world.Moving||world.Combat?.Pending!=null)return WorldResult(WorldFailure.InvalidInput);
                if(notice.result==null){if(!notice.awaitResult){f=ScenarioReturnOrigin(visit);if(f!=WorldFailure.None)return WorldResult(f);}}
                else if(!notice.result.IsRetreat)return WorldResult(WorldFailure.InvalidInput);
            }
            else
            {
                if(!notice.approved||!string.IsNullOrEmpty(notice.request))return WorldResult(WorldFailure.StaleRequest);
                if(!notice.awaitResult&&(notice.kind==CampaignExitKind.Defeat?notice.result?.IsDefeat!=true:notice.result?.IsTerminated!=true))return WorldResult(WorldFailure.InvalidInput);
            }
            var report=notice.result;
            if(report!=null&&(report.id!=notice.id||report.visit!=visit.id||report.map!=world.Record.map||!CampaignResultRecord.Valid(report)
                ||report.allies.Concat(report.enemies).Any(u=>world.Definition.campaign.jobs.All(j=>j.job!=u.job)
                    ||u.classJob.HasValue&&world.Definition.campaign.jobs.All(j=>j.job!=u.classJob.Value))))return WorldResult(WorldFailure.InvalidInput);
            if(notice.health!=null&&(!notice.health.Valid||world.Record.ghost&&notice.health.hp>0))return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();if(next.scenarioVisit!=null)next.scenarioVisit.nativeEnd=null;
            if(notice.health!=null){next.healthKnown=true;next.hp=notice.health.hp;next.maxHp=notice.health.maxHp;next.mp=notice.health.mp;next.maxMp=notice.health.maxMp;}
            next.lastScenarioExit=new CampaignExitRecord{id=notice.id,visit=visit.id,map=world.Record.map,kind=notice.kind};
            next.scenarioBattle=null;
            if(notice.kind==CampaignExitKind.Retreat&&report==null&&!notice.awaitResult)SetScenarioReturn(next,visit);
            else{next.campaignPhase="complete";next.scenarioVisit.result=notice.result?.Copy();}
            f=CommitWorld(next);if(f!=WorldFailure.None)return WorldResult(f);
            world.Relocate(next);world.PendingRetreat=null;world.Combat=null;world.Presentation=null;world.Effects.Clear();world.PhaseSavePending=false;
            world.CampaignNotices.Add(notice.id);return WorldResult(WorldFailure.None);
        }
    }
}
