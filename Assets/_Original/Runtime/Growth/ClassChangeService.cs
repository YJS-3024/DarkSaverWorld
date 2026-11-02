using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure ClassReady()
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Moving||world.Kind!=FacilityKind.None||world.PresentationLocked||world.PendingPortal!=null||world.Combat!=null||world.PendingItemUse!=null)return WorldFailure.Busy;
            if(!world.AdjustmentTargetMatches)return WorldFailure.StaleRequest;
            if(world.AdjustmentRecord?.growth==null||world.Definition.growthCatalog==null)return WorldFailure.RulesUnavailable;
            if(world.Map(world.AdjustmentRecord.map).training||world.Definition.campaign?.Map(world.AdjustmentRecord.map)?.kind=="scenario")return WorldFailure.RulesUnavailable;
            return WorldFailure.None;
        }
        public WorldReply CloseClassOptions()
        {var f=ReadyWorld();if(f==WorldFailure.None)world.ClearClassChange();return WorldResult(f);}
        public WorldReply RequestClassOptions(string request)
        {
            var f=ClassReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.PendingStatAdjustment!=null||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingClassOptions!=null||world.PendingClassChange!=null)return WorldResult(WorldFailure.Busy);
            if(world.AdjustmentRecord.growth.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            world.ClearClassChange();world.ClassOptionsState=world.AdjustmentRecord.Copy();
            world.PendingClassOptions=new ClassOptionsRequest{id=request,character=worldCharacter,session=world.GrowthSession,target=world.AdjustmentTarget?.Copy(),map=world.AdjustmentRecord.map,revision=world.AdjustmentRecord.growth.revision};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveClassOptions(ClassOptionsNotice notice)
        {
            if(notice?.request==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ClassReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.PendingStatAdjustment!=null||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            if(!ClassOptionsRequest.Same(world.PendingClassOptions,notice.request)||!Guid.TryParseExact(notice.id,"N",out _)||world.ClassNotices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.AdjustmentRecord,world.ClassOptionsState)){world.ClearClassChange();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearClassChange();return WorldResult(WorldFailure.InvalidInput);}
            if(!notice.classPoints.HasValue||notice.options==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(notice.classPoints<0||notice.classPoints>ushort.MaxValue)return WorldResult(WorldFailure.InvalidInput);
            var candidates=new[]{notice.left,notice.right}.Concat(notice.options).Where(c=>c!=null);
            if(notice.options.Any(c=>c==null)||candidates.Any(c=>world.Definition.growthCatalog.Job(c.job)==null))return WorldResult(WorldFailure.InvalidInput);
            world.ClassOffer=new ClassOffer{Options=notice.Copy(),State=world.AdjustmentRecord.Copy()};world.ClassNotices.Add(notice.id);world.PendingClassOptions=null;world.ClassOptionsState=null;
            return WorldResult(WorldFailure.None);
        }
        public WorldReply RequestClassChange(int slot,int index,string options,string request)
        {
            var f=ClassReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.PendingStatAdjustment!=null||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingClassChange!=null||world.PendingClassOptions!=null)return WorldResult(WorldFailure.Busy);
            var offer=world.ClassOffer;if(offer==null||offer.Options.id!=options)return WorldResult(WorldFailure.StaleRequest);
            if(!offer.Matches(world)||!TempleRevivalOffer.SameState(world.AdjustmentRecord,offer.State)){world.ClearClassChange();return WorldResult(WorldFailure.StaleRequest);}
            var candidate=offer.Options.Candidate(slot,index);
            if(offer.Options.classPoints<=0||candidate==null||candidate.job==world.AdjustmentJob)return WorldResult(WorldFailure.InvalidInput);
            world.PendingClassChange=new ClassChangeRequest{id=request,options=options,character=worldCharacter,session=world.GrowthSession,target=world.AdjustmentTarget?.Copy(),map=world.AdjustmentRecord.map,
                revision=world.AdjustmentRecord.growth.revision,slot=slot,index=index,job=candidate.job};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveClassChange(ClassChangeNotice notice)
        {
            if(notice?.request==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ClassReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.PendingStatAdjustment!=null||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            var pending=world.PendingClassChange;var offer=world.ClassOffer;
            if(!ClassChangeRequest.Same(pending,notice.request)||offer==null||!Guid.TryParseExact(notice.id,"N",out _)||world.ClassNotices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            if(!offer.Matches(world)||!TempleRevivalOffer.SameState(world.AdjustmentRecord,offer.State)){world.ClearClassChange();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearClassChange();return WorldResult(WorldFailure.InvalidInput);}
            var state=notice.state;
            if(state==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(!world.Definition.growthCatalog.Accepts(state)||state.job!=pending.job||state.lastNotice!=notice.id||state.revision!=pending.revision+1
                ||notice.health!=null&&(!notice.health.Valid||world.AdjustmentRecord.ghost&&notice.health.hp!=0))return WorldResult(WorldFailure.InvalidInput);
            var next=world.AdjustmentRecord.Copy();next.growth=state.Copy();next.level=state.totalLevel;
            if(notice.health!=null){next.healthKnown=true;next.hp=notice.health.hp;next.maxHp=notice.health.maxHp;next.mp=notice.health.mp;next.maxMp=notice.health.maxMp;}
            next.classChange=new ClassChangeReceipt{id=notice.id,request=pending.id,options=pending.options,growthRevision=state.revision,previousJob=world.AdjustmentJob,job=state.job,slot=pending.slot,index=pending.index};
            f=CommitAdjustment(next,offer.State,notice.id);if(f!=WorldFailure.None)return WorldResult(f);
            world.ClassNotices.Add(notice.id);world.ClearClassChange();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteClassOptions(string request,Func<ClassOptionsRequest,WorldView,ClassOptionsNotice> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestClassOptions(request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.classOptionsRequest.Copy(),queued.View);
                if(notice?.request?.id!=request){if(world.PendingClassOptions?.id==request)world.ClearClassChange();return WorldResult(WorldFailure.StaleRequest);}
                var result=ReceiveClassOptions(notice);if(!result.Accepted&&world.PendingClassOptions?.id==request)world.ClearClassChange();return WorldResult(result.Failure);
            }
            catch(Exception){if(world.PendingClassOptions?.id==request)world.ClearClassChange();return WorldResult(WorldFailure.RulesUnavailable);}
        }
        public WorldReply ExecuteClassChange(int slot,int index,string options,string request,Func<ClassChangeRequest,WorldView,ClassChangeNotice> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestClassChange(slot,index,options,request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.classChangeRequest.Copy(),queued.View);
                if(notice?.request?.id!=request){if(world.PendingClassChange?.id==request)world.PendingClassChange=null;return WorldResult(WorldFailure.StaleRequest);}
                var result=ReceiveClassChange(notice);if(!result.Accepted&&world.PendingClassChange?.id==request)world.PendingClassChange=null;return WorldResult(result.Failure);
            }
            catch(Exception){if(world.PendingClassChange?.id==request)world.PendingClassChange=null;return WorldResult(WorldFailure.RulesUnavailable);}
        }
    }
}
