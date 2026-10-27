using UnityEngine;
using DarkSaver.Jobs;
using DarkSaver.UI;
using DarkSaver.Network;

namespace DarkSaver.Combat
{
    public sealed partial class BattleSceneRoot : MonoBehaviour
    {
        private const short PlayerId = 100;
        private short EnemyId = 800;
        public int PlayerJobId { get; private set; }
        public int EnemyJobId { get; private set; }
        [SerializeField] private JobCatalog[] traversalCatalogs = System.Array.Empty<JobCatalog>();
        internal System.Collections.Generic.IReadOnlyList<JobCatalog> TraversalCatalogsForPreparation
            => System.Array.AsReadOnly(traversalCatalogs ?? System.Array.Empty<JobCatalog>());

        public bool ConfigureTraversalCatalogs(System.Collections.Generic.IReadOnlyList<JobCatalog> catalogs)
        {
            if (GridConfigured || catalogs == null || catalogs.Count == 0) return false;
            var copy = new JobCatalog[catalogs.Count];
            var unique = new System.Collections.Generic.HashSet<JobCatalog>();
            for (var i = 0; i < copy.Length; i++)
            {
                if (catalogs[i] == null || !unique.Add(catalogs[i])) return false;
                copy[i] = catalogs[i];
            }
            traversalCatalogs = copy; return true;
        }

        public bool TryGetTroopColumn(short unitId, out int column)
        {
            column = -1;
            return TryGetUnit(unitId, out var unit)
                && CombatTerrainTraversal.TryGetTroopColumn(unit.JobId, traversalCatalogs, out column);
        }

        public bool TryGetTerrainMovement(short unitId, int normalizedAttribute, out int value)
        {
            value = 0;
            return TryGetTroopColumn(unitId, out var column)
                && CombatTerrainTraversal.TryGetMovementValue(normalizedAttribute, column, out value);
        }
        public int PlayerAttackApCost { get { EnsureInitialized(); return flow.GetAttackApCost(PlayerId); } }
        public int GetUnitAttackApCost(short unitId)
        {
            EnsureInitialized(); return world.TryGet(unitId, out _) ? flow.GetAttackApCost(unitId) : 0;
        }
        public bool ConfigurePhysicalAccuracy(short unitId, double accuracyPercent, double evasionPercent)
        {
            EnsureInitialized(); return flow.ConfigurePhysicalAccuracy(unitId, accuracyPercent, evasionPercent);
        }
        public int EnemyAttackApCost { get { EnsureInitialized(); return flow.GetAttackApCost(EnemyId); } }
        public int SelectedMagicApCost { get { EnsureInitialized(); return flow.GetMagicApCost(CombatServerBoundary.PrototypeMagicId); } }
        public bool ConfigurePlayerJob(int jobId)
        {
            EnsureInitialized();
            if (jobId <= 0 || !flow.ConfigureAttackApCostForJob(PlayerId, jobId)) return false;
            PlayerJobId = jobId;
            world.Get(PlayerId).JobId = jobId;
            return true;
        }
        // Same native base columns as BattleEnemyDefinition. No level/growth formula.
        // Apply before restoring saved vitals and before binding map occupancy.
        public bool ConfigurePlayerJobBaseStats()=>ConfigurePlayerJobBaseStats(null);

        public bool ConfigurePlayerJobBaseStats(DarkSaver.Core.NativeCharacterProfile profile,DarkSaver.Items.ItemCatalog equipmentCatalog=null)
        {
            EnsureInitialized();
            if (GridConfigured || PlayerJobId <= 0 || (profile!=null && (!profile.IsValid || profile.jobId!=PlayerJobId))) return false;
            JobCatalogEntry entry = null;
            foreach (var catalog in traversalCatalogs ?? System.Array.Empty<JobCatalog>())
                if (catalog != null && catalog.TryGet(PlayerJobId, out var candidate))
                {
                    if (entry != null) return false;
                    entry = candidate;
                }
            if (entry == null) return false;
            var hp = entry.GetNumericValue(2); var mp = entry.GetNumericValue(4);
            var attack = entry.GetNumericValue(6); var defense = entry.GetNumericValue(8);
            var magicAttack=entry.GetNumericValue(10);var magicDefense=entry.GetNumericValue(12);
            if (hp <= 0 || hp > int.MaxValue || mp < 0 || mp > int.MaxValue
                || attack < 0 || attack > int.MaxValue || defense < 0 || defense > int.MaxValue) return false;
            attack+=profile?.allocatedAttack ?? 0; defense+=profile?.allocatedDefense ?? 0;
            magicAttack+=profile?.allocatedMagicAttack ?? 0;magicDefense+=profile?.allocatedMagicDefense ?? 0;
            if(attack>int.MaxValue || defense>int.MaxValue || magicAttack<0 || magicDefense<0 || magicAttack>int.MaxValue || magicDefense>int.MaxValue) return false;
            if(TemporaryLocalRulesEnabled && profile!=null && profile.HasEquippedItems)
            {
                if(!DarkSaver.Items.UnresolvedEquipmentServerRules.TryResolve(profile,entry,equipmentCatalog,out var equipped))return false;
                hp=equipped.Hp;mp=equipped.Mp;attack=equipped.Attack;defense=equipped.Defense;
                magicAttack=equipped.MagicAttack;magicDefense=equipped.MagicDefense;
            }
            // An unavailable provisional conversion must not block native base stats.
            if (TemporaryLocalRulesEnabled && entry.CategoryLabel == "player_job"
                && UnresolvedJobActionCosts.TryGetAttackApCost(entry, out var temporaryCost))
                flow.ConfigureTemporaryJobAttackCost(PlayerJobId, temporaryCost);
            var player = world.Get(PlayerId);
            player.Hp = player.MaxHp = (int)hp; player.Mp = player.MaxMp = (int)mp;
            player.Attack = (int)attack; player.Defense = (int)defense;
            player.HasMagicStats=true;player.MagicAttack=(int)magicAttack;player.MagicDefense=(int)magicDefense;
            return true;
        }
        private readonly CombatWorld world = new();
        private readonly System.Collections.Generic.Dictionary<short, System.Collections.Generic.HashSet<short>> learnedMagicIds = new();

