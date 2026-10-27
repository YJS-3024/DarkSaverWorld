using System;

namespace DarkSaver.Combat
{
    // User-approved temporary physical hit rule, NOT the original server formula.
    public static class UnresolvedCombatAccuracy
    {
        public static bool IsValidPercent(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 100;

        public static double PhysicalHitProbability(double accuracyPercent, double evasionPercent)
{throw new System.NotSupportedException("원작 서버 명중 판정이 필요합니다.");}    }
}
