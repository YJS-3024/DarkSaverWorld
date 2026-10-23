namespace DarkSaver.Combat
{
    // Top-left source-sheet coordinates, not Unity's bottom-left sprite coordinates.
    public readonly struct CombatDeathFrame
    {
        internal CombatDeathFrame(int bodyRow, int effectColumn)
        { BodyRow = bodyRow; EffectColumn = effectColumn; }

        public int BodyRow { get; }
        public int EffectColumn { get; }
        public int BodySourceLeft => 0;
        public int BodySourceTop => BodyRow * 32;
        public int BodySourceWidth => 32;
        public int BodySourceHeight => 32;
        public int EffectSourceLeft => EffectColumn * 32;
        public int EffectSourceTop => 0;
        public int EffectSourceWidth => 31;
        public int EffectSourceHeight => 31;
    }

    public static class CombatDeathPresentation
    {
        // Pure frame selection for the recovered 0x80 && !0x40 drawing branch.
        // Its existence does not establish ordinary gameplay death animation.
        // Caller supplies its counter; this is not an HP/death decision, timer,
        // automatic removal, sprite binding, or original-file loader.
        public static bool TryGetFrame(int counter, out CombatDeathFrame frame)
        {
            frame = default;
            // Ordinary death update removes the record after counter 18.
            // Other immediate-removal branches must be decided by the caller.
            if (counter < 0 || counter > 18) return false;
            frame = new CombatDeathFrame(counter < 8 ? 32 : counter % 4, counter / 3);
            return true;
        }
    }
}
