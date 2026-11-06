using System;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    // Local return-stone approval only. No inferred native opcode, destination or consumption rule.
    public sealed class ItemUseRequest
    {
        public string id,character,session,owned;
        public int map,item,slot;public long revision;
        public ItemUseRequest Copy()=>(ItemUseRequest)MemberwiseClone();
        public static bool Same(ItemUseRequest a,ItemUseRequest b)=>a!=null&&b!=null&&a.id==b.id&&a.character==b.character&&a.session==b.session
            &&a.owned==b.owned&&a.map==b.map&&a.item==b.item&&a.slot==b.slot&&a.revision==b.revision;
    }
    public sealed class ItemUseDestination
    {
        public int map,x,y,direction;
        public ItemUseDestination Copy()=>(ItemUseDestination)MemberwiseClone();
    }
    public sealed class ItemUseNotice
    {
        public string id;public ItemUseRequest request;public bool approved;public bool? consumed;
        public ItemUseDestination destination;
    }
    [Serializable,DataContract] public sealed class ItemUseReceipt
    {
        [DataMember] public string id,request,owned;
        [DataMember] public long revision;
        [DataMember] public int item,sourceMap,map,x,y,direction;
        [DataMember] public bool consumed;
        public ItemUseReceipt Copy()=>(ItemUseReceipt)MemberwiseClone();
        public static bool Valid(ItemUseReceipt r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)&&Guid.TryParseExact(r.owned,"N",out _)
            &&r.revision>0&&(r.item==324||r.item==361)&&r.sourceMap>=0&&r.map>=0&&r.x>=0&&r.y>=0&&r.direction>=0&&r.direction<=3;
    }
}
