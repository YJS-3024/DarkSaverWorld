using System;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    // Local authoritative-value contracts. Neither currency wire widths/opcodes nor starting funds are inferred.
    public sealed class CurrencyRequest
    {
        public string id,character,session;public long revision;
        public CurrencyRequest Copy()=>(CurrencyRequest)MemberwiseClone();
    }
    public sealed class CurrencyNotice
    {
        public string id,request,character,session;public long expectedRevision;
        public bool approved;
        // Null leaves the corresponding existing balance/known flag unchanged; explicit zero is known zero.
        public ulong? gold,bank;
    }
    [Serializable,DataContract] public sealed class CurrencyReceipt
    {
        [DataMember] public string id;
        [DataMember] public long revision;
        public CurrencyReceipt Copy()=>(CurrencyReceipt)MemberwiseClone();
        public static bool Valid(CurrencyReceipt r)=>r==null||r.revision>0&&Guid.TryParseExact(r.id,"N",out _);
    }
}
