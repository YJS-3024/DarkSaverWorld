using DarkSaver.Magic;

namespace DarkSaver.Items
{

    public static class UnresolvedItemRecoveryServerRules
    {
        public const int ApCost = 0; // Unknown; never an executable action cost.
        public static bool TryGet(ItemCatalogEntry item, MagicCatalog magic, out int hp, out int mp,
            out int range, out bool amountConfirmed)
{hp=default;mp=default;range=default;amountConfirmed=default;return false;}    }
}
