using DarkSaver.Magic;
namespace DarkSaver.Items
{
    public static class OriginalItemMagicLink
    {
        // Native item column17 references the magic catalog. No effect or use approval.
        public static bool TryGet(ItemCatalogEntry item,MagicCatalog catalog,out MagicCatalogEntry magic)
        {
            magic=null;if(item==null || catalog==null || item.GetNumericValue(3)!=7)return false;
            long id=item.GetNumericValue(17);
            return id>0 && id<=short.MaxValue && catalog.TryGet((int)id,out magic);
        }
    }
}
