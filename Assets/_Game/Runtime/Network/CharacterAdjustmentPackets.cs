using System;

namespace DarkSaver.Network
{
    // Names identify the source list only; learning/forgetting semantics are not inferred.
    public enum CharacterMagicListCommand { List0=0x83, List1=0x85 }
    public sealed class CharacterMagicListRequest : ClientPacket
    {
        public const int OriginalPayloadSize=5;
        public CharacterMagicListCommand Command { get; }
        public byte OriginalOpcode=>(byte)Command;
        public ushort UnitRecordId { get; }
        public ushort MagicValue { get; }
        public CharacterMagicListRequest(CharacterMagicListCommand command,ushort unitRecordId,ushort magicValue)
        {
            if(command!=CharacterMagicListCommand.List0 && command!=CharacterMagicListCommand.List1) throw new ArgumentOutOfRangeException(nameof(command));
            Command=command; UnitRecordId=unitRecordId; MagicValue=magicValue;
        }
        public bool TryWriteOriginalPayload(Span<byte> destination,out int written)
        {
            written=0; if(destination.Length<OriginalPayloadSize) return false;
            destination[0]=OriginalOpcode; destination[1]=unchecked((byte)UnitRecordId); destination[2]=(byte)(UnitRecordId>>8);
            destination[3]=unchecked((byte)MagicValue); destination[4]=(byte)(MagicValue>>8);
            written=OriginalPayloadSize; return true;
        }
    }
    // 0x419f40: opcode84 + caller-supplied word + info-record word + selected magic word.
    // The first word's meaning remains unknown. Do not substitute a guessed list/slot index.
    public sealed class CharacterMagicSelectionRequest : ClientPacket
    {
        public const int OriginalPayloadSize=7;
        public byte OriginalOpcode=>0x84;
        public ushort AdditionalValue { get; }
        public ushort UnitRecordId { get; }
        public ushort MagicValue { get; }
        public CharacterMagicSelectionRequest(ushort additionalValue,ushort unitRecordId,ushort magicValue)
        { AdditionalValue=additionalValue; UnitRecordId=unitRecordId; MagicValue=magicValue; }
        public bool TryWriteOriginalPayload(Span<byte> destination,out int written)
        {
            written=0; if(destination.Length<OriginalPayloadSize) return false;
            destination[0]=OriginalOpcode;
            destination[1]=unchecked((byte)AdditionalValue); destination[2]=(byte)(AdditionalValue>>8);
            destination[3]=unchecked((byte)UnitRecordId); destination[4]=(byte)(UnitRecordId>>8);
            destination[5]=unchecked((byte)MagicValue); destination[6]=(byte)(MagicValue>>8);
            written=OriginalPayloadSize; return true;
        }
    }
    public enum CharacterStatAdjustmentDirection { Increase=1, Decrease=2 }
    // 0x419d80: opcode82 + record word + stat index(0..5) word + direction(1/2) word.
    // Separate from battle AttackRequest's opcode82 and its different dispatch context.
    public sealed class CharacterStatAdjustmentRequest : ClientPacket
    {
        public const int OriginalPayloadSize=7;
        public byte OriginalOpcode=>0x82;
        public ushort UnitRecordId { get; }
        public int StatIndex { get; }
        public CharacterStatAdjustmentDirection Adjustment { get; }
        public CharacterStatAdjustmentRequest(ushort unitRecordId,int statIndex,CharacterStatAdjustmentDirection adjustment)
        {
            if((uint)statIndex>=6) throw new ArgumentOutOfRangeException(nameof(statIndex));
            if(adjustment!=CharacterStatAdjustmentDirection.Increase && adjustment!=CharacterStatAdjustmentDirection.Decrease)
                throw new ArgumentOutOfRangeException(nameof(adjustment));
            UnitRecordId=unitRecordId; StatIndex=statIndex; Adjustment=adjustment;
        }
        public bool TryWriteOriginalPayload(Span<byte> destination,out int written)
        {
            written=0; if(destination.Length<OriginalPayloadSize) return false;
            destination[0]=OriginalOpcode; destination[1]=unchecked((byte)UnitRecordId); destination[2]=(byte)(UnitRecordId>>8);
            destination[3]=(byte)StatIndex; destination[4]=0; destination[5]=(byte)Adjustment; destination[6]=0;
            written=OriginalPayloadSize; return true;
        }
    }
    public enum CharacterAdjustmentCommand { Confirm=0x88, Cancel=0x89, QueryValues=0x8a }
    // Original 0x41a640/6a0/7d0: opcode + one raw info-record word.
    // Confirmation is a request, not proof of acceptance or a local state mutation.
    public sealed class CharacterAdjustmentCommandRequest : ClientPacket
    {
        public const int OriginalPayloadSize=3;
        public CharacterAdjustmentCommand Command { get; }
        public byte OriginalOpcode=>(byte)Command;
        public ushort UnitRecordId { get; }
        public CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand command,ushort unitRecordId)
        {
            if(command!=CharacterAdjustmentCommand.Confirm && command!=CharacterAdjustmentCommand.Cancel && command!=CharacterAdjustmentCommand.QueryValues)
                throw new ArgumentOutOfRangeException(nameof(command));
            Command=command; UnitRecordId=unitRecordId;
        }
        public bool TryWriteOriginalPayload(Span<byte> destination,out int written)
        {
            written=0; if(destination.Length<OriginalPayloadSize) return false;
            destination[0]=OriginalOpcode; destination[1]=unchecked((byte)UnitRecordId); destination[2]=(byte)(UnitRecordId>>8);
            written=OriginalPayloadSize; return true;
        }
    }
    // 0x41a700 writes opcode81 + info-record word + selected class-table lookup word.
    // Eligibility/point consumption/server acknowledgement are deliberately not inferred.
    public sealed class CharacterAdjustmentClassRequest : ClientPacket
    {
        public const int OriginalPayloadSize=5;
        public byte OriginalOpcode=>0x81;
        public ushort UnitRecordId { get; }
        public ushort SelectedClassValue { get; }
        public CharacterAdjustmentClassRequest(ushort unitRecordId,ushort selectedClassValue)
        { UnitRecordId=unitRecordId; SelectedClassValue=selectedClassValue; }
        public bool TryWriteOriginalPayload(Span<byte> destination,out int written)
        {
            written=0; if(destination.Length<OriginalPayloadSize) return false;
            destination[0]=OriginalOpcode; destination[1]=unchecked((byte)UnitRecordId); destination[2]=(byte)(UnitRecordId>>8);
            destination[3]=unchecked((byte)SelectedClassValue); destination[4]=(byte)(SelectedClassValue>>8);
            written=OriginalPayloadSize; return true;
        }
    }
    // 0x41a300: opcode87, one skipped word, two signed class-table lookup values,
    // signed count and up to ten signed lookup candidates. Previous/next meanings
    // and the skipped word are unresolved; no class changes are applied here.
    public sealed class CharacterAdjustmentOptionsNotification : ServerPacket
    {
        public const int OriginalHeaderSize=9;
        public const int MaximumSupportedOptions=10;
        public byte OriginalOpcode=>0x87;
        public ushort UninterpretedHeaderWord { get; }
        public short ClassLookupValue0 { get; }
        public short ClassLookupValue1 { get; }
        private readonly short[] options;
        public int OptionCount=>options.Length;
        private CharacterAdjustmentOptionsNotification(ushort header,short value0,short value1,short[] entries)
        { UninterpretedHeaderWord=header; ClassLookupValue0=value0; ClassLookupValue1=value1; options=entries; }
        public bool TryGetOption(int index,out short value)
        {
            value=0; if((uint)index>=(uint)options.Length) return false;
            value=options[index]; return true;
        }
        public static bool TryReadOriginalPrefix(ReadOnlySpan<byte> source,out CharacterAdjustmentOptionsNotification packet,out int consumed)
        {
            packet=null; consumed=0;
            if(source.Length<OriginalHeaderSize || source[0]!=0x87) return false;
            short count=ReadWord(source,7);
            // Original storage has ten entries. Reject unsupported counts rather
            // than guessing how malformed/extended messages should be truncated.
            if(count<0 || count>MaximumSupportedOptions || source.Length<OriginalHeaderSize+count*2) return false;
            var entries=new short[count];
            for(int i=0;i<count;i++) entries[i]=ReadWord(source,OriginalHeaderSize+i*2);
            packet=new CharacterAdjustmentOptionsNotification(unchecked((ushort)ReadWord(source,1)),ReadWord(source,3),ReadWord(source,5),entries);
            consumed=OriginalHeaderSize+count*2; return true;
        }
        private static short ReadWord(ReadOnlySpan<byte> source,int offset)=>unchecked((short)(source[offset] | source[offset+1]<<8));
    }

    // 0x41a210 writes u8 0x86 + the info payload's first word.
    // Definition only: do not send on opening the native shell or invent a reply.
    public sealed class CharacterAdjustmentQueryRequest : ClientPacket
    {
        public const int OriginalPayloadSize = 3;
        public byte OriginalOpcode => 0x86;
        public ushort UnitRecordId { get; }
        public CharacterAdjustmentQueryRequest(ushort unitRecordId) { UnitRecordId=unitRecordId; }
        public bool TryWriteOriginalPayload(Span<byte> destination,out int written)
        {
            written=0;
            if(destination.Length<OriginalPayloadSize) return false;
            destination[0]=OriginalOpcode; destination[1]=unchecked((byte)UnitRecordId);
            destination[2]=(byte)(UnitRecordId>>8); written=OriginalPayloadSize; return true;
        }
    }

    // 0x41a290 handles 0x8b by reading six signed words starting at byte1.
    // 0x419dfc/0x419e00/0x419e66/0x419e6e confirm these are stat point costs.
    // Preserve signed raw values, including unsupported negative costs.
    // Only a known prefix is decoded; no transport envelope or total message size is inferred.
    public sealed class CharacterAdjustmentValuesNotification : ServerPacket
    {
        public const int OriginalPrefixSize = 13;
        public const int ValueCount = 6;
        public byte OriginalOpcode => 0x8b;
        private readonly short[] values;
        private CharacterAdjustmentValuesNotification(short[] source) { values=source; }
        public bool TryGetValue(int index,out short value)
        {
            value=0;
            if((uint)index>=ValueCount) return false;
            value=values[index]; return true;
        }
        public static bool TryReadOriginalPrefix(ReadOnlySpan<byte> source,out CharacterAdjustmentValuesNotification packet,out int consumed)
        {
            packet=null; consumed=0;
            if(source.Length<OriginalPrefixSize || source[0]!=0x8b) return false;
            var values=new short[ValueCount];
            for(int i=0;i<ValueCount;i++) values[i]=unchecked((short)(source[1+i*2] | source[2+i*2]<<8));
            packet=new CharacterAdjustmentValuesNotification(values); consumed=OriginalPrefixSize; return true;
        }
    }
}
