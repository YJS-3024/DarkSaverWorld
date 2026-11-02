using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure StatReady()
        {
            var f=ClassReady();if(f!=WorldFailure.None)return f;
            return world.PendingClassOptions!=null||world.PendingClassChange!=null||world.PendingMagicLoadout!=null?WorldFailure.Busy:WorldFailure.None;
        }
        public WorldReply CancelStatAdjustment()
        {var f=ReadyWorld();if(f==WorldFailure.None)world.ClearStatAdjustment();return WorldResult(f);}
        public WorldReply RequestStatAdjustment(StatAdjustmentOperation operation,int stat,int direction,string request)
        {
            var f=StatReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(!Enum.IsDefined(typeof(StatAdjustmentOperation),operation))return WorldResult(WorldFailure.InvalidInput);
            if(world.PendingStatAdjustment!=null)return WorldResult(WorldFailure.Busy);
            if(operation==StatAdjustmentOperation.Confirm&&world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            long revision=world.AdjustmentRecord.statAllocation?.revision??0;
            if(revision==long.MaxValue||world.AdjustmentRecord.growth.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var draft=world.StatDraft;AttributeTriplet[] proposed=null;int points=0;
            if(operation==StatAdjustmentOperation.State)
            {if(draft?.Dirty==true)return WorldResult(WorldFailure.Busy);world.ClearStatAdjustment();draft=null;}
            else
            {
                if(draft==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(!TempleRevivalOffer.SameState(world.AdjustmentRecord,world.StatBaseline)){world.ClearStatAdjustment();return WorldResult(WorldFailure.StaleRequest);}
                if(operation==StatAdjustmentOperation.Step)
                {if(draft.draftRevision==long.MaxValue||!draft.TryStep(stat,direction,out proposed,out points))return WorldResult(WorldFailure.InvalidInput);}
                else
                {if(!draft.Dirty)return WorldResult(WorldFailure.InvalidInput);proposed=draft.values.Select(v=>v.Copy()).ToArray();points=draft.points;}
            }
            world.StatRequestState=world.AdjustmentRecord.Copy();
            world.PendingStatAdjustment=new StatAdjustmentRequest{id=request,character=worldCharacter,session=world.GrowthSession,target=world.AdjustmentTarget?.Copy(),map=world.AdjustmentRecord.map,revision=revision,growthRevision=world.AdjustmentRecord.growth.revision,
                draftRevision=draft?.draftRevision??0,terms=draft?.terms,operation=operation,stat=stat,direction=direction,values=proposed,points=points};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveStatAdjustment(StatAdjustmentNotice notice)
        {
            if(notice?.request==null)return WorldResult(WorldFailure.InvalidInput);
            var f=StatReady();if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingStatAdjustment;
            if(!StatAdjustmentRequest.Same(pending,notice.request)||!Guid.TryParseExact(notice.id,"N",out _)||world.StatNotices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.AdjustmentRecord,world.StatRequestState)){world.ClearStatAdjustment();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearStatRequest();return WorldResult(WorldFailure.InvalidInput);}
            if(notice.values==null||!notice.points.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            if(notice.values.Length!=6||notice.values.Any(v=>v?.Valid!=true)||notice.points<0||notice.points>short.MaxValue)return WorldResult(WorldFailure.InvalidInput);
            if(pending.operation==StatAdjustmentOperation.State)
            {
                if(notice.costs==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(notice.costs.Length!=6||notice.costs.Any(v=>v<0||v>short.MaxValue)||notice.effectiveStats!=null||notice.health!=null)return WorldResult(WorldFailure.InvalidInput);
                world.StatDraft=new StatAdjustmentView{terms=notice.id,initial=notice.values.Select(v=>v.Copy()).ToArray(),values=notice.values.Select(v=>v.Copy()).ToArray(),costs=(int[])notice.costs.Clone(),initialPoints=notice.points.Value,points=notice.points.Value};
                world.StatBaseline=world.AdjustmentRecord.Copy();
            }
            else
            {
                var draft=world.StatDraft;
                if(draft==null||draft.terms!=pending.terms||draft.draftRevision!=pending.draftRevision)return WorldResult(WorldFailure.StaleRequest);
                if(!AttributeTriplet.Same(notice.values,pending.values)||notice.points!=pending.points||notice.costs!=null&&!notice.costs.SequenceEqual(draft.costs))return WorldResult(WorldFailure.InvalidInput);
                if(pending.operation==StatAdjustmentOperation.Step)
                {
                    if(notice.effectiveStats!=null||notice.health!=null)return WorldResult(WorldFailure.InvalidInput);
                    draft.values=notice.values.Select(v=>v.Copy()).ToArray();draft.points=notice.points.Value;draft.draftRevision++;
                }
                else
                {
                    if(notice.effectiveStats!=null&&!notice.effectiveStats.Valid||notice.health!=null&&(!notice.health.Valid||world.AdjustmentRecord.ghost&&notice.health.hp!=0))return WorldResult(WorldFailure.InvalidInput);
                    var next=world.AdjustmentRecord.Copy();
                    if(notice.effectiveStats!=null||notice.health!=null)
                    {
                        next.growth.revision++;next.growth.lastNotice=notice.id;
                        if(notice.effectiveStats!=null)next.growth.stats=notice.effectiveStats.Copy();
                        if(notice.health!=null){next.healthKnown=true;next.hp=notice.health.hp;next.maxHp=notice.health.maxHp;next.mp=notice.health.mp;next.maxMp=notice.health.maxMp;}
                    }
                    next.statAllocation=new StatAllocationRecord{id=notice.id,request=pending.id,terms=draft.terms,revision=pending.revision+1,growthRevision=next.growth.revision,job=world.AdjustmentJob,points=notice.points.Value,values=notice.values.Select(v=>v.Copy()).ToArray()};
                    f=CommitAdjustment(next,world.StatBaseline,notice.id);if(f!=WorldFailure.None)return WorldResult(f);
                    world.StatNotices.Add(notice.id);world.ClearStatAdjustment();world.ClearClassChange();return WorldResult(WorldFailure.None);
                }
            }
            world.StatNotices.Add(notice.id);world.ClearStatRequest();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteStatAdjustment(StatAdjustmentOperation operation,int stat,int direction,string request,Func<StatAdjustmentRequest,WorldView,StatAdjustmentNotice> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestStatAdjustment(operation,stat,direction,request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.statAdjustmentRequest.Copy(),queued.View);
                if(notice?.request?.id!=request){if(world.PendingStatAdjustment?.id==request)world.ClearStatRequest();return WorldResult(WorldFailure.StaleRequest);}
                var reply=ReceiveStatAdjustment(notice);if(!reply.Accepted&&world.PendingStatAdjustment?.id==request)world.ClearStatRequest();return WorldResult(reply.Failure);
            }
            catch(Exception){if(world.PendingStatAdjustment?.id==request)world.ClearStatRequest();return WorldResult(WorldFailure.RulesUnavailable);}
        }
    }
}
