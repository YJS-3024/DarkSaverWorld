namespace DarkSaver.Items
{
    // User-confirmed class tiers1-3. The growth table spans5+10+15 base
    // levels; exact seller inventory is a reconstruction, not recovered stock.
    public static class KaosiaShopRules
    {
        // Authored accessory/consumable stock using native position and positive table price.
        public static bool TryCreateAdditionalOffer(ItemCatalogEntry item,int position,out NativeShopOffer offer)
        {
            offer=null;
            if(item==null || item.Id<=0 || (position!=6 && position!=7) || item.GetNumericValue(3)!=position
                || item.GetNumericValue(4)<0 || item.GetNumericValue(4)>MaximumRequiredLevel || item.GetNumericValue(ItemCatalogEntry.PriceColumn)<=0)return false;
            offer=new NativeShopOffer {itemId=item.Id,buyPrice=item.GetNumericValue(ItemCatalogEntry.PriceColumn),sellPrice=item.GetNumericValue(ItemCatalogEntry.PriceColumn)/2};return true;
        }
        public const int MaximumRequiredLevel = 30;
        public static bool TryCreateOffer(ItemCatalogEntry item, out NativeShopOffer offer)
{offer=default;return false;}    }
}
