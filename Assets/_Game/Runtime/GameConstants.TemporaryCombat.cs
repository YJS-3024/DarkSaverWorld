namespace DarkSaver
{
    // Provisional single-player values, NOT recovered original/server rules.
    // Keep presentation independent: tune these after the visual restoration.
    public static partial class GameConstants
    {
        // Temporary family/level scaling, not raw ATR TIME/TICK/MP interpretation.
        public const int BattleTemporaryMagicMpPerLevel = 2;
        public const int BattleTemporaryMagicPowerPerLevel = 5;
        public const int BattleTemporaryHealPerLevel = 10;
        public const int BattleTemporaryBuffPerLevel = 2;
        // 2101 Attack / 2001 Protection names are catalog evidence; all values here are provisional.
        public const int BattleTemporaryBuffApCost = 4;
        public const int BattleTemporaryBuffMpCost = 6;
        public const int BattleTemporaryBuffRange = 3;
        public const int BattleTemporaryBuffAmount = 10;
        public const float BattleTemporaryBuffSeconds = 20;
        // Catalog confirms 2401 = Heal Lv1. Values below are provisional, not recovered server rules.
        public const int BattleTemporaryHeal2401ApCost = 4;
        public const int BattleTemporaryHeal2401MpCost = 7;
        public const int BattleTemporaryHeal2401Range = 3;
        public const int BattleTemporaryHeal2401Amount = 20;
        public const bool BattleUseTemporaryLocalRules = true;
        public const int BattleTemporaryAttackApCost = 4;
        public const int BattleTemporaryEnemyAttackApCost = 6;
        public const int BattleTemporaryJob100AttackApCost = 7;
        public const int BattleTemporaryJob115AttackApCost = 6;
        public const int BattleTemporaryJob135AttackApCost = 5;
        // Temporary ceil(raw job attack tick * numerator / denominator) + offset.
        // These are tuning knobs, NOT a recovered final native AP formula.
        public const int BattleTemporaryJobAttackTickNumerator = 1;
        public const int BattleTemporaryJobAttackTickDenominator = 1;
        public const int BattleTemporaryJobAttackTickOffset = 0;
        public const int BattleTemporaryMagic101ApCost = 6;
        public const int BattleTemporaryMagic101MpCost = 10;
        public const int BattleTemporaryMagic101Range = 6;
        public const int BattleTemporaryRestApCost = 4;
        public const double BattleTemporaryRestHpFraction = .2;
        public const int BattleTemporarySearchRange = 6;
        public const int BattleTemporaryAttackRange = 1;
        public const int BattleTemporaryAiMoveCells = 3;
        public const float BattleTemporaryAiDecisionSeconds = .2f;
        public const double BattleTemporaryAccuracyPercent = 90;
        public const double BattleTemporaryEvasionPercent = 5;
        public const int BattleTemporaryMinimumDamage = 1;
        public const int BattleTemporaryMagicPowerBonus = 20;
        public const int BattleTemporaryMagicDefenseDivisor = 2;
        public static int GetTemporaryAttackApCost(int jobId, bool enemy) => jobId switch
        {
            100 => BattleTemporaryJob100AttackApCost,
            115 => BattleTemporaryJob115AttackApCost,
            135 => BattleTemporaryJob135AttackApCost,
            _ => enemy ? BattleTemporaryEnemyAttackApCost : BattleTemporaryAttackApCost
        };
    }
}