        public bool ConfigureUnitLearnedMagic(short unitId, System.Collections.Generic.IReadOnlyList<short> ids)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || !world.TryGet(unitId, out var unit) || unit.Enemy || ids == null) return false;
            var staged = new System.Collections.Generic.HashSet<short>();
            foreach (short id in ids) if (id <= 0 || !staged.Add(id)) return false;
            learnedMagicIds[unitId] = staged; return true;
        }

        public bool ConfigureFormationLearnedMagic(BattleFormationSnapshot formation)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || formation == null) return false;
            var staged = new System.Collections.Generic.Dictionary<short, System.Collections.Generic.HashSet<short>>();
            foreach (var member in formation.Members)
            {
                if (!world.TryGetSnapshot(member.Unit.Id, out var unit) || unit.JobId != member.Unit.JobId) return false;
                var ids = new System.Collections.Generic.HashSet<short>();
                foreach (var id in member.LearnedMagicIds) if (id <= 0 || !ids.Add(id)) return false;
                staged.Add(unit.Id, ids);
            }
            foreach (var entry in staged) learnedMagicIds[entry.Key] = entry.Value;
            return true;
        }

        // Unknown learning metadata stays unknown; a confirmed empty list is restrictive.
        public bool IsLearnedMagicAllowed(short casterId, int magicId) =>
            !learnedMagicIds.TryGetValue(casterId, out var ids)
                || (magicId > 0 && magicId <= short.MaxValue && ids.Contains((short)magicId));
        private readonly System.Collections.Generic.Dictionary<short, (short Raw, byte Menu, byte Render)> tickMenus = new();

        public bool ApplyTickMenu(short unitId, short absoluteTick, short rawFlags)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || !world.TryGet(unitId, out var unit)
                || unit.LifeState == UnitLifeState.Removed || !unit.ActionPoints.ApplyAbsolute(absoluteTick)) return false;
            // Native +0x10C is replaced, not OR-accumulated. +0x100 mirrors raw bits1/16
            // as 0x10/0x20; do not conflate them with native life/death state here.
            var raw = unchecked((byte)rawFlags);
            var menu = (byte)(((raw & 1) << 1) | ((raw & 2) << 3) | ((raw & 4) << 1)
                | ((raw & 8) << 2) | ((raw & 16) >> 4) | ((raw & 32) >> 3) | (raw & 0xC0));
            var render = (byte)(((raw & 1) << 4) | ((raw & 16) << 1));
            tickMenus[unitId] = (rawFlags, menu, render); return true;
        }

        public bool TryGetTickMenu(short unitId, out short rawFlags, out byte menuBits)
        {
            rawFlags = 0; menuBits = 0;
            if (!TryGetUnit(unitId, out var unit) || unit.LifeState == UnitLifeState.Removed
                || !tickMenus.TryGetValue(unitId, out var value)) return false;
            rawFlags = value.Raw; menuBits = value.Menu; return true;
        }

        // Only the two bits written by SetTickMenu, not the complete native +0x100.
        // Their visual/action meaning is unconfirmed; never infer death, buffs or AI.
        public bool TryGetTickMenuRenderBits(short unitId, out byte renderBits)
        {
            renderBits = 0;
            if (!TryGetUnit(unitId, out var unit) || unit.LifeState == UnitLifeState.Removed
                || !tickMenus.TryGetValue(unitId, out var value)) return false;
            renderBits = value.Render; return true;
        }
        private CombatOccupancyGrid occupancy;
        private short? selectedUnitId;
        public bool GridConfigured => occupancy != null;
        public short PrimaryEnemyId => EnemyId;
        public bool MapPresentationActive { get; private set; }
        public void SetMapPresentationActive(bool value) => MapPresentationActive = value && occupancy != null;
        private bool prototypeHudVisible = GameConstants.BattlePrototypeHudDefaultVisible;
        public bool IsPrototypeHudVisible => TemporaryLocalRulesEnabled && (!MapPresentationActive || prototypeHudVisible);
        public void SetPrototypeHudVisible(bool visible) => prototypeHudVisible = visible;
        public Rect BattleHudRect => IsPrototypeHudVisible ? PrototypeBattleHudRect : default;
        private Rect PrototypeBattleHudRect => MapPresentationActive
            ? new Rect(Rendering.GameScreen.Width - 240, Rendering.GameScreen.Height - 278, 220, 258)
            : new Rect(Rendering.GameScreen.Width * .5f - 180f, Rendering.GameScreen.Height * .5f - 145f, 360f, 290f);
        public bool IsUnitInformationCompact { get; private set; }
        public Rect UnitInfoRect => new Rect(12, Rendering.GameScreen.Height - (IsUnitInformationCompact ? 105 : 233),
            120, IsUnitInformationCompact ? 85 : 213);
        public bool TrySetUnitInformationCompact(bool compact)
        {
            if (!TryGetInspectedUnit(out _) || IsUnitInformationCompact == compact) return false;
            IsUnitInformationCompact = compact; return true;
        }
        public bool IsAlliedRosterVisible { get; private set; } = true;
        public bool IsAlliedRosterMinimized { get; private set; }
        public void CloseAlliedRoster() { IsAlliedRosterVisible = false; IsAlliedRosterMinimized = false; }
        public void OpenAlliedRoster() { ClearUnitInspection(); IsAlliedRosterVisible = true; IsAlliedRosterMinimized = false; }
        public void MinimizeAlliedRoster() { if (IsAlliedRosterVisible) IsAlliedRosterMinimized = true; }
        public Rect AlliedRosterRect
        {
            get
            {
                if (!IsAlliedRosterVisible) return default;
                if (IsAlliedRosterMinimized) return new Rect(12, 12, 33, 22);
                var commanders = GetAlliedRosterCommanders();
                return commanders.Count >= 1 && commanders.Count <= 4
                    ? new Rect(12, 12, 120, 44 + 41 * commanders.Count) : new Rect(12, 12, 180, 110);
            }
        }
        private InGameUi ui;
        public bool DefeatAchieved
        {
            get
            {
                EnsureInitialized();
                var hasCommander = false;
                foreach (var unit in world.Units)
                {
                    if (unit.Enemy || unit.Id < 0 || unit.Id % 10 != 0) continue;
                    hasCommander = true;
                    if (unit.Alive) return false;
                }
                return hasCommander;
            }
        }
        // A removed monster or HP0 is not a native battle-end notification.
        public bool IsBattleFinished => TemporaryLocalRulesEnabled && !IsPersistentTraining && (VictoryAchieved || DefeatAchieved);
        public Rect OutcomeRect
        {
            get
            {
                const float width = 434f;
                return new Rect((Rendering.GameScreen.Width - width) * .5f,
                    (Rendering.GameScreen.Height - 344f) * .5f, width, 344f);
            }
        }
        public bool OutcomeAcknowledged { get; private set; }
        public bool DefersOutcomeToScenario { get; private set; }
        public bool ShowsEncounterOutcome=>MapPresentationActive && IsBattleFinished && !OutcomeAcknowledged && !DefersOutcomeToScenario;
        public void SetScenarioOutcomeDeferred(bool value) { DefersOutcomeToScenario=value;if(!value)OutcomeAcknowledged=false; }
        public string OutcomeAllies => BuildOutcomeNames(false);
        public string OutcomeEnemies => BuildOutcomeNames(true);
        public string OutcomeGrantedExperience => IsBattleFinished && defeatExperienceGranted
            ? defeatExperience.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
        public string OutcomeSummary => !IsBattleFinished ? string.Empty : VictoryAchieved ? "승리" : "패배";
        private string BuildOutcomeNames(bool enemies)
        {
            if (!IsBattleFinished) return string.Empty;
            var units = new System.Collections.Generic.List<CombatUnit>(world.Units);
            units.Sort((a, b) => a.Id.CompareTo(b.Id));
            var names = new System.Text.StringBuilder();
            foreach (var unit in units)
            {
                if (unit.Enemy != enemies) continue;
                if (names.Length > 0) names.Append('\n');
                names.Append((unit.Name ?? string.Empty).Replace('\r', ' ').Replace('\n', ' '));
            }
            return names.ToString();
        }
        public bool TryAcknowledgeOutcome()
        {
            if (!isActiveAndEnabled || !IsBattleFinished) return false;
            OutcomeAcknowledged = true; return true;
        }
        private short? inspectedUnitId;
        public bool TryInspectUnit(short unitId)
        {
            if (!isActiveAndEnabled || !TryGetUnit(unitId, out var unit) || unit.LifeState != UnitLifeState.Active) return false;
            CloseAlliedRoster(); inspectedUnitId = unitId; return true;
        }
        public void ClearUnitInspection() { inspectedUnitId = null; IsUnitInformationCompact = false; }
        public bool TryGetInspectedUnit(out CombatUnitSnapshot unit)
        {
            unit = default;
            return isActiveAndEnabled && MapPresentationActive && inspectedUnitId.HasValue
                && TryGetUnit(inspectedUnitId.Value, out unit) && unit.LifeState == UnitLifeState.Active;
        }
        public bool IsPointerOverBattleHud(Vector2 screenPoint)
        {
            if (!Rendering.GameScreen.TryScreenToGui(screenPoint, out var point)) return true;
            var presentation = GetComponent<BattleMapPresentation>();
            return BattleHudRect.Contains(point) || (TryGetInspectedUnit(out _) && UnitInfoRect.Contains(point))
                || (MapPresentationActive && !IsBattleFinished && AlliedRosterRect.Contains(point))
                || (presentation != null && presentation.TryGetActionMenuRect(out var menu) && menu.Contains(point))
                || (presentation != null && presentation.RequiresMagicDirection && presentation.MagicDirectionRect.Contains(point))
                || (IsBattleFinished && !OutcomeAcknowledged && OutcomeRect.Contains(point));
        }
        public short? SelectedUnitId => selectedUnitId;
        public event System.Action<short?> UnitSelectionChanged;
        public event System.Action<CombatMovementStep> MovementStepApplied;
        private readonly System.Collections.Generic.Dictionary<short, CombatMovementStep> movementTickets = new();
        private sealed class MovementRoute
        {
            public System.Collections.Generic.IReadOnlyList<CombatGridCell> Cells;
            public int Index;
            public int Phase;
            public CombatMovementStep Ticket;
        }
        private readonly System.Collections.Generic.Dictionary<short, MovementRoute> movementRoutes = new();
        private double movementTickDebt;
        private readonly System.Collections.Generic.Dictionary<short, int> movementBudgets = new();
        // Original general-path flag image initializer is 1 (0x1001516C).
        // Native lifetime is this battle, not the original process-wide lifetime.
        private bool movementVerticalFirst = true;

        // Final movement budget, NOT AP, raw ATR, or an assumed server unitinfo value.
        public bool ConfigureMovementBudget(short unitId, int budget)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || budget < 0 || budget == int.MaxValue || IsMovementInProgress(unitId)
                || !world.TryGetSnapshot(unitId, out var unit) || !unit.Alive) return false;
            movementBudgets[unitId] = budget; return true;
        }
        public bool IsMovementInProgress(short unitId) =>
            movementTickets.ContainsKey(unitId) || movementRoutes.ContainsKey(unitId);

        private readonly System.Collections.Generic.Dictionary<short, CombatGridCell> initialGridPositions = new();

        public bool TryGetInitialGridPosition(short unitId, out CombatGridCell cell)
        {
            cell = default;
            return TryGetUnit(unitId, out var unit) && unit.LifeState != UnitLifeState.Removed
                && initialGridPositions.TryGetValue(unitId, out cell);
        }

        // Explicit return request only. Target scanning/rest timing remain caller-owned.
        public bool TryReturnMonsterToInitialPosition(short unitId, out string reason)
        {
            reason = string.Empty;
            if (!isActiveAndEnabled || IsBattleFinished || !TryGetUnit(unitId, out var unit)
                || !unit.Enemy || !unit.Alive || !TryGetInitialGridPosition(unitId, out var initial))
            { reason = "살아 있는 몬스터와 최초 배치 정보가 필요합니다."; return false; }
            if (unit.X == initial.X && unit.Y == initial.Y)
            { reason = "이미 최초 위치에 있습니다."; return false; }
            return TryMoveTo(unitId, initial, out reason);
        }

        public bool TryMoveTo(short unitId, CombatGridCell destination, out string reason)
        {
            if(!TemporaryLocalRulesEnabled)
            {reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}
            if (TemporaryLocalRulesEnabled && !processingServerRequest) return TrySubmitLocalRequest(new BattleMoveRequest(unitId,destination.X,destination.Y),out reason);
            EnsureInitialized();
            if (!isActiveAndEnabled || occupancy == null || !world.TryGet(unitId, out var actor) || !actor.Alive)
            { reason = "이동할 유닛과 활성 전투 격자가 필요합니다."; return false; }
            if (IsMovementInProgress(unitId)) { reason = "이동 중입니다."; return false; }
            var hasBudget = movementBudgets.TryGetValue(unitId, out var budget);
            if (!hasBudget && flow.TryGetTemporaryStatus(unitId, TemporaryStatusEffect.MovementReduction, out _, out _))
            { reason = "이동 감소를 적용할 기준 이동력이 미확인입니다."; return false; }
            if (hasBudget) budget = flow.GetEffectiveMovementBudget(unitId, budget);
            var nextTie = movementVerticalFirst;
            System.Collections.Generic.IReadOnlyList<CombatGridCell> path;
            var found = hasBudget
                ? occupancy.TryFindBudgetedPath(unitId, destination, budget, movementVerticalFirst, out path, out nextTie)
                : occupancy.TryFindPath(unitId, destination, out path, allowAlliedTransit: false);
            if (!found || path.Count == 0)
            { reason = "빈 칸으로 연결된 이동 경로가 없습니다."; return false; }
            if (float.IsNaN(GameConstants.BattleMoveTickSeconds) || float.IsInfinity(GameConstants.BattleMoveTickSeconds)
                || GameConstants.BattleMoveTickSeconds <= 0 || GameConstants.BattleCardinalMoveTicks <= 0
                || GameConstants.BattleDiagonalMoveTicks <= 0 || GameConstants.BattleMoveSourceTilePixels <= 0)
            { reason = "이동 표시 시간이 유효하지 않습니다."; return false; }
            var cost = actor.Enemy ? GameConstants.BattleEnemyMoveApCost : GameConstants.BattleAllyMoveApCost;
            if (!TryStartMovementAction(unitId, path[0], cost, out var ticket, out reason)) return false;
            movementRoutes.Add(unitId, new MovementRoute { Cells = path, Ticket = ticket });
            if (hasBudget) movementVerticalFirst = nextTie;
            return true;
        }

        // One shared tick per update, including idle updates. Original GetTickCount
        // keeps <=500ms debt for later calls, but discards a longer stall.
        public bool AdvanceMovement(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return false;
            if (!isActiveAndEnabled) { CancelMovementRoutes(); return false; }
            movementTickDebt += seconds;
            if (movementTickDebt + 1e-8 < GameConstants.BattleMoveTickSeconds) return true;
            movementTickDebt = movementTickDebt > GameConstants.BattleMoveLagResetSeconds
                ? 0 : System.Math.Max(0, movementTickDebt - GameConstants.BattleMoveTickSeconds);
            foreach (var id in new System.Collections.Generic.List<short>(movementRoutes.Keys))
            {
                if (!movementRoutes.TryGetValue(id, out var route)) continue;
                if (IsBattleFinished || !world.TryGet(id, out var actor) || !actor.Alive)
                { CancelMovementStep(id); continue; }
                route.Phase++;
                if (route.Phase < MovementPhaseCount(route.Ticket)) continue;
                if (!TryApplyReservedMovementStep(route.Ticket)) { CancelMovementStep(id); continue; }
                // A handler can cancel/remove this route; never resurrect it.
                if (!movementRoutes.TryGetValue(id, out var current) || current != route) continue;
                route.Phase = 0; route.Index++;
                if (route.Index == route.Cells.Count) { movementRoutes.Remove(id); continue; }
                if (!TryReserveMovementStep(id, route.Cells[route.Index], out var next))
                { CancelMovementStep(id); continue; }
                route.Ticket = next;
            }
            return true;
        }

        private static bool IsDiagonal(CombatMovementStep step) =>
            step.From.X != step.To.X && step.From.Y != step.To.Y;

        private static int MovementPhaseCount(CombatMovementStep step) => IsDiagonal(step)
            ? GameConstants.BattleDiagonalMoveTicks : GameConstants.BattleCardinalMoveTicks;

        // Display-only offset: model coordinates and occupancy change only on confirmation.
        public bool TryGetMovementPresentation(short unitId, out CombatMovementStep step, out float progress)
        {
            step = default; progress = 0;
            if (!isActiveAndEnabled || !movementRoutes.TryGetValue(unitId, out var route)
                || !world.TryGet(unitId, out var actor) || !actor.Alive) return false;
            step = route.Ticket;
            progress = route.Phase * (IsDiagonal(step) ? GameConstants.BattleDiagonalMoveTickPixels
                : GameConstants.BattleCardinalMoveTickPixels) / (float)GameConstants.BattleMoveSourceTilePixels;
            return true;
        }

        public void CancelMovementRoutes()
        {
            foreach (var id in new System.Collections.Generic.List<short>(movementRoutes.Keys)) CancelMovementStep(id);
        }

        // Explicit native action cost supplied by the host, not a recovered MoveCharPath field.
        // This starts ONE action with its first step; the host decides cost granularity.
        // No route execution, default cost, or speculative cancellation refund here.
        public bool TryStartMovementAction(short unitId, CombatGridCell firstDestination, int actionCost,
            out CombatMovementStep ticket, out string reason)
        {
            EnsureInitialized(); ticket = default;
            if (!isActiveAndEnabled || occupancy == null)
            { reason = "전투 격자가 비활성 상태입니다."; return false; }
            if (!world.TryGet(unitId, out var unit) || !unit.Alive || !flow.CanUnitAct(unitId) || IsBattleFinished)
            { reason = "이동 행동을 시작할 수 없는 유닛 또는 종료된 전투입니다."; return false; }
            if (IsMovementInProgress(unitId))
            { reason = "이전 이동 확정을 기다리고 있습니다."; return false; }
            if (actionCost <= 0 || actionCost > unit.ActionPoints.Maximum)
            { reason = "이동 행동 AP 비용이 유효하게 지정되지 않았습니다."; return false; }
            actionCost = flow.GetEffectiveActionCost(unitId, actionCost);
            if (!unit.ActionPoints.CanStart(actionCost))
            { reason = "AP가 최대치에 도달해야 행동할 수 있습니다."; return false; }
            if (!TryReserveMovementStep(unitId, firstDestination, out ticket))
            { reason = "다음 이동 칸을 예약할 수 없습니다."; return false; }
            // Reservation policies are pure; no callback may spend AP during validation.
            if (!unit.ActionPoints.TrySpend(actionCost))
            {
                CancelMovementStep(unitId); ticket = default;
                reason = "이동 AP 승인이 실패했습니다."; return false;
            }
            reason = string.Empty; return true;
        }

        // Authority plumbing only: these methods do not authorize a player action,
        // generate a route, or infer movement AP costs/refunds.
        public bool TryReserveMovementStep(short unitId, CombatGridCell destination, out CombatMovementStep ticket)
        {
            ticket = default;
            if (!isActiveAndEnabled || occupancy == null || !occupancy.TryGetPosition(unitId, out var origin)
                || !occupancy.TryReserveStep(unitId, destination)) return false;
            ticket = new CombatMovementStep(unitId, origin, destination, System.Guid.NewGuid());
            movementTickets[unitId] = ticket; return true;
        }

        public bool CancelMovementStep(short unitId)
        {
            var removed = movementRoutes.Remove(unitId);
            removed = movementTickets.Remove(unitId) || removed;
            return (occupancy != null && occupancy.CancelReservation(unitId)) || removed;
        }

        public bool TryApplyReservedMovementStep(CombatMovementStep step)
        {
            if (!isActiveAndEnabled || occupancy == null
                || !movementTickets.TryGetValue(step.UnitId, out var ticket) || step.ReservationId != ticket.ReservationId
                || !step.From.Equals(ticket.From) || !step.To.Equals(ticket.To)
                || !occupancy.TryGetPosition(step.UnitId, out var origin) || !origin.Equals(step.From)
                || !occupancy.TryGetReservation(step.To, out var reservedBy) || reservedBy != step.UnitId
                || !occupancy.TryCommitStep(step.UnitId)) return false;
            movementTickets.Remove(step.UnitId);
            // Publish after model and occupancy commit; never announce a rejected step.
            MovementStepApplied?.Invoke(step);
            return true;
        }

        // Explicit native encounter setup only: no automatic formation or guessed spawn.
        // Terrain/direction policies are pure queries, not callbacks that modify world state.
        public bool TryConfigureGrid(int width, int height,
            System.Collections.Generic.IReadOnlyList<CombatGridPlacement> placements,
            System.Func<CombatUnitSnapshot, CombatGridCell, bool> terrainAllows,
            System.Func<CombatGridCell, CombatGridCell, bool> directionAllows,
            System.Func<CombatUnitSnapshot, CombatGridCell, bool> placementAllows = null,
            bool preventDiagonalCornerCutting = false,
            System.Func<CombatUnitSnapshot, CombatGridCell, int?> movementCost = null)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || occupancy != null || width <= 0 || height <= 0
                || placements == null || terrainAllows == null || directionAllows == null) return false;
            var expected = new System.Collections.Generic.HashSet<short>();
            foreach (var unit in world.Units) expected.Add(unit.Id);
            if (placements.Count != expected.Count) return false;
            foreach (var placement in placements)
                if (!expected.Remove(placement.UnitId)) return false;
            var candidate = new CombatOccupancyGrid(world, width, height, terrainAllows, directionAllows, placementAllows,
                preventDiagonalCornerCutting, movementCost);
            if (!candidate.TryPlaceUnits(placements)) return false;
            occupancy = candidate;
            foreach (var placement in placements)
                initialGridPositions.Add(placement.UnitId, placement.Cell);
            return true;
        }

        public bool TryGetGridOccupant(CombatGridCell cell, out short unitId)
        {
            unitId = default;
            return occupancy != null && occupancy.TryGetOccupant(cell, out unitId);
        }

        public bool TrySelectUnit(short unitId)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || IsBattleFinished || occupancy == null || !world.TryGetSnapshot(unitId, out var unit)
                || !unit.Alive || unit.Enemy || !occupancy.TryGetPosition(unitId, out var cell)
                || unit.X != cell.X || unit.Y != cell.Y) return false;
            TryInspectUnit(unitId);
            if (selectedUnitId == unitId) return true;
            selectedUnitId = unitId; UnitSelectionChanged?.Invoke(selectedUnitId); return true;
        }

        public bool TrySelectAtCell(CombatGridCell cell) =>
            TryGetGridOccupant(cell, out var unitId) && TrySelectUnit(unitId);

        public bool TryGetSelectedUnit(out CombatUnitSnapshot unit)
        {
            unit = default;
            return isActiveAndEnabled && selectedUnitId.HasValue
                && world.TryGetSnapshot(selectedUnitId.Value, out unit) && unit.Alive && !unit.Enemy;
        }

        public bool CanSelectedUnitTraverse(CombatGridCell cell) =>
            TryGetSelectedUnit(out var unit) && occupancy != null && occupancy.CanTraverse(unit.Id, cell);

        public bool TryFindSelectedMovementPath(CombatGridCell destination,
            out System.Collections.Generic.IReadOnlyList<CombatGridCell> path)
        {
            path = System.Array.Empty<CombatGridCell>();
            if (!TryGetSelectedUnit(out var unit) || occupancy == null || !flow.CanUnitAct(unit.Id)) return false;
            if (!movementBudgets.ContainsKey(unit.Id) && flow.TryGetTemporaryStatus(unit.Id, TemporaryStatusEffect.MovementReduction, out _, out _)) return false;
            return movementBudgets.TryGetValue(unit.Id, out var budget)
                ? occupancy.TryFindBudgetedPath(unit.Id, destination, flow.GetEffectiveMovementBudget(unit.Id, budget), movementVerticalFirst, out path, out _)
                : occupancy.TryFindPath(unit.Id, destination, out path);
        }

        public void ClearUnitSelection()
        {
            if (!selectedUnitId.HasValue) return;
            selectedUnitId = null; UnitSelectionChanged?.Invoke(null);
        }

        public bool TryGetSelectedReachableDestinations(
            out System.Collections.Generic.IReadOnlyList<CombatGridCell> cells)
        {
            cells = System.Array.Empty<CombatGridCell>();
            if (!TryGetSelectedUnit(out var unit) || occupancy == null || !flow.CanUnitAct(unit.Id)) return false;
            if (!movementBudgets.ContainsKey(unit.Id) && flow.TryGetTemporaryStatus(unit.Id, TemporaryStatusEffect.MovementReduction, out _, out _)) return false;
            return movementBudgets.TryGetValue(unit.Id, out var budget)
                ? occupancy.TryGetBudgetedDestinations(unit.Id, flow.GetEffectiveMovementBudget(unit.Id, budget), movementVerticalFirst, out cells)
                : occupancy.TryGetReachableDestinations(unit.Id, out cells);
        }

        private readonly System.Collections.Generic.Dictionary<short,int> fallenMovementBudgets=new();
        private void SynchronizeGridState(short unitId)
        {
            if (!world.TryGetSnapshot(unitId, out var unit)) return;
            // Invalidate pending movement immediately, including manually issued
            // tickets. HP recovery must never make a pre-HP0 ticket valid again.
            if (!unit.Alive) CancelMovementStep(unitId);
            if (unit.LifeState != UnitLifeState.Active)
            { if(movementBudgets.TryGetValue(unitId,out var fallenBudget))fallenMovementBudgets[unitId]=fallenBudget;occupancy?.ReleaseUnit(unitId); movementTickets.Remove(unitId); movementRoutes.Remove(unitId); movementBudgets.Remove(unitId); }
            if (selectedUnitId == unitId && !unit.Alive) ClearUnitSelection();
        }

        public event System.Action<CombatResult> CombatResultApplied;
        public event System.Action<short, int, bool> VitalsDisplayChanged;
        private void OnCombatResultApplied(CombatResult result)
{}        private void PresentCombatResult(CombatResult result)
        {
            if (result.StatusEffect == TemporaryStatusEffect.MovementReduction || result.StatusEffect == TemporaryStatusEffect.Sleep) CancelMovementStep(result.TargetId);
            if (result.BuffSeconds > 0 && world.TryGet(result.TargetId, out var buffTarget))
                status = $"{buffTarget.Name}: 임시 공격 +{result.AttackBonus} / 방어 +{result.DefenseBonus} ({result.BuffSeconds:0.#}초)";
            if (result.Healing > 0 && world.TryGet(result.ActorId, out var healer) && world.TryGet(result.TargetId, out var healed))
                status = $"{healer.Name} → {healed.Name}: HP {result.Healing} 회복";
            if (TemporaryLocalRulesEnabled && world.TryGet(result.ActorId, out var actor)
                && world.TryGet(result.TargetId, out var target) && actor.Enemy != target.Enemy)
            {
                latestAttackers[result.TargetId] = result.ActorId;
                status = result.Hit ? $"{actor.Name} → {target.Name}: {result.Damage} 피해" : $"{actor.Name} → {target.Name}: MISS";
            }
            if (world.TryGet(result.TargetId, out var affected))
            {
                if (result.ActionCostReduction > 0) status = $"{affected.Name}: 퀵 · 행동 소모 {result.ActionCostReduction}% 감소";
                else if (result.StatusEffect == TemporaryStatusEffect.Silence) status = $"{affected.Name}: 뮤트";
                else if (result.StatusEffect == TemporaryStatusEffect.Sleep) status = $"{affected.Name}: 수면";
                else if (result.StatusEffect == TemporaryStatusEffect.Wake) status = $"{affected.Name}: 각성";
                else if (result.StatusEffect == TemporaryStatusEffect.Poison) status = $"{affected.Name}: 독";
                else if (result.StatusEffect == TemporaryStatusEffect.CurePoison) status = $"{affected.Name}: 해독";
                else if (result.StatusEffect == TemporaryStatusEffect.MovementReduction) status = $"{affected.Name}: 이동력 {result.StatusPower}% 감소";
            }
            SynchronizeGridState(result.ActorId); SynchronizeGridState(result.TargetId);
            // The existing opt-in base reward also belongs to the current local
            // attack/magic path, not only the legacy diagnostic round buttons.
            CombatResultApplied?.Invoke(result);
        }
        [SerializeField] private short[] victoryTargetInstanceIds;
        [SerializeField] private BattleObjectiveDefinition victoryObjective;
        public bool VictoryAchieved => TemporaryLocalRulesEnabled && !IsPersistentTraining && (victoryObjective != null
            ? victoryObjective.IsSatisfied(world)
            : BattleVictoryRules.IsSatisfied(world, victoryTargetInstanceIds));
        public string VictoryConditionDescription
        {
            get
            {
                EnsureInitialized();
                if (!TemporaryLocalRulesEnabled) return string.Empty;
                if (victoryObjective != null) return victoryObjective.Description ?? string.Empty;
                if (victoryTargetInstanceIds == null || victoryTargetInstanceIds.Length == 0)
                    return "적 전원 처치 (로컬 규칙)";
                var names = new System.Collections.Generic.List<string>();
                foreach (var id in victoryTargetInstanceIds)
                {
                    if (!world.TryGet(id, out var target) || !target.Enemy) return string.Empty;
                    names.Add((target.Name ?? string.Empty).Replace('\r', ' ').Replace('\n', ' '));
                }
                return string.Join(", ", names) + " 처치";
            }
        }
        private CombatFlow flow;
        private string status = string.Empty;
        private bool useDefeatExperience;
        private uint defeatExperience;
        private bool defeatExperienceGranted;
        public BattleAttackPresentation? LastAttackPresentation { get; private set; }
        public event System.Action<BattleAttackPresentation> AttackPresentationStarted;

        public int PlayerHp { get { EnsureInitialized(); return world.Get(PlayerId).Hp; } }
        public int PlayerMaxHp { get { EnsureInitialized(); return world.Get(PlayerId).MaxHp; } }
        public int PlayerMaxMp { get { EnsureInitialized(); return world.Get(PlayerId).MaxMp; } }
        public UnitLifeState PlayerLifeState { get { EnsureInitialized(); return world.Get(PlayerId).LifeState; } }
        public UnitLifeState EnemyLifeState { get { EnsureInitialized(); return world.Get(EnemyId).LifeState; } }
        public int EnemyHp => world.Get(EnemyId).Hp;
        public int PlayerMp { get { EnsureInitialized(); return world.Get(PlayerId).Mp; } }
        public int PlayerAp { get { EnsureInitialized(); return world.Get(PlayerId).ActionPoints.Current; } }
        public int EnemyAp { get { EnsureInitialized(); return world.Get(EnemyId).ActionPoints.Current; } }
        public bool ConfigureActionPointCosts(int attack, int magic)
        {
            EnsureInitialized(); return flow.ConfigureActionPointCosts(attack, magic);
        }

        public bool AdvanceActionPoints(float seconds)
        {
            if (TemporaryLocalRulesEnabled && !processingServerRequest) return TrySubmitLocalRequest(new BattleClockRequest(seconds),out _);
            EnsureInitialized();
            // Primary death alone does not finish a battle with another living commander.
            if (IsBattleFinished) return true;
            return flow.AdvanceActionPoints(seconds);
        }

        public bool TryGetUnit(short id, out CombatUnitSnapshot snapshot)
        {
            EnsureInitialized(); return world.TryGetSnapshot(id, out snapshot);
        }

        public bool TryGetGroup(short memberId, out CombatGroupSnapshot group)
        {
            EnsureInitialized(); return world.TryGetGroup(memberId, out group);
        }

        public System.Collections.Generic.IReadOnlyList<CombatGroupSnapshot> GetAlliedGroups()
        {
            EnsureInitialized();
            var ids = new System.Collections.Generic.SortedSet<short>();
            foreach (var unit in world.Units)
                if (!unit.Enemy && unit.Id >= 0) ids.Add((short)(unit.Id / 10 * 10));
            var groups = new System.Collections.Generic.List<CombatGroupSnapshot>();
            foreach (var id in ids)
                if (world.TryGetGroup(id, out var group) && !group.Enemy) groups.Add(group);
            return groups.AsReadOnly();
        }

        public System.Collections.Generic.IReadOnlyList<CombatUnitSnapshot> GetAlliedRosterCommanders()
        {
            var commanders = new System.Collections.Generic.List<CombatUnitSnapshot>();
            foreach (var group in GetAlliedGroups())
                if (TryGetUnit(group.CommanderId, out var unit) && unit.LifeState != UnitLifeState.Removed)
                    commanders.Add(unit);
            return commanders.AsReadOnly();
        }

        public bool CanSelectRosterUnit(short unitId)
        {
            var presentation = GetComponent<BattleMapPresentation>();
            return isActiveAndEnabled && MapPresentationActive && !IsBattleFinished && presentation != null
                && presentation.isActiveAndEnabled && presentation.IsBound
                && presentation.GetUnitPortrait(unitId) != null && TryGetUnit(unitId, out var unit)
                && unit.Alive && !unit.Enemy && occupancy != null
                && occupancy.TryGetPosition(unitId, out var cell) && unit.X == cell.X && unit.Y == cell.Y;
        }

        public bool TrySelectRosterUnit(short unitId)
        {
            if (!CanSelectRosterUnit(unitId) || !TrySelectUnit(unitId)) return false;
            var presentation = GetComponent<BattleMapPresentation>();
            // Selection is not an action. Drop the previous target/mode without sending a packet.
            presentation.TryOpenActionMenu();
            presentation.TryChooseCommand(BattleMapCommand.Cancel);
            return presentation.TryOpenActionMenu();
        }

        // Formation gate only. Generic registration below retains the original
        // nine-soldier group convention and does not impose allied UI limits on enemies.
        public bool TryRegisterAlliedFormationGroup(System.Collections.Generic.IReadOnlyList<CombatUnit> units, out string reason)
        {
            EnsureInitialized(); reason = string.Empty;
            if (!isActiveAndEnabled || occupancy != null || units == null || units.Count == 0
                || units[0] == null || units[0].Id < 0)
            { reason = "편성은 격자 배치 전에 유효한 아군 그룹으로 등록해야 합니다."; return false; }
            var commander = (short)(units[0].Id / 10 * 10);
            var groups = new System.Collections.Generic.HashSet<short>();
            var soldiers = 0;
            foreach (var existing in world.Units)
            {
                if (existing.Enemy || existing.Id < 0) continue;
                var group = (short)(existing.Id / 10 * 10); groups.Add(group);
                if (group == commander && existing.Id != commander) soldiers++;
            }
            if (!groups.Contains(commander) && groups.Count >= GameConstants.BattleMaximumAlliedGroups)
            { reason = $"아군은 최대 {GameConstants.BattleMaximumAlliedGroups}그룹까지 편성할 수 있습니다."; return false; }
            foreach (var unit in units)
            {
                if (unit == null || unit.Enemy || unit.Id < 0 || unit.Id / 10 * 10 != commander)
                { reason = "편성 입력은 같은 지휘관의 아군만 포함해야 합니다."; return false; }
                if (unit.Id != commander) soldiers++;
            }
            if (soldiers > GameConstants.BattleMaximumAlliedSoldiersPerGroup)
            { reason = $"그룹당 부하는 최대 {GameConstants.BattleMaximumAlliedSoldiersPerGroup}명입니다."; return false; }
            if (!TryRegisterGroup(units))
            { reason = "중복 ID·지휘관·능력치 또는 전투 등록 조건이 유효하지 않습니다."; return false; }
            return true;
        }

        public bool TryRegisterAlliedFormation(BattleFormationSnapshot formation, out string reason)
        {
            EnsureInitialized(); reason = "편성 등록 조건이 유효하지 않습니다.";
            if (!isActiveAndEnabled || occupancy != null || formation == null || formation.Members.Count == 0) return false;
            var incoming = new System.Collections.Generic.List<CombatUnit>();
            foreach (var member in formation.Members)
            {
                var unit = member.Unit; var cost = GameConstants.GetBattleAttackApCost(unit.JobId);
                if (cost < 0 || cost > GameConstants.GetBattleMaximumAp(unit.Enemy)) return false;
                incoming.Add(new CombatUnit { Id = unit.Id, JobId = unit.JobId, Name = unit.Name, Enemy = unit.Enemy,
                    X = member.Position.X, Y = member.Position.Y, Hp = unit.Hp, MaxHp = unit.MaxHp,
                    Mp = unit.Mp, MaxMp = unit.MaxMp, Attack = unit.Attack, Defense = unit.Defense, HasMagicAttack=unit.HasMagicAttack,HasMagicDefense=unit.HasMagicDefense,MagicAttack=unit.MagicAttack,MagicDefense=unit.MagicDefense, Experience = unit.Experience });
            }
            if (!world.TryRegisterAlliedFormation(incoming)) return false;
            foreach (var unit in incoming) flow.ConfigureAttackApCostForJob(unit.Id, unit.JobId);
            reason = string.Empty; return true;
        }

        public bool TryCaptureFormationReturn(BattleFormationSnapshot formation, out BattleFormationReturnSnapshot returned, out string reason)
        {
            EnsureInitialized(); returned = null; reason = "출전 스냅샷과 모든 출전 결과가 필요합니다.";
            if (formation == null) return false;
            var observed = new System.Collections.Generic.List<CombatUnitSnapshot>();
            foreach (var member in formation.Members)
            { if (!world.TryGetSnapshot(member.Unit.Id, out var unit)) return false; observed.Add(unit); }
            return BattleFormationReturn.TryCapture(formation, observed, out returned, out reason);
        }

        public bool TryRegisterGroup(System.Collections.Generic.IReadOnlyList<CombatUnit> units)
        {
            EnsureInitialized();
            if (units == null || !isActiveAndEnabled || occupancy != null) return false;
            foreach (var unit in units)
            {
                if (unit == null || unit.JobId < 0) return false;
                var cost = GameConstants.GetBattleAttackApCost(unit.JobId);
                if (cost < 0 || cost > GameConstants.GetBattleMaximumAp(unit.Enemy)) return false;
            }
            if (!world.TryRegisterGroup(units)) return false;
            foreach (var unit in units) flow.ConfigureAttackApCostForJob(unit.Id, unit.JobId);
            return true;
        }
        public string Status => status;
        public uint PlayerExperience
        {
            get { EnsureInitialized(); return world.Get(PlayerId).Experience; }
        }

        public string ClearedScenarioKey
        {
            get
            {
                EnsureInitialized();
                return TemporaryLocalRulesEnabled && !IsPersistentTraining && victoryObjective != null ? victoryObjective.GetClearedScenarioKey(world) : string.Empty;
            }
        }

        public bool RestorePlayerExperience(uint absoluteExperience) =>
            ApplyExperience(new CombatExperienceResult(PlayerId, absoluteExperience));

        public bool TrySetPlayerName(string name)
        {
            if (!DarkSaver.Core.NativeCharacterProfile.IsValidName(name)) return false;
            EnsureInitialized(); world.Get(PlayerId).Name = name; return true;
        }

        public string PlayerName { get { EnsureInitialized(); return world.Get(PlayerId).Name; } }

        // Session restoration only. This is not a healing or resurrection action.
        public bool TryRestorePlayerVitals(int absoluteHp, int absoluteMp)
        {
            EnsureInitialized();
            var actor = world.Get(PlayerId);
            if (actor.LifeState != UnitLifeState.Active || absoluteHp < 0 || absoluteHp > actor.MaxHp
                || absoluteMp < 0 || absoluteMp > actor.MaxMp) return false;
            actor.Hp = absoluteHp;
            actor.Mp = absoluteMp;
            SynchronizeGridState(PlayerId);
            return true;
        }

        public bool ApplyHp(short unitId, int absoluteHp)
        {
            EnsureInitialized();
            if (!flow.TryApplyHp(unitId, absoluteHp)) return false;
            SynchronizeGridState(unitId); return true;
        }

        internal bool CanApplyItemRecovery(short targetId,int hp,int mp,int range,int cost)
        {
            EnsureInitialized();
            return isActiveAndEnabled && !IsBattleFinished && occupancy!=null && !IsMovementInProgress(100)
                && !IsMovementInProgress(targetId) && flow.CanApplyItemRecovery(100,targetId,hp,mp,range,cost);
        }
        internal bool TryApplyItemRecovery(short targetId,int hp,int mp,int range,int cost,int magicId)
        {
            if(!CanApplyItemRecovery(targetId,hp,mp,range,cost) || !flow.TryApplyItemRecovery(100,targetId,hp,mp,range,cost))return false;
            // Presentation only: no learned-spell grant, magic request, or second MP/AP charge.
            MagicPresentationStarted?.Invoke(new CombatRequest(100,targetId,CombatActionType.Magic,magicId));
            return true;
        }

        internal bool CanApplyItemMagic(short targetId, int magicId, int range, int cost)
        {
            EnsureInitialized();
            return isActiveAndEnabled && !IsBattleFinished && occupancy != null && !IsMovementInProgress(100)
                && !IsMovementInProgress(targetId) && flow.CanApplyItemMagic(100, targetId, magicId, range, cost);
        }
        internal bool TryApplyItemMagic(short targetId, int magicId, int range, int cost)
        {
            if (!CanApplyItemMagic(targetId, magicId, range, cost) || !flow.TryApplyItemMagic(100, targetId, magicId, range, cost)) return false;
            MagicPresentationStarted?.Invoke(new CombatRequest(100, targetId, CombatActionType.Magic, magicId)); return true;
        }

        public bool ApplyMp(short unitId, int absoluteMp)
        {
            EnsureInitialized();
            return flow.TryApplyMp(unitId, absoluteMp);
        }

        // Native presentation guard, not an original target/skill approval formula.
        // HP0/corpse may still receive delayed effects; removed records cannot render.
        public bool CanReceiveEffect(short unitId)
        {
            EnsureInitialized();
            return world.TryGet(unitId, out var unit) && unit.LifeState != UnitLifeState.Removed;
        }

        private readonly System.Collections.Generic.Dictionary<short, short> latestAttackers = new();

        public bool TryGetLatestAttacker(short unitId, out short attackerId)
        {
            attackerId = default;
            return CanReceiveEffect(unitId) && latestAttackers.TryGetValue(unitId, out attackerId);
        }

        public bool ApplyAttackPresentation(short targetId, short attackerId, short effect)
        {
            if (!CanReceiveEffect(attackerId) || !CanReceiveEffect(targetId)) return false;
            // Accepted attack notification, not anticipated input or proof of HP damage.
            // Store before publishing so AI/event consumers observe the new attacker.
            if (world.Get(attackerId).Enemy != world.Get(targetId).Enemy)
                latestAttackers[targetId] = attackerId;
            return RecordAttackPresentation(targetId, attackerId, effect, false);
        }

        private bool RecordAttackPresentation(short targetId, short attackerId, short effect, bool anticipation)
        {
            if (!CanReceiveEffect(attackerId) || !CanReceiveEffect(targetId)) return false;
            var actor = world.Get(attackerId); var target = world.Get(targetId);
            // CombatUnit positions use original script axes (Y grows downward).
            // Compare instead of subtraction so extreme native coordinates cannot overflow.
            var facing = new Vector2Int(target.X.CompareTo(actor.X), actor.Y.CompareTo(target.Y));
            var presentation = new BattleAttackPresentation(attackerId, targetId, effect, facing, anticipation);
            LastAttackPresentation = presentation;
            AttackPresentationStarted?.Invoke(presentation);
            return true;
        }

        private void OnActionStarted(CombatRequest request)
        {
            if(!TemporaryLocalRulesEnabled)
            {
                if(request.Action==CombatActionType.Attack)RecordAttackPresentation(request.TargetId,request.ActorId,0,true);
                else if(request.Action==CombatActionType.Magic)
                {if(localMagicPresentation!=null)MagicCastPresentationStarted?.Invoke(localMagicPresentation);else MagicPresentationStarted?.Invoke(request);}
                return;
            }
            if (request.Action == CombatActionType.Attack)
                SendLocalNotification(new AttackEffectNotification(request.TargetId, request.ActorId, 0));
            else if (request.Action == CombatActionType.Magic)
            {
                SendLocalNotification(new LocalMagicPresentationNotification(request,localMagicPresentation));
            }
        }
        public event System.Action<CombatRequest> MagicPresentationStarted;
        public event System.Action<MagicRequest> MagicCastPresentationStarted;
        private MagicRequest localMagicPresentation;

        public bool CanStartAttackRequest(short targetId, short attackerId, out string reason)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled) { reason = "전투가 비활성 상태입니다."; return false; }
            if (IsBattleFinished) { reason = "전투가 종료되었습니다."; return false; }
            if (IsMovementInProgress(attackerId)) { reason = "이전 이동 확정을 기다리고 있습니다."; return false; }
            if (TemporaryLocalRulesEnabled && (!world.TryGet(attackerId, out var attacker)
                || !world.TryGet(targetId, out var target) || attacker.Enemy == target.Enemy
                || GridDistance(new CombatGridCell(attacker.X, attacker.Y), new CombatGridCell(target.X, target.Y))
                    > GameConstants.BattleTemporaryAttackRange))
            { reason = "임시 공격 사거리 밖이거나 적대 대상이 아닙니다."; return false; }
            return flow.CanStartAction(new CombatRequest(attackerId, targetId, CombatActionType.Attack), out reason);
        }

        public bool TryStartAttackRequest(short targetId, short attackerId, out string reason)
        {
            if (TemporaryLocalRulesEnabled && !processingServerRequest) return TrySubmitLocalRequest(new BattleAttackCommand(targetId,attackerId),out reason);
            EnsureInitialized();
            if (!isActiveAndEnabled) { reason = "전투가 비활성 상태입니다."; return false; }
            if (IsBattleFinished) { reason = "전투가 종료되었습니다."; return false; }
            if (IsMovementInProgress(attackerId)) { reason = "이전 이동 확정을 기다리고 있습니다."; return false; }
            if (!CanStartAttackRequest(targetId, attackerId, out reason)) return false;
            var request = new CombatRequest(attackerId, targetId, CombatActionType.Attack);
            return TemporaryLocalRulesEnabled ? flow.TryExecute(request, out _, out reason)
                : flow.TryStartAction(request, out reason);
        }

        public bool ConfigureMagicApCost(int magicId, int cost)
        {
            EnsureInitialized();
            return flow.ConfigureMagicApCost(magicId, cost);
        }

        private readonly System.Collections.Generic.HashSet<int> completeAbilitiesConfigured=new();
        public bool ConfigureSinglePlayerAbility(DarkSaver.Magic.MagicCatalogEntry entry)
        {
            EnsureInitialized();
            if(!TemporaryLocalRulesEnabled)return false;
            if(entry!=null && completeAbilitiesConfigured.Contains(entry.Id))return true;
            if(!UnresolvedAbilityRules.TryCreate(entry,out var rule) || !rule.Combat || !flow.ConfigureAbility(rule) || !flow.ConfigureMagicApCost(entry.Id,rule.Ap))return false;
            completeAbilitiesConfigured.Add(entry.Id);return true;
        }
        public bool IsRevivalMagic(int id){EnsureInitialized();return flow.IsRevivalMagic(id);}
        public bool TryGetMagicTargetAt(int magicId,CombatGridCell cell,out short id)
        {
            if(TryGetGridOccupant(cell,out id))return true;
            if(IsRevivalMagic(magicId))foreach(var unit in world.Units)
                if(!unit.Alive && unit.X==cell.X && unit.Y==cell.Y){id=unit.Id;return true;}
            id=-1;return false;
        }
        public bool ConfigureTemporaryDamageMagic(int magicId, int mpCost, int range, int powerBonus, int defenseDivisor)
        {
            EnsureInitialized();
            return flow.ConfigureTemporaryDamageMagic(magicId, mpCost, range, powerBonus, defenseDivisor);
        }

        public bool TryGetTemporaryMagicMpCost(int magicId, out int cost)
        {
            EnsureInitialized();
            return flow.TryGetTemporaryMagicMpCost(magicId, out cost);
        }

        public bool ConfigureTemporaryHealingMagic(int magicId, int mpCost, int range, int healing)
        {
            EnsureInitialized();
            return flow.ConfigureTemporaryHealingMagic(magicId, mpCost, range, healing);
        }
        public bool ConfigureTemporaryStatusMagic(int id, int mp, int range, TemporaryStatusEffect effect, int power, float seconds)
        {
            EnsureInitialized(); return flow.ConfigureTemporaryStatusMagic(id, mp, range, effect, power, seconds);
        }
        public bool TryGetTemporaryStatus(short id, TemporaryStatusEffect effect, out int power, out double seconds)
        {
            EnsureInitialized(); return flow.TryGetTemporaryStatus(id, effect, out power, out seconds);
        }
        public bool ConfigureTemporaryQuickMagic(int magicId, int mp, int range, int percent, float seconds)
        {
            EnsureInitialized(); return flow.ConfigureTemporaryQuickMagic(magicId, mp, range, percent, seconds);
        }
        public bool TryGetTemporaryQuick(short unitId, out int percent, out double seconds)
        {
            EnsureInitialized(); return flow.TryGetTemporaryQuick(unitId, out percent, out seconds);
        }
        public bool ConfigureTemporaryBuffMagic(int magicId, int mpCost, int range, int attack, int defense, float seconds)
        {
            EnsureInitialized();
            return flow.ConfigureTemporaryBuffMagic(magicId, mpCost, range, attack, defense, seconds);
        }
        public bool TryGetEffectiveStats(short unitId, out int attack, out int defense)
        {
            EnsureInitialized();
            return flow.TryGetEffectiveStats(unitId, out attack, out defense);
        }
        public string GetStatTooltip(short unitId, bool attack)
        {
            EnsureInitialized();
            if (!world.TryGet(unitId, out var unit) || !flow.TryGetEffectiveStats(unitId, out var atk, out var def)) return "";
            var label = attack ? "공격" : "방어";
            var text = $"{label}: 기본 {(attack ? unit.Attack : unit.Defense)} / 유효 {(attack ? atk : def)}";
            if (flow.TryGetTemporaryBuff(unitId, attack, out var bonus, out var seconds))
                text += $"\n임시 버프 +{bonus} · 남은 {seconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}초";
            return text;
        }

        public bool CanStartMagicRequest(int magicId, short casterId, System.Collections.Generic.IReadOnlyList<short> targets, out string reason)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled) { reason = "전투가 비활성 상태입니다."; return false; }
            if (IsBattleFinished) { reason = "전투가 종료되었습니다."; return false; }
            if (!IsLearnedMagicAllowed(casterId, magicId)) { reason = "확인된 습득 목록에 없는 마법입니다."; return false; }
            if (IsMovementInProgress(casterId)) { reason = "이전 이동 확정을 기다리고 있습니다."; return false; }
            if (TemporaryLocalRulesEnabled)
            {
                if (!flow.TryGetTemporaryMagicRange(magicId, out var range) || targets == null || targets.Count == 0
                    || !world.TryGet(casterId, out var caster))
                { reason = "이 기술의 대상 진영과 사거리를 확인하세요."; return false; }
                var checkedTargets=new System.Collections.Generic.HashSet<short>();
                var revivalCells=new System.Collections.Generic.HashSet<CombatGridCell>();
                foreach (short id in targets)
                {
                    if(!checkedTargets.Add(id))continue;
                    if (!world.TryGet(id, out var target)
                        || (flow.IsTemporarySupportMagic(magicId) ? caster.Enemy != target.Enemy : caster.Enemy == target.Enemy)
                        || GridDistance(new CombatGridCell(caster.X, caster.Y), new CombatGridCell(target.X, target.Y)) > range)
                    { reason = "이 기술의 대상 진영과 사거리를 확인하세요."; return false; }
                    if(flow.IsRevivalMagic(magicId) && occupancy!=null)
                    {
                        var cell=new CombatGridCell(target.X,target.Y);
                        if(!revivalCells.Add(cell) || !occupancy.CanReviveAt(id,cell))
                        {reason="부활할 자리가 다른 유닛에 점유되어 있습니다.";return false;}
                    }
                    if (!flow.CanStartAction(new CombatRequest(casterId, id, CombatActionType.Magic, magicId), out reason)) return false;
                }
                reason = string.Empty; return true;
            }
            return flow.CanStartMagicRequest(magicId, casterId, targets, out reason);
        }

        public bool TryStartMagicRequest(MagicRequest packet, out string reason)
        {
            if (TemporaryLocalRulesEnabled && !processingServerRequest) return TrySubmitLocalRequest(packet,out reason);
            if (packet == null || localMagicPresentation != null)
            { reason = "유효하지 않거나 중첩된 마법 요청입니다."; return false; }
            // Preserve the cast's clicked XY/direction and complete target list.
            // This is presentation context, never an independent server result.
            if (TemporaryLocalRulesEnabled) localMagicPresentation = packet;
            try { return TryStartMagicRequest(packet.MagicId, packet.ActingUnitId, packet.TargetUnitIds, out reason); }
            finally { localMagicPresentation = null; }
        }

        public bool TryStartMagicRequest(int magicId, short casterId, System.Collections.Generic.IReadOnlyList<short> targets, out string reason)
        {
            if (TemporaryLocalRulesEnabled && !processingServerRequest)
            {
                if(magicId<short.MinValue || magicId>short.MaxValue){reason="마법 ID 범위를 확인하세요.";return false;}
                return TrySubmitLocalRequest(new MagicRequest((short)magicId,casterId,-1,-1,0,System.Linq.Enumerable.ToArray(targets ?? System.Array.Empty<short>())),out reason);
            }
            EnsureInitialized();
            if (!isActiveAndEnabled) { reason = "전투가 비활성 상태입니다."; return false; }
            if (IsBattleFinished) { reason = "전투가 종료되었습니다."; return false; }
            if (!IsLearnedMagicAllowed(casterId, magicId)) { reason = "확인된 습득 목록에 없는 마법입니다."; return false; }
            if (IsMovementInProgress(casterId)) { reason = "이전 이동 확정을 기다리고 있습니다."; return false; }
            if (!CanStartMagicRequest(magicId, casterId, targets, out reason)) return false;
            return TemporaryLocalRulesEnabled
                ? flow.TryExecuteMagicTargets(magicId, casterId, targets, out reason)
                : flow.TryStartMagicRequest(magicId, casterId, targets, out reason);
        }

        public bool ApplyDeath(short unitId, bool leaveCorpse)
        {
            EnsureInitialized();
            if (!flow.TryApplyDeath(unitId, leaveCorpse, preserveReceivedHp: !TemporaryLocalRulesEnabled)) return false;
            SynchronizeGridState(unitId); return true;
        }

        public bool ApplyExperience(CombatExperienceResult result)
        {
            EnsureInitialized();
            return flow.TryApplyExperience(result);
        }

        public bool ConfigureEnemy(BattleEnemyDefinition definition)
        {
            if (definition == null) return true;
            EnsureInitialized();
            if (occupancy != null) return false;
            uint reward = 0;
            if (!definition.TryCreateUnit(out var enemy) || enemy.Id == PlayerId
                || (TemporaryLocalRulesEnabled && !definition.TryGetDefeatExperience(out reward)))
            {
                Debug.LogError("Invalid battle enemy definition.");
                return false;
            }
            var candidate = new CombatWorld();
            candidate.Add(enemy);
            if (definition.VictoryObjective != null && !definition.VictoryObjective.HasRequiredUnits(candidate))
            {
                Debug.LogError("Enemy definition does not provide the required objective target.");
                return false;
            }
            var jobCost = GameConstants.GetBattleAttackApCost(definition.JobId);
            if (definition.JobId < 0 || jobCost < 0 || jobCost > GameConstants.BattleEnemyMaximumAp) return false;
            if (enemy.Id != EnemyId && world.TryGet(enemy.Id, out _)) return false;
            world.Remove(EnemyId);
            EnemyId = enemy.Id;
            enemy.JobId = definition.JobId;
            world.Add(enemy);
            flow.ConfigureAttackApCostForJob(EnemyId, definition.JobId);
            EnemyJobId = definition.JobId;
            useDefeatExperience = TemporaryLocalRulesEnabled && definition.UsesNativeBaseDefeatExperience;
            defeatExperience = reward;
            defeatExperienceGranted = false;
            OutcomeAcknowledged = false;
            status = $"{enemy.Name} 등장";
            return true;
        }

        public bool ConfigureObjective(BattleObjectiveDefinition objective)
        {
            EnsureInitialized();
            if (objective != null && !objective.HasRequiredUnits(world))
            {
                Debug.LogError($"Battle objective for map {objective.MapKey} requires units missing from this encounter.");
                return false;
            }
            victoryObjective = objective;
            return true;
        }

        // Scene-load continuations bind learned abilities before Start. Enable their
        // execution rules in Awake so catalog registration cannot silently be skipped.
        private void Awake()
        {
            SetTemporaryLocalRules(GameConstants.BattleUseTemporaryLocalRules);
            BindOriginalProtocol();
        }

        private float temporaryAiDebt;
        private readonly System.Collections.Generic.HashSet<short> returningMonsters = new();
        private readonly System.Collections.Generic.Dictionary<short, short> chasingMonsterTargets = new();
        public bool TemporaryLocalRulesEnabled { get { EnsureInitialized(); return flow.TemporaryRulesEnabled; } }
        public void SetTemporaryLocalRules(bool enabled)
{EnsureInitialized();flow.TemporaryRulesEnabled=false;}
        public bool TryTemporaryRestUnit(short unitId)
{return false;}
        public bool CanTemporaryRestUnit(short unitId, out string reason)
        {
            reason = string.Empty;
            if (!isActiveAndEnabled || !TemporaryLocalRulesEnabled || IsBattleFinished
                || !TryGetUnit(unitId, out var unit) || !unit.Alive || IsMovementInProgress(unitId))
            { reason = "현재 휴식할 수 없습니다."; return false; }
            if (unit.Hp >= unit.MaxHp) { reason = "HP가 이미 최대치입니다."; return false; }
            if (unit.Ap != unit.MaxAp) { reason = $"AP 회복 중: {unit.Ap}/{unit.MaxAp}"; return false; }
            if (GameConstants.BattleTemporaryRestApCost <= 0 || GameConstants.BattleTemporaryRestApCost > unit.MaxAp
                || double.IsNaN(GameConstants.BattleTemporaryRestHpFraction)
                || GameConstants.BattleTemporaryRestHpFraction <= 0 || GameConstants.BattleTemporaryRestHpFraction > 1)
            { reason = "휴식 조정값이 유효하지 않습니다."; return false; }
            return true;
        }

        // Temporary Chebyshev geometry matches eight-direction movement; not native range evidence.
        private static long GridDistance(CombatGridCell a, CombatGridCell b) =>
            System.Math.Max(System.Math.Abs((long)a.X - b.X), System.Math.Abs((long)a.Y - b.Y));

        private bool TryTemporaryApproach(short unitId, CombatUnit target,
            out System.Collections.Generic.IReadOnlyList<CombatGridCell> best)
{best=null;return false;}        private bool TryTemporaryMovePath(short unitId, System.Collections.Generic.IReadOnlyList<CombatGridCell> path)
{return false;}
        // Menu choices recovered from GameCtl::setsolai. Target/movement policy below is local.
        public enum TroopOrder { Direct = 1, AutomaticAttack = 2, AutomaticDefense = 8 }
        private readonly System.Collections.Generic.Dictionary<short, TroopOrder> troopOrders = new();
        private readonly System.Collections.Generic.HashSet<short> automaticTroopMovements = new();
        public bool TryGetTroopOrder(short memberId, out TroopOrder order)
        {
            order = TroopOrder.Direct;
            if (!TryGetGroup(memberId, out var group) || group.Enemy || !group.CommanderAlive) return false;
            if (troopOrders.TryGetValue(group.CommanderId, out var stored)) order = stored;
            return true;
        }
        public bool TrySetTroopOrder(short memberId, TroopOrder order)
        {
            if (!isActiveAndEnabled || IsBattleFinished || !TemporaryLocalRulesEnabled
                || !TryGetUnit(memberId, out var issuer) || !issuer.Alive
                || (order != TroopOrder.Direct && order != TroopOrder.AutomaticAttack && order != TroopOrder.AutomaticDefense)
                || !TryGetGroup(memberId, out var group) || group.Enemy || !group.CommanderAlive || group.SoldierCount == 0) return false;
            if (TryGetTroopOrder(memberId, out var previous) && previous == order) return true;
            // Cancelling a pending step uses the normal reservation cleanup; no AP refund.
            foreach (var member in group.Members)
                if (member.Id != group.CommanderId && automaticTroopMovements.Remove(member.Id)) CancelMovementStep(member.Id);
            troopOrders[group.CommanderId] = order;
            return true;
        }
        private void AdvanceTemporaryTroopOrders(System.Collections.Generic.List<CombatUnit> units)
{}
        public bool AdvanceTemporaryMonsterAi(float seconds)
{return false;}
        private void EnsureInitialized()
        {
            if (flow != null) return;
            world.Add(new CombatUnit
            {
                Id = PlayerId, Name = string.Empty // Reserved client record; await native initialization.
            });
            world.Add(new CombatUnit
            {
                Id = EnemyId, Name = string.Empty, Enemy = true // No demonstration enemy stats.
            });
            flow = new CombatFlow(world, new CombatServerBoundary(), useActionPoints: true);
            flow.ActionStarted += OnActionStarted;
            flow.AdditionalMagicTargetStarted += request => {
                if (localMagicPresentation == null) SendLocalNotification(new LocalMagicPresentationNotification(request));
            };
            flow.ResultApplied += OnCombatResultApplied;
            flow.VitalsDisplayChanged += (id, delta, mp) => {
                if(TemporaryLocalRulesEnabled)SendLocalNotification(new LocalVitalsDisplayNotification(id,delta,mp));
                else VitalsDisplayChanged?.Invoke(id,delta,mp);
            };
        }

        private void Update()
        {
            if (MapPresentationActive && Input.GetKeyDown(GameConstants.BattlePrototypeHudKey))
                SetPrototypeHudVisible(!prototypeHudVisible);
            if(TemporaryLocalRulesEnabled)TrySubmitLocalRequest(new BattleSimulationTickRequest(Time.deltaTime,Time.unscaledDeltaTime),out _);
            else { AdvanceMovement(Time.unscaledDeltaTime); } // Native AP comes from SetTickMenu, not the old 1 AP/s tuning.
            if(MapPresentationActive && DefersOutcomeToScenario && IsBattleFinished && !OutcomeAcknowledged)
            {
                if(!VictoryAchieved)SetScenarioOutcomeDeferred(false); // A party defeat still ends this attempt explicitly.
                else
                {
                    var session=FindFirstObjectByType<BattleTransitionController>();
                    if(session!=null && !session.IsTransitioning){OutcomeAcknowledged=true;session.ExitBattle();}
                }
            }
            // Map input owns cancellation and explicit retreat confirmation.
            if (!MapPresentationActive && Input.GetKeyDown(KeyCode.Escape)) ReturnToWorld();
        }

        private void OnDisable() => CancelMovementRoutes();

        public string GetBattleHudText()
        {
            EnsureInitialized();
            var enemy = world.Get(EnemyId);
            CombatUnitSnapshot actor;
            if (MapPresentationActive)
            {
                if (!TryGetSelectedUnit(out actor)) return "아군을 선택하세요.\n" + status;
                var cost = flow.GetAttackApCost(actor.Id);
                return $"선택: {actor.Name} [{actor.Id}]\n" +
                    $"HP {actor.Hp}/{actor.MaxHp}  MP {actor.Mp}/{actor.MaxMp}  AP {actor.Ap}/{actor.MaxAp}\n" +
                    $"경험치 {actor.Experience}  공격 AP {(cost > 0 ? cost.ToString() : "미확정")}\n" +
                    $"주적 {enemy.Name}  HP {enemy.Hp}/{enemy.MaxHp}  AP {EnemyAp}/{GameConstants.GetBattleMaximumAp(enemy.Enemy)}\n{status}";
            }
            world.TryGetSnapshot(PlayerId, out actor);
            return $"{actor.Name}  HP {actor.Hp}/{actor.MaxHp}  MP {actor.Mp}/{actor.MaxMp}\n" +
                $"{enemy.Name}  HP {enemy.Hp}/{enemy.MaxHp}\n" +
                $"AP {actor.Ap}/{actor.MaxAp}  적 AP {EnemyAp}/{GameConstants.GetBattleMaximumAp(enemy.Enemy)}\n" +
                $"경험치 {actor.Experience}\n{(flow.GetAttackApCost(PlayerId) > 0 && flow.GetMagicApCost(CombatServerBoundary.PrototypeMagicId) > 0 ? status : "공격·마법 AP 비용 미설정")}";
        }

        private void LateUpdate()
        {
            if(!TemporaryLocalRulesEnabled && (PlayerJobId<=0 || EnemyJobId<=0)){ui?.Hide();return;}
            InGameUi.Ensure(ref ui, this, 30);
            ui.Begin();
            if (MapPresentationActive && !IsBattleFinished) DrawAlliedRoster();
            if (TryGetInspectedUnit(out var inspected)) DrawUnitInformation(inspected);
            var actor = world.Get(PlayerId);
            var panel = PrototypeBattleHudRect;
            if (IsPrototypeHudVisible)
            {
            var enemy = world.Get(EnemyId);
            ui.BattlePanel("battle", panel, "전투 진단 · F10");
            if (MapPresentationActive && TryGetSelectedUnit(out var selected))
            {
                ui.BattlePortrait("selected-portrait", new Rect(panel.x + 12, panel.y + 28, 32, 32),
                    GetComponent<BattleMapPresentation>()?.GetUnitPortrait(selected.Id));
                ui.BattleLabel("selected", new Rect(panel.x + 52, panel.y + 28, panel.width - 68, 28), selected.Name, singleLine: true);
                void StatusBar(float y, string label, int value, int maximum, Color color)
                {
                    ui.Bar(label, new Rect(panel.x + 16, panel.y + y, panel.width - 32, 16), label, value, maximum, color, fontSize: 12);
                }
                StatusBar(66, "HP", selected.Hp, selected.MaxHp, new Color(.55f, .2f, .18f));
                StatusBar(84, "MP", selected.Mp, selected.MaxMp, new Color(.18f, .35f, .55f));
                StatusBar(102, "AP", selected.Ap, selected.MaxAp, new Color(.5f, .42f, .15f));
                ui.BattleLabel("ready", new Rect(panel.x + 16, panel.y + 122, panel.width - 32, 22),
                    IsMovementInProgress(selected.Id) ? "이동 중" : selected.MaxAp<=0 ? "AP 최대치 미확인" : CombatActionPoints.IsReady(selected.Ap,selected.MaxAp) ? "행동 준비 완료" : "AP 회복 중");
            }
            else ui.BattleLabel("summary", new Rect(panel.x + 24, panel.y + 40, panel.width - 48, 105), MapPresentationActive ? "아군을 선택하세요." : GetBattleHudText());
            if (!MapPresentationActive)
            {
                ui.Button("attack", new Rect(panel.x + 24, panel.y + 160, 145, 38), "공격", PlayerAttack,
                    actor.Alive && enemy.Alive && !VictoryAchieved && actor.ActionPoints.CanStart(flow.GetAttackApCost(PlayerId)));
                ui.Button("magic", new Rect(panel.x + 191, panel.y + 160, 145, 38), "마법", PlayerMagic,
                    actor.Alive && enemy.Alive && !VictoryAchieved && actor.Mp >= CombatServerBoundary.PrototypeMagicMpCost &&
                    actor.ActionPoints.CanStart(flow.GetMagicApCost(CombatServerBoundary.PrototypeMagicId)));
            }
            if (MapPresentationActive && !IsAlliedRosterVisible)
                ui.Button("restore-roster", new Rect(panel.x + 24, panel.y + 190, panel.width - 48, 24),
                    "부대창 열기 (진단)", OpenAlliedRoster);
            var returnLabel = VictoryAchieved ? "승리 - 필드로 복귀" :
                DefeatAchieved ? "패배 - 필드로 복귀" : "퇴각";
            ui.Button("return", new Rect(panel.x + 24, panel.y + 220, panel.width - 48, MapPresentationActive ? 28 : 38), returnLabel, ReturnToWorld,
                !MapPresentationActive || !IsBattleFinished || OutcomeAcknowledged);
            }
            var dialogue = TemporaryLocalRulesEnabled && victoryObjective != null ? victoryObjective.GetCompletionDialogue(world) : string.Empty;
            if (!string.IsNullOrWhiteSpace(dialogue))
            {
                var dialogueRect = new Rect(panel.x, MapPresentationActive ? Mathf.Max(0, panel.y - 96) : panel.y + 300, panel.width, 90);
                ui.Panel("dialogue", dialogueRect);
                var displayText = victoryObjective.TryGetCompletionMessage(world, out var message)
                    ? message.DisplayText
                    : string.Join("\n", DarkSaver.Events.ScenarioDialogueLayout.SplitLines(dialogue));
                ui.NativeBitmapText("dialogue-text", new Rect(dialogueRect.x + 12, dialogueRect.y + 10,
                    dialogueRect.width - 24, dialogueRect.height - 20), displayText, Color.white,
                    maximumLines: int.MaxValue);
            }
            if (ShowsEncounterOutcome)
            {
                var outcome = OutcomeRect;
                ui.DungeonResult("outcome", outcome, OutcomeAllies, () => {if(TryAcknowledgeOutcome())ReturnToWorld();},
                    OutcomeEnemies, OutcomeGrantedExperience, OutcomeSummary);
            }
            ui.End();
        }

        public void PlayerAttack() => ResolveRound(CombatActionType.Attack);

        private void DrawAlliedRoster()
        {
            if (!IsAlliedRosterVisible) return;
            var area = AlliedRosterRect;
            if (IsAlliedRosterMinimized)
            {
                ui.NativeStatusPanel("native-roster-collapsed", area, -1);
                ui.NativeRosterSelection("restore-collapsed-roster", area, OpenAlliedRoster, true, "부대창 복원");
                return;
            }
            var groups = GetAlliedGroups();
            // GameCtl 0x100063CD: team*50 + slot*10, slots 0..3 (commanders, not soldiers).
            var members = GetAlliedRosterCommanders();
            if (members.Count >= 1 && members.Count <= 4)
            {
                ui.NativeStatusPanel("native-roster", area, members.Count);
                ui.NativeStatusName("native-roster-name", new Rect(area.x + 9, area.y + 9, 65, 14), members[0].Name);
                ui.NativeStatusCloseButton("native-roster-minimize", new Rect(area.x + 77, area.y + 9, 15, 15), MinimizeAlliedRoster, true);
                ui.NativeStatusCloseButton("native-roster-close", new Rect(area.x + 95, area.y + 9, 15, 15), CloseAlliedRoster);
                var view = GetComponent<BattleMapPresentation>();
                for (int i = 0; i < members.Count; i++)
                {
                    var unit = members[i];
                    if (!unit.Alive || unit.LifeState == UnitLifeState.Removed) continue;
                    var id = unit.Id;
                    float rowY = area.y + 34 + 41 * i;
                    string key = "native-roster-" + id;
                    ui.NativeRosterSelection(key, new Rect(area.x + 5, rowY, 110, 38),
                        () => TrySelectRosterUnit(id), CanSelectRosterUnit(id), unit.Name);
                    ui.Portrait(key + "-portrait", new Rect(area.x + 8, rowY + 2, 32, 32), view?.GetUnitPortrait(id));
                    bool full = CombatActionPoints.IsReady(unit.Ap,unit.MaxAp);
                    ui.NativeStatusGauge(key + "-ap", new Vector2(area.x + 65, rowY + (full ? 1 : 0)),
                        unit.Ap, unit.MaxAp, full ? Time.frameCount % 3 : 0, false, 48);
                    ui.NativeStatusGauge(key + "-hp", new Vector2(area.x + 65, rowY + 14), unit.Hp, unit.MaxHp, 1, true, 48);
                    ui.NativeStatusGauge(key + "-mp", new Vector2(area.x + 65, rowY + 27), unit.Mp, unit.MaxMp, 2, true, 48);
                }
                return;
            }
            ui.BattlePanel("roster", area, "아군 부대 (임시)");
            var rows = 0;
            foreach (var group in groups) rows += 1 + group.Members.Count;
            var parent = ui.Scroll("roster-scroll", new Rect(area.x + 8, area.y + 28, area.width - 16, area.height - 36), rows * 32);
            var y = 0f;
            foreach (var group in groups)
            {
                ui.BattleLabel("group-" + group.CommanderId, new Rect(0, y, area.width - 38, 28), $"생존 {group.AliveCount}/{group.Members.Count}", parent: parent);
                y += 32;
                foreach (var unit in group.Members)
                {
                    if (unit.LifeState == UnitLifeState.Removed) continue;
                    var role = unit.Id == group.CommanderId ? "대장" : "병사";
                    var state = unit.LifeState == UnitLifeState.Corpse ? " [사망]" : !unit.Alive ? " [행동 불가]" : "";
                    var id = unit.Id;
                    ui.Button("unit-" + id, new Rect(0, y, area.width - 38, 28), $"{unit.Name} AP {unit.Ap}{state}",
                        () => TrySelectRosterUnit(id), CanSelectRosterUnit(id), SelectedUnitId == id,
                        tooltip: $"{role} {unit.Name} · AP {unit.Ap}/{unit.MaxAp}{state}", parent: parent,
                        singleLine: true, fontSize: 12);
                    y += 32;
                }
            }
        }

        private void DrawUnitInformation(CombatUnitSnapshot unit)
        {
            var panel = UnitInfoRect;
            if (IsUnitInformationCompact)
            {
                ui.NativeStatusPanel("inspect-compact", panel, 1);
                ui.NativeStatusName("inspect-compact-name", new Rect(panel.x+9,panel.y+9,65,14),unit.Name);
                // Reuse the converted one-row native status artwork; same live unit, no snapshot cache.
                ui.NativeRosterSelection("inspect-expand",new Rect(panel.x+5,panel.y+30,110,48),
                    () => TrySetUnitInformationCompact(false),true,"크게 보기");
                ui.Portrait("inspect-compact-portrait",new Rect(panel.x+8,panel.y+36,32,32),GetComponent<BattleMapPresentation>()?.GetUnitPortrait(unit.Id));
                ui.NativeStatusGauge("inspect-compact-ap",new Vector2(panel.x+65,panel.y+34),unit.Ap,unit.MaxAp,CombatActionPoints.IsReady(unit.Ap,unit.MaxAp) ? Time.frameCount%3 : 0,false,48);
                ui.NativeStatusGauge("inspect-compact-hp",new Vector2(panel.x+65,panel.y+48),unit.Hp,unit.MaxHp,1,true,48);
                ui.NativeStatusGauge("inspect-compact-mp",new Vector2(panel.x+65,panel.y+61),unit.Mp,unit.MaxMp,2,true,48);
                ui.NativeStatusCloseButton("inspect-expand-button",new Rect(panel.x+77,panel.y+9,15,15),()=>TrySetUnitInformationCompact(false),true);
                ui.NativeStatusCloseButton("inspect-compact-close",new Rect(panel.x+95,panel.y+9,15,15),ClearUnitInspection);
                return;
            }
            ui.NativeStatusPanel("inspect", panel);
            ui.NativeStatusCloseButton("inspect-compact-button",new Rect(panel.x+78,panel.y+6,15,15),()=>TrySetUnitInformationCompact(true),true);
            ui.NativeStatusName("inspect-name", new Rect(panel.x + 9, panel.y + 7, 68, 14), unit.Name);
            ui.NativeStatusCloseButton("inspect-close", new Rect(panel.x + 96, panel.y + 6, 15, 15), ClearUnitInspection);
            var portrait = GetComponent<BattleMapPresentation>()?.GetUnitPortrait(unit.Id);
            // GameCtl.dll 0x10005410 and Glib/gscene1.lsc: 120x213, portrait (44,79), gauges x30.
            ui.Portrait("portrait", new Rect(panel.x + 44, panel.y + 79, 32, 32), portrait);
            ui.NativeStatusGauge("inspect-ap", new Vector2(panel.x + 30, panel.y + 143), unit.Ap, unit.MaxAp,
                CombatActionPoints.IsReady(unit.Ap,unit.MaxAp) ? Time.frameCount % 3 : 0, false);
            ui.NativeStatusGauge("inspect-hp", new Vector2(panel.x + 30, panel.y + 156), unit.Hp, unit.MaxHp, 1);
            ui.NativeStatusGauge("inspect-mp", new Vector2(panel.x + 30, panel.y + 169), unit.Mp, unit.MaxMp, 2);
            // Native static labels are baked from gscene1. Do not duplicate them with modern text.
            ui.NativeStatusNumber("inspect-exp", new Vector2(panel.x + 34, panel.y + 183), unit.Experience.ToString(System.Globalization.CultureInfo.InvariantCulture));
            flow.TryGetEffectiveStats(unit.Id, out var effectiveAttack, out var effectiveDefense);
            ui.NativeStatusNumber("inspect-attack", new Vector2(panel.x + 32, panel.y + 197), effectiveAttack.ToString(System.Globalization.CultureInfo.InvariantCulture), GetStatTooltip(unit.Id, true));
            ui.NativeStatusNumber("inspect-defense", new Vector2(panel.x + 87, panel.y + 197), effectiveDefense.ToString(System.Globalization.CultureInfo.InvariantCulture), GetStatTooltip(unit.Id, false));
        }

        public void PlayerMagic() => ResolveRound(CombatActionType.Magic);

        // Explicit single-player base reward, not the original server's distribution formula.
        public bool TryAwardDefeatExperience()
{return false;}
        private void ResolveRound(CombatActionType action)
{}
        private void ReturnToWorld()
        {
            if (MapPresentationActive && !IsBattleFinished)
            {
                var presentation = GetComponent<BattleMapPresentation>();
                if (presentation != null && presentation.TryOpenSystemMenu()) presentation.TryRetreatImmediately();
                return;
            }
            if (MapPresentationActive && !OutcomeAcknowledged) return;
            var transition = FindFirstObjectByType<BattleTransitionController>();
            if (transition != null) transition.ExitBattle();
        }
    }
}
