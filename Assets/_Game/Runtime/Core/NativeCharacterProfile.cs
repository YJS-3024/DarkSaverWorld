using System;
using System.IO;
using UnityEngine;

namespace DarkSaver.Core
{
    [Serializable]
    public sealed partial class NativeCharacterProfile
    {
        public const int SupportedStartingJob = 100;
        public string name;
        public int jobId;
        public NativeTutorialProgress tutorial;
        // Optional for legacy saves: unknown levels stay unknown, never inferred from XP.
        public bool hasLevels;
        public int baseLevel, jobLevel;
        // New rewards tracked locally. Legacy default0 does not infer historical class-change rewards.
        public int unspentClassChangeStatPoints;
        // Confirmed allocations only, additive to class base stats. Old saves default to zero.
        public int allocatedAttack, allocatedDefense, allocatedMagicAttack, allocatedMagicDefense;
        private static NativeCharacterProfile pending;
        private static string pendingPath;

        // Native single-player name policy, not a recovered original creation rule.
        public static bool IsValidName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Length > GameConstants.CharacterNameMaxLength) return false;
            foreach (var c in value) if (char.IsControl(c) || char.IsSurrogate(c)) return false;
            return true;
        }
        public bool IsValid => (tutorial == null || tutorial.IsValid) && IsValidName(name) && IsEquipmentStateValid && IsClassAbilityStateValid && IsBattleMagicSelectionValid && unspentClassChangeStatPoints>=0 &&
            allocatedAttack>=0 && allocatedDefense>=0 && allocatedMagicAttack>=0 && allocatedMagicDefense>=0 &&
            (hasLevels ? baseLevel >= 1 && jobLevel >= 1 && jobLevel <= DarkSaver.Jobs.NativePlayerJobs.MaximumLevel(jobId) && baseLevel >= jobLevel
                : baseLevel == 0 && jobLevel == 0);
        public NativeCharacterProfile Clone() => new NativeCharacterProfile {
            name = name, jobId = jobId, tutorial = tutorial?.Clone(), hasClassAbilities = hasClassAbilities,
            equipmentItemIds=equipmentItemIds==null ? null : (int[])equipmentItemIds.Clone(),
            hasLevels = hasLevels, baseLevel = baseLevel, jobLevel = jobLevel,
            unspentClassChangeStatPoints=unspentClassChangeStatPoints,
            allocatedAttack=allocatedAttack, allocatedDefense=allocatedDefense,
            allocatedMagicAttack=allocatedMagicAttack, allocatedMagicDefense=allocatedMagicDefense,
            hasBattleMagicSelection=hasBattleMagicSelection,
            battleMagicIds=battleMagicIds==null ? null : (int[])battleMagicIds.Clone(),
            learnedMagicIds = learnedMagicIds == null ? null : (int[])learnedMagicIds.Clone(),
            usableSkillIds = usableSkillIds == null ? null : (int[])usableSkillIds.Clone()
        };

        // The caller replaces the live profile only on confirmation. Discarding this candidate is cancel.
        // Deltas cannot refund allocations confirmed in an earlier transaction. HP/MP have no inputs.
        public bool TryBuildStatAllocation(int attack,int defense,int magicAttack,int magicDefense,
            out NativeCharacterProfile changed)
        {
            changed=null;
            if(!IsValid || attack<0 || defense<0 || magicAttack<0 || magicDefense<0
                || GameConstants.ClassStatAttackPointCost<=0 || GameConstants.ClassStatDefensePointCost<=0
                || GameConstants.ClassStatMagicAttackPointCost<=0 || GameConstants.ClassStatMagicDefensePointCost<=0) return false;
            decimal cost=(decimal)attack*GameConstants.ClassStatAttackPointCost+(decimal)defense*GameConstants.ClassStatDefensePointCost
                +(decimal)magicAttack*GameConstants.ClassStatMagicAttackPointCost+(decimal)magicDefense*GameConstants.ClassStatMagicDefensePointCost;
            if(cost==0 || cost>unspentClassChangeStatPoints
                || (long)allocatedAttack+attack>int.MaxValue || (long)allocatedDefense+defense>int.MaxValue
                || (long)allocatedMagicAttack+magicAttack>int.MaxValue || (long)allocatedMagicDefense+magicDefense>int.MaxValue) return false;
            var candidate=Clone();
            candidate.unspentClassChangeStatPoints-=(int)cost;
            candidate.allocatedAttack+=attack; candidate.allocatedDefense+=defense;
            candidate.allocatedMagicAttack+=magicAttack; candidate.allocatedMagicDefense+=magicDefense;
            changed=candidate; return true;
        }

        public static bool TryQueue(string name, int jobId, string path, out string message)
        {
            ClearPending();
            var profile = new NativeCharacterProfile { name = name?.Trim(), jobId = jobId,
                hasLevels = true, baseLevel = 1, jobLevel = 1, tutorial = new NativeTutorialProgress() };
            message = $"이름은 1~{GameConstants.CharacterNameMaxLength}자여야 하며 현재 지원 직업은 100입니다.";
            if (jobId != SupportedStartingJob || !profile.IsValid) return false;
            string fullPath;
            try { fullPath = Path.GetFullPath(path ?? NativeSaveStore.DefaultPath); }
            catch (Exception) { message = "저장 경로가 올바르지 않습니다."; return false; }
            NativeSaveStore.ClearResume();
            pending = profile; pendingPath = fullPath;
            message = string.Empty;
            return true;
        }

        public static bool TakePending(out NativeCharacterProfile profile, out string path)
        {
            profile = pending?.Clone(); path = pendingPath;
            ClearPending(); return profile != null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearPending() { pending = null; pendingPath = null; }
    }
}
