using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure MagicLoadoutReady()
        {
            var f=ClassReady();if(f!=WorldFailure.None)return f;
            return world.PendingStatAdjustment!=null||world.PendingClassOptions!=null||world.PendingClassChange!=null?WorldFailure.Busy:WorldFailure.None;
        }
        private bool ValidMagicLists(int[] learned,int[] selected)=>learned!=null&&selected!=null&&learned.Length<=16&&selected.Length<=8
            &&learned.Distinct().Count()==learned.Length&&selected.Distinct().Count()==selected.Length
            &&learned.All(id=>id>0&&world.AdjustmentRecord.growth.magic.Contains(id)&&world.Definition.growthCatalog.magicIds.Contains(id))&&selected.All(id=>learned.Contains(id));
        public WorldReply CancelMagicLoadout()
        {var f=ReadyWorld();if(f==WorldFailure.None)world.ClearMagicLoadout();return WorldResult(f);}
        public WorldReply RequestMagicLoadout(MagicLoadoutOperation operation,int index,string request)
        {
            var f=MagicLoadoutReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(!Enum.IsDefined(typeof(MagicLoadoutOperation),operation))return WorldResult(WorldFailure.InvalidInput);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingMagicLoadout!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.AdjustmentRecord.magicLoadout?.revision??0;
            if(revision==long.MaxValue||world.AdjustmentRecord.growth.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var draft=world.MagicDraft;int[] selected=null;int magic=-1;
            if(operation==MagicLoadoutOperation.State)
            {if(draft?.Dirty==true)return WorldResult(WorldFailure.Busy);world.ClearMagicLoadout();draft=null;}
            else
            {
                if(draft==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(!TempleRevivalOffer.SameState(world.AdjustmentRecord,world.MagicBaseline)){world.ClearMagicLoadout();return WorldResult(WorldFailure.StaleRequest);}
                if(draft.draftRevision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                if(operation==MagicLoadoutOperation.Add)
                {if(!draft.CanAdd(index))return WorldResult(WorldFailure.InvalidInput);magic=draft.learned[index];selected=draft.selected.Concat(new[]{magic}).ToArray();}
                else if(operation==MagicLoadoutOperation.Remove)
                {if(!draft.CanRemove(index))return WorldResult(WorldFailure.InvalidInput);magic=draft.selected[index];selected=draft.selected.Where((_,i)=>i!=index).ToArray();}
                else
                {if(!draft.Dirty&&world.StatDraft?.Dirty!=true&&world.AdjustmentRecord.magicLoadout?.Matches(world.AdjustmentRecord.growth)==true&&world.AdjustmentRecord.magicLoadout.selected.SequenceEqual(draft.selected))return WorldResult(WorldFailure.InvalidInput);selected=(int[])draft.selected.Clone();}
            }
            var pending=new MagicLoadoutRequest{id=request,character=worldCharacter,session=world.GrowthSession,target=world.AdjustmentTarget?.Copy(),map=world.AdjustmentRecord.map,revision=revision,growthRevision=world.AdjustmentRecord.growth.revision,
                operation=operation,index=index,magic=magic,terms=draft?.terms,draftRevision=draft?.draftRevision??0,selected=selected};
            if(operation==MagicLoadoutOperation.Confirm&&world.StatDraft?.Dirty==true)
            {
                if(!TempleRevivalOffer.SameState(world.AdjustmentRecord,world.StatBaseline))return WorldResult(WorldFailure.StaleRequest);
                if((world.AdjustmentRecord.statAllocation?.revision??0)==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                pending.statTerms=world.StatDraft.terms;pending.statRevision=world.AdjustmentRecord.statAllocation?.revision??0;pending.statDraftRevision=world.StatDraft.draftRevision;
                pending.attributes=world.StatDraft.values.Select(v=>v.Copy()).ToArray();pending.statPoints=world.StatDraft.points;
            }
            world.PendingMagicLoadout=pending;world.MagicRequestState=world.AdjustmentRecord.Copy();world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveMagicLoadout(MagicLoadoutNotice notice)
        {
            if(notice?.request==null)return WorldResult(WorldFailure.InvalidInput);
            var f=MagicLoadoutReady();if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingMagicLoadout;
            if(!MagicLoadoutRequest.Same(pending,notice.request)||!Guid.TryParseExact(notice.id,"N",out _)||world.MagicNotices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.AdjustmentRecord,world.MagicRequestState)){world.ClearMagicLoadout();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearMagicRequest();return WorldResult(WorldFailure.InvalidInput);}
            if(notice.selected==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(pending.operation==MagicLoadoutOperation.State)
            {
                if(notice.learned==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(!ValidMagicLists(notice.learned,notice.selected)||notice.attributes!=null||notice.statPoints.HasValue||notice.effectiveStats!=null||notice.health!=null)return WorldResult(WorldFailure.InvalidInput);
                world.MagicDraft=new MagicLoadoutView{terms=notice.id,learned=(int[])notice.learned.Clone(),initial=(int[])notice.selected.Clone(),selected=(int[])notice.selected.Clone()};world.MagicBaseline=world.AdjustmentRecord.Copy();
            }
            else
            {
                var draft=world.MagicDraft;
                if(draft==null||draft.terms!=pending.terms||draft.draftRevision!=pending.draftRevision)return WorldResult(WorldFailure.StaleRequest);
                if(!ValidMagicLists(draft.learned,notice.selected)||!notice.selected.SequenceEqual(pending.selected)||notice.learned!=null&&!notice.learned.SequenceEqual(draft.learned))return WorldResult(WorldFailure.InvalidInput);
                if(pending.operation!=MagicLoadoutOperation.Confirm)
                {
                    if(notice.attributes!=null||notice.statPoints.HasValue||notice.effectiveStats!=null||notice.health!=null)return WorldResult(WorldFailure.InvalidInput);
                    draft.selected=(int[])notice.selected.Clone();draft.draftRevision++;
                }
                else
                {
                    bool joint=pending.attributes!=null;
                    if(joint)
                    {
                        var stats=world.StatDraft;
                        if(stats==null||stats.terms!=pending.statTerms||stats.draftRevision!=pending.statDraftRevision||!TempleRevivalOffer.SameState(world.AdjustmentRecord,world.StatBaseline))return WorldResult(WorldFailure.StaleRequest);
                        if(!AttributeTriplet.Same(notice.attributes,pending.attributes)||notice.statPoints!=pending.statPoints)return WorldResult(WorldFailure.InvalidInput);
                    }
                    else if(notice.attributes!=null||notice.statPoints.HasValue||notice.effectiveStats!=null||notice.health!=null)return WorldResult(WorldFailure.InvalidInput);
                    if(notice.effectiveStats!=null&&!notice.effectiveStats.Valid||notice.health!=null&&(!notice.health.Valid||world.AdjustmentRecord.ghost&&notice.health.hp!=0))return WorldResult(WorldFailure.InvalidInput);
                    var next=world.AdjustmentRecord.Copy();
                    if(notice.effectiveStats!=null||notice.health!=null)
                    {
                        next.growth.revision++;next.growth.lastNotice=notice.id;if(notice.effectiveStats!=null)next.growth.stats=notice.effectiveStats.Copy();
                        if(notice.health!=null){next.healthKnown=true;next.hp=notice.health.hp;next.maxHp=notice.health.maxHp;next.mp=notice.health.mp;next.maxMp=notice.health.maxMp;}
                    }
                    if(joint)next.statAllocation=new StatAllocationRecord{id=notice.id,request=pending.id,terms=pending.statTerms,revision=pending.statRevision+1,growthRevision=next.growth.revision,job=world.AdjustmentJob,points=notice.statPoints.Value,values=notice.attributes.Select(v=>v.Copy()).ToArray()};
                    next.magicLoadout=new MagicLoadoutRecord{id=notice.id,request=pending.id,terms=draft.terms,revision=pending.revision+1,growthRevision=next.growth.revision,job=world.AdjustmentJob,selected=(int[])notice.selected.Clone()};
                    f=CommitAdjustment(next,world.MagicBaseline,notice.id);if(f!=WorldFailure.None)return WorldResult(f);
                    world.MagicNotices.Add(notice.id);if(joint)world.StatNotices.Add(notice.id);world.ClearMagicLoadout();world.ClearStatAdjustment();world.ClearClassChange();return WorldResult(WorldFailure.None);
                }
            }
            world.MagicNotices.Add(notice.id);world.ClearMagicRequest();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteMagicLoadout(MagicLoadoutOperation operation,int index,string request,Func<MagicLoadoutRequest,WorldView,MagicLoadoutNotice> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestMagicLoadout(operation,index,request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.magicLoadoutRequest.Copy(),queued.View);
                if(notice?.request?.id!=request){if(world.PendingMagicLoadout?.id==request)world.ClearMagicRequest();return WorldResult(WorldFailure.StaleRequest);}
                var result=ReceiveMagicLoadout(notice);if(!result.Accepted&&world.PendingMagicLoadout?.id==request)world.ClearMagicRequest();return WorldResult(result.Failure);
            }
            catch(Exception){if(world.PendingMagicLoadout?.id==request)world.ClearMagicRequest();return WorldResult(WorldFailure.RulesUnavailable);}
        }
    }
}
