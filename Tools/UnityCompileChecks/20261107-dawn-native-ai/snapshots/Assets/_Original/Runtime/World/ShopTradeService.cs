using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure ShopReady(string token)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Moving)return WorldFailure.Moving;
            if(world.Kind!=FacilityKind.Shop||token==null||world.Token!=token)return WorldFailure.StaleRequest;
            if(world.Combat!=null||world.PresentationLocked||world.PendingPortal!=null)return WorldFailure.Busy;
            var npc=world.Map(world.Record.map).npcs.FirstOrDefault(n=>n.id==world.Npc&&n.kind==(int)FacilityKind.Shop);
            return world.CanInteract(npc)?WorldFailure.None:WorldFailure.TooFar;
        }
        public WorldReply RequestShopQuote(bool buy,int item,string owned,string token,string request)
        {
            var f=ShopReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingShopQuote!=null||world.PendingShopTrade!=null)return WorldResult(WorldFailure.Busy);
            if((world.Record.shop?.revision??0)==long.MaxValue||(world.Record.currency?.revision??0)==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            int slot;
            if(buy)
            {
                var shop=world.Definition.shops.FirstOrDefault(s=>s.map==world.Record.map);var product=world.Definition.Item(item);
                if(product==null||shop==null||!shop.items.Contains(item)||product.price<0||owned!=null)return WorldResult(WorldFailure.InvalidInput);
                slot=Enumerable.Range(0,28).Where(i=>world.Record.inventory.All(v=>v.slot!=i)).DefaultIfEmpty(-1).First();
                if(slot<0)return WorldResult(WorldFailure.InventoryFull);
            }
            else
            {
                var instance=world.Record.inventory.FirstOrDefault(i=>i.id==owned);if(instance==null)return WorldResult(WorldFailure.NotOwned);
                if(instance.item!=item||world.Definition.Item(item)==null)return WorldResult(WorldFailure.InvalidInput);
                slot=instance.slot;
            }
            world.ClearShop();world.ShopQuoteState=world.Record.Copy();
            world.PendingShopQuote=new ShopRequest{id=request,character=worldCharacter,session=world.CurrencySession,token=token,npc=world.Npc,map=world.Record.map,
                buy=buy,item=item,owned=owned,slot=slot,revision=world.Record.shop?.revision??0,currencyRevision=world.Record.currency?.revision??0};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveShopQuote(ShopQuote quote)
        {
            if(quote?.request==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ShopReady(quote.request.token);if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingShopQuote;
            if(!ShopRequest.Same(pending,quote.request)||!Guid.TryParseExact(quote.id,"N",out _)||world.ShopNotices.Contains(quote.id))return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.Record,world.ShopQuoteState)){world.ClearShop();return WorldResult(WorldFailure.StaleRequest);}
            if(!quote.approved){world.ClearShop();return WorldResult(WorldFailure.InvalidInput);}
            if(!quote.price.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            var product=world.Definition.Item(pending.item);
            if(product==null||pending.buy&&(product.price<0||quote.price.Value!=(ulong)product.price)
                ||!pending.buy&&product.sellKnown&&(product.sellPrice<0||quote.price.Value!=(ulong)product.sellPrice))return WorldResult(WorldFailure.InvalidInput);
            world.ShopOffer=new ShopOffer{Quote=quote.Copy(),State=world.Record.Copy()};world.ShopNotices.Add(quote.id);
            world.PendingShopQuote=null;world.ShopQuoteState=null;return WorldResult(WorldFailure.None);
        }
        public WorldReply RequestShopTrade(string quote,string token,string request)
        {
            var f=ShopReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingShopTrade!=null||world.PendingShopQuote!=null)return WorldResult(WorldFailure.Busy);
            var offer=world.ShopOffer;
            if(offer==null||offer.Quote.id!=quote)return WorldResult(WorldFailure.StaleRequest);
            if(!offer.Matches(world)||!TempleRevivalOffer.SameState(world.Record,offer.State)){world.ClearShop();return WorldResult(WorldFailure.StaleRequest);}
            if(!world.Record.goldKnown)return WorldResult(WorldFailure.RulesUnavailable);
            if(offer.Quote.request.buy&&world.Record.gold<offer.Quote.price.Value)return WorldResult(WorldFailure.InsufficientFunds);
            world.PendingShopTrade=offer.Quote.request.Copy();world.PendingShopTrade.id=request;world.PendingShopTrade.quote=quote;world.PendingShopTrade.price=offer.Quote.price;
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveShopTrade(ShopNotice notice)
        {
            if(notice?.request==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ShopReady(notice.request.token);if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingShopTrade;var offer=world.ShopOffer;
            if(!ShopRequest.Same(pending,notice.request)||offer==null||!Guid.TryParseExact(notice.id,"N",out _)||world.ShopNotices.Contains(notice.id)
                ||world.CurrencyNotices.Contains(notice.id)||world.Record.currency?.id==notice.id)return WorldResult(WorldFailure.StaleRequest);
            if(!offer.Matches(world)||!TempleRevivalOffer.SameState(world.Record,offer.State)){world.ClearShop();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.PendingShopTrade=null;return WorldResult(WorldFailure.InvalidInput);}
            if(!notice.price.HasValue||!notice.gold.HasValue||!world.Record.goldKnown)return WorldResult(WorldFailure.RulesUnavailable);
            if(notice.price!=pending.price)return WorldResult(WorldFailure.InvalidInput);
            decimal gold=world.Record.gold;gold+=pending.buy?-(decimal)notice.price.Value:notice.price.Value;
            if(gold<0)return WorldResult(WorldFailure.InsufficientFunds);
            if(gold>ulong.MaxValue||notice.gold.Value!=(ulong)gold)return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.gold=notice.gold.Value;
            string owned=pending.owned;
            if(pending.buy)
            {
                var item=notice.acquired;
                if(item==null||!Guid.TryParseExact(item.id,"N",out _)||item.item!=pending.item||item.slot!=pending.slot
                    ||world.Record.inventory.Concat(world.Record.storage).Concat(world.Record.Equipment).Any(i=>i.id==item.id))return WorldResult(WorldFailure.InvalidInput);
                next.inventory=next.inventory.Concat(new[]{item.Copy()}).ToArray();owned=item.id;
            }
            else
            {
                if(notice.acquired!=null)return WorldResult(WorldFailure.InvalidInput);
                next.inventory=next.inventory.Where(i=>i.id!=owned).ToArray();
            }
            next.shop=new ShopReceipt{id=notice.id,request=pending.id,quote=pending.quote,revision=pending.revision+1,buy=pending.buy,item=pending.item,slot=pending.slot,owned=owned,price=notice.price.Value};
            f=CommitWorld(next,offer.State);if(f!=WorldFailure.None)return WorldResult(f);
            world.Record=next;world.ShopNotices.Add(notice.id);if(notice.price.Value>0)world.CurrencyNotices.Add(notice.id);world.ClearShop();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteShopQuote(bool buy,int item,string owned,string token,string request,Func<ShopRequest,WorldView,ShopQuote> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestShopQuote(buy,item,owned,token,request);if(!queued.Accepted)return queued;
            var origin=world;
            try
            {
                var quote=authority(queued.View.shopQuoteRequest.Copy(),queued.View);
                if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
                if(quote?.request?.id!=request)
                {if(world.PendingShopQuote?.id==request)world.ClearShop();return WorldResult(quote==null?WorldFailure.RulesUnavailable:WorldFailure.StaleRequest);}
                var reply=ReceiveShopQuote(quote);
                if(!reply.Accepted&&world.PendingShopQuote?.id==request)world.ClearShop();return WorldResult(reply.Failure);
            }
            catch(Exception){if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());if(world.PendingShopQuote?.id==request)world.ClearShop();return WorldResult(WorldFailure.RulesUnavailable);}
        }
        public WorldReply ExecuteShopTrade(string quote,string token,string request,Func<ShopRequest,WorldView,ShopNotice> authority)
        {
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestShopTrade(quote,token,request);if(!queued.Accepted)return queued;
            var origin=world;
            try
            {
                var notice=authority(queued.View.shopTradeRequest.Copy(),queued.View);
                if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
                if(notice?.request?.id!=request)
                {if(world.PendingShopTrade?.id==request)world.PendingShopTrade=null;return WorldResult(notice==null?WorldFailure.RulesUnavailable:WorldFailure.StaleRequest);}
                var reply=ReceiveShopTrade(notice);
                if(!reply.Accepted&&world.PendingShopTrade?.id==request)world.PendingShopTrade=null;return WorldResult(reply.Failure);
            }
            catch(Exception){if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());if(world.PendingShopTrade?.id==request)world.PendingShopTrade=null;return WorldResult(WorldFailure.RulesUnavailable);}
        }
    }
}
