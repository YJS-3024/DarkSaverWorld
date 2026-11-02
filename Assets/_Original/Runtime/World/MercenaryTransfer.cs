using System;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    // Local approval contracts. These fields are not reconstructed native packet layouts.
    public sealed class MercenaryRequest
    {
        public string id,character,session,token,npc;
        public int map,commander,job,count,limit;public bool hire;public ulong? price;
        public long revision,currencyRevision;
        public MercenaryRequest Copy()=>(MercenaryRequest)MemberwiseClone();
    }
    public sealed class MercenaryNotice
    {
        public string id,request,character,session,token,npc;
        public int map,commander,job,count;public bool approved,hire;
        public long expectedRevision,currencyRevision;
        public ulong? amount,gold;
    }
    [Serializable,DataContract] public sealed class MercenaryReceipt
    {
        [DataMember] public string id,request;
        [DataMember] public long revision;
        [DataMember] public bool hire;
        [DataMember] public int job,count;
        [DataMember] public ulong amount;
        public MercenaryReceipt Copy()=>(MercenaryReceipt)MemberwiseClone();
        public static bool Valid(MercenaryReceipt r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)
            &&r.revision>0&&r.job>=0&&r.count>=0;
    }
}
