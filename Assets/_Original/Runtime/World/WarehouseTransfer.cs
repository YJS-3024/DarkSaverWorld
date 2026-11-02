using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    public enum WarehouseOperation { State, Move, Confirm }
    // Local staged-storage contracts; these are not recovered original wire commands or storage limits.
    public sealed class WarehouseRequest
    {
        public string id,character,session,token,npc,owned;
        public int map,slot=-1;public bool deposit;public WarehouseOperation operation;
        public long revision,draftRevision,currencyRevision;
        public WorldOwnedItem[] inventory,storage;
        public WarehouseRequest Copy(){var r=(WarehouseRequest)MemberwiseClone();r.inventory=inventory?.Select(i=>i.Copy()).ToArray();r.storage=storage?.Select(i=>i.Copy()).ToArray();return r;}
    }
    public sealed class WarehouseNotice
    {
        public string id,request,character,session,token,npc;
        public int map;public bool approved;public WarehouseOperation operation;
        public long expectedRevision,draftRevision,currencyRevision;
        public int? capacity;public ulong? fee,gold,bank;public BankFeeSource? feeSource;
        public WorldOwnedItem[] inventory,storage;
    }
    public sealed class WarehouseTerms
    {
        public int capacity;public ulong? fee;public BankFeeSource? feeSource;
        public WarehouseTerms Copy()=>(WarehouseTerms)MemberwiseClone();
    }
    [Serializable,DataContract] public sealed class WarehouseReceipt
    {
        [DataMember] public string id,request;
        [DataMember] public long revision,draftRevision;
        [DataMember] public int capacity;
        [DataMember] public ulong fee;
        [DataMember] public BankFeeSource feeSource;
        public WarehouseReceipt Copy()=>(WarehouseReceipt)MemberwiseClone();
        public static bool Valid(WarehouseReceipt r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)
            &&r.revision>0&&r.draftRevision>=0&&r.capacity>=0&&Enum.IsDefined(typeof(BankFeeSource),r.feeSource);
    }
}
