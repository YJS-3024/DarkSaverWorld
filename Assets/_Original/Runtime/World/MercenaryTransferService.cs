using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure MercenaryReady(string token)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Moving)return WorldFailure.Moving;
            if(world.Kind!=FacilityKind.Mercenary||token==null||token!=world.Token)return WorldFailure.StaleRequest;
            if(world.Combat!=null||world.PresentationLocked||world.PendingPortal!=null)return WorldFailure.Busy;
            var npc=world.Map(world.Record.map).npcs.FirstOrDefault(n=>n.id==world.Npc&&n.kind==(int)FacilityKind.Mercenary);
            if(!world.CanInteract(npc))return WorldFailure.TooFar;
            return world.Record.goldKnown?WorldFailure.None:WorldFailure.RulesUnavailable;
        }
        public WorldReply RequestMercenaryTransfer(bool hire,int job,string token,string request)
        {
            var f=MercenaryReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingMercenary!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.mercenary?.revision??0;
            if(revision==long.MaxValue||(world.Record.currency?.revision??0)==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            int limit=0;ulong? price=null;
            if(hire)
            {
                // Native selection uses the currently owned type until its last member is dismissed.
                if(world.Record.hiredCount>0&&world.Record.hiredJob!=job)return WorldResult(WorldFailure.InvalidInput);
                var offer=world.Definition.hires.FirstOrDefault(h=>h.commander==world.CommanderJob&&h.job==job);
                if(offer==null||offer.limit<=0||offer.price<0||world.Record.hiredCount>=offer.limit)return WorldResult(WorldFailure.InvalidInput);
                limit=offer.limit;price=(ulong)offer.price;
                if(world.Record.gold<price.Value)return WorldResult(WorldFailure.InsufficientFunds);
            }
            else
            {
                if(world.Record.hiredCount==0)return WorldResult(WorldFailure.NotOwned);
                job=world.Record.hiredJob;
            }
            world.MercenaryState=world.Record.Copy();
            world.PendingMercenary=new MercenaryRequest{id=request,character=worldCharacter,session=world.CurrencySession,token=token,npc=world.Npc,map=world.Record.map,
                commander=world.CommanderJob,job=job,count=world.Record.hiredCount,hire=hire,limit=limit,price=price,revision=revision,currencyRevision=world.Record.currency?.revision??0};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveMercenaryTransfer(MercenaryNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=MercenaryReady(notice.token);if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingMercenary;var baseline=world.MercenaryState;
            if(pending==null||baseline==null||notice.request!=pending.id||notice.character!=worldCharacter||notice.session!=world.CurrencySession||notice.npc!=world.Npc||notice.map!=world.Record.map
                ||notice.commander!=world.CommanderJob||notice.commander!=pending.commander||!Guid.TryParseExact(notice.id,"N",out _)
                ||world.MercenaryNotices.Contains(notice.id)||world.CurrencyNotices.Contains(notice.id)||world.Record.currency?.id==notice.id
                ||notice.expectedRevision!=pending.revision||notice.expectedRevision!=(world.Record.mercenary?.revision??0)
                ||notice.currencyRevision!=pending.currencyRevision||notice.currencyRevision!=(world.Record.currency?.revision??0))return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.Record,baseline)){world.ClearMercenary();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearMercenary();return WorldResult(WorldFailure.InvalidInput);}
            if(notice.hire!=pending.hire||notice.job!=pending.job||notice.count!=pending.count+(pending.hire?1:-1))return WorldResult(WorldFailure.InvalidInput);
            if(!notice.amount.HasValue||!notice.gold.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            if(pending.hire&&notice.amount!=pending.price)return WorldResult(WorldFailure.InvalidInput);
            decimal gold=baseline.gold;gold+=pending.hire?-(decimal)notice.amount.Value:notice.amount.Value;
            if(gold<0)return WorldResult(WorldFailure.InsufficientFunds);
            if(gold>ulong.MaxValue||notice.gold.Value!=(ulong)gold)return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.hiredCount=notice.count;next.hiredJob=notice.count==0?-1:pending.job;next.gold=notice.gold.Value;
            next.mercenary=new MercenaryReceipt{id=notice.id,request=notice.request,revision=notice.expectedRevision+1,hire=pending.hire,job=pending.job,count=notice.count,amount=notice.amount.Value};
            f=CommitWorld(next,baseline);if(f!=WorldFailure.None)return WorldResult(f);
            world.Record=next;world.MercenaryNotices.Add(notice.id);if(notice.amount.Value>0)world.CurrencyNotices.Add(notice.id);world.ClearMercenary();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteMercenaryTransfer(bool hire,int job,string token,string request,Func<MercenaryRequest,WorldView,MercenaryNotice> authority)
        {
            var f=MercenaryReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestMercenaryTransfer(hire,job,token,request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.mercenaryRequest.Copy(),queued.View);
                if(notice!=null)
                {
                    if(notice.request!=request){if(world.PendingMercenary?.id==request)world.ClearMercenary();return WorldResult(WorldFailure.StaleRequest);}
                    var result=ReceiveMercenaryTransfer(notice);if(!result.Accepted&&world.PendingMercenary?.id==request)world.ClearMercenary();return WorldResult(result.Failure);
                }
            }
            catch(Exception){/* A failed local provider leaves no speculative hire/refund. */}
            if(world.PendingMercenary?.id==request)world.ClearMercenary();return WorldResult(WorldFailure.RulesUnavailable);
        }
    }
}
