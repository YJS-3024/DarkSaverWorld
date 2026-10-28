namespace DarkSaver
{
    // Change these values to tune the game without editing individual systems.
    public static partial class GameConstants
    {
        // Tiles/second. Approximate video match: Naver cafe 4603, town 19-20s / field 108-110s.
        // Visual estimate, not an original executable/server constant.
        public const float PlayerMoveSpeed = 6f;
        // World movement cadence requested for the legacy feel; local presentation setting.
        public const float WorldMoveTickSeconds = .08f;
        public const int CharacterNameMaxLength = 20;
        // User-confirmed single-player starting funds; never applied during save restoration.
        public const long NewCharacterStartingGold = 0; // Unused compatibility constant; original initial funds are unknown.
        // User-confirmed reward per completed class change; not an inferred original server constant.
        public const int ClassChangeAdditionalStatPoints = 0; // No local point grant.
        public const int ClassStatAttackPointCost = 2;
        public const int ClassStatDefensePointCost = 1;
        public const int ClassStatMagicAttackPointCost = 2;
        public const int ClassStatMagicDefensePointCost = 1;
        // Display order: HP, MP, attack, defense, magic attack, magic defense. -1 means locked.
        public static int GetClassStatPointCost(int index)=>index switch {
            2=>ClassStatAttackPointCost,3=>ClassStatDefensePointCost,
            4=>ClassStatMagicAttackPointCost,5=>ClassStatMagicDefensePointCost,_=>-1 };
        // 480 reference pixels / (2 * 32 world pixels per unit): world and UI share the same pixel scale.
        public const float WorldCameraOrthographicSize = 7.5f;
        // Reference UI resolution; preserve aspect ratio on the actual display.
        public const int ReferenceScreenWidth = 640;
        public const int ReferenceScreenHeight = 480;
        public const float CameraFollowSmoothing = 10f;
        // User-selected AP tuning (2026-10-27), not recovered original-server values.
        // Edit these two values when the original maximum/cadence is established.
        public const float BattleApRecoveryPerSecond = 2f;
        public const int BattleMaximumAp = 20;
        public const int BattleStartingAp = 0; // Initial ticks still come from native state.
        public const int BattleAllyMaximumAp = BattleMaximumAp;
        public const int BattleEnemyMaximumAp = BattleMaximumAp;
        public static int GetBattleMaximumAp(bool enemy) => enemy ? BattleEnemyMaximumAp : BattleAllyMaximumAp;
        public static int BattleHighestMaximumAp => System.Math.Max(BattleAllyMaximumAp, BattleEnemyMaximumAp);
        // One point per interval: rate2 means +1 every0.5 seconds, not +2 each0.5 seconds.
        // Configuration only; CombatActionPoints/CombatFlow do not yet run a recovery timer.
        public const int BattleApRecoveryAmount = 1;
        public const float BattleApRecoverySeconds = BattleApRecoveryAmount / BattleApRecoveryPerSecond;
        // User-approved combat rule; checked for query, reservation and confirmation.
        public const bool BattlePreventDiagonalCornerCutting = true;
        public const int BattleAllyMoveApCost = 0;
        public const int BattleEnemyMoveApCost = 0;
        // Provisional combat tuning lives in GameConstants.TemporaryCombat.cs.
        // Native presentation tuning, not recovered original color/sorting values.
        public static readonly UnityEngine.Color BattleMagicPreviewColor = new(.15f, .5f, 1f, .28f);
        public const int BattleMagicPreviewSortOffset = 1;
        public const float BattleActionMenuRadius = 32f;
        public const float BattleActionMenuButtonSize = 32f;
        // Unity diagnostics only; not a recovered native window or shortcut.
        public const bool BattlePrototypeHudDefaultVisible = false;
        public const UnityEngine.KeyCode BattlePrototypeHudKey = UnityEngine.KeyCode.F10;
        // Presentation timing is adjustable; not an original damage/critical rule.
        public const float BattleFeedbackSeconds = 0.8f;
        public const int BattleFeedbackMaximum = 32;
        public const float BattleFeedbackPixelScale = 1f; // Native glyph pixels in the 640x480 logical screen.
        // Original counter order/48 limit recovered; wall-clock tick remains adjustable.
        public const float BattleAttackCounterSeconds = .02f;
        // Corpse counter 0..18 is native; wall-clock conversion remains provisional.
        public const float BattleDeathCounterSeconds = .02f;
        // Native counter phases recovered; seconds conversion is provisional.
        public const float BattleMagicEffectCounterSeconds = .04f;
        // GameCtl.dll 0x1000A87D: signed GetTickCount / 300, even phases use Y + 160.
        public const double BattleIconFrameSeconds = 0.3;
        // Zero-based columns of Micon's first row. -1 preserves the text button until identified.
        public const int BattleMoveIconColumn = 0;
        public const int BattleAttackIconColumn = 1;
        public const int BattleMagicIconColumn = 2;
        public const int BattleAttackBuffIconColumn = -1;
        public const int BattleDefenseBuffIconColumn = -1;
        // GameCtl.dll 0x1000B9C6: source rectangle (256,0)-(288,32), label 휴식.
        public const int BattleRestIconColumn = 8;
        public const int BattleCancelIconColumn = -1;
        // User-defined allied deployment limits, not source world/AI queue limits.
        public const int BattleMaximumAlliedGroups = 3;
        public const int BattleMaximumAlliedSoldiersPerGroup = 4;
        // userunit.dll: 80ms ticks, two cardinal / three diagonal phases.
        public const float BattleMoveTickSeconds = .08f;
        public const float BattleMoveLagResetSeconds = .5f;
        public const int BattleCardinalMoveTicks = 2;
        public const int BattleDiagonalMoveTicks = 3;
        public const int BattleMoveSourceTilePixels = 32;
        public const int BattleCardinalMoveTickPixels = 16;
        public const int BattleDiagonalMoveTickPixels = 10;
        // Compatibility: cardinal duration only; diagonals use three ticks.
        public const float BattleMoveStepSeconds = BattleMoveTickSeconds * BattleCardinalMoveTicks;
        // Legacy/unbound attack fallback; created characters use the job entries below.
        // 0 means unconfirmed. Never substitute past demonstration values.
        public const int BattleAttackApCost = 0;
        // Compatibility alias. Tune BattleMagic101ApCost instead.
        public const int BattleMagicApCost = BattleMagic101ApCost;
        // Final native AP costs only: raw ATR TICK is not an AP cost.
        // Add explicit job/magic cases here when a value is confirmed or chosen.
        public const int BattleJob100AttackApCost = 0;
        public const int BattleJob701AttackApCost = 0;
        public const int BattleMagic101ApCost = 0;
        public static int GetBattleAttackApCost(int jobId) => jobId switch
        {
            100 => BattleJob100AttackApCost,
            701 => BattleJob701AttackApCost,
            _ => 0
        };
        public static int GetBattleMagicApCost(int magicId) => magicId switch
        {
            101 => BattleMagic101ApCost,
            _ => 0
        };
        // Original normal-job chips use a shared clock and a 0, 1, 2, 1 cycle.
        // Tune the millisecond values; second-based aliases are derived for callers.
        public const int CharacterIdleFrameMilliseconds = 333;
        public const int CharacterMoveFrameMilliseconds = 125;
        public const float CharacterIdleFrameInterval = CharacterIdleFrameMilliseconds / 1000f;
        public const float CharacterMoveFrameInterval = CharacterMoveFrameMilliseconds / 1000f;
        public const int WaterSurfaceFrameMilliseconds = 250;
        public const int WaterSurfaceYOffsetPixels = 5;
    }
}
