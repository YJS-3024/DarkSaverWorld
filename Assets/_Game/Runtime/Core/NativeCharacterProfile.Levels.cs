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
        {
            progressed = null; remainingExperience = availableExperience;
            if (!IsValid || !hasLevels || table == null || !table.TryGet(jobId, jobLevel, out _)) return false;
            var candidate = Clone(); uint remaining = availableExperience;
            while (candidate.jobLevel < 100)
            {
                if (!table.TryGet(candidate.jobId, candidate.jobLevel, out var row)) return false;
                if (row.nextJobId != candidate.jobId) break; // Explicit class-change boundary, not auto promotion.
                if (row.requiredXp <= 0) return false;
                if ((ulong)remaining < (ulong)row.requiredXp) break;
                if (candidate.baseLevel == int.MaxValue || !table.TryGet(candidate.jobId, candidate.jobLevel + 1, out _)) return false;
                remaining -= (uint)row.requiredXp;
                candidate.jobLevel++; candidate.baseLevel++;
                if(candidate.hasClassAbilities && table.TryGetLevelAbilities(candidate.jobId,candidate.jobLevel,out var gained,out var skills))
                {
                    var magic=new System.Collections.Generic.List<int>(candidate.learnedMagicIds);
                    foreach(int id in gained)if(!magic.Contains(id))magic.Add(id);
                    candidate.learnedMagicIds=magic.ToArray();candidate.usableSkillIds=skills;
                }
            }
            if (!candidate.IsValid) return false;
            progressed = candidate; remainingExperience = remaining; return true;
        }

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
