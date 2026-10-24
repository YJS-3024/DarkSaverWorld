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
            offer=new NativeShopOffer {itemId=item.Id,buyPrice=item.GetNumericValue(ItemCatalogEntry.PriceColumn),sellPrice=-1};return true;
        }
        public const int MaximumRequiredLevel = 30;
        public static bool TryCreateOffer(ItemCatalogEntry item, out NativeShopOffer offer)
        {
            offer = null;
            if (item == null || item.Id <= 0) return false;
            long position = item.GetNumericValue(3), required = item.GetNumericValue(4);
            if (position != 1 && position != 2 && position != 3 && position != 4 && position != 5 && position != 8) return false;
            long price = item.GetNumericValue(ItemCatalogEntry.PriceColumn);
            if (required < 0 || required > MaximumRequiredLevel || price <= 0) return false;
            bool wearable = false;
            for (int column = 6; column < 16; column++)
            {
                long permission = item.GetNumericValue(column);
                if (permission != 0 && permission != 1) return false;
                wearable |= permission == 1;
            }
            if (!wearable) return false;
            // Table price is the local purchase quote. Buyback remains unknown;
            // never infer a half-price rule or sell a zero-price quest reward.
            offer = new NativeShopOffer { itemId = item.Id, buyPrice = price, sellPrice = -1 };
            return true;
        }
    }
}
