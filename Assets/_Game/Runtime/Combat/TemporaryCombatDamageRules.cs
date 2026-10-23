using System;
namespace DarkSaver.Combat
{
    // Replace this provisional formula without changing battle UI or animation.
    public static class TemporaryCombatDamageRules
    {
        public static int Calculate(int attack, int defense, bool magic)
        {
            long power = (long)attack + (magic ? GameConstants.BattleTemporaryMagicPowerBonus : 0);
            int mitigation = magic ? defense / Math.Max(1, GameConstants.BattleTemporaryMagicDefenseDivisor) : defense;
            return (int)Math.Min(int.MaxValue, Math.Max((long)Math.Max(1, GameConstants.BattleTemporaryMinimumDamage), power - Math.Max(0, mitigation)));
        }
    }
}
