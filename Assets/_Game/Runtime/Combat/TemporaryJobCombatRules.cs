using DarkSaver.Jobs;

namespace DarkSaver.Combat
{
    // Provisional AP conversion only, not the original server's resolved cost.
    // Do not reinterpret raw accuracy (>100) or the all-zero job range here.
    public static class TemporaryJobCombatRules
    {
        public static bool TryGetAttackApCost(JobCatalogEntry job, out int cost)
        {
            cost = 0;
            if (job == null || job.CategoryLabel != "player_job" || !NativePlayerJobs.Contains(job.Id)) return false;
            long raw = job.GetNumericValue(25);
            if (raw < 1 || raw > int.MaxValue || GameConstants.BattleTemporaryJobAttackTickNumerator < 1
                || GameConstants.BattleTemporaryJobAttackTickDenominator < 1) return false;
            long converted = (raw * GameConstants.BattleTemporaryJobAttackTickNumerator
                + GameConstants.BattleTemporaryJobAttackTickDenominator - 1) / GameConstants.BattleTemporaryJobAttackTickDenominator
                + GameConstants.BattleTemporaryJobAttackTickOffset;
            // Preserve the existing user-tunable representative job overrides.
            if (job.Id == 100 || job.Id == 115 || job.Id == 135)
                converted = GameConstants.GetTemporaryAttackApCost(job.Id, false);
            if (converted < 1 || converted > GameConstants.BattleMaximumAp) return false;
            cost = (int)converted; return true;
        }
    }
}
