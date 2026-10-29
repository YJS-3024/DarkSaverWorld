using System;
using DarkSaver.Core;
namespace DarkSaver.Network
{
    // Client view of a received role record. Missing values are not invented.
    [Serializable] public sealed class WorldRosterEntry
    {
        public ushort RecordId;
        public int JobId;
        public string Name,JobName,Family;
        public ushort? OriginalFlag;
        public NativeCharacterProfile Profile;
        public int? MaxHp,MaxMp,Attack,Defense;
        public uint? Experience;
        public byte[] CharacterInfoPayload;
        public WorldRosterEntry Clone()=>new WorldRosterEntry{RecordId=RecordId,JobId=JobId,Name=Name,JobName=JobName,Family=Family,OriginalFlag=OriginalFlag,Profile=Profile?.Clone(),MaxHp=MaxHp,MaxMp=MaxMp,Attack=Attack,Defense=Defense,Experience=Experience,CharacterInfoPayload=CharacterInfoPayload==null?null:(byte[])CharacterInfoPayload.Clone()};
    }
    public sealed class Notify_WorldRoster : ServerPacket
    {
        private readonly WorldRosterEntry[] entries;
        public WorldRosterEntry[] Capture()=>Array.ConvertAll(entries,e=>e.Clone());
        public Notify_WorldRoster(WorldRosterEntry[] entries)
        {
            if(entries==null || entries.Length>10)throw new ArgumentException("At most ten role slots.");
            var ids=new System.Collections.Generic.HashSet<ushort>();
            foreach(var e in entries)if(e==null || !ids.Add(e.RecordId) || e.JobId<=0 || string.IsNullOrWhiteSpace(e.Name)
                || e.OriginalFlag.HasValue && e.OriginalFlag!=1 && e.OriginalFlag!=2
                || e.CharacterInfoPayload!=null && e.CharacterInfoPayload.Length!=OriginalCharacterInfoValues.PayloadSize)throw new ArgumentException("Invalid role snapshot.");
            this.entries=Array.ConvertAll(entries,e=>e.Clone());
        }
    }
    // Original checkbox 0x41C330 ->0x41C9C0: opcode42, record-id word, raw flag1/2.
    public sealed class Request_WorldRosterFlag : ClientPacket
    {
        public ushort RecordId {get;} public ushort Flag {get;}
        public Request_WorldRosterFlag(ushort id,ushort flag){if(flag!=1 && flag!=2)throw new ArgumentOutOfRangeException(nameof(flag));RecordId=id;Flag=flag;}
    }
    // Original drag/drop0x41C650: opcode3B, destination slot, source slot (not record ids).
    public sealed class Request_WorldRosterOrder : ClientPacket
    {
        public ushort Destination {get;} public ushort Source {get;}
        public Request_WorldRosterOrder(ushort destination,ushort source){if(destination>9 || source>9)throw new ArgumentOutOfRangeException();Destination=destination;Source=source;}
    }
    public sealed class Notify_WorldInventory : ServerPacket
    {
        [Serializable] public sealed class Item
        {
            public int ItemId,Quantity;
            public ushort? OriginalAdditionalValue;
            public Item Clone()=>(Item)MemberwiseClone();
        }
        private readonly Item[] items;
        public Item[] Capture()=>Array.ConvertAll(items,i=>i.Clone());
        public Notify_WorldInventory(Item[] source)
        {
            if(source==null || source.Length>28)throw new ArgumentException("Original inventory has 28 cells.");
            foreach(var i in source)if(i==null || i.ItemId<1 || i.ItemId>short.MaxValue || i.Quantity<1)throw new ArgumentException("Invalid item record.");
            items=Array.ConvertAll(source,i=>i.Clone());
        }
    }
}
