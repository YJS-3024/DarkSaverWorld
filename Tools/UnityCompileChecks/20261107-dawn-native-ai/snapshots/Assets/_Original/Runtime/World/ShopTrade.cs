using System;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    // Local quote/approval contracts, not recovered native wire fields or sales-price policy.
    public sealed class ShopRequest
    {
        public string id,character,session,token,npc,owned,quote;
        public int map,item,slot;public bool buy;public ulong? price;
        public long revision,currencyRevision;
        public ShopRequest Copy()=>(ShopRequest)MemberwiseClone();
        public static bool Same(ShopRequest a,ShopRequest b)=>a!=null&&b!=null&&a.id==b.id&&a.character==b.character&&a.session==b.session&&a.token==b.token&&a.npc==b.npc
            &&a.owned==b.owned&&a.quote==b.quote&&a.map==b.map&&a.item==b.item&&a.slot==b.slot&&a.buy==b.buy&&a.price==b.price&&a.revision==b.revision&&a.currencyRevision==b.currencyRevision;
    }
    public sealed class ShopQuote
    {
        public string id;public ShopRequest request;public bool approved;public ulong? price;
        public ShopQuote Copy()=>new ShopQuote{id=id,request=request?.Copy(),approved=approved,price=price};
    }
    public sealed class ShopNotice
    {
        public string id;public ShopRequest request;public bool approved;public ulong? gold,price;
        public WorldOwnedItem acquired;
    }
    [Serializable,DataContract] public sealed class ShopReceipt
    {
        [DataMember] public string id,request,quote,owned;
        [DataMember] public long revision;
        [DataMember] public bool buy;
        [DataMember] public int item,slot;
        [DataMember] public ulong price;
        public ShopReceipt Copy()=>(ShopReceipt)MemberwiseClone();
        public static bool Valid(ShopReceipt r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)
            &&(r.quote==null||Guid.TryParseExact(r.quote,"N",out _))&&Guid.TryParseExact(r.owned,"N",out _)&&r.revision>0&&r.item>=0&&r.slot>=0&&r.slot<28;
    }
    internal sealed class ShopOffer
    {
        public ShopQuote Quote;public WorldRecord State;
        public bool Matches(WorldSession s)=>Quote?.request!=null&&s.Kind==FacilityKind.Shop&&s.Token==Quote.request.token&&s.Npc==Quote.request.npc
            &&s.Record.map==Quote.request.map&&s.CurrencySession==Quote.request.session&&s.CharacterId==Quote.request.character
            &&(s.Record.shop?.revision??0)==Quote.request.revision&&(s.Record.currency?.revision??0)==Quote.request.currencyRevision;
    }
}
