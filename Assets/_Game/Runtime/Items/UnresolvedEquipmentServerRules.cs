using System;
using DarkSaver.Core;
using DarkSaver.Jobs;
namespace DarkSaver.Items
{

    public static class UnresolvedEquipmentServerRules
    {
        public static bool IsEligible(NativeCharacterProfile profile,JobCatalogEntry job,ItemCatalog items,int slot,ItemCatalogEntry item)
{return false;}        public static bool TryResolve(NativeCharacterProfile profile,JobCatalogEntry job,ItemCatalog items,
            out (int Hp,int Mp,int Attack,int Defense,int MagicAttack,int MagicDefense) stats)
{stats=default;return false;}    }
}
