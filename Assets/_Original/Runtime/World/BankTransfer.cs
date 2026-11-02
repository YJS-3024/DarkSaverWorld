using System;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    // Explicit local receipt semantics, not original wire codes or recovered fee policy.
    public enum BankFeeSource { Wallet, Savings }
    public sealed class BankTransferRequest
    {
        public string id,character,session,token,npc;
        public int map;public bool deposit;public ulong amount;public long currencyRevision;
        public BankTransferRequest Copy()=>(BankTransferRequest)MemberwiseClone();
    }
    public sealed class BankTransferNotice
    {
        public string id,request,character,session,token,npc;
        public int map;public bool approved,deposit;public ulong amount;public long expectedRevision;
        public ulong? gold,bank,fee;public BankFeeSource? feeSource;
    }
    [Serializable,DataContract] public sealed class BankTransferReceipt
    {
        [DataMember] public string id,request;
        [DataMember] public long currencyRevision;
        [DataMember] public bool deposit;
        [DataMember] public ulong amount,fee;
        [DataMember] public BankFeeSource feeSource;
        public BankTransferReceipt Copy()=>(BankTransferReceipt)MemberwiseClone();
        public static bool Valid(BankTransferReceipt r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)
            &&r.currencyRevision>0&&r.amount>0&&Enum.IsDefined(typeof(BankFeeSource),r.feeSource);
    }
}
