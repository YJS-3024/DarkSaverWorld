using System;
using DarkSaver.Core;
using DarkSaver.Network;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class MercenaryTradeTests
    {
        private static MercenaryOffer[] Offers()=>new[]{new MercenaryOffer{JobId=200,Maximum=2,Hp=140,Mp=-1,Attack=80,Defense=71,Price=200,Name="솔져D",Type="전사용병고용"},new MercenaryOffer{JobId=201,Maximum=2,Hp=150,Mp=-1,Attack=90,Defense=80,Price=300,Name="솔져C",Type="전사용병고용"}};
        [Test] public void OriginalHireDismissPayloadsRoundTripWithoutAnInventedQuantity()
        {
            Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(new Request_HireMercenary(100,200,3),out var h));CollectionAssert.AreEqual(new byte[]{0x4d,100,0,200,0,3,0},h);
            Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,h,out var decoded));Assert.AreEqual(3,((Request_HireMercenary)decoded).Maximum);
            Assert.IsTrue(OriginalLocalPacketCodec.TryEncodePayload(new Request_DismissMercenary(100,200),out var d));CollectionAssert.AreEqual(new byte[]{0x4e,100,0,200,0},d);
            Assert.IsTrue(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,d,out decoded));Assert.AreEqual(200,((Request_DismissMercenary)decoded).JobId);
            Assert.IsFalse(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,new byte[]{0x4d,100,0,200,0},out _));
        }
        [Test] public void HiringUsesExactPriceAndIndependentOwnedUnitWithStableIdentity()
        {
            var original=new NativeInventory{gold=500};
            Assert.IsTrue(MercenaryTradeRules.TryHire(100,200,2,Offers(),null,original,out var roster,out var money,out _));
            Assert.AreEqual(500,original.gold);Assert.AreEqual(300,money.gold);Assert.AreEqual(1,roster.units.Length);Assert.IsTrue(roster.IsValid);
            var unit=roster.units[0];Assert.AreEqual(200,unit.jobId);Assert.AreEqual(140,unit.hp);Assert.AreEqual(0,unit.mp);Assert.AreEqual(100,unit.hireOwnerId);Assert.AreEqual(20000,unit.instanceId);
            Assert.IsTrue(MercenaryTradeRules.TryHire(100,200,2,Offers(),roster,money,out var second,out _,out _));Assert.AreEqual(20001,second.units[1].instanceId);Assert.AreNotEqual(unit.persistentId,second.units[1].persistentId);Assert.AreEqual(1,roster.units.Length);
        }
        [Test] public void WrongMaximumFundsOwnerMixedJobAndMagicDataAreRejectedWithoutMutation()
        {
            var inv=new NativeInventory{gold=1000};var offers=Offers();
            Assert.IsFalse(MercenaryTradeRules.TryHire(100,200,99,offers,null,inv,out _,out _,out _));Assert.IsFalse(MercenaryTradeRules.TryHire(200,200,2,offers,null,inv,out _,out _,out _));
            Assert.IsFalse(MercenaryTradeRules.TryHire(100,200,2,offers,null,new NativeInventory{gold=199},out _,out _,out _));
            Assert.IsTrue(MercenaryTradeRules.TryHire(100,200,2,offers,null,inv,out var one,out var money,out _));
            Assert.IsFalse(MercenaryTradeRules.TryHire(100,201,2,offers,one,money,out _,out _,out _));
            Assert.IsTrue(MercenaryTradeRules.TryHire(100,200,2,offers,one,money,out var two,out var rest,out _));
            Assert.IsFalse(MercenaryTradeRules.TryHire(100,200,2,offers,two,rest,out _,out _,out _));
            offers[0].Mp=20;Assert.IsFalse(MercenaryTradeRules.TryHire(100,200,2,offers,null,inv,out _,out _,out _));Assert.AreEqual(1000,inv.gold);
        }
        [Test] public void DismissalRemovesOneAndAllowsTypeChangeOnlyAfterTheLastUnit()
        {
            var offers=Offers();var inv=new NativeInventory{gold=1000};
            Assert.IsTrue(MercenaryTradeRules.TryHire(100,200,2,offers,null,inv,out var one,out var money,out _));
            Assert.IsFalse(MercenaryTradeRules.TryDismiss(100,201,one,money,out _,out _));
            Assert.IsTrue(MercenaryTradeRules.TryDismiss(100,200,one,money,out var empty,out _));Assert.AreEqual(0,empty.units.Length);Assert.AreEqual(1,one.units.Length);Assert.AreEqual(800,money.gold);
            Assert.IsTrue(MercenaryTradeRules.TryHire(100,201,2,offers,empty,money,out var changed,out var rest,out _));Assert.AreEqual(201,changed.units[0].jobId);Assert.AreEqual(500,rest.gold);
        }
    }
}
