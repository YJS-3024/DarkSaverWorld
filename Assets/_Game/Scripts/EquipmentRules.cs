namespace DarkSaver.Prototype
{
    public static class EquipmentRules
    {
        public const int RecruitSwordAttackBonus = 4;
        public const int LeatherArmorDefenseBonus = 3;

        public static int GetAttack(int baseAttack, bool swordEquipped)
        {
            return baseAttack + (swordEquipped ? RecruitSwordAttackBonus : 0);
        }

        public static int GetDefense(int baseDefense, bool armorEquipped)
        {
            return baseDefense + (armorEquipped ? LeatherArmorDefenseBonus : 0);
        }
    }
}
