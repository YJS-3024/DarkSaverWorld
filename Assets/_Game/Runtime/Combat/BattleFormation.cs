using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    // Preparation input only. Not a recovered server packet or saved-account roster.
    public sealed class BattleFormationMemberDraft
    {
        public string PersistentId;
        public short? OwnedInstanceId;
        public string CommanderPersistentId;
        public short? CommanderId;
        public CombatUnit Unit;
        public bool IsOwned;
        public bool FinalStateConfirmed;
        public CombatGridCell? Position;
        public int? TeamId;
        public bool? RecordEligible;
        public IReadOnlyList<short> LearnedMagicIds;
    }

    public sealed class BattleFormationMemberSnapshot
    {
        internal BattleFormationMemberSnapshot(BattleFormationMemberDraft draft)
        {
            PersistentId = draft.PersistentId; CommanderPersistentId = draft.CommanderPersistentId;
            OwnedInstanceId = draft.OwnedInstanceId ?? draft.Unit.Id;
            CommanderId = draft.CommanderId.Value; Unit = new CombatUnitSnapshot(draft.Unit.Id, draft.Unit);
            Position = draft.Position.Value; TeamId = draft.TeamId.Value; RecordEligible = draft.RecordEligible.Value;
            LearnedMagicIds = new List<short>(draft.LearnedMagicIds).AsReadOnly();
        }
        public string PersistentId { get; }
        public short OwnedInstanceId { get; }
        public string CommanderPersistentId { get; }
        public short CommanderId { get; }
        public CombatUnitSnapshot Unit { get; }
        public CombatGridCell Position { get; }
        public int TeamId { get; }
        public bool RecordEligible { get; }
        public IReadOnlyList<short> LearnedMagicIds { get; }
    }

    public sealed class BattleFormationSnapshot
    {
        internal BattleFormationSnapshot(List<BattleFormationMemberSnapshot> members, int groupCount)
        {
            members.Sort((left, right) => left.Unit.Id.CompareTo(right.Unit.Id));
            Members = members.AsReadOnly(); GroupCount = groupCount;
        }
        public IReadOnlyList<BattleFormationMemberSnapshot> Members { get; }
        public int GroupCount { get; }
    }

    public static class BattleFormation
    {
        public const string PrimaryCommanderPersistentId = "primary:100";

        public static bool MatchesOwnedIdentity(int ownedId, int hireOwnerId, short deployedId,
            short sourceId, short commanderId, string commanderPersistentId)
            => ownedId == sourceId && (hireOwnerId == 100
                ? deployedId > 100 && deployedId <= 100 + GameConstants.BattleMaximumAlliedSoldiersPerGroup
                    && commanderId == 100 && commanderPersistentId == PrimaryCommanderPersistentId
                : ownedId == deployedId);
        // Pure structural validation. Floor, troop traversal, art, catalogs and
        // final action costs still require validation at the deployment boundary.
        public static bool TryCreateSnapshot(IReadOnlyList<BattleFormationMemberDraft> draft,
            int mapWidth, int mapHeight, IReadOnlyCollection<short> reservedInstanceIds,
            out BattleFormationSnapshot snapshot, out string reason, bool allowPrimaryCommander = false)
        {
            snapshot = null; reason = string.Empty;
            var maximum = GameConstants.BattleMaximumAlliedGroups * (GameConstants.BattleMaximumAlliedSoldiersPerGroup + 1);
            if (draft == null || draft.Count == 0 || draft.Count > maximum || mapWidth <= 0 || mapHeight <= 0
                || reservedInstanceIds == null)
            { reason = "편성 목록·맵 크기·기존 인스턴스 목록이 명시되어야 합니다."; return false; }
            var reserved = new HashSet<short>(reservedInstanceIds);
            var persistentIds = new HashSet<string>(StringComparer.Ordinal);
            var units = new Dictionary<short, BattleFormationMemberDraft>();
            var positions = new HashSet<CombatGridCell>();
            var groups = new Dictionary<short, int>();
            foreach (var member in draft)
            {
                if (member == null || !member.IsOwned || !member.FinalStateConfirmed || member.Unit == null
                    || string.IsNullOrWhiteSpace(member.PersistentId) || string.IsNullOrWhiteSpace(member.CommanderPersistentId)
                    || !member.CommanderId.HasValue || !member.Position.HasValue || !member.TeamId.HasValue
                    || member.TeamId < 0 || !member.RecordEligible.HasValue || member.LearnedMagicIds == null)
                { reason = "보유·최종 상태·소속·좌표·팀·유효성·습득 마법 정보가 누락되었습니다."; return false; }
                var unit = member.Unit; var commander = member.CommanderId.Value; var position = member.Position.Value;
                if (unit.Id < 0 || commander < 0 || commander % 10 != 0 || unit.Id / 10 * 10 != commander
                    || unit.Enemy || unit.JobId <= 0 || string.IsNullOrWhiteSpace(unit.Name)
                    || !unit.Alive || unit.MaxHp < unit.Hp || unit.Mp < 0 || unit.MaxMp < unit.Mp
                    || unit.Attack < 0 || unit.Defense < 0)
                { reason = "아군 ID·직업·생명·최종 능력치 또는 그룹 규칙이 유효하지 않습니다."; return false; }
                if (reserved.Contains(unit.Id) || !persistentIds.Add(member.PersistentId) || units.ContainsKey(unit.Id))
                { reason = "영구 ID 또는 전투 인스턴스 ID가 중복됩니다."; return false; }
                if (position.X < 0 || position.Y < 0 || position.X >= mapWidth || position.Y >= mapHeight
                    || unit.X != position.X || unit.Y != position.Y || !positions.Add(position))
                { reason = "출전 좌표가 맵 밖이거나 상태 좌표와 불일치하거나 중복됩니다."; return false; }
                var magicIds = new HashSet<short>();
                foreach (var magicId in member.LearnedMagicIds)
                    if (magicId <= 0 || !magicIds.Add(magicId))
                    { reason = "습득 마법 ID가 유효하지 않거나 중복됩니다."; return false; }
                units.Add(unit.Id, member);
                groups.TryGetValue(commander, out var count); groups[commander] = count + (unit.Id == commander ? 0 : 1);
            }
            if (groups.Count > GameConstants.BattleMaximumAlliedGroups)
            { reason = "아군 그룹 상한을 초과했습니다."; return false; }
            foreach (var group in groups)
            {
                if (allowPrimaryCommander && group.Key == 100 && reserved.Contains(100) && !units.ContainsKey(100))
                {
                    if (group.Value > GameConstants.BattleMaximumAlliedSoldiersPerGroup)
                    { reason = "주인공 부대 병사 상한을 초과했습니다."; return false; }
                    foreach (var member in units.Values)
                        if (member.CommanderId == 100 && (member.CommanderPersistentId != PrimaryCommanderPersistentId
                            || !member.OwnedInstanceId.HasValue || member.Unit.Id <= 100
                            || member.Unit.Id > 100 + GameConstants.BattleMaximumAlliedSoldiersPerGroup))
                        { reason = "주인공 부대의 보유 식별자와 출전 슬롯이 필요합니다."; return false; }
                    continue;
                }
                if (group.Value > GameConstants.BattleMaximumAlliedSoldiersPerGroup
                    || !units.TryGetValue(group.Key, out var commander)
                    || commander.PersistentId != commander.CommanderPersistentId)
                { reason = "대장이 없거나 대장 소속 또는 병사 수가 유효하지 않습니다."; return false; }
                foreach (var member in units.Values)
                    if (member.CommanderId == group.Key && member.CommanderPersistentId != commander.PersistentId)
                    { reason = "병사의 영구 대장 ID와 전투 그룹이 일치하지 않습니다."; return false; }
            }
            var members = new List<BattleFormationMemberSnapshot>(draft.Count);
            foreach (var member in draft) members.Add(new BattleFormationMemberSnapshot(member));
            snapshot = new BattleFormationSnapshot(members, groups.Count); return true;
        }
    }
}
