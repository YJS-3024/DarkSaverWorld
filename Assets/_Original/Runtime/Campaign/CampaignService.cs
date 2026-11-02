using System;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ContinueCampaignPresentation()
        { var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f); if (world.Presentation == null) return WorldResult(WorldFailure.InvalidStage); world.Failure=WorldFailure.None;world.Presentation.Continue(); return WorldResult(world.Failure); }
        public CampaignCommand[] DrainCampaignEffects()
        { if (world == null) return Array.Empty<CampaignCommand>(); var result = world.Effects.ToArray(); world.Effects.Clear(); return result; }
        public WorldReply RequestCampaignClear()
        {var f=ReadyWorld();return WorldResult(f==WorldFailure.None?WorldFailure.RulesUnavailable:f);}
        public WorldReply ReceiveCampaignClear(CampaignClearNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            var map=world.Definition.campaign?.Map(world.Record.map);
            if(notice==null||!notice.approved||notice.map!=world.Record.map||notice.session!=world.CampaignSession||!Guid.TryParseExact(notice.id,"N",out _)||world.CampaignNotices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            if(map?.kind!="scenario"||world.Moving||world.SceneLocked||world.Record.campaignPhase=="outro"||world.Record.campaignPhase=="complete")return WorldResult(WorldFailure.InvalidStage);
            if(world.Combat?.Pending!=null||world.Record.campaignProgress?.events.Any(e=>!e.completed)==true)return WorldResult(WorldFailure.Busy);
            var next=world.Record.Copy();next.campaignPhase="outro";next.scenarioBattle=null;f=CommitWorld(next);
            if(f==WorldFailure.None){world.Record=next;world.Combat=null;world.PendingRetreat=null;world.CampaignNotices.Add(notice.id);world.SetPresentation(map.outro);}
            return WorldResult(f);
        }
        public WorldReply CampaignUnitAction(int unit)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            return WorldResult(world.Definition.campaign?.Map(world.Record.map)?.units.Any(u => u.unit == unit) == true ? WorldFailure.RulesUnavailable : WorldFailure.InvalidInput);
        }
        public WorldReply EnterCampaignMap(int target)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            var map = world.Definition.campaign?.Map(target); if (map == null) return WorldResult(WorldFailure.InvalidInput);
            if (world.Moving || world.Kind != FacilityKind.None || world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            var authored=world.Map(world.Record.map).portals.FirstOrDefault(p=>p.id==world.PendingPortal&&p.targetMap==target&&!string.IsNullOrEmpty(p.gateway));
            if(authored!=null)return EnterWorldPortal(authored.id,world.Map(target).training);
            var link = world.Definition.campaign.connections.FirstOrDefault(c => c.fromMap == world.Record.map && c.toMap == target
                && !string.IsNullOrWhiteSpace(c.evidence) && c.cells.Any(p => p.x == world.Record.x && p.y == world.Record.y));
            if (link == null) return WorldResult(WorldFailure.RulesUnavailable);
            var next = world.Record.Copy(); next.map = target; next.x = link.x; next.y = link.y; next.campaignPhase = map.kind == "scenario" ? "intro" : null;
            if (!world.Map(target).map.Contains(new DarkSaver.Original.Tutorial.TutorialTile(next.x, next.y))) return WorldResult(WorldFailure.InvalidInput);
            f=PrepareScenarioVisit(next);if(f!=WorldFailure.None)return WorldResult(f);
            f = CommitWorld(next); if (f == WorldFailure.None) world.Relocate(next); return WorldResult(f);
        }
        private bool ValidateCampaignMapChange(CampaignCommand command)
        {
            int expected=command.name=="CHANGESPRITE"?6:7;
            if(command.args.Length!=expected||command.args.Any(v=>!int.TryParse(v,out _))||world.Definition.campaign?.HasNativeMaps!=true){world.Failure=WorldFailure.RulesUnavailable;return false;}
            try
            {
                var steps=(world.Record.campaignProgress?.steps??Array.Empty<CampaignScriptStep>()).Concat(new[]{new CampaignScriptStep{command=command.raw,scene=command.scene}}).ToArray();
                world.Definition.campaign.ApplyChanges(command.scene,steps);return true;
            }
            catch(System.IO.InvalidDataException){world.Failure=WorldFailure.InvalidInput;return false;}
            catch(InvalidOperationException){world.Failure=WorldFailure.RulesUnavailable;return false;}
        }
        private int? ExecuteCampaignScript(CampaignCommand command)
        {
            string key=command.context+":"+command.line;
            var previous=world.Record.campaignProgress?.steps.FirstOrDefault(s=>s.key==key);
            if(previous!=null)return previous.result;
            var next=world.Record.Copy();
            var step=new CampaignScriptStep{key=key,command=command.raw,scene=command.scene,result=1,packet=OriginalCampaignScript.Packet(command)};
            switch(command.name)
            {
                case "FIRSTCLEAR":step.result=(next.clearedScenarios??Array.Empty<int>()).Contains(next.map)?0:1;break;
                case "SAVECLEAR":next.clearedScenarios=(next.clearedScenarios??Array.Empty<int>()).Concat(new[]{next.map}).Distinct().ToArray();break;
                case "RANDOM":step.result=OriginalCampaignScript.Random(command.Int(0),unchecked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));break;
                case "CHANGESPRITE":case "CHANGETILE2":
                    if(!ValidateCampaignMapChange(command))return null;break;
                default:if(step.packet==null){world.Failure=WorldFailure.RulesUnavailable;return null;}break;
            }
            var progress=next.campaignProgress??new CampaignProgressRecord{visit=next.scenarioVisit?.id??"legacy:"+next.map};
            progress.steps=progress.steps.Concat(new[]{step}).ToArray();progress.revision++;next.campaignProgress=progress;
            if(step.packet!=null)
            {
                var delivery=next.campaignDelivery??new CampaignDeliveryRecord();
                delivery.requests=delivery.requests.Where(d=>!d.acknowledged||d.visit==progress.visit).Concat(new[]{new CampaignDelivery{id=progress.visit+":"+key,visit=progress.visit,map=next.map,command=step.command,packet=(byte[])step.packet.Clone()}}).ToArray();
                delivery.revision++;next.campaignDelivery=delivery;
            }
            var failure=CommitWorld(next);if(failure!=WorldFailure.None){world.Failure=failure;return null;}
            world.Record=next;world.Failure=WorldFailure.None;return step.result;
        }
        public WorldReply RetryCampaignPresentation()
        {var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);if(world.Presentation==null)return WorldResult(WorldFailure.InvalidStage);world.Presentation.Retry();return WorldResult(world.Failure);}
        public CampaignScriptRequest[] PendingCampaignRequests()
        {
            if(ReadyWorld()!=WorldFailure.None)return Array.Empty<CampaignScriptRequest>();
            return (world.Record.campaignDelivery?.requests??Array.Empty<CampaignDelivery>()).Where(s=>!s.acknowledged)
                .Select(s=>new CampaignScriptRequest{id=s.id,character=worldCharacter,session=world.CampaignSession,visit=s.visit,
                    map=s.map,command=s.command,packet=(byte[])s.packet.Clone()}).ToArray();
        }
        public WorldReply AcknowledgeCampaignRequest(CampaignScriptRequest request)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            var delivery=world.Record.campaignDelivery?.requests.FirstOrDefault(s=>s.id==request?.id);
            if(delivery==null||delivery.acknowledged||request.character!=worldCharacter||request.session!=world.CampaignSession
                ||request.visit!=delivery.visit||request.map!=delivery.map||request.command!=delivery.command||request.packet==null||!request.packet.SequenceEqual(delivery.packet))return WorldResult(WorldFailure.StaleRequest);
            var next=world.Record.Copy();next.campaignDelivery.requests.First(s=>s.id==request.id).acknowledged=true;
            next.campaignDelivery.requests=next.campaignDelivery.requests.Where(d=>!d.acknowledged||d.visit==next.scenarioVisit?.id).ToArray();next.campaignDelivery.revision++;
            f=CommitWorld(next);if(f==WorldFailure.None)world.Record=next;return WorldResult(f);
        }
        public WorldReply ReceiveCampaignEvents(CampaignEventsNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(notice==null||!notice.approved||notice.character!=worldCharacter||notice.session!=world.CampaignSession||notice.visit!=world.Record.scenarioVisit?.id
                ||notice.map!=world.Record.map||!Guid.TryParseExact(notice.id,"N",out _))return WorldResult(WorldFailure.StaleRequest);
            var map=world.Definition.campaign?.Map(notice.map);
            if(map?.kind!="scenario"||world.Record.campaignPhase=="outro"||world.Record.campaignPhase=="complete"||world.Moving||world.Kind!=FacilityKind.None||world.Combat?.Pending!=null)return WorldResult(WorldFailure.InvalidStage);
            if(!OriginalCampaignScript.DecodeEvents(notice.packet,out var numbers))return WorldResult(WorldFailure.InvalidInput);
            var bodies=OriginalCampaignScript.Events(map.events);
            if(numbers.Any(n=>!bodies.ContainsKey(n)))return WorldResult(WorldFailure.InvalidInput);
            if(world.Record.campaignProgress?.events.Any(e=>e.id.StartsWith(notice.id+":",StringComparison.Ordinal))==true)return WorldResult(WorldFailure.StaleRequest);
            var next=world.Record.Copy();var progress=next.campaignProgress??new CampaignProgressRecord{visit=next.scenarioVisit?.id??"legacy:"+next.map};
            if(progress.events.Count(e=>!e.completed&&e.id!=world.ActiveCampaignEvent)+numbers.Length>100)return WorldResult(WorldFailure.Busy);
            progress.events=progress.events.Concat(numbers.Select((n,i)=>new CampaignEventRecord{id=notice.id+":"+i,number=n})).ToArray();
            progress.revision++;next.campaignProgress=progress;f=CommitWorld(next);if(f==WorldFailure.None)world.Record=next;return WorldResult(f);
        }
        private bool AdvanceCampaignEvents(double ms)
        {
            world.Presentation?.Advance(ms);
            if(world.ActiveCampaignEvent!=null&&world.Presentation?.Snapshot().completed==true)
            {
                var next=world.Record.Copy();next.campaignProgress.events.First(e=>e.id==world.ActiveCampaignEvent).completed=true;next.campaignProgress.revision++;
                var f=CommitWorld(next);world.PhaseSavePending=f!=WorldFailure.None;
                if(f!=WorldFailure.None){world.Failure=f;return false;}world.Record=next;world.ActiveCampaignEvent=null;
            }
            if(world.ActiveCampaignEvent==null&&world.Record.campaignPhase=="explore"&&(world.Presentation==null||world.Presentation.Snapshot().completed))
            {
                var pending=world.Record.campaignProgress?.events.FirstOrDefault(e=>!e.completed);
                if(pending!=null)
                {
                    var body=OriginalCampaignScript.Events(world.Definition.campaign.Map(world.Record.map).events)[pending.number];
                    var actors=world.Combat!=null?world.Combat.Units.Select(u=>new CampaignActor{unit=u.id,job=u.job,sourceJob=u.id==0?"COM0":u.job.ToString(),x=u.x,y=u.y,renderX=u.x,renderY=u.y,direction=u.direction,dead=u.hp==0}).ToArray():
                        world.Definition.campaign.Map(world.Record.map).units.Select(u=>new CampaignActor{unit=u.unit,job=u.job,x=u.x,y=u.y,renderX=u.x,renderY=u.y,direction=u.direction})
                        .Concat(new[]{new CampaignActor{unit=0,job=world.CommanderJob,sourceJob="COM0",x=world.Record.x,y=world.Record.y,renderX=world.Record.x,renderY=world.Record.y,direction=world.Record.direction}}).ToArray();
                    world.SetPresentation(string.Join("\n",body.Select(c=>c.raw)),"event:"+pending.id,actors);world.ActiveCampaignEvent=pending.id;
                }
            }
            return true;
        }

    }
}
