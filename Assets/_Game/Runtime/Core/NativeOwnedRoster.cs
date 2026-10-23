using System;
using System.Collections.Generic;
using DarkSaver.Combat;

namespace DarkSaver.Core
{
    [Serializable]
    public sealed class NativeOwnedUnitData
    {
        public string persistentId, name;
        public int instanceId, jobId;
        public int hp, maxHp, mp, maxMp, attack, defense, lifeState;
        public uint experience;
        public bool hasMagicAttack,hasMagicDefense;
        public int magicAttack,magicDefense;
        public int teamId;
        public bool recordEligible, finalStateConfirmed;
        public short[] learnedMagicIds;
        // Local ownership metadata; null/0 in older saves means unclassified.
        public string hireType;
        public int hireOwnerId;

        public NativeOwnedUnitData Clone()
        {
            var copy = (NativeOwnedUnitData)MemberwiseClone();
            copy.learnedMagicIds = learnedMagicIds == null ? null : (short[])learnedMagicIds.Clone(); return copy;
        }
    }

    // Explicitly owned units. Primary 100 keeps its separate session vitals;
    // hired soldiers use temporary deployment aliases, never rewritten save IDs.
    [Serializable]
    public sealed class NativeOwnedRoster
    {
        public NativeOwnedUnitData[] units;
        public bool IsValid
        {
            get
            {
                if (units == null || units.Length > 10000) return false;
                var stable = new HashSet<string>(StringComparer.Ordinal); var instances = new HashSet<int>();
                foreach (var unit in units)
                {
                    if (unit == null || !unit.finalStateConfirmed || string.IsNullOrWhiteSpace(unit.persistentId) ||
                        !stable.Add(unit.persistentId) || unit.instanceId < 0 || unit.instanceId > short.MaxValue ||
                        unit.instanceId == 100 || !instances.Add(unit.instanceId) || unit.jobId <= 0 ||
                        string.IsNullOrWhiteSpace(unit.name) || unit.maxHp <= 0 || unit.hp < 0 || unit.hp > unit.maxHp ||
                        unit.maxMp < 0 || unit.mp < 0 || unit.mp > unit.maxMp || unit.attack < 0 || unit.defense < 0 ||
                        ((unit.hasMagicAttack && unit.magicAttack<0) || (unit.hasMagicDefense && unit.magicDefense<0)) || unit.lifeState < 0 || unit.lifeState > 2 || (unit.lifeState != 0 && unit.hp > 0) ||
                        unit.teamId < 0 || unit.learnedMagicIds == null) return false;
                    if (string.IsNullOrEmpty(unit.hireType) ? unit.hireOwnerId != 0 : unit.hireOwnerId != 100) return false;
                    var spells = new HashSet<short>();
                    foreach (var spell in unit.learnedMagicIds) if (spell <= 0 || !spells.Add(spell)) return false;
                }
                return true;
            }
        }

        public NativeOwnedRoster Clone()
        {
            var copy = new NativeOwnedRoster { units = units == null ? null : new NativeOwnedUnitData[units.Length] };
            if (units != null) for (int i = 0; i < units.Length; i++) copy.units[i] = units[i]?.Clone();
            return copy;
        }

        public IReadOnlyList<BattleFormationMemberDraft> CreatePreparationDrafts()
        {
            if (!IsValid) throw new InvalidOperationException("Invalid owned roster.");
            var members = new List<BattleFormationMemberDraft>(units.Length);
            foreach (var unit in units) members.Add(new BattleFormationMemberDraft
            {
                PersistentId = unit.persistentId, IsOwned = true, FinalStateConfirmed = unit.finalStateConfirmed,
                Unit = new CombatUnit { Id = (short)unit.instanceId, JobId = unit.jobId, Name = unit.name,
                    Hp = unit.hp, MaxHp = unit.maxHp, Mp = unit.mp, MaxMp = unit.maxMp,
                    Attack = unit.attack, Defense = unit.defense,HasMagicAttack=unit.hasMagicAttack,HasMagicDefense=unit.hasMagicDefense,MagicAttack=unit.magicAttack,MagicDefense=unit.magicDefense, Experience = unit.experience,
                    LifeState = (UnitLifeState)unit.lifeState },
                TeamId = unit.teamId, RecordEligible = unit.recordEligible,
                LearnedMagicIds = (short[])unit.learnedMagicIds.Clone()
                // No assigned commander or position: these are per-deployment inputs.
            });
            return members.AsReadOnly();
        }

        // Explicit observed-value application, not an automatic return policy.
        // Keep baseline stats; reject transformations requiring unknown rules.
        public bool TryApplyObservedReturn(BattleFormationReturnSnapshot returned, out NativeOwnedRoster updated, out string reason)
            => TryApplyReturn(returned, false, out updated, out reason);

        // User-confirmed battle-end policy: all deployed allies recover maximum MP.
        // HP/life state remain observed; recovering MP never revives a dead unit.
        public bool TryApplyBattleReturn(BattleFormationReturnSnapshot returned, out NativeOwnedRoster updated, out string reason)
            => TryApplyReturn(returned, true, out updated, out reason);

        private bool TryApplyReturn(BattleFormationReturnSnapshot returned, bool recoverMp,
            out NativeOwnedRoster updated, out string reason)
        {
            updated = null; reason = "보유 목록과 명시적 복귀 결과가 필요합니다.";
            if (!IsValid || returned == null || returned.Members.Count == 0) return false;
            var byId = new Dictionary<string, NativeOwnedUnitData>(StringComparer.Ordinal);
            var copy = Clone(); foreach (var unit in copy.units) byId.Add(unit.persistentId, unit);
            foreach (var result in returned.Members)
            {
                var observed = result.Unit;
                if (!byId.TryGetValue(result.PersistentId, out var unit) || !BattleFormation.MatchesOwnedIdentity(
                    unit.instanceId, unit.hireOwnerId, observed.Id, result.OwnedInstanceId, result.CommanderId, result.CommanderPersistentId) ||
                    unit.jobId != observed.JobId || unit.maxHp != observed.MaxHp || unit.maxMp != observed.MaxMp)
                { reason = "복귀 식별자·직업·최대 능력치가 보유 상태와 다릅니다."; return false; }
                unit.hp = observed.Hp; unit.mp = recoverMp ? unit.maxMp : observed.Mp; unit.experience = observed.Experience;
                unit.lifeState = (int)observed.LifeState;
            }
            if (!copy.IsValid) { reason = "복귀 상태를 보유 목록에 반영할 수 없습니다."; return false; }
            updated = copy; reason = string.Empty; return true;
        }
    }
}
