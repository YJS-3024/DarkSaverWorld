using DarkSaver.Combat;

namespace DarkSaver.Core
{
    public sealed partial class NativeCharacterProfile
    {
        // User-confirmed local progression policy. The current job-level row supplies
        // the cost to advance; a class-change edge holds XP until explicit class change.
        // Applies confirmed per-level ability rows; no stats/gold/healing or automatic class change.
        public bool TryAdvanceLevels(ExperienceProgressionTable table, uint availableExperience,
            out NativeCharacterProfile progressed, out uint remainingExperience)
{progressed=default;remainingExperience=default;return false;}
        public bool TryBuildEligibleClassChange(ExperienceProgressionTable table, DarkSaver.Magic.MagicCatalog catalog,
            out NativeCharacterProfile changed)
        {
            changed = null;
            return IsValid && hasLevels && table != null
                && table.TryGetEligibleClassChange(jobId, jobLevel, out var edge)
                && TryBuildClassChange(table, catalog, edge.level, out changed);
        }
    }
}
