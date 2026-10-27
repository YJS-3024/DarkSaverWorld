using System;

namespace DarkSaver.Combat
{
    // Explicit-call decision boundary, not an automatic AI clock or authority.
    // A caller must still resolve targets, costs and server-approved results.
    public sealed class BattleAiDecisionScheduler
    {
        private readonly BattleSceneRoot battle;
        private readonly CombatAiCatalog catalog;
        private readonly string mapKey;
        private readonly CombatAiUnitQueue queue = new();
        public BattleAiDecisionScheduler(BattleSceneRoot battle, CombatAiCatalog catalog, string mapKey)
        {
            if (battle == null || catalog == null || string.IsNullOrWhiteSpace(mapKey))
                throw new ArgumentException("Battle, AI catalog and map key are required.");
            this.battle = battle; this.catalog = catalog; this.mapKey = mapKey;
        }
        public int Count => queue.Count;
        public bool TrySetUnits(System.Collections.Generic.IReadOnlyList<short> ids) => queue.TrySetUnits(ids);

        public bool TrySelectMonsterTarget(short monsterId,
            System.Collections.Generic.IReadOnlyList<short> candidates,
            Func<CombatUnitSnapshot, bool> inSpawnRange,
            Func<CombatUnitSnapshot, bool> canApproach,
            Func<CombatUnitSnapshot, long> distanceFromMonster,
            out short? targetId, out string reason)
        {
            short? attacker = battle != null && battle.TryGetLatestAttacker(monsterId, out var id)
                ? id : null;
            return TrySelectMonsterTarget(monsterId, candidates, attacker, inSpawnRange,
                canApproach, distanceFromMonster, out targetId, out reason);
        }

        // User-confirmed priority, not a recovered server target formula.
        // Caller supplies the INITIAL-position range and confirmed approach/distance
        // queries. This never changes AP, targets, paths, queue or attack history.
        public bool TrySelectMonsterTarget(short monsterId,
            System.Collections.Generic.IReadOnlyList<short> candidates, short? latestAttackerId,
            Func<CombatUnitSnapshot, bool> inSpawnRange,
            Func<CombatUnitSnapshot, bool> canApproach,
            Func<CombatUnitSnapshot, long> distanceFromMonster,
            out short? targetId, out string reason)
        {
            targetId = null; reason = string.Empty;
            if (battle == null || !battle.isActiveAndEnabled
                || !battle.TryGetUnit(monsterId, out var monster) || !monster.Alive || !monster.Enemy)
            { reason = "활성 몬스터가 필요합니다."; return false; }
            if (candidates == null || inSpawnRange == null || canApproach == null || distanceFromMonster == null)
            { reason = "최초 위치 범위·접근·거리 정보가 필요합니다."; return false; }
            var unique = new System.Collections.Generic.HashSet<short>();
            foreach (var id in candidates)
                if (!unique.Add(id)) { reason = "중복 대상 목록입니다."; return false; }

            short? nearest = null; long best = long.MaxValue; var tied = false;
            foreach (var id in candidates)
            {
                if (!battle.TryGetUnit(id, out var candidate) || !candidate.Alive
                    || candidate.Enemy == monster.Enemy || !inSpawnRange(candidate) || !canApproach(candidate))
                    continue;
                if (latestAttackerId == id) { targetId = id; return true; }
                var distance = distanceFromMonster(candidate);
                if (distance < 0) { reason = "거리 정보가 유효하지 않습니다."; return false; }
                if (!nearest.HasValue || distance < best)
                { nearest = id; best = distance; tied = false; }
                else if (distance == best) tied = true;
            }
            if (tied) { reason = "동거리 대상의 우선순위가 미확정입니다."; return false; }
            if (!nearest.HasValue) { reason = "범위 안에 접근 가능한 적이 없습니다."; return false; }
            targetId = nearest; return true;
        }

        public bool TryStep(int roll, out short unitId, out CombatAiRule rule,
            out CombatAiAction action, out string reason)
        {
            unitId = default; rule = null; action = default; reason = string.Empty;
            if (roll < 0 || roll >= 100) { reason = "AI 난수는 0~99여야 합니다."; return false; }
            if (battle == null || !battle.isActiveAndEnabled) { reason = "전투가 비활성 상태입니다."; return false; }
            if (!queue.TryNext(out unitId)) { reason = "AI 큐가 비어 있습니다."; return false; }
            if (!battle.TryGetUnit(unitId, out var unit) || !unit.Alive
                || !CombatActionPoints.IsReady(unit.Ap,unit.MaxAp) || battle.IsMovementInProgress(unitId))
            { reason = "해당 유닛은 행동 준비 상태가 아닙니다."; return false; }
            if (!catalog.TryGetUniqueActiveRule(mapKey, unitId, out rule))
            { reason = "활성 AI 규칙이 없거나 중복되어 있습니다."; return false; }
            if (!rule.HasSupportedMode)
            { reason = "AI 이동 모드의 의미가 미확정입니다."; return false; }
            if (!rule.TrySelectAction(roll, out action))
            { reason = "누적 가중치에서 선택되는 행동이 없습니다."; return false; }
            if (action.Kind != "ATTACK" && action.Kind != "MAGIC" && action.Kind != "REST")
            { action = default; reason = "AI 행동 토큰이 미해석 상태입니다."; return false; }
            return true; // Candidate only: no AP/HP/MP, packet or movement side effects.
        }
    }
}
