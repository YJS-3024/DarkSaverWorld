using System.Collections.Generic;

namespace DarkSaver.Combat
{
    // Observed return values only. Does not heal, revive, save, or update ownership.
    public sealed class BattleFormationReturnMember
    {
        internal BattleFormationReturnMember(BattleFormationMemberSnapshot deployed, CombatUnitSnapshot observed)
        {
            PersistentId = deployed.PersistentId; CommanderPersistentId = deployed.CommanderPersistentId;
            CommanderId = deployed.CommanderId; Unit = observed;
            OwnedInstanceId = deployed.OwnedInstanceId;
        }
        public string PersistentId { get; }
        public short OwnedInstanceId { get; }
        public string CommanderPersistentId { get; }
        public short CommanderId { get; }
        public CombatUnitSnapshot Unit { get; }
    }

    public sealed class BattleFormationReturnSnapshot
    {
        internal BattleFormationReturnSnapshot(List<BattleFormationReturnMember> members)
        { Members = members.AsReadOnly(); }
        public IReadOnlyList<BattleFormationReturnMember> Members { get; }
    }

    public static class BattleFormationReturn
    {
        // Caller must capture every deployed ally before cleanup. Missing units are
        // not silently treated as dead, dismissed, or unchanged. Explicit Removed
        // observations are accepted; no resurrection or MP recovery is inferred.
        public static bool TryCapture(BattleFormationSnapshot deployment, IReadOnlyList<CombatUnitSnapshot> observations,
            out BattleFormationReturnSnapshot snapshot, out string reason)
        {
            snapshot = null; reason = string.Empty;
            if (deployment == null || observations == null || deployment.Members.Count == 0 ||
                observations.Count != deployment.Members.Count)
            { reason = "출전 스냅샷과 모든 출전 아군의 결과가 필요합니다."; return false; }
            var deployedById = new Dictionary<short, BattleFormationMemberSnapshot>();
            foreach (var deployed in deployment.Members) deployedById.Add(deployed.Unit.Id, deployed);
            var observedById = new Dictionary<short, CombatUnitSnapshot>();
            foreach (var observed in observations)
            {
                if (!deployedById.TryGetValue(observed.Id, out var deployed) || observedById.ContainsKey(observed.Id) ||
                    observed.Enemy || observed.JobId != deployed.Unit.JobId || string.IsNullOrWhiteSpace(observed.Name))
                { reason = "결과 ID가 중복되거나 출전한 아군/직업과 일치하지 않습니다."; return false; }
                if (observed.MaxHp <= 0 || observed.Hp < 0 || observed.Hp > observed.MaxHp ||
                    observed.MaxMp < 0 || observed.Mp < 0 || observed.Mp > observed.MaxMp ||
                    observed.Attack < 0 || observed.Defense < 0 || observed.Ap < 0 || (observed.MaxAp > 0 && observed.Ap > observed.MaxAp) ||
                    (observed.LifeState != UnitLifeState.Active && observed.LifeState != UnitLifeState.Corpse && observed.LifeState != UnitLifeState.Removed) ||
                    (observed.LifeState != UnitLifeState.Active && observed.Hp > 0))
                { reason = "결과의 생명 상태 또는 능력치가 유효하지 않습니다."; return false; }
                observedById.Add(observed.Id, observed);
            }
            var members = new List<BattleFormationReturnMember>(deployment.Members.Count);
            foreach (var deployed in deployment.Members)
                members.Add(new BattleFormationReturnMember(deployed, observedById[deployed.Unit.Id]));
            snapshot = new BattleFormationReturnSnapshot(members); return true;
        }
    }
}
