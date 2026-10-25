using System.Linq;
using DarkSaver.Items;
using NUnit.Framework;
using UnityEditor;

namespace DarkSaver.Tests
{
    public sealed class KaosiaShopTests
    {
        [Test]
        public void NativeCatalogProducesTierScopeWithConfirmedHalfPriceBuyback()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
            Assert.IsNotNull(catalog);int count=0;
            for(int i=0;i<catalog.Count;i++)
            {
                var item=catalog.GetAt(i);
                if(!KaosiaShopRules.TryCreateOffer(item,out var offer))continue;
                count++;Assert.AreEqual(item.Id,offer.itemId);
                Assert.AreEqual(item.GetNumericValue(16),offer.buyPrice);Assert.AreEqual(offer.buyPrice/2,offer.sellPrice);
                Assert.LessOrEqual(item.GetNumericValue(4),30);Assert.Greater(offer.buyPrice,0);
            }
            Assert.AreEqual(202,count);
        }
        [Test]
        public void TierBoundaryAndPermissionsAreCheckedWithoutIdOrNameGuessing()
        {
            ItemCatalogEntry Item(long level,long price=100,long position=2,long permission=1)
            {
                var values=new long[41];values[0]=999;values[3]=position;values[4]=level;values[6]=permission;values[16]=price;
                return new ItemCatalogEntry(values,price.ToString(),"","fixture","","weapon");
            }
            Assert.IsTrue(KaosiaShopRules.TryCreateOffer(Item(30),out _));
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(Item(31),out _));
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(Item(-1),out _));
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(Item(1,0),out _));
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(Item(1,100,6),out _));
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(Item(1,100,2,0),out _));
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(Item(1,100,2,2),out _));
        }
    }
}
