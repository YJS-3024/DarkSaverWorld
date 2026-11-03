using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure TempleReady(string token)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Moving)return WorldFailure.Moving;
            if(world.Kind!=FacilityKind.Temple||token==null||world.Token!=token)return WorldFailure.StaleRequest;
            if(world.Combat!=null||world.PresentationLocked||world.PendingPortal!=null)return WorldFailure.Busy;
            var npc=world.Map(world.Record.map).npcs.FirstOrDefault(n=>n.id==world.Npc&&n.kind==(int)FacilityKind.Temple);
            if(!world.CanInteract(npc))return WorldFailure.TooFar;
            if(!world.Record.healthKnown)return WorldFailure.RulesUnavailable;
            if(!world.Record.ghost||world.Record.hp!=0)return WorldFailure.InvalidStage;
            return WorldFailure.None;
        }
        public WorldReply RequestRevivalQuote(string token,string request)
        {
            var f=TempleReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingRevival!=null||world.PendingRevivalQuote!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.revival?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            world.RevivalOffer=null;world.RevivalQuoteState=world.Record.Copy();
            world.PendingRevivalQuote=new RevivalQuoteRequest{id=request,character=worldCharacter,session=world.CampaignSession,token=token,npc=world.Npc,map=world.Record.map,revision=revision};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveRevivalQuote(RevivalQuote quote)
        {
            if(quote==null)return WorldResult(WorldFailure.InvalidInput);
            var f=TempleReady(quote.token);if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingRevivalQuote;
            if(pending==null||pending.id!=quote.request||quote.character!=worldCharacter||quote.session!=world.CampaignSession||quote.npc!=world.Npc||quote.map!=world.Record.map
                ||!Guid.TryParseExact(quote.id,"N",out _)||quote.revision!=pending.revision||quote.revision!=(world.Record.revival?.revision??0))return WorldResult(WorldFailure.StaleRequest);
            if(!quote.approved){world.ClearRevival();return WorldResult(WorldFailure.InvalidInput);}
            if(!quote.fee.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            if(!TempleRevivalOffer.SameState(world.Record,world.RevivalQuoteState)){world.ClearRevival();return WorldResult(WorldFailure.StaleRequest);}
            world.RevivalOffer=new TempleRevivalOffer{Quote=quote.Copy(),State=world.Record.Copy()};world.PendingRevivalQuote=null;world.RevivalQuoteState=null;
            return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteRevivalQuote(string token,string request,Func<RevivalQuoteRequest,WorldView,RevivalQuote> authority)
        {
            var f=TempleReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestRevivalQuote(token,request);if(!queued.Accepted)return queued;
            var origin=world;
            try
            {
                var quote=authority(queued.View.revivalQuoteRequest.Copy(),queued.View);
                if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
                if(quote!=null)
                {if(quote.request!=request){if(world.PendingRevivalQuote?.id==request)world.ClearRevival();return WorldResult(WorldFailure.StaleRequest);}
                    var reply=ReceiveRevivalQuote(quote);if(!reply.Accepted&&world.PendingRevivalQuote?.id==request)world.ClearRevival();return WorldResult(reply.Failure);}
            }
            catch(Exception){/* No permanent input lock after local authority failure. */}
            if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
            if(world.PendingRevivalQuote?.id==request)world.ClearRevival();return WorldResult(WorldFailure.RulesUnavailable);
        }
        public WorldReply RequestTempleRevival(string token,string request)
        {
            var f=TempleReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingRevival!=null||world.PendingRevivalQuote!=null)return WorldResult(WorldFailure.Busy);
            var offer=world.RevivalOffer;if(offer==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(!offer.Matches(world)||!TempleRevivalOffer.SameState(world.Record,offer.State)){world.ClearRevival();return WorldResult(WorldFailure.StaleRequest);}
            if(!world.Record.goldKnown)return WorldResult(WorldFailure.RulesUnavailable);
            if(world.Record.gold<offer.Quote.fee.Value)return WorldResult(WorldFailure.InsufficientFunds);
            world.PendingRevival=new RevivalRequest{id=request,quote=offer.Quote.id,character=worldCharacter,session=world.CampaignSession,token=token,npc=world.Npc,map=world.Record.map,revision=offer.Quote.revision};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveTempleRevival(RevivalNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=TempleReady(notice.token);if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingRevival;var offer=world.RevivalOffer;
            if(pending==null||offer==null||pending.id!=notice.request||pending.quote!=notice.quote||notice.character!=worldCharacter||notice.session!=world.CampaignSession||notice.map!=world.Record.map
                ||notice.npc!=world.Npc||!Guid.TryParseExact(notice.id,"N",out _)||notice.revision!=pending.revision||notice.revision!=(world.Record.revival?.revision??0)
                ||notice.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            if(!offer.Matches(world)||!TempleRevivalOffer.SameState(world.Record,offer.State)){world.ClearRevival();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.PendingRevival=null;return WorldResult(WorldFailure.InvalidInput);}
            if(!world.Record.goldKnown||!notice.gold.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            var health=notice.health;
            if(health==null||!health.Valid||health.hp<=0||health.maxHp!=world.Record.maxHp||health.maxMp!=world.Record.maxMp
                ||world.Record.gold<offer.Quote.fee.Value||notice.gold.Value!=world.Record.gold-offer.Quote.fee.Value)return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.ghost=false;next.hp=health.hp;next.mp=health.mp;next.gold=notice.gold.Value;
            next.revival=new RevivalReceipt{id=notice.id,request=notice.request,quote=notice.quote,revision=notice.revision+1,fee=offer.Quote.fee.Value};
            f=CommitWorld(next,offer.State);if(f!=WorldFailure.None)return WorldResult(f);
            world.Record=next;world.ClearRevival();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteTempleRevival(string token,string request,Func<RevivalRequest,WorldView,RevivalNotice> authority)
        {
            var f=TempleReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestTempleRevival(token,request);if(!queued.Accepted)return queued;
            var origin=world;
            try
            {
                var notice=authority(queued.View.revivalRequest.Copy(),queued.View);
                if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
                if(notice!=null)
                {if(notice.request!=request){if(world.PendingRevival?.id==request)world.PendingRevival=null;return WorldResult(WorldFailure.StaleRequest);}
                    var reply=ReceiveTempleRevival(notice);if(!reply.Accepted&&world.PendingRevival?.id==request)world.PendingRevival=null;return WorldResult(reply.Failure);}
            }
            catch(Exception){/* A fresh user confirmation may retry after local authority failure. */}
            if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
            if(world.PendingRevival?.id==request)world.PendingRevival=null;return WorldResult(WorldFailure.RulesUnavailable);
        }
    }
}
