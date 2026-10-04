namespace DarkSaver.Prototype
{
    public static class ShopRules
    {
        public const int HealingPotionPrice = 25;

        public static bool TryBuyHealingPotion(ref int gold, ref int potionCount)
        {
            if (gold < HealingPotionPrice) return false;
            gold -= HealingPotionPrice;
            potionCount++;
            return true;
        }
    }
}
