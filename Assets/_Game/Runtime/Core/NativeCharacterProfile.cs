using System;
using System.IO;
using UnityEngine;

namespace DarkSaver.Core
{
    [Serializable]
    public sealed partial class NativeCharacterProfile
    {
        public const int SupportedStartingJob = 100; // Default for older callers.
        public static bool IsStartingJob(int id)=>id==100 || id==101 || id==103;
        public static string StartingJobName(int id)=>id==100?"파이터":id==101?"나이트":id==103?"매지션":null;
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
{changed=default;return false;}
        public static bool TryQueue(string name, int jobId, string path, out string message)
{message=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}
        public static bool TakePending(out NativeCharacterProfile profile, out string path)
        {
            profile = pending?.Clone(); path = pendingPath;
            ClearPending(); return profile != null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearPending() { pending = null; pendingPath = null; }
    }
}
