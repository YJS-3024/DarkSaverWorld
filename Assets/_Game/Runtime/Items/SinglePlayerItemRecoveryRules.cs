using DarkSaver.Magic;

namespace DarkSaver.Items
{
    // Original item -> magic link and range; unresolved amounts/AP are explicit single-player policy.
    public static class SinglePlayerItemRecoveryRules
    {
        public const int ApCost = 6;
        public static bool TryGet(ItemCatalogEntry item, MagicCatalog magic, out int hp, out int mp,
            out int range, out bool amountConfirmed)
        {
            hp=mp=range=0;amountConfirmed=false;
            if(item==null || item.GetNumericValue(3)!=7 || magic==null)return false;
            int linked;bool mana=false;
            switch(item.Id)
            {
                case 300: case 301: case 316: case 317: linked=3001;break;
                case 302: case 318: linked=3101;break;
                case 303: case 319: linked=3102;break;
                case 304: case 320: linked=3103;break;
                case 305: case 321: linked=3201;mana=true;break;
                case 963:linked=3104;break;
                case 1163:linked=3105;break;
                default:return false;
            }
            if(item.GetNumericValue(17)!=linked || !magic.TryGet(linked,out var entry) || entry.Range<0 || entry.Range>255)return false;
            // Local effect rule uses the native negative EA magnitude; no caster scaling is inferred.
            long ea=entry.GetNumericValue(9);if(ea>=0 || ea< -int.MaxValue)return false;
            if(mana)mp=(int)-ea;else hp=(int)-ea;
            amountConfirmed=(item.Id==304 || item.Id==320) && hp==999;
            range=(int)entry.Range;return true;
        }
    }
}
