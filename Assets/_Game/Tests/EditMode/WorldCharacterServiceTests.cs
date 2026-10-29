using System;
using System.Collections.Generic;
using DarkSaver.Network;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class WorldCharacterServiceTests
    {
        private sealed class Host
        {
            public bool Ready=true,Writable=true;public int Writes;
            public WorldCharacterData[] Saved;
            public readonly List<ServerPacket> Packets=new();
            public readonly WorldCharacterService Service;
            public Host()
            {Service=new WorldCharacterService(()=>Ready,()=>new[]{new WorldRosterEntry{RecordId=100,Name="본인",JobId=100},new WorldRosterEntry{RecordId=200,Name="동료",JobId=103}},()=>Saved,Save,Packets.Add);}
            private bool Save(WorldCharacterData[] data,out string reason)
            {Writes++;reason=Writable?"":"쓰기 실패";if(!Writable)return false;Saved=WorldCharacterData.Copy(data);return true;}
            public WorldCharacterData Data(int id=200)
            {var bytes=new byte[724];bytes[0]=(byte)id;bytes[1]=(byte)(id>>8);return new WorldCharacterData{RecordId=id,Info=bytes,StatCosts=new short[]{1,2,3,4,5,6},ClassOptions=new byte[]{0x87,0,0,0xff,0xff,0xff,0xff,1,0,103,0}};}
        }
        [Test] public void FlagAndOrderPersistBeforeReceiptAndReloadFromAuthority()
        {
            var h=new Host();Assert.IsTrue(h.Service.Flag(new Request_WorldRosterFlag(200,1)).Accepted);Assert.AreEqual(1,h.Saved[1].Flag);
            Assert.AreEqual(1,((Notify_WorldRoster)h.Packets[0]).Capture()[1].OriginalFlag);
            Assert.IsTrue(h.Service.Order(new Request_WorldRosterOrder(0,1)).Accepted);Assert.AreEqual(200,h.Saved[0].RecordId);
            h.Packets.Clear();Assert.IsTrue(h.Service.Query(new Request_WorldCharacters()).Accepted);
            var rows=((Notify_WorldRoster)h.Packets[0]).Capture();Assert.AreEqual(200,rows[0].RecordId);Assert.AreEqual(1,rows[0].OriginalFlag);
            Assert.IsFalse(h.Service.Flag(new Request_WorldRosterFlag(200,2)).Accepted); // Original slot0 has no checkbox.
            Assert.IsTrue(h.Service.Flag(new Request_WorldRosterFlag(100,2)).Accepted); // Slot semantics, not guessed primary-id semantics.
        }
        [Test] public void FailedWriteUnknownUnitInvalidSlotAndUnavailableContextNeverPublishOrMutate()
        {
            var h=new Host();Assert.IsTrue(h.Service.Flag(new Request_WorldRosterFlag(200,1)).Accepted);h.Packets.Clear();h.Writable=false;
            Assert.IsFalse(h.Service.Flag(new Request_WorldRosterFlag(200,2)).Accepted);Assert.AreEqual(1,h.Saved[1].Flag);Assert.IsEmpty(h.Packets);
            Assert.IsFalse(h.Service.Order(new Request_WorldRosterOrder(9,1)).Accepted);
            Assert.IsFalse(h.Service.Flag(new Request_WorldRosterFlag(999,1)).Accepted);
            Assert.IsFalse(h.Service.Flag(new Request_WorldRosterFlag(100,1)).Accepted);
            h.Ready=false;Assert.IsFalse(h.Service.Query(new Request_WorldCharacters()).Accepted);Assert.IsEmpty(h.Packets);
        }
        [Test] public void VerifiedAbilityQueryPublishesRosterThenOriginalCostsAndCandidates()
        {
            var h=new Host();var seed=h.Data();Assert.IsTrue(h.Service.InstallVerifiedData(new[]{seed}).Accepted);seed.Info[0]=0;
            h.Packets.Clear();Assert.IsTrue(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);
            Assert.AreEqual(3,h.Packets.Count);Assert.IsInstanceOf<Notify_WorldRoster>(h.Packets[0]);
            Assert.IsInstanceOf<CharacterAdjustmentValuesNotification>(h.Packets[1]);Assert.IsInstanceOf<CharacterAdjustmentOptionsNotification>(h.Packets[2]);
            Assert.IsTrue(((CharacterAdjustmentValuesNotification)h.Packets[1]).TryGetValue(5,out var cost));Assert.AreEqual(6,cost);
            Assert.IsTrue(((CharacterAdjustmentOptionsNotification)h.Packets[2]).TryGetOption(0,out var job));Assert.AreEqual(103,job);
            Assert.AreEqual(200,Array.Find(h.Saved,d=>d.RecordId==200).Info[0]);
        }
        [Test] public void MissingServerRulesDoNotGrantClassesMagicOrPointsAndCancelIsExplicit()
        {
            var h=new Host();Assert.IsFalse(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);
            Assert.IsTrue(h.Service.InstallVerifiedData(new[]{h.Data()}).Accepted);
            Assert.IsFalse(h.Service.Adjust(new CharacterAdjustmentClassRequest(200,103),200).Accepted);
            Assert.IsTrue(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);int writes=h.Writes;
            Assert.IsFalse(h.Service.Adjust(new CharacterAdjustmentClassRequest(200,103),200).Accepted);
            Assert.IsFalse(h.Service.Adjust(new CharacterMagicSelectionRequest(1,200,101),200).Accepted);
            Assert.IsFalse(h.Service.Adjust(new CharacterStatAdjustmentRequest(200,0,CharacterStatAdjustmentDirection.Increase),200).Accepted);
            Assert.AreEqual(writes,h.Writes);
            Assert.IsTrue(h.Service.Adjust(new CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand.Cancel,200),200).Accepted);
        }
        [Test] public void SuppliedMutationPolicyRequiresOwnedIdentityValidResultAndSuccessfulStorage()
        {
            var h=new Host();Assert.IsTrue(h.Service.InstallVerifiedData(new[]{h.Data()}).Accepted);Assert.IsTrue(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);
            h.Service.AdjustmentPolicy=(p,row,data)=>{data.Info[0x16]=20;return new WorldCharacterMutation(new LocalCommandResponse(true),data);};
            var request=new CharacterStatAdjustmentRequest(200,0,CharacterStatAdjustmentDirection.Increase);
            h.Writable=false;Assert.IsFalse(h.Service.Adjust(request,200).Accepted);Assert.AreEqual(0,Array.Find(h.Saved,d=>d.RecordId==200).Info[0x16]);
            h.Writable=true;Assert.IsTrue(h.Service.Adjust(request,200).Accepted);Assert.AreEqual(20,Array.Find(h.Saved,d=>d.RecordId==200).Info[0x16]);
            h.Service.AdjustmentPolicy=(p,row,data)=>new WorldCharacterMutation(new LocalCommandResponse(true),h.Data(999));
            Assert.IsFalse(h.Service.Adjust(request,200).Accepted);
            h.Service.AdjustmentPolicy=(p,row,data)=>new WorldCharacterMutation(new LocalCommandResponse(true));Assert.IsFalse(h.Service.Adjust(request,200).Accepted);
        }
        [Test] public void VerifiedStatCostsStageRefundCancelAndCommitWithoutInventedPointAwards()
        {
            var h=new Host();var d=h.Data();d.Info[0x38]=3;d.StatCosts[0]=2;
            Assert.IsTrue(h.Service.InstallVerifiedData(new[]{d}).Accepted);Assert.IsTrue(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);
            var increase=new CharacterStatAdjustmentRequest(200,0,CharacterStatAdjustmentDirection.Increase);
            var decrease=new CharacterStatAdjustmentRequest(200,0,CharacterStatAdjustmentDirection.Decrease);
            int writes=h.Writes;Assert.IsFalse(h.Service.Adjust(decrease,200).Accepted);
            Assert.IsTrue(h.Service.Adjust(increase,200).Accepted);Assert.AreEqual(writes,h.Writes);
            var stored=Array.Find(h.Saved,x=>x.RecordId==200);Assert.AreEqual(0,stored.Info[0x16]);Assert.AreEqual(3,stored.Info[0x38]);
            Assert.IsFalse(h.Service.Adjust(increase,200).Accepted); // Remaining1 cannot pay2.
            Assert.IsTrue(h.Service.Adjust(decrease,200).Accepted);Assert.IsTrue(h.Service.Adjust(increase,200).Accepted);
            Assert.IsTrue(h.Service.Adjust(new CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand.Cancel,200),200).Accepted);Assert.AreEqual(writes,h.Writes);
            Assert.IsTrue(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);Assert.IsTrue(h.Service.Adjust(increase,200).Accepted);
            h.Writable=false;Assert.IsFalse(h.Service.Adjust(new CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand.Confirm,200),200).Accepted);
            Assert.AreEqual(0,Array.Find(h.Saved,x=>x.RecordId==200).Info[0x16]);
            h.Writable=true;Assert.IsTrue(h.Service.Adjust(new CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand.Confirm,200),200).Accepted);
            stored=Array.Find(h.Saved,x=>x.RecordId==200);Assert.AreEqual(1,stored.Info[0x16]);Assert.AreEqual(1,stored.Info[0x38]);
        }
        [Test] public void StatConfirmationPreservesConcurrentDeploymentFlagAndRejectsChangedInfo()
        {
            var h=new Host();var d=h.Data();d.Info[0x38]=5;Assert.IsTrue(h.Service.InstallVerifiedData(new[]{d}).Accepted);
            Assert.IsTrue(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);
            Assert.IsTrue(h.Service.Adjust(new CharacterStatAdjustmentRequest(200,0,CharacterStatAdjustmentDirection.Increase),200).Accepted);
            Assert.IsTrue(h.Service.Flag(new Request_WorldRosterFlag(200,1)).Accepted);
            Assert.IsTrue(h.Service.Adjust(new CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand.Confirm,200),200).Accepted);
            Assert.AreEqual(1,Array.Find(h.Saved,x=>x.RecordId==200).Flag);
            Assert.IsTrue(h.Service.QueryAdjustment(new CharacterAdjustmentQueryRequest(200)).Accepted);
            Array.Find(h.Saved,x=>x.RecordId==200).Info[0x16]=7;
            int writes=h.Writes;
            Assert.IsFalse(h.Service.Adjust(new CharacterAdjustmentCommandRequest(CharacterAdjustmentCommand.Confirm,200),200).Accepted);Assert.AreEqual(writes,h.Writes);
        }
        [Test] public void AuthorityDataRejectsMalformedAndDuplicateRecordsAndMagicListsKeepWireShape()
        {
            var h=new Host();var data=h.Data();Assert.IsFalse(h.Service.InstallVerifiedData(new[]{data,data}).Accepted);
            data.StatCosts=new short[]{1};Assert.IsFalse(data.IsValid);Assert.IsFalse(h.Service.InstallVerifiedData(new[]{data}).Accepted);
            Assert.IsTrue(WorldCharacterData.Validate(null));Assert.IsFalse(WorldCharacterData.Validate(new[]{h.Data(999),h.Data(999)}));
            foreach(var command in new[]{CharacterMagicListCommand.List0,CharacterMagicListCommand.List1})
            {var packet=new CharacterMagicListRequest(command,200,101);Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(packet,out var bytes));Assert.AreEqual(5,bytes.Length);Assert.AreEqual((byte)command,bytes[0]);Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,bytes,out var copy));Assert.AreEqual(101,((CharacterMagicListRequest)copy).MagicValue);}
        }
    }
}
