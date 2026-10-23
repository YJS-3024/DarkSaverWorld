using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Magic;

namespace DarkSaver.Core
{
    public sealed partial class NativeCharacterProfile
    {
        // Optional additive save fields. Old profiles remain unknown, not auto-granted.
        public bool hasClassAbilities;
        public int[] learnedMagicIds;
        public int[] usableSkillIds;
        public bool hasBattleMagicSelection;
        public int[] battleMagicIds;
        private bool IsBattleMagicSelectionValid
        {
            get {
                if (!hasBattleMagicSelection) return (battleMagicIds?.Length ?? 0) == 0;
                if (!hasClassAbilities || !ValidIds(battleMagicIds,9999) || battleMagicIds.Length>8 || learnedMagicIds==null) return false;
                foreach(int id in battleMagicIds) if(Array.IndexOf(learnedMagicIds,id)<0) return false;
                return true;
            }
        }
        public bool TryBuildBattleMagicSelection(int[] ids,out NativeCharacterProfile changed)
        {
            changed=null;
            if(!IsValid || !hasClassAbilities || ids==null) return false;
            var candidate=Clone(); candidate.hasBattleMagicSelection=true;candidate.battleMagicIds=(int[])ids.Clone();
            if(!candidate.IsValid) return false;
            changed=candidate;return true;
        }
        private bool IsClassAbilityStateValid => hasClassAbilities
            ? jobId >= 100 && jobId <= 169 && ValidIds(learnedMagicIds, 9999) && ValidIds(usableSkillIds, int.MaxValue)
            : jobId == SupportedStartingJob && (learnedMagicIds?.Length ?? 0) == 0 && (usableSkillIds?.Length ?? 0) == 0;

        private static bool ValidIds(int[] ids, int maximum)
        {
            if (ids == null || ids.Length > 10000) return false;
            var seen = new HashSet<int>();
            foreach (int id in ids) if (id <= 0 || id > maximum || !seen.Add(id)) return false;
            return true;
        }

        // Build a new profile only after an externally confirmed class change.
        // confirmedLevel is not an XP eligibility check, and no HP/MP policy is inferred.
        public bool TryBuildClassChange(ExperienceProgressionTable table, MagicCatalog catalog,
            int confirmedLevel, out NativeCharacterProfile changed)
        {
            changed = null;
            if (!IsValid || table == null || catalog == null
                || GameConstants.ClassChangeAdditionalStatPoints<0 || unspentClassChangeStatPoints>int.MaxValue-GameConstants.ClassChangeAdditionalStatPoints
                || !table.TryGetClassChangeAbilities(jobId, confirmedLevel, out int next, out var added, out var skills)) return false;
            int[] previous = learnedMagicIds;
            if (!hasClassAbilities && !table.TryGetClassEntryAbilities(jobId, out previous, out _)) return false;
            if (!catalog.TryMergeLearnedMagic(previous, added, out var merged)) return false;
            var candidate = Clone();
            candidate.jobId=next; candidate.hasClassAbilities=true;
            candidate.jobLevel=hasLevels ? 1 : 0;
            candidate.unspentClassChangeStatPoints+=GameConstants.ClassChangeAdditionalStatPoints;
            candidate.learnedMagicIds=merged; candidate.usableSkillIds=skills;
            if (!candidate.IsValid) return false;
            changed = candidate; return true;
        }
    }
}
