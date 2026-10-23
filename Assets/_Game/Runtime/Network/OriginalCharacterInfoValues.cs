using System;

namespace DarkSaver.Network
{
    // Saver 0x42c50b copies this payload into the separate 0x2dc-byte info record at +4.
    // NOT a complete network packet or the 0x2e0-byte battle/render record.
    // Rows are display order, not inferred base/current/effective stat semantics.
    public static class OriginalCharacterInfoValues
    {
        public const int PayloadSize = 724;
        public const int ColumnCount = 2;
        public const int RowsPerColumn = 8;
        // Original 412E81/85 and 412F5D/60: first level is +0A, parenthesized is +08.
        // User-confirmed labels: job level(base level). No automatic session-to-payload conversion.
        // Two unsigned32 experience fields, NOT a single signed/unsigned64 value.
        public static bool TryReadHeader(ReadOnlySpan<byte> payload,out short firstLevel,out short parenthesizedLevel,out uint firstExperience,out uint secondExperience)
        {
            firstLevel=parenthesizedLevel=0; firstExperience=secondExperience=0;
            if(payload.Length!=PayloadSize) return false;
            firstLevel=ReadSignedWord(payload,0x0a); parenthesizedLevel=ReadSignedWord(payload,8);
            firstExperience=ReadUnsignedDword(payload,0x0c); secondExperience=ReadUnsignedDword(payload,0x10); return true;
        }
        private static uint ReadUnsignedDword(ReadOnlySpan<byte> payload,int offset)=>
            (uint)payload[offset] | (uint)payload[offset+1]<<8 | (uint)payload[offset+2]<<16 | (uint)payload[offset+3]<<24;
        // 0x41b960 uses Win32 MulDiv(before,width,limit), upper clamps both widths,
        // then bounds the opening-snapshot width by the current width.
        public static bool TryCalculateEquipmentStatBar(ReadOnlySpan<byte> current,ReadOnlySpan<byte> baseline,int index,int width,
            out int currentPixels,out int baselinePixels)
        {
            currentPixels=baselinePixels=0;
            if(width<0 || !TryReadEquipmentStatTriplet(current,index,out short before,out _,out short limit)
                || !TryReadEquipmentStatTriplet(baseline,index,out short initial,out _,out _) || before<0 || initial<0 || limit<=0) return false;
            // Positive MulDiv rounds to nearest, with half rounded upward. long prevents product overflow.
            currentPixels=(int)Math.Min(width,((long)before*width+limit/2)/limit);
            baselinePixels=(int)Math.Min(currentPixels,((long)initial*width+limit/2)/limit); return true;
        }
        // Original format: equipment-before / equipment-after / limit. Not current HP/MP.
        public static bool TryReadEquipmentStatTriplet(ReadOnlySpan<byte> payload,int index,out short before,out short after,out short limit)
        {
            before=after=limit=0;
            if(payload.Length!=PayloadSize || (uint)index>=6) return false;
            int offset=0x14+index*6;
            before=ReadSignedWord(payload,offset); after=ReadSignedWord(payload,offset+2); limit=ReadSignedWord(payload,offset+4); return true;
        }
        public static bool TryReadRemainingStatPoints(ReadOnlySpan<byte> payload,out short points)
        {
            points=0; if(payload.Length!=PayloadSize) return false;
            points=ReadSignedWord(payload,0x38); return true;
        }
        // 0x41a1a0 compacts only list1, decrements count, then clamps selection.
        // Return an isolated copy; never edit the live session or grant/remove learned spells.
        public static bool TryPreviewRemoveSecondMagic(ReadOnlySpan<byte> source,int selectedIndex,out byte[] preview,out int nextSelection)
        {
            preview=null; nextSelection=-1;
            if(!TryReadMagicCount(source,1,out int count) || (uint)selectedIndex>=(uint)count) return false;
            preview=source.ToArray();
            for(int i=selectedIndex;i<count-1;i++) {
                preview[0x6c+i*2]=source[0x6c+(i+1)*2]; preview[0x6d+i*2]=source[0x6d+(i+1)*2];
            }
            // The original leaves the now-unused trailing word unchanged.
            preview[0x6a]=(byte)(count-1); preview[0x6b]=0;
            nextSelection=count==1?-1:Math.Min(selectedIndex,count-2); return true;
        }
        private static readonly int[] AdjustableStatOffsets={0x16,0x1c,0x22,0x28,0x2e,0x34};
        // Pure preview against the snapshot captured when the original window opens.
        // Does not write either payload, emit a request, or change the live session.
        public static bool TryPreviewStatAdjustment(ReadOnlySpan<byte> current,ReadOnlySpan<byte> baseline,
            CharacterAdjustmentValuesNotification costs,int statIndex,CharacterStatAdjustmentDirection adjustment,
            out short value,out short points)
        {
            value=points=0;
            if(costs==null || !costs.TryGetValue(statIndex,out short cost)) return false;
            return TryPreviewStatAdjustment(current,baseline,cost,statIndex,adjustment,out value,out points);
        }
        // Explicit cost allows a separate user-configured local policy without fabricating an 8B response.
        public static bool TryPreviewStatAdjustment(ReadOnlySpan<byte> current,ReadOnlySpan<byte> baseline,
            int cost,int statIndex,CharacterStatAdjustmentDirection adjustment,out short value,out short points)
        {
            value=points=0;
            if(current.Length!=PayloadSize || baseline.Length!=PayloadSize || (uint)statIndex>=6 || cost<0 || cost>short.MaxValue) return false;
            int offset=AdjustableStatOffsets[statIndex];
            short currentValue=ReadSignedWord(current,offset), initialValue=ReadSignedWord(baseline,offset);
            int remaining=ReadSignedWord(current,0x38), next;
            if(adjustment==CharacterStatAdjustmentDirection.Increase) {
                if(remaining<cost) return false;
                next=currentValue+1; remaining-=cost;
            } else if(adjustment==CharacterStatAdjustmentDirection.Decrease) {
                if(currentValue<=initialValue) return false;
                next=currentValue-1; remaining+=cost;
            } else return false;
            // Safe native policy: do not emulate signed16 wraparound.
            if(next<short.MinValue || next>short.MaxValue || remaining<short.MinValue || remaining>short.MaxValue) return false;
            value=(short)next; points=(short)remaining; return true;
        }
        private static short ReadSignedWord(ReadOnlySpan<byte> source,int offset)=>unchecked((short)(source[offset] | source[offset+1]<<8));
        // User-confirmed: top list0=learned, second list1=battle-use selection; removing list1 is not unlearning.
        public static int MagicListCapacity(int list)=>list==0?16:list==1?8:0;
        public static bool TryReadMagicCount(ReadOnlySpan<byte> payload,int list,out int count)
        {
            count=0;
            int capacity=MagicListCapacity(list);
            if(payload.Length!=PayloadSize || capacity==0) return false;
            int offset=list==0?0x40:0x6a;
            short raw=unchecked((short)(payload[offset] | payload[offset+1]<<8));
            // Reject unsupported counts; do not reproduce the original unchecked record reads.
            if(raw<0 || raw>capacity) return false;
            count=raw; return true;
        }
        public static bool TryReadMagicId(ReadOnlySpan<byte> payload,int list,int index,out short id)
        {
            id=0;
            if(!TryReadMagicCount(payload,list,out int count) || (uint)index>=(uint)count) return false;
            int offset=(list==0?0x42:0x6c)+index*2;
            id=unchecked((short)(payload[offset] | payload[offset+1]<<8)); return true;
        }
        // Original scene23 positions; each icon occupies 32px with a 1px gap.
        public static bool TryGetMagicCellOrigin(int list,int index,out int x,out int y)
        {
            x=y=0;
            if((uint)index>=(uint)MagicListCapacity(list)) return false;
            x=208+(index%8)*33; y=(list==0?47:121)+(index/8)*33; return true;
        }
        private static readonly int[] Offsets = {
            0x16,0x1c,0x22,0x28,0x2b8,0x2ba,0x2e,0x34,
            0x2bc,0x2be,0x2c0,0x2c2,0x2c4,0x2c6,0x2c8,0x2ca
        };

        public static bool TryGetPayloadOffset(int column, int row, out int offset)
        {
            offset = 0;
            if ((uint)column >= ColumnCount || (uint)row >= RowsPerColumn) return false;
            offset = Offsets[column * RowsPerColumn + row]; return true;
        }

        // Exact size deliberately rejects a prefixed record or a truncated/other record.
        // Decode signed LE16 just as the original movsx does; retain negative values.
        public static bool TryRead(ReadOnlySpan<byte> payload, int column, int row, out short value)
        {
            value = 0;
            if (payload.Length != PayloadSize || !TryGetPayloadOffset(column,row,out int offset)) return false;
            value = unchecked((short)(payload[offset] | payload[offset+1] << 8)); return true;
        }
    }
}
