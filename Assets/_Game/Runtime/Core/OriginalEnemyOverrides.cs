namespace DarkSaver.Core
{
    // User-confirmed screenshot observation. Keep ATR/source content and save hashes intact.
    // This is one explicit observed maximum, not a universal HP multiplier.
    public static class OriginalEnemyOverrides
    {
        public static int MaximumHp(int jobId,int sourceHp)=>jobId==701 && sourceHp==300 ? 450 : sourceHp;
    }
}
