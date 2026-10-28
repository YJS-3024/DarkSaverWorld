using DarkSaver.Core;

namespace DarkSaver.Network
{
    // Typed local session contracts. No original opcode is fabricated for native-only fields.
    public sealed class LocalBattleTransitionRequest : ClientPacket
    {public bool Enter {get;} public LocalBattleTransitionRequest(bool enter){Enter=enter;}}
    public sealed class LocalFormationRequest : ClientPacket
    {
        private readonly NativeOwnedRoster roster;public NativeOwnedRoster CaptureRoster()=>roster?.Clone();
        public LocalFormationRequest(NativeOwnedRoster units){roster=units?.Clone();}
    }
    public sealed class LocalTempleRecoveryRequest : ClientPacket { }
    public sealed class SaveWorldRequest : ClientPacket { }
    public sealed class Request_SaveTrainingReturn : ClientPacket { }
    public sealed class LocalBattleCheckpointRequest : ClientPacket { }
    public sealed class TownQuestRequest : ClientPacket
    {public int QuestId {get;} public bool Claim {get;} public TownQuestRequest(int id,bool claim){QuestId=id;Claim=claim;}}
    public sealed class LocalEquipmentChangeRequest : ClientPacket
    {
        public int Slot {get;} public int Item {get;} private readonly NativeCharacterProfile baseline;
        public NativeCharacterProfile CaptureBaseline()=>baseline?.Clone();
        public LocalEquipmentChangeRequest(int slot,int item,NativeCharacterProfile profile){Slot=slot;Item=item;baseline=profile?.Clone();}
    }
    public sealed class LocalBattleItemUseRequest : ClientPacket
    {
        public int Item {get;} public short Target {get;} private readonly NativeInventory baseline;
        public NativeInventory CaptureBaseline()=>baseline?.Clone();
        public LocalBattleItemUseRequest(int item,short target,NativeInventory inventory){Item=item;Target=target;baseline=inventory?.Clone();}
    }
    public sealed class LocalTradeRequest : ClientPacket
    {public int Item {get;} public int Quantity {get;} public bool Buying {get;} public LocalTradeRequest(int item,int quantity,bool buying){Item=item;Quantity=quantity;Buying=buying;}}
    public sealed class LocalHireRequest : ClientPacket
    {public int Skill {get;} public int Job {get;} public LocalHireRequest(int skill,int job){Skill=skill;Job=job;}}
    public sealed class LocalClassChangeRequest : ClientPacket { }
    public sealed class LocalStatAllocationRequest : ClientPacket
    {
        public int Attack {get;} public int Defense {get;} public int MagicAttack {get;} public int MagicDefense {get;}
        private readonly NativeCharacterProfile baseline;private readonly int[] magic;
        public NativeCharacterProfile CaptureBaseline()=>baseline?.Clone(); public int[] CaptureMagic()=>magic==null?null:(int[])magic.Clone();
        public LocalStatAllocationRequest(NativeCharacterProfile profile,int attack,int defense,int magicAttack,int magicDefense,int[] selected)
        {baseline=profile?.Clone();Attack=attack;Defense=defense;MagicAttack=magicAttack;MagicDefense=magicDefense;magic=selected==null?null:(int[])selected.Clone();}
    }
    public sealed class SessionStateNotification : LocalCommandResponse
    {
        private readonly NativeCharacterProfile character;private readonly NativeInventory inventory;private readonly NativeOwnedRoster roster;
        public NativeCharacterProfile CaptureCharacter()=>character?.Clone();public NativeInventory CaptureInventory()=>inventory?.Clone();public NativeOwnedRoster CaptureRoster()=>roster?.Clone();
        public SessionStateNotification(bool accepted,string reason,NativeCharacterProfile profile,NativeInventory items,NativeOwnedRoster units):base(accepted,reason)
        {character=profile?.Clone();inventory=items?.Clone();roster=units?.Clone();}
    }
}
