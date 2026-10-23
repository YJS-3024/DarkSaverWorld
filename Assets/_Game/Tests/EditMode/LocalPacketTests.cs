using System;
using System.Collections.Generic;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class LocalPacketTests
    {
        [TestCase(10,8,20,176,88,70)] [TestCase(1,0,32,176,6,0)]
        [TestCase(3,1,10,176,53,18)] [TestCase(25,30,20,176,176,176)]
        [TestCase(5,10,20,176,44,44)] [TestCase(0,1,20,176,0,0)]
        [TestCase(32767,32767,1,2147483647,2147483647,2147483647)]
        public void EquipmentStatBarsMatchPositiveMulDivRoundingAndOriginalClamps(int current,int baseline,int limit,int width,int expected,int initialExpected)
        {
            var now=new byte[724]; var initial=new byte[724];
            now[0x14]=unchecked((byte)current); now[0x15]=(byte)(current>>8); now[0x18]=unchecked((byte)limit); now[0x19]=(byte)(limit>>8);
            initial[0x14]=unchecked((byte)baseline); initial[0x15]=(byte)(baseline>>8);
            var before=(byte[])now.Clone(); var original=(byte[])initial.Clone();
            Assert.That(OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(now,initial,0,width,out int actual,out int initialPixels),Is.True);
            Assert.That(actual,Is.EqualTo(expected)); Assert.That(initialPixels,Is.EqualTo(initialExpected));
            CollectionAssert.AreEqual(before,now); CollectionAssert.AreEqual(original,initial);
        }
        [Test]
        public void EquipmentStatBarsRejectInvalidDivisorsValuesAndSnapshots()
        {
            var current=new byte[724]; var baseline=new byte[724];
            Assert.That(OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(current,baseline,0,176,out _,out _),Is.False);
            current[0x18]=20; current[0x14]=current[0x15]=0xff;
            Assert.That(OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(current,baseline,0,176,out _,out _),Is.False);
            current[0x14]=current[0x15]=0; baseline[0x14]=baseline[0x15]=0xff;
            Assert.That(OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(current,baseline,0,176,out _,out _),Is.False);
            baseline[0x14]=baseline[0x15]=0;
            Assert.That(OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(current,baseline,0,-1,out _,out _),Is.False);
            Assert.That(OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(current,baseline,6,176,out _,out _),Is.False);
            Assert.That(OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(current,new byte[728],0,176,out _,out _),Is.False);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void EquipmentStatTripletsPreserveSignedWordsWithoutTreatingThemAsCurrentVitals(int index)
        {
            var payload=new byte[724]; int offset=0x14+index*6;
            payload[offset+1]=0x80; payload[offset+2]=0xff; payload[offset+3]=0x7f; payload[offset+4]=payload[offset+5]=0xff;
            payload[0x38]=0xff; payload[0x39]=0xff; var initial=(byte[])payload.Clone();
            Assert.That(OriginalCharacterInfoValues.TryReadEquipmentStatTriplet(payload,index,out short before,out short after,out short limit),Is.True);
            Assert.That(before,Is.EqualTo(short.MinValue)); Assert.That(after,Is.EqualTo(short.MaxValue)); Assert.That(limit,Is.EqualTo(-1));
            Assert.That(OriginalCharacterInfoValues.TryReadRemainingStatPoints(payload,out short points),Is.True); Assert.That(points,Is.EqualTo(-1));
            CollectionAssert.AreEqual(initial,payload);
            Assert.That(OriginalCharacterInfoValues.TryReadEquipmentStatTriplet(payload,6,out before,out after,out limit),Is.False); Assert.That(before+after+limit,Is.Zero);
            Assert.That(OriginalCharacterInfoValues.TryReadEquipmentStatTriplet(new byte[728],index,out _,out _,out _),Is.False);
            Assert.That(OriginalCharacterInfoValues.TryReadRemainingStatPoints(new byte[723],out points),Is.False); Assert.That(points,Is.Zero);
        }
        [TestCase(CharacterMagicListCommand.List0,0x83)] [TestCase(CharacterMagicListCommand.List1,0x85)]
        public void CharacterMagicListRequestsKeepRawWordsAndCallerBuffer(CharacterMagicListCommand command,int opcode)
        {
            foreach(ushort id in new ushort[]{0,0x1234,ushort.MaxValue}) {
                var request=new CharacterMagicListRequest(command,id,0x8123); var target=new byte[6]; Array.Fill(target,(byte)0xcc);
                Assert.That(request.Direction,Is.EqualTo(LocalPacketDirection.ClientToServer));
                Assert.That(request.TryWriteOriginalPayload(target,out int written),Is.True); Assert.That(written,Is.EqualTo(5));
                CollectionAssert.AreEqual(new byte[]{(byte)opcode,unchecked((byte)id),(byte)(id>>8),0x23,0x81,0xcc},target);
                for(int size=0;size<5;size++) { var small=new byte[size]; Array.Fill(small,(byte)0xaa); Assert.That(request.TryWriteOriginalPayload(small,out written),Is.False); Assert.That(written,Is.Zero); foreach(byte b in small) Assert.That(b,Is.EqualTo(0xaa)); }
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CharacterMagicListRequest((CharacterMagicListCommand)0x84,0,0));
        }
        [Test]
        public void CharacterMagicSelectionPreservesUninterpretedFirstWordAndOrdering()
        {
            var request=new CharacterMagicSelectionRequest(0xbeef,0x1234,ushort.MaxValue); var target=new byte[9]; Array.Fill(target,(byte)0xcc);
            Assert.That(request.TryWriteOriginalPayload(target,out int written),Is.True); Assert.That(written,Is.EqualTo(7));
            CollectionAssert.AreEqual(new byte[]{0x84,0xef,0xbe,0x34,0x12,0xff,0xff,0xcc,0xcc},target);
            for(int size=0;size<7;size++) { var small=new byte[size]; Array.Fill(small,(byte)0xaa); Assert.That(request.TryWriteOriginalPayload(small,out written),Is.False); Assert.That(written,Is.Zero); foreach(byte b in small) Assert.That(b,Is.EqualTo(0xaa)); }
        }
        [TestCase(0)] [TestCase(3)] [TestCase(7)]
        public void SecondMagicRemovalCompactsOnlyActiveEntriesAndClampsSelection(int selected)
        {
            var source=new byte[724]; Array.Fill(source,(byte)0xcc); source[0x6a]=8; source[0x6b]=0;
            for(int i=0;i<8;i++) { source[0x6c+i*2]=(byte)(100+i); source[0x6d+i*2]=0; }
            var before=(byte[])source.Clone();
            Assert.That(OriginalCharacterInfoValues.TryPreviewRemoveSecondMagic(source,selected,out var preview,out int next),Is.True);
            Assert.That(next,Is.EqualTo(Math.Min(selected,6))); Assert.That(preview,Is.Not.SameAs(source));
            Assert.That(OriginalCharacterInfoValues.TryReadMagicCount(preview,1,out int count),Is.True); Assert.That(count,Is.EqualTo(7));
            for(int i=0;i<7;i++) { Assert.That(OriginalCharacterInfoValues.TryReadMagicId(preview,1,i,out short id),Is.True); Assert.That(id,Is.EqualTo(100+(i<selected?i:i+1))); }
            for(int i=0;i<724;i++) if(i!=0x6a && !(i>=0x6c+selected*2 && i<0x7a)) Assert.That(preview[i],Is.EqualTo(before[i]));
            Assert.That(preview[0x7a],Is.EqualTo(before[0x7a])); CollectionAssert.AreEqual(before,source);
        }
        [Test]
        public void SecondMagicRemovalClearsLastSelectionAndRejectsInvalidInput()
        {
            var source=new byte[724]; source[0x6a]=1; source[0x6c]=0xff; source[0x6d]=0xff;
            Assert.That(OriginalCharacterInfoValues.TryPreviewRemoveSecondMagic(source,0,out var preview,out int selected),Is.True);
            Assert.That(selected,Is.EqualTo(-1)); Assert.That(preview[0x6a],Is.Zero); Assert.That(preview[0x6c],Is.EqualTo(0xff));
            foreach(int index in new[]{-1,1,int.MaxValue}) { Assert.That(OriginalCharacterInfoValues.TryPreviewRemoveSecondMagic(source,index,out preview,out selected),Is.False); Assert.That(preview,Is.Null); Assert.That(selected,Is.EqualTo(-1)); }
            foreach(byte count in new byte[]{0,9,255}) { source[0x6a]=count; Assert.That(OriginalCharacterInfoValues.TryPreviewRemoveSecondMagic(source,0,out _,out _),Is.False); }
            Assert.That(OriginalCharacterInfoValues.TryPreviewRemoveSecondMagic(new byte[728],0,out _,out _),Is.False);
        }
        [TestCase(0,0x16)] [TestCase(1,0x1c)] [TestCase(2,0x22)]
        [TestCase(3,0x28)] [TestCase(4,0x2e)] [TestCase(5,0x34)]
        public void OriginalStatPreviewChargesRefundsAndRespectsOpeningBaseline(int index,int offset)
        {
            var current=new byte[724]; var baseline=new byte[724]; current[offset]=11; baseline[offset]=10; current[0x38]=6;
            var raw=new byte[13]; raw[0]=0x8b; raw[1+index*2]=3;
            Assert.That(CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(raw,out var costs,out _),Is.True);
            var before=(byte[])current.Clone(); var initial=(byte[])baseline.Clone();
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,costs,index,CharacterStatAdjustmentDirection.Increase,out short value,out short points),Is.True);
            Assert.That(value,Is.EqualTo(12)); Assert.That(points,Is.EqualTo(3));
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,costs,index,CharacterStatAdjustmentDirection.Decrease,out value,out points),Is.True);
            Assert.That(value,Is.EqualTo(10)); Assert.That(points,Is.EqualTo(9));
            CollectionAssert.AreEqual(before,current); CollectionAssert.AreEqual(initial,baseline);
            current[offset]=10;
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,costs,index,CharacterStatAdjustmentDirection.Decrease,out value,out points),Is.False);
            Assert.That(value,Is.Zero); Assert.That(points,Is.Zero);
            current[0x38]=2;
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,costs,index,CharacterStatAdjustmentDirection.Increase,out _,out _),Is.False);
        }
        [Test]
        public void StatPreviewRejectsUnknownCostsBadInputsAndSignedOverflow()
        {
            var current=new byte[724]; var baseline=new byte[724]; var raw=new byte[13]; raw[0]=0x8b;
            Assert.That(CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(raw,out var free,out _),Is.True);
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,free,0,CharacterStatAdjustmentDirection.Increase,out short value,out short points),Is.True);
            Assert.That(value,Is.EqualTo(1)); Assert.That(points,Is.Zero);
            current[0x16]=0xff; current[0x17]=0x7f;
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,free,0,CharacterStatAdjustmentDirection.Increase,out _,out _),Is.False);
            raw[1]=1; Assert.That(CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(raw,out var cost,out _),Is.True);
            current[0x16]=1; current[0x17]=0; current[0x38]=0xff; current[0x39]=0x7f;
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,cost,0,CharacterStatAdjustmentDirection.Decrease,out _,out _),Is.False);
            raw[1]=raw[2]=0xff; Assert.That(CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(raw,out var negative,out _),Is.True);
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,negative,0,CharacterStatAdjustmentDirection.Increase,out _,out _),Is.False);
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,null,0,CharacterStatAdjustmentDirection.Increase,out _,out _),Is.False);
            foreach(int index in new[]{-1,6,int.MaxValue}) Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,free,index,CharacterStatAdjustmentDirection.Increase,out _,out _),Is.False);
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,free,0,(CharacterStatAdjustmentDirection)0,out _,out _),Is.False);
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(new byte[723],baseline,free,0,CharacterStatAdjustmentDirection.Increase,out _,out _),Is.False);
            Assert.That(OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,new byte[728],free,0,CharacterStatAdjustmentDirection.Increase,out _,out _),Is.False);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void StatAdjustmentRequestKeepsItsOwnOpcode82Layout(int stat)
        {
            foreach(var direction in new[]{CharacterStatAdjustmentDirection.Increase,CharacterStatAdjustmentDirection.Decrease}) {
                var request=new CharacterStatAdjustmentRequest(ushort.MaxValue,stat,direction); var target=new byte[8]; Array.Fill(target,(byte)0xcc);
                Assert.That(request.TryWriteOriginalPayload(target,out int count),Is.True); Assert.That(count,Is.EqualTo(7));
                CollectionAssert.AreEqual(new byte[]{0x82,0xff,0xff,(byte)stat,0,(byte)direction,0,0xcc},target);
                for(int size=0;size<7;size++) { var small=new byte[size]; Array.Fill(small,(byte)0xaa); Assert.That(request.TryWriteOriginalPayload(small,out count),Is.False); Assert.That(count,Is.Zero); foreach(byte b in small) Assert.That(b,Is.EqualTo(0xaa)); }
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CharacterStatAdjustmentRequest(0,-1,CharacterStatAdjustmentDirection.Increase));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CharacterStatAdjustmentRequest(0,6,CharacterStatAdjustmentDirection.Increase));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CharacterStatAdjustmentRequest(0,0,(CharacterStatAdjustmentDirection)0));
        }
        [TestCase(CharacterAdjustmentCommand.Confirm,0x88)]
        [TestCase(CharacterAdjustmentCommand.Cancel,0x89)]
        [TestCase(CharacterAdjustmentCommand.QueryValues,0x8a)]
        public void AdjustmentCommandsWriteOnlyKnownOpcodeAndRecordWord(CharacterAdjustmentCommand command,int opcode)
        {
            foreach(ushort id in new ushort[]{0,0x1234,ushort.MaxValue}) {
                var request=new CharacterAdjustmentCommandRequest(command,id);
                Assert.That(request.Direction,Is.EqualTo(LocalPacketDirection.ClientToServer));
                var destination=new byte[]{0xcc,0xcc,0xcc,0xcc};
                Assert.That(request.TryWriteOriginalPayload(destination,out int written),Is.True); Assert.That(written,Is.EqualTo(3));
                CollectionAssert.AreEqual(new byte[]{(byte)opcode,unchecked((byte)id),(byte)(id>>8),0xcc},destination);
                for(int size=0;size<3;size++) {
                    var small=new byte[size]; Array.Fill(small,(byte)0xaa);
                    Assert.That(request.TryWriteOriginalPayload(small,out written),Is.False); Assert.That(written,Is.Zero);
                    foreach(byte value in small) Assert.That(value,Is.EqualTo(0xaa));
                }
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CharacterAdjustmentCommandRequest((CharacterAdjustmentCommand)0,1));
        }
        [TestCase(0,0)] [TestCase(0x1234,0x5678)] [TestCase(0xffff,0xffff)]
        public void AdjustmentClassRequestPreservesRawWordsWithoutApplyingClass(int unit,int selected)
        {
            var request=new CharacterAdjustmentClassRequest((ushort)unit,(ushort)selected);
            Assert.That(request.Direction,Is.EqualTo(LocalPacketDirection.ClientToServer));
            var destination=new byte[7]; Array.Fill(destination,(byte)0xcc);
            Assert.That(request.TryWriteOriginalPayload(destination,out int count),Is.True); Assert.That(count,Is.EqualTo(5));
            CollectionAssert.AreEqual(new byte[]{0x81,unchecked((byte)unit),(byte)(unit>>8),unchecked((byte)selected),(byte)(selected>>8),0xcc,0xcc},destination);
            for(int size=0;size<5;size++) {
                var small=new byte[size]; Array.Fill(small,(byte)0xaa);
                Assert.That(request.TryWriteOriginalPayload(small,out count),Is.False); Assert.That(count,Is.Zero);
                foreach(byte value in small) Assert.That(value,Is.EqualTo(0xaa));
            }
        }
        [TestCase(0,16,0x40,0x42)] [TestCase(1,8,0x6a,0x6c)]
        public void OriginalInfoMagicListsPreserveOrderSignedIdsAndSource(int list,int capacity,int countOffset,int idsOffset)
        {
            var source=new byte[724]; source[countOffset]=(byte)capacity;
            for(int i=0;i<capacity;i++) { short id=(short)(i==0?-1:i==1?short.MinValue:short.MaxValue-i); source[idsOffset+i*2]=unchecked((byte)id); source[idsOffset+i*2+1]=unchecked((byte)(id>>8)); }
            var before=(byte[])source.Clone();
            Assert.That(OriginalCharacterInfoValues.TryReadMagicCount(source,list,out int count),Is.True); Assert.That(count,Is.EqualTo(capacity));
            for(int i=0;i<capacity;i++) {
                Assert.That(OriginalCharacterInfoValues.TryReadMagicId(source,list,i,out short id),Is.True);
                Assert.That(id,Is.EqualTo(i==0?-1:i==1?short.MinValue:short.MaxValue-i));
                Assert.That(OriginalCharacterInfoValues.TryGetMagicCellOrigin(list,i,out int x,out int y),Is.True);
                Assert.That(x,Is.EqualTo(208+(i%8)*33)); Assert.That(y,Is.EqualTo((list==0?47:121)+(i/8)*33));
            }
            CollectionAssert.AreEqual(before,source);
            source[countOffset]=0;
            Assert.That(OriginalCharacterInfoValues.TryReadMagicCount(source,list,out count),Is.True); Assert.That(count,Is.Zero);
            Assert.That(OriginalCharacterInfoValues.TryReadMagicId(source,list,0,out _),Is.False);
        }
        [Test]
        public void OriginalInfoMagicListsRejectInvalidCountsSizesAndSelection()
        {
            var source=new byte[724];
            foreach(int list in new[]{0,1}) {
                int offset=list==0?0x40:0x6a;
                foreach(short count in new short[]{-1,short.MinValue,short.MaxValue,(short)(OriginalCharacterInfoValues.MagicListCapacity(list)+1)}) {
                    source[offset]=unchecked((byte)count); source[offset+1]=unchecked((byte)(count>>8));
                    Assert.That(OriginalCharacterInfoValues.TryReadMagicCount(source,list,out int actual),Is.False); Assert.That(actual,Is.Zero);
                    Assert.That(OriginalCharacterInfoValues.TryReadMagicId(source,list,0,out _),Is.False);
                }
                source[offset]=1; source[offset+1]=0;
                foreach(int index in new[]{-1,1,int.MaxValue}) Assert.That(OriginalCharacterInfoValues.TryReadMagicId(source,list,index,out _),Is.False);
            }
            foreach(int list in new[]{-1,2,int.MaxValue}) {
                Assert.That(OriginalCharacterInfoValues.MagicListCapacity(list),Is.Zero);
                Assert.That(OriginalCharacterInfoValues.TryReadMagicCount(source,list,out _),Is.False);
                Assert.That(OriginalCharacterInfoValues.TryGetMagicCellOrigin(list,0,out int x,out int y),Is.False); Assert.That(x+y,Is.Zero);
            }
            foreach(int size in new[]{0,723,725,732,736}) Assert.That(OriginalCharacterInfoValues.TryReadMagicCount(new byte[size],0,out _),Is.False);
            foreach(int list in new[]{0,1}) foreach(int index in new[]{-1,OriginalCharacterInfoValues.MagicListCapacity(list),int.MaxValue})
                Assert.That(OriginalCharacterInfoValues.TryGetMagicCellOrigin(list,index,out _,out _),Is.False);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(10)]
        public void AdjustmentOptionsPreserveRawHeaderSignedValuesAndUnknownSuffix(int count)
        {
            var source=new byte[9+count*2+2];
            source[0]=0x87; source[1]=0xef; source[2]=0xbe; source[3]=0xff; source[4]=0xff;
            source[5]=0; source[6]=0x80; source[7]=(byte)count;
            for(int i=0;i<count;i++) { short value=(short)(i==0?short.MaxValue:-i); source[9+i*2]=unchecked((byte)value); source[10+i*2]=unchecked((byte)(value>>8)); }
            source[source.Length-2]=0xaa; source[source.Length-1]=0xbb; var before=(byte[])source.Clone();
            Assert.That(CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(source,out var packet,out int consumed),Is.True);
            Assert.That(consumed,Is.EqualTo(9+count*2)); Assert.That(packet.UninterpretedHeaderWord,Is.EqualTo(0xbeef));
            Assert.That(packet.ClassLookupValue0,Is.EqualTo(-1)); Assert.That(packet.ClassLookupValue1,Is.EqualTo(short.MinValue));
            Assert.That(packet.Direction,Is.EqualTo(LocalPacketDirection.ServerToClient)); Assert.That(packet.OptionCount,Is.EqualTo(count));
            for(int i=0;i<count;i++) { Assert.That(packet.TryGetOption(i,out short value),Is.True); Assert.That(value,Is.EqualTo(i==0?short.MaxValue:-i)); }
            foreach(int index in new[]{-1,count,int.MaxValue}) { Assert.That(packet.TryGetOption(index,out short value),Is.False); Assert.That(value,Is.Zero); }
            CollectionAssert.AreEqual(before,source); Array.Clear(source,0,source.Length);
            Assert.That(packet.UninterpretedHeaderWord,Is.EqualTo(0xbeef));
            if(count>0) { Assert.That(packet.TryGetOption(0,out short value),Is.True); Assert.That(value,Is.EqualTo(short.MaxValue)); }
        }
        [Test]
        public void AdjustmentOptionsRejectAllTruncationsUnsupportedCountsAndOtherOpcodes()
        {
            var valid=new byte[29]; valid[0]=0x87; valid[7]=10;
            for(int length=0;length<valid.Length;length++) {
                Assert.That(CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(valid.AsSpan(0,length),out var packet,out int consumed),Is.False);
                Assert.That(packet,Is.Null); Assert.That(consumed,Is.Zero);
            }
            foreach(short count in new short[]{-1,11,short.MinValue,short.MaxValue}) {
                valid[7]=unchecked((byte)count); valid[8]=unchecked((byte)(count>>8));
                Assert.That(CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(valid,out var packet,out int consumed),Is.False);
                Assert.That(packet,Is.Null); Assert.That(consumed,Is.Zero);
            }
            valid[7]=0; valid[8]=0; valid[0]=0x8b;
            Assert.That(CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(valid,out _,out _),Is.False);
        }
        [TestCase(0)] [TestCase(0x1234)] [TestCase(0xffff)]
        public void AdjustmentQueryPreservesRawIdAndCallerBuffer(int id)
        {
            var packet=new CharacterAdjustmentQueryRequest((ushort)id);
            Assert.That(packet.Direction,Is.EqualTo(LocalPacketDirection.ClientToServer));
            var buffer=new byte[]{0xcc,0xcc,0xcc,0xcc};
            Assert.That(packet.TryWriteOriginalPayload(buffer,out int count),Is.True); Assert.That(count,Is.EqualTo(3));
            CollectionAssert.AreEqual(new byte[]{0x86,unchecked((byte)id),(byte)(id>>8),0xcc},buffer);
            var shortBuffer=new byte[]{0xaa,0xbb};
            Assert.That(packet.TryWriteOriginalPayload(shortBuffer,out count),Is.False); Assert.That(count,Is.Zero);
            CollectionAssert.AreEqual(new byte[]{0xaa,0xbb},shortBuffer);
        }
        [Test]
        public void AdjustmentValuesKeepSignedWordsAndDoNotConsumeUnknownSuffix()
        {
            var source=new byte[]{0x8b,0,0x80,0xff,0xff,0,0,1,0,0xff,0x7f,0x34,0x12,0xaa,0xbb};
            var before=(byte[])source.Clone();
            Assert.That(CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(source,out var packet,out int consumed),Is.True);
            Assert.That(consumed,Is.EqualTo(13)); Assert.That(packet.Direction,Is.EqualTo(LocalPacketDirection.ServerToClient));
            short[] expected={short.MinValue,-1,0,1,short.MaxValue,0x1234};
            for(int i=0;i<6;i++) { Assert.That(packet.TryGetValue(i,out short value),Is.True); Assert.That(value,Is.EqualTo(expected[i])); }
            foreach(int index in new[]{-1,6,int.MaxValue}) { Assert.That(packet.TryGetValue(index,out short value),Is.False); Assert.That(value,Is.Zero); }
            CollectionAssert.AreEqual(before,source); Array.Clear(source,0,source.Length);
            Assert.That(packet.TryGetValue(5,out short retained),Is.True); Assert.That(retained,Is.EqualTo(0x1234));
        }
        [Test]
        public void AdjustmentValuesRejectWrongOpcodeAndEveryTruncatedPrefix()
        {
            for(int length=0;length<13;length++) {
                var source=new byte[length]; if(length>0) source[0]=0x8b;
                Assert.That(CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(source,out var packet,out int consumed),Is.False);
                Assert.That(packet,Is.Null); Assert.That(consumed,Is.Zero);
            }
            var wrong=new byte[13]; wrong[0]=0x87;
            Assert.That(CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(wrong,out var result,out int count),Is.False);
            Assert.That(result,Is.Null); Assert.That(count,Is.Zero);
        }
        [TestCase(0,0,0x16)] [TestCase(0,1,0x1c)] [TestCase(0,2,0x22)] [TestCase(0,3,0x28)]
        [TestCase(0,4,0x2b8)] [TestCase(0,5,0x2ba)] [TestCase(0,6,0x2e)] [TestCase(0,7,0x34)]
        [TestCase(1,0,0x2bc)] [TestCase(1,1,0x2be)] [TestCase(1,2,0x2c0)] [TestCase(1,3,0x2c2)]
        [TestCase(1,4,0x2c4)] [TestCase(1,5,0x2c6)] [TestCase(1,6,0x2c8)] [TestCase(1,7,0x2ca)]
        public void OriginalInfoRowsReadSignedWordsWithoutMutatingPayload(int column,int row,int expected)
        {
            Assert.That(OriginalCharacterInfoValues.TryGetPayloadOffset(column,row,out int offset),Is.True);
            Assert.That(offset,Is.EqualTo(expected));
            foreach(short value in new short[]{short.MinValue,-1,0,1,short.MaxValue}) {
                var payload=new byte[OriginalCharacterInfoValues.PayloadSize]; Array.Fill(payload,(byte)0xcc);
                payload[offset]=unchecked((byte)value); payload[offset+1]=unchecked((byte)(value>>8));
                var before=(byte[])payload.Clone();
                Assert.That(OriginalCharacterInfoValues.TryRead(payload,column,row,out short actual),Is.True);
                Assert.That(actual,Is.EqualTo(value)); CollectionAssert.AreEqual(before,payload);
            }
        }
        [Test]
        public void OriginalInfoRejectsDifferentRecordSizesAndInvalidRows()
        {
            foreach(int length in new[]{0,723,725,728,732,736}) {
                Assert.That(OriginalCharacterInfoValues.TryRead(new byte[length],0,0,out short value),Is.False);
                Assert.That(value,Is.Zero);
            }
            var payload=new byte[724];
            foreach(var cell in new[]{(-1,0),(2,0),(0,-1),(0,8),(int.MaxValue,int.MinValue)}) {
                Assert.That(OriginalCharacterInfoValues.TryGetPayloadOffset(cell.Item1,cell.Item2,out int offset),Is.False);
                Assert.That(offset,Is.Zero);
                Assert.That(OriginalCharacterInfoValues.TryRead(payload,cell.Item1,cell.Item2,out short value),Is.False);
                Assert.That(value,Is.Zero);
            }
        }
        [Test]
        public void EquipmentPayloadWritesOnlyConfirmedLittleEndianWords()
        {
            var request = new EquipmentRequest(0x1234,6,ushort.MaxValue,0x8123,0x4567);
            var buffer = new byte[13]; Array.Fill(buffer,(byte)0xcc);
            Assert.That(request.TryWriteOriginalPayload(buffer,out int count), Is.True);
            Assert.That(count, Is.EqualTo(11));
            CollectionAssert.AreEqual(new byte[] {0x40,0x34,0x12,6,0,0xff,0xff,0x23,0x81,0x67,0x45,0xcc,0xcc},buffer);
        }
        [Test]
        public void EquipmentPayloadFailuresLeaveDestinationUntouched()
        {
            var request = new EquipmentRequest(1,0,2,3,ushort.MaxValue);
            var shortBuffer = new byte[10]; Array.Fill(shortBuffer,(byte)0xcc);
            Assert.That(request.TryWriteOriginalPayload(shortBuffer,out int count), Is.False); Assert.That(count, Is.Zero);
            foreach(byte value in shortBuffer) Assert.That(value, Is.EqualTo(0xcc));
            var unknown = new EquipmentRequest(); var buffer = new byte[11]; Array.Fill(buffer,(byte)0xaa);
            Assert.That(unknown.TryWriteOriginalPayload(buffer,out count), Is.False); Assert.That(count, Is.Zero);
            foreach(byte value in buffer) Assert.That(value, Is.EqualTo(0xaa));
        }
        [Test]
        public void EquipmentRequestPreservesRawWordsAndUnknownPlaceholderValues()
        {
            var empty = new EquipmentRequest();
            Assert.That(empty.HasOriginalFieldValues, Is.False); Assert.That(empty.SlotIndex, Is.Null);
            var request = new EquipmentRequest(ushort.MaxValue,6,ushort.MaxValue,0x8123,7);
            Assert.That(request.Direction, Is.EqualTo(LocalPacketDirection.ClientToServer));
            Assert.That(request.OriginalOpcode, Is.EqualTo(0x40)); Assert.That(request.HasOriginalFieldValues, Is.True);
            Assert.That(request.UnitRecordId, Is.EqualTo(ushort.MaxValue)); Assert.That(request.ItemId, Is.EqualTo(ushort.MaxValue));
            Assert.That(request.SlotIndex, Is.EqualTo(6)); Assert.That(request.AdditionalValue, Is.EqualTo(0x8123));
            Assert.That(request.PreviousItemId, Is.EqualTo(7));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new EquipmentRequest(1,7,1,0,ushort.MaxValue));
        }
        [Test]
        public void KnownFieldsRetainSignedAndUnsignedValuesAndSeparateResults()
        {
            var attack = new AttackRequest(700, 100, -1, short.MaxValue);
            Assert.That(attack.TargetUnitId, Is.EqualTo(700)); Assert.That(attack.AttackerUnitId, Is.EqualTo(100));
            Assert.That(attack.TargetTileAttribute, Is.EqualTo(-1)); Assert.That(attack.AttackerTileAttribute, Is.EqualTo(short.MaxValue));
            Assert.That(attack.Direction, Is.EqualTo(LocalPacketDirection.ClientToServer));
            Assert.That(new AttackEffectNotification(700, 100, -3).Effect, Is.EqualTo(-3));
            Assert.That(new HpSetNotification(700, -1).AbsoluteHp, Is.EqualTo(-1));
            Assert.That(new MpSetNotification(100, 30).AbsoluteMp, Is.EqualTo(30));
            Assert.That(new ExperienceSetNotification(100, uint.MaxValue).AbsoluteExperience, Is.EqualTo(uint.MaxValue));
            Assert.That(new UnitRemoveNotification(700).UnitId, Is.EqualTo(700));
            Assert.That(new UnitCorpseNotification(700).UnitId, Is.EqualTo(700));
            Assert.That(new TickMenuNotification().Direction, Is.EqualTo(LocalPacketDirection.ServerToClient));
        }

        [Test]
        public void MagicTargetsAreOwnedReadonlyAndCountsCannotOverflow()
        {
            var targets = new short[] { 700, -1 };
            var request = new MagicRequest(101, 100, -2, 5, int.MinValue, targets);
            var response = new MagicEffectNotification(101, 100, -2, 5, int.MaxValue, targets);
            targets[0] = 999;
            Assert.That(request.TargetUnitIds[0], Is.EqualTo(700)); Assert.That(response.TargetUnitIds[0], Is.EqualTo(700));
            Assert.That(request.TargetCount, Is.EqualTo(2)); Assert.That(response.TargetCount, Is.EqualTo(2));
            Assert.That(request.OriginalOpcode, Is.Null); Assert.That(request.Mode, Is.Null);
            Assert.That(request.ComputedCost, Is.EqualTo(int.MinValue)); Assert.That(response.Value, Is.EqualTo(int.MaxValue));
            Assert.Throws<NotSupportedException>(() => ((IList<short>)request.TargetUnitIds)[0] = 12);
            Assert.That(new MagicRequest(101, 100, 0, 0, 0, Array.Empty<short>(), 0x89).TargetCount, Is.EqualTo(0));
            Assert.That(new MagicRequest(101, 100, 0, 0, 0, new short[255], 0x92).TargetCount, Is.EqualTo(255));
            Assert.Throws<ArgumentNullException>(() => new MagicRequest(101, 100, 0, 0, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicEffectNotification(101, 100, 0, 0, 0, new short[256]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicRequest(101, 100, 0, 0, 0, targets, 0x82));
        }

        [Test]
        public void LocalServerRoutesRequestAndIndependentResponsesInOrder()
        {
            WithManager(manager =>
            {
                var received = new List<LocalPacket>();
                var request = new AttackRequest(700, 100, 0, 0);
                Assert.That(manager.SendToServer(request), Is.False);
                Assert.That(manager.SendToClient(new TickMenuNotification()), Is.False);
                manager.ResponseReceived += packet => received.Add(packet);
                manager.RequestReceived += packet =>
                {
                    received.Add(packet);
                    Assert.That(manager.SendToClient(new AttackEffectNotification(700, 100, 0)), Is.True);
                    Assert.That(manager.SendToClient(new HpSetNotification(700, 250)), Is.True);
                    Assert.That(manager.SendToClient(new ExperienceSetNotification(100, uint.MaxValue)), Is.True);
                };
                Assert.That(manager.SendToServer(request), Is.True);
                Assert.That(received.Count, Is.EqualTo(4)); Assert.That(received[0], Is.SameAs(request));
                Assert.That(received[1], Is.InstanceOf<AttackEffectNotification>());
                Assert.That(received[2], Is.InstanceOf<HpSetNotification>());
                Assert.That(received[3], Is.InstanceOf<ExperienceSetNotification>());
                Assert.That(manager.SendToServer(null), Is.False); Assert.That(manager.SendToClient(null), Is.False);
                Assert.That(received.Count, Is.EqualTo(4));
            });
        }

        [Test]
        public void EmptyPlaceholdersRouteButDisabledManagerDoesNotDeliver()
        {
            WithManager(manager =>
            {
                var requests = 0; var responses = 0;
                manager.RequestReceived += _ => requests++; manager.ResponseReceived += _ => responses++;
                Assert.That(manager.SendToServer(new LoginRequest()), Is.True);
                Assert.That(manager.SendToServer(new UnknownClientPacket()), Is.True);
                Assert.That(manager.SendToClient(new LoginResponse()), Is.True);
                Assert.That(manager.SendToClient(new UnknownServerPacket()), Is.True);
                manager.enabled = false;
                Assert.That(manager.SendToServer(new WorldMoveRequest()), Is.False);
                Assert.That(manager.SendToClient(new WorldMoveNotification()), Is.False);
                Assert.That(requests, Is.EqualTo(2)); Assert.That(responses, Is.EqualTo(2));
            });
        }

        [Test]
        public void HandlerFailureIsNotSilentlyAcknowledgedOrRetried()
        {
            WithManager(manager =>
            {
                var attempts = 0;
                manager.RequestReceived += _ => { attempts++; throw new InvalidOperationException("fixture failure"); };
                Assert.Throws<InvalidOperationException>(() => manager.SendToServer(new UnknownClientPacket()));
                Assert.That(attempts, Is.EqualTo(1));
            });
        }

        [Test]
        public void SplitEndpointsAndLegacyFacadeShareOneOrderedRoute()
        {
            WithManager(manager => {
                Assert.That(manager.Sender, Is.SameAs(manager.Sender));
                Assert.That(manager.Receiver, Is.SameAs(manager.Receiver));
                var order = new List<string>();
                manager.ResponseReceived += _ => order.Add("client-facade");
                manager.Sender.ResponseSent += _ => order.Add("client-endpoint");
                Action<ClientPacket> handler = _ => {
                    order.Add("server");
                    Assert.That(manager.Sender.Send(new LoginResponse()), Is.True);
                };
                manager.RequestReceived += handler;
                Assert.That(manager.Receiver.Receive(new LoginRequest(), () => order.Add("dispatch")), Is.True);
                Assert.That(order, Is.EqualTo(new[] {"dispatch", "server", "client-facade", "client-endpoint"}));
                manager.Receiver.RequestReceived -= handler;
                Assert.That(manager.SendToServer(new LoginRequest()), Is.False);
                Assert.That(order.Count, Is.EqualTo(4));
            });
        }

        [Test]
        public void DisabledSplitEndpointsRejectWithoutDispatchOrDelivery()
        {
            WithManager(manager => {
                int dispatch = 0, requests = 0, responses = 0;
                manager.Receiver.RequestReceived += _ => requests++;
                manager.Sender.ResponseSent += _ => responses++;
                Assert.That(manager.Receiver.Receive(null, () => dispatch++), Is.False);
                Assert.That(manager.Sender.Send(null), Is.False);
                manager.enabled = false;
                Assert.That(manager.Receiver.Receive(new LoginRequest(), () => dispatch++), Is.False);
                Assert.That(manager.Sender.Send(new LoginResponse()), Is.False);
                Assert.That((dispatch, requests, responses), Is.EqualTo((0, 0, 0)));
                manager.enabled = true;
                Assert.That(manager.SendToServer(new LoginRequest(), () => dispatch++), Is.True);
                Assert.That((dispatch, requests, responses), Is.EqualTo((1, 1, 0)));
            });
        }

        [Test]
        public void SplitSenderExceptionsPropagateAndNeverRetry()
        {
            WithManager(manager => {
                int attempts = 0;
                manager.Sender.ResponseSent += _ => { attempts++; throw new InvalidOperationException("fixture failure"); };
                Assert.Throws<InvalidOperationException>(() => manager.SendToClient(new UnknownServerPacket()));
                Assert.That(attempts, Is.EqualTo(1));
            });
        }

        [Test]
        public void EndpointOwnershipIsIsolatedAndDestroyedOwnersCannotDeliver()
        {
            ServerManager_Receiver receiver = null; ServerManager_Sender sender = null;
            int requests = 0, responses = 0;
            WithManager(first => {
                receiver = first.Receiver; sender = first.Sender;
                receiver.RequestReceived += _ => requests++;
                sender.ResponseSent += _ => responses++;
                WithManager(second => {
                    Assert.That(second.Receiver.Receive(new UnknownClientPacket()), Is.False);
                    Assert.That(second.Sender.Send(new UnknownServerPacket()), Is.False);
                    Assert.That(receiver.Receive(new UnknownClientPacket()), Is.True);
                    Assert.That(sender.Send(new UnknownServerPacket()), Is.True);
                });
            });
            Assert.That(receiver.Receive(new UnknownClientPacket()), Is.False);
            Assert.That(sender.Send(new UnknownServerPacket()), Is.False);
            Assert.That((requests, responses), Is.EqualTo((1, 1)));
        }

        private static void WithManager(Action<ServerManager> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Local server check"); SceneManager.MoveGameObjectToScene(root, scene);
                check(root.AddComponent<ServerManager>());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
