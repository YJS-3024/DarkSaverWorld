using System;

namespace DarkSaver.Combat
{
    // User-approved temporary physical hit rule, NOT the original server formula.
    public static class TemporaryCombatHitRules
    {
        public static bool IsValidPercent(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 100;

        public static double PhysicalHitProbability(double accuracyPercent, double evasionPercent)
        {
            if (!IsValidPercent(accuracyPercent) || !IsValidPercent(evasionPercent))
                throw new ArgumentOutOfRangeException("Percent must be between 0 and 100.");
            return accuracyPercent / 100d * (1d - evasionPercent / 100d);
        }
    }
}
