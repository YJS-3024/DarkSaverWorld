using System;
using DarkSaver.Network;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class WorldFunctionProtocolTests
    {
        [Test] public void ChatAndF12UseOriginalCp949CountAndModeWithoutNullTerminator()
        {
            Assert.IsTrue(OriginalWorldTextCodec.TryEncode(new Request_WorldText("가G"),out var bytes));
            CollectionAssert.AreEqual(new byte[]{0x7a,1,3,0xb0,0xa1,0x47},bytes);
            Assert.IsTrue(OriginalLocalPacketCodec.TryDecodeFrame(OriginalLocalPacketCodec.EncodeFrame(bytes),out var unframed));
            Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,unframed,out var copy));
            Assert.AreEqual("가G",((Request_WorldText)copy).Text);
            Assert.IsTrue(OriginalWorldTextCodec.TryEncode(new Request_WorldText("/공간이동"),out var command));
            Assert.AreEqual(9,command[2]);Assert.AreEqual(12,command.Length);
        }
        [Test] public void OriginalByteBoundariesRejectTruncationUnsupportedCharactersAndTrailingPayload()
        {
            Assert.IsTrue(OriginalWorldTextCodec.TryEncode(new Request_WorldText(new string('a',255)),out var maximum));Assert.AreEqual(258,maximum.Length);
            foreach(string bad in new[]{new string('a',256),new string('가',128),"a\0b","a\nb","😀",""})Assert.IsFalse(OriginalWorldTextCodec.TryEncode(new Request_WorldText(bad),out _),bad);
            Assert.IsFalse(OriginalWorldTextCodec.TryDecode(new byte[]{0x7a,1,2,65},out _));
            Assert.IsFalse(OriginalWorldTextCodec.TryDecode(new byte[]{0x7a,1,1,65,0},out _));
        }
        [Test] public void LocalChatEchoesOnlyConnectedCharacterAndWhispersNeverFabricateARecipient()
        {
            bool ready=true;var service=new WorldTextService(()=>new WorldTextContext("검증",ready));
            var reply=service.Handle(new Request_WorldText("안녕"));Assert.IsTrue(reply.Accepted);Assert.AreEqual(WorldTextChannel.General,reply.Message.Channel);Assert.AreEqual("검증: 안녕",reply.Message.Text);
            Assert.IsFalse(service.Handle(new Request_WorldText("/to 다른사람 안녕")).Accepted);
            foreach(string prefix in new[]{"/to ","/새 "})
            {var whisper=service.Handle(new Request_WorldText(prefix+"검증 안녕"));Assert.IsTrue(whisper.Accepted);Assert.AreEqual(WorldTextChannel.Whisper,whisper.Message.Channel);}
            Assert.IsFalse(service.Handle(new Request_WorldText("/to 검증")).Accepted);
            Assert.IsFalse(service.Handle(new Request_WorldText("hello",2)).Accepted);
            ready=false;Assert.IsFalse(service.Handle(new Request_WorldText("안녕")).Accepted);
        }
        [Test] public void TeleportIsServerCommandAndUnknownCommandsNeverFallThroughToGeneralChat()
        {
            var service=new WorldTextService(()=>new WorldTextContext("검증",true));
            Assert.IsFalse(service.Handle(new Request_WorldText("/공간이동")).Accepted);
            int calls=0;service.ConfigureCommandHandler((request,context)=>{calls++;Assert.AreEqual("검증",context.CharacterName);return new Response_WorldText(false,"거부");});
            var reply=service.Handle(new Request_WorldText("/공간이동"));Assert.AreEqual("거부",reply.Reason);Assert.IsNull(reply.Message);Assert.AreEqual(1,calls);
            service.ConfigureCommandHandler(null);Assert.IsFalse(service.Handle(new Request_WorldText("/미정명령")).Accepted);
        }
        [Test] public void OriginalRoleFlagsReorderAndEquipmentRoundTripTheirOwnWordFields()
        {
            var flag=new Request_WorldRosterFlag(0x1234,2);Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(flag,out var f));CollectionAssert.AreEqual(new byte[]{0x42,0x34,0x12,2,0},f);
            Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,f,out var fc));Assert.AreEqual(flag.RecordId,((Request_WorldRosterFlag)fc).RecordId);
            var order=new Request_WorldRosterOrder(9,0);Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(order,out var o));CollectionAssert.AreEqual(new byte[]{0x3b,9,0,0,0},o);
            Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,o,out _));
            var equipment=new EquipmentRequest(200,3,ushort.MaxValue,0,321);Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(equipment,out var e));
            Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,e,out var ec));Assert.AreEqual(ushort.MaxValue,((EquipmentRequest)ec).ItemId);Assert.AreEqual(321,((EquipmentRequest)ec).PreviousItemId);
            Assert.IsFalse(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,new byte[]{0x42,0,0,3,0},out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new Request_WorldRosterOrder(10,0));
        }
        [Test] public void AbilityRequestsKeepSelectedRoleAndMagicArrowLiteralOneOrTwo()
        {
            ClientPacket[] source={new CharacterAdjustmentQueryRequest(200),new CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand.Confirm,200),new CharacterStatAdjustmentRequest(200,2,CharacterStatAdjustmentDirection.Increase),new CharacterMagicSelectionRequest(2,200,101),new CharacterAdjustmentClassRequest(200,103)};
            foreach(var request in source){Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(request,out var bytes));Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,bytes,out var parsed));Assert.AreEqual(request.GetType(),parsed.GetType());}
        }
        [Test] public void WhisperPreferenceUsesOriginal7dWordAndServerHonorsIt()
        {
            var service=new WorldTextService(()=>new WorldTextContext("검증",true));
            foreach(bool enabled in new[]{false,true})
            {
                var request=new Request_WorldWhisperSetting(enabled);
                Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(request,out var bytes));CollectionAssert.AreEqual(new byte[]{0x7d,(byte)(enabled?1:2),0},bytes);
                Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,bytes,out var decoded));
                Assert.IsTrue(service.SetWhispers((Request_WorldWhisperSetting)decoded).Accepted);
                Assert.AreEqual(enabled,service.Handle(new Request_WorldText("/to 검증 안녕")).Accepted);
            }
        }
        [Test] public void RoleNotificationsCopyProfilesAndPayloadsAndRejectDuplicateSlots()
        {
            var profile=new DarkSaver.Core.NativeCharacterProfile{name="검증",jobId=100,equipmentItemIds=new[]{0,0,321,0,0,0,0}};
            var payload=new byte[OriginalCharacterInfoValues.PayloadSize];
            var source=new WorldRosterEntry{RecordId=100,JobId=100,Name="검증",Profile=profile,OriginalFlag=1,CharacterInfoPayload=payload};
            var packet=new Notify_WorldRoster(new[]{source});profile.equipmentItemIds[2]=999;payload[0]=99;
            var first=packet.Capture();Assert.AreEqual(321,first[0].Profile.EquipmentItemAt(2));Assert.AreEqual(0,first[0].CharacterInfoPayload[0]);
            first[0].Profile.equipmentItemIds[2]=123;Assert.AreEqual(321,packet.Capture()[0].Profile.EquipmentItemAt(2));
            Assert.Throws<ArgumentException>(()=>new Notify_WorldRoster(new[]{source,source}));
        }
    }
}
