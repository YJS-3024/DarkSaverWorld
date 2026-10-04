using System;

namespace DarkSaver.Prototype
{
    public static class BattleRules
    {
        public const int MoveActionPointCost = 4;
        public const int AttackActionPointCost = 2;
        public const int MagicActionPointCost = 6;
        public const int SkillActionPointCost = 5;
        public const int ItemActionPointCost = 3;
        public const int RestActionPointCost = 4;
        public const int MagicManaCost = 10;
        public const int MagicDamage = 42;
        public const int HealingPotionAmount = 35;
        public const int RestHealthAmount = 18;
        public const int RestManaAmount = 8;

        public static int CalculateDamage(int rawDamage, int defense, bool halveDefense)
        {
            var effectiveDefense = Math.Max(0, defense);
            if (halveDefense)
                effectiveDefense /= 2;

            return Math.Max(1, rawDamage - effectiveDefense);
        }

        public static int ApplyRecovery(int current, int maximum, int amount)
        {
            return Math.Min(Math.Max(0, maximum), Math.Max(0, current) + Math.Max(0, amount));
        }

        public static int CalculateRecoveredActionPoints(float charge, float deltaTime, float rate)
        {
            return Math.Max(0, (int)Math.Floor(charge + Math.Max(0f, deltaTime) * Math.Max(0f, rate)));
        }

        public static bool CanAfford(int current, int cost)
        {
            return cost >= 0 && current >= cost;
        }

        public static int GetNextLevelExperience(int level)
        {
            return Math.Max(1, level) * 60;
        }
    }
}
