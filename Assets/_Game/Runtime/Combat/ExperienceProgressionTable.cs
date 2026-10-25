using System;
using System.Linq;
using UnityEngine;

namespace DarkSaver.Combat
{
    [Serializable]
    public struct ExperienceLevelEntry
    {
        public int jobId;
        public string jobName;
        public int level;
        public long requiredXp;
        public int nextJobId;
        public long rewardGold;
        public long rewardXp;
        // Preserve source lists without guessing execution semantics.
        public string learnedMagic;
        public string usableSkills;
    }

    [CreateAssetMenu(menuName = "DarkSaver/Experience Progression Table")]
    public sealed class ExperienceProgressionTable : ScriptableObject
    {
        [SerializeField] private ExperienceLevelEntry[] entries = Array.Empty<ExperienceLevelEntry>();
        public int Count => entries.Length;
        public ExperienceLevelEntry[] GetClassChangeRows(int jobId) => jobId < 100 || jobId > 169
            ? Array.Empty<ExperienceLevelEntry>()
            : entries.Where(row => row.jobId == jobId && row.nextJobId != jobId).Distinct().ToArray();

        // User-confirmed eligibility: a separately confirmed level reaches the source change level.
        // Never infer a level from experience, fees or post-change vitals here.
        public bool TryGetEligibleClassChange(int jobId, int confirmedLevel, out ExperienceLevelEntry change)
        {
            change = default;
            if (jobId < 100 || jobId > 169 || confirmedLevel < 1 || confirmedLevel > 100) return false;
            bool found = false;
            foreach (var row in entries)
            {
                if (row.jobId != jobId || row.level > confirmedLevel || row.nextJobId == jobId) continue;
                if (row.nextJobId < 100 || row.nextJobId > 169 || !TryGet(row.nextJobId, 1, out _))
                { change = default; return false; }
                if (found && row.nextJobId != change.nextJobId) { change = default; return false; }
                if (!found || row.level < change.level) change = row;
                found = true;
            }
            return found;
        }

        // Creation/class-change input: read the class's first row, not all level rewards.
        // This does not grant ownership, merge previous spells, or resolve skill effects.
        public bool TryGetClassEntryAbilities(int jobId, out int[] magicIds, out int[] skillIds)
        {
            magicIds = skillIds = Array.Empty<int>();
            if (jobId < 100 || jobId > 169 || !TryGet(jobId, 1, out var row)
                || !TryReadAbilityIds(row.learnedMagic, out var magic)
                || !TryReadAbilityIds(row.usableSkills, out var skills)) return false;
            magicIds = magic; skillIds = skills; return true;
        }

        // Explicit source next-class edge only. Does not spend XP or mutate a character.
        // The caller must handle eligibility and ownership persistence separately.
        public bool TryGetClassChangeAbilities(int currentJobId, int currentLevel,
            out int nextJobId, out int[] magicIds, out int[] skillIds)
        {
            nextJobId = 0; magicIds = skillIds = Array.Empty<int>();
            if (currentJobId < 100 || currentJobId > 169
                || !TryGet(currentJobId, currentLevel, out var row)
                || row.nextJobId == currentJobId
                || !TryGetClassEntryAbilities(row.nextJobId, out var magic, out var skills)) return false;
            nextJobId = row.nextJobId; magicIds = magic; skillIds = skills; return true;
        }

        public bool TryGetLevelAbilities(int job,int level,out int[] learned,out int[] skills)
        {
            learned=skills=Array.Empty<int>();
            if(!TryGet(job,level,out var row) || !TryReadAbilityIds(row.learnedMagic,out var magic) || !TryReadAbilityIds(row.usableSkills,out var usable))return false;
            foreach(int id in magic)if(id>=10000)return false;
            learned=magic;skills=usable;return true;
        }
        private static bool TryReadAbilityIds(string raw, out int[] ids)
        {
            ids = Array.Empty<int>();
            if (string.IsNullOrWhiteSpace(raw)) return false; // Unknown is not confirmed empty.
            if (raw.Trim() == "0") return true;
            var fields = raw.Split(',');
            // The source includes a trailing comma, e.g. class166's skill list.
            int count = fields.Length;
            if (fields[count - 1].Trim().Length == 0) count--;
            if (count == 0) return false;
            var parsed = new int[count];
            for (int i = 0; i < count; i++)
                if (!int.TryParse(fields[i].Trim(), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out parsed[i]) || parsed[i] <= 0) return false;
            ids = parsed; return true; // Preserve original order and IDs outside MagicCatalog too.
        }

        public void Initialize(ExperienceLevelEntry[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Experience table is empty.");
            var ordered = source.OrderBy(row => row.jobId).ThenBy(row => row.level).ToArray();
            for (var i = 0; i < ordered.Length; i++)
            {
                var row = ordered[i];
                if (row.jobId < 0 || row.level < 1 || row.level > 100 || row.nextJobId < 0 ||
                    row.requiredXp < 0 || row.rewardGold < 0 || row.rewardXp < 0)
                    throw new ArgumentException("Invalid experience record.");
                if (i > 0 && ordered[i - 1].jobId == row.jobId &&
                    ((ordered[i - 1].level == row.level && !ordered[i - 1].Equals(row)) ||
                     ordered[i - 1].requiredXp > row.requiredXp))
                    throw new ArgumentException("Conflicting duplicate level or decreasing required experience.");
                // Source 163/35 is repeated identically. Preserve it without ambiguous lookup values.
            }
            entries = ordered;
        }

        public bool TryGet(int jobId, int level, out ExperienceLevelEntry entry)
        {
            var low = 0; var high = entries.Length - 1;
            while (low <= high)
            {
                var middle = low + (high - low) / 2;
                var candidate = entries[middle];
                var comparison = candidate.jobId.CompareTo(jobId);
                if (comparison == 0) comparison = candidate.level.CompareTo(level);
                if (comparison == 0) { entry = candidate; return true; }
                if (comparison < 0) low = middle + 1; else high = middle - 1;
            }
            entry = default;
            return false; // Missing data is not a zero-XP level.
        }
    }
}
