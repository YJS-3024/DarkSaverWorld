using DarkSaver.Combat;
using DarkSaver.Magic;

namespace DarkSaver.Items
{
    public static class SinglePlayerItemMagicRules
    {
        // Exact native item->magic links. Effects use the separately named local combat rules.
        public static bool TryGet(ItemCatalogEntry item, MagicCatalog catalog, out MagicCatalogEntry magic)
        {
            magic = null;
            if (item == null || catalog == null || item.GetNumericValue(3) != 7) return false;
            int expected = item.Id switch { 310 or 323 => 2203, 311 => 101, 312 => 301, 314 => 701, 398 => 201, 307 => 30801, 308 => 30802, 309 => 30803, 313 => 1601, 315 => 3601, 322 => 3501, _ => 0 };
            return expected > 0 && item.GetNumericValue(17) == expected && catalog.TryGet(expected, out magic)
                && magic.Range >= 0 && magic.Range <= 255
                && TemporaryMagicCatalogRules.TryClassify(magic, out _, out _);
        }
    }
}
