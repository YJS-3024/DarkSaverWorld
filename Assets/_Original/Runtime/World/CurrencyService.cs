using System;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        // All committed balance changes share this revision, including legacy local trades/fees.
        private WorldFailure PrepareCurrencyCommit(WorldRecord candidate)
        {
            var previous=world.Record.currency;var next=candidate.currency;
            long revision=previous?.revision??0;
            bool stampChanged=next?.revision!=previous?.revision||next?.id!=previous?.id;
            if(stampChanged)
                return revision!=long.MaxValue&&next!=null&&next.revision==revision+1&&next.id!=previous?.id?WorldFailure.None:WorldFailure.StaleRequest;
            if(candidate.goldKnown==world.Record.goldKnown&&candidate.bankKnown==world.Record.bankKnown&&candidate.gold==world.Record.gold&&candidate.bank==world.Record.bank)
                return WorldFailure.None;
            if(revision==long.MaxValue)return WorldFailure.StaleRequest;
            candidate.currency=new CurrencyReceipt{id=Guid.NewGuid().ToString("N"),revision=revision+1};return WorldFailure.None;
        }
        private bool CurrencyStoredStateMatches(WorldRecord stored,WorldRecord current)
        {
            if(stored==null)return false;
            // A scalar notice can checkpoint an already reached grid tile while a route is still animating.
            // Training persists its town return point; neither case permits overwriting unrelated fresh data.
            var expected=stored.Copy();expected.map=current.map;expected.x=current.x;expected.y=current.y;expected.direction=current.direction;expected.trainingEntrance=current.trainingEntrance;
            return TempleRevivalOffer.SameState(expected,current);
        }
        public WorldReply RequestCurrencyState(string request)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingCurrency!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.currency?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            world.PendingCurrency=new CurrencyRequest{id=request,character=worldCharacter,session=world.CurrencySession,revision=revision};world.Applied.Add(request);
            return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveCurrencyState(CurrencyNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            long revision=world.Record.currency?.revision??0;var pending=world.PendingCurrency;
            if(notice.character!=worldCharacter||notice.session!=world.CurrencySession||!Guid.TryParseExact(notice.id,"N",out _)
                ||notice.expectedRevision!=revision||revision==long.MaxValue||world.CurrencyNotices.Contains(notice.id)||world.Record.currency?.id==notice.id)
                return WorldResult(WorldFailure.StaleRequest);
            if(!string.IsNullOrEmpty(notice.request)&&(pending==null||pending.id!=notice.request||pending.revision!=revision))return WorldResult(WorldFailure.StaleRequest);
            if(!notice.approved)
            {
                if(pending==null||notice.request!=pending.id)return WorldResult(WorldFailure.StaleRequest);
                world.PendingCurrency=null;return WorldResult(WorldFailure.InvalidInput);
            }
            if(!notice.gold.HasValue&&!notice.bank.HasValue)return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();
            if(notice.gold.HasValue){next.goldKnown=true;next.gold=notice.gold.Value;}
            if(notice.bank.HasValue){next.bankKnown=true;next.bank=notice.bank.Value;}
            next.currency=new CurrencyReceipt{id=notice.id,revision=revision+1};
            f=CommitWorld(next,expectedCurrencyState:world.Record);if(f!=WorldFailure.None)return WorldResult(f);
            world.Record=next;world.CurrencyNotices.Add(notice.id);world.PendingCurrency=null;world.ClearRevival();
            if(world.Draft!=null)
            {
                world.Draft.goldKnown=next.goldKnown;world.Draft.gold=next.gold;world.Draft.bankKnown=next.bankKnown;world.Draft.bank=next.bank;world.Draft.currency=next.currency.Copy();
            }
            if(world.Moving&&worldBeforeMove!=null)worldBeforeMove=next.Copy();
            return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteCurrencyState(string request,Func<CurrencyRequest,WorldView,CurrencyNotice> authority)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestCurrencyState(request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.currencyRequest.Copy(),queued.View);
                if(notice!=null)
                {
                    if(notice.request!=request){if(world.PendingCurrency?.id==request)world.PendingCurrency=null;return WorldResult(WorldFailure.StaleRequest);}
                    var result=ReceiveCurrencyState(notice);if(!result.Accepted&&world.PendingCurrency?.id==request)world.PendingCurrency=null;return WorldResult(result.Failure);
                }
            }
            catch(Exception){/* A failed local state query must not leave subsequent queries locked. */}
            if(world.PendingCurrency?.id==request)world.PendingCurrency=null;return WorldResult(WorldFailure.RulesUnavailable);
        }
    }
}
