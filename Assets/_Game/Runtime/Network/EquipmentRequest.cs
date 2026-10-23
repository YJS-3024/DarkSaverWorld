using System;

namespace DarkSaver.Network
{
    // Original 0x420800 writes opcode u8 followed by five u16 fields.
    // This is an in-memory definition, not a transport encoder or equip acceptance.
    public sealed class EquipmentRequest : ClientPacket
    {
        public const int OriginalPayloadSize = 11; // Excludes any unconfirmed transport envelope.
        public byte OriginalOpcode => 0x40;
        public ushort? UnitRecordId { get; }
        public ushort? SlotIndex { get; }
        public ushort? ItemId { get; }
        public ushort? AdditionalValue { get; } // Original third argument; meaning unresolved.
        public ushort? PreviousItemId { get; }
        public bool HasOriginalFieldValues => UnitRecordId.HasValue && SlotIndex.HasValue
            && ItemId.HasValue && AdditionalValue.HasValue && PreviousItemId.HasValue;

        // Preserve the existing placeholder API without manufacturing zero-valued fields.
        public EquipmentRequest() { }
        public EquipmentRequest(ushort unitRecordId, ushort slotIndex, ushort itemId, ushort additionalValue, ushort previousItemId)
        {
            if (slotIndex > 6) throw new ArgumentOutOfRangeException(nameof(slotIndex));
            UnitRecordId = unitRecordId; SlotIndex = slotIndex; ItemId = itemId;
            AdditionalValue = additionalValue; PreviousItemId = previousItemId;
        }
        // x86 original writes little-endian words. Unknown fields stay unknown;
        // a failed write leaves the entire caller buffer unchanged.
        public bool TryWriteOriginalPayload(Span<byte> destination, out int written)
        {
            written = 0;
            if (!HasOriginalFieldValues || destination.Length < OriginalPayloadSize) return false;
            destination[0] = OriginalOpcode;
            WriteWord(destination,1,UnitRecordId.Value); WriteWord(destination,3,SlotIndex.Value);
            WriteWord(destination,5,ItemId.Value); WriteWord(destination,7,AdditionalValue.Value);
            WriteWord(destination,9,PreviousItemId.Value);
            written = OriginalPayloadSize; return true;
        }
        private static void WriteWord(Span<byte> destination, int offset, ushort value)
        {
            destination[offset] = (byte)(value & 0xff);
            destination[offset+1] = (byte)(value >> 8);
        }
    }
}
