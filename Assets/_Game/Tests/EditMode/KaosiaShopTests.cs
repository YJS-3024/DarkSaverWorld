using DarkSaver.Items;
using NUnit.Framework;
using UnityEditor;
namespace DarkSaver.Tests
{
    public sealed class KaosiaShopTests
    {
        [Test]
        public void ItemCatalogAloneNeverInventsServerStockOrPrices()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
            Assert.IsNotNull(catalog);Assert.Greater(catalog.Count,0);
            for(int i=0;i<catalog.Count;i++)
            {
                var item=catalog.GetAt(i);long price=item.GetNumericValue(16);
                Assert.IsFalse(KaosiaShopRules.TryCreateOffer(item,out var offer),"unreceived stock "+item.Id);
                Assert.IsNull(offer);Assert.AreEqual(price,item.GetNumericValue(16),"catalog remains immutable");
            }
        }
        [TestCase(30,8000,2,1)]
        public void PlausibleTierOrPermissionCannotSubstituteForReceivedOffer(long level,long price,long position,long permission)
        {
            var values=new long[41];values[0]=999;values[3]=position;values[4]=level;values[6]=permission;values[16]=price;
            var item=new ItemCatalogEntry(values,price.ToString(),"","fixture","","weapon");
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(item,out var offer));Assert.IsNull(offer);
            Assert.IsFalse(KaosiaShopRules.TryCreateOffer(null,out _));
        }
    }
}
