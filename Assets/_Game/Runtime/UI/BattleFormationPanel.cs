using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.UI
{
    // Unity preparation UI, not an original account roster or deployment command.
    [DisallowMultipleComponent]
    public sealed class BattleFormationPanel : MonoBehaviour
    {
        private readonly List<BattleFormationMemberDraft> owned = new();
        private readonly List<BattleFormationMemberDraft> draft = new();
        private int mapWidth, mapHeight;
        private short[] reservedIds;
        private TopDownPlayerController player;
        private TopDownPlayerController configuredPlayer;
        private InGameUi ui;
        private bool previousInputBlocked;
        private bool useSessionRoster = true;
        private int loadedRosterRevision = -1;
        private System.Func<int, CombatGridCell, bool> placementAllows;
        private readonly Dictionary<string, string[]> coordinateInputs = new();
        private bool requiresPlacementValidation;
        public void UseSessionRoster() { Cancel(); useSessionRoster = true; loadedRosterRevision = -1; ConfirmedSnapshot = null; }
        public bool IsOpen { get; private set; }
        public int OwnedCount => owned.Count;
        public int AssignedCount => draft.Count;
        public string Message { get; private set; } = "보유 유닛 데이터가 연결되지 않았습니다.";
        public BattleFormationSnapshot ConfirmedSnapshot { get; private set; }
        public bool CanReturnToTown => IsOpen && !previousInputBlocked && player != null
            && player.isActiveAndEnabled && (GetComponent<TownServicesPanel>()?.CanOpen ?? false);

        public bool ReturnToTown()
        {
            if (!CanReturnToTown) return false;
            // Return to the NPC world without restoring the removed facility overview.
            // Previously confirmed deployment remains unchanged; this is not Confirm.
            Cancel();return true;
        }

        public string MemberStatus(string persistentId)
        {
            var member = FindOwned(persistentId);
            var unit = member?.Unit;
            if (unit == null) return "상태 미확인";
            string state = !member.IsOwned || !member.FinalStateConfirmed || !unit.Alive || unit.Enemy
                ? "출전 불가" : IsAssigned(persistentId) ? "출전 배정" : "대기";
            return $"HP {unit.Hp}/{unit.MaxHp} · MP {unit.Mp}/{unit.MaxMp} · {state}";
        }

        // Explicit supplier only. Catalogs/initial player stats never synthesize ownership.
        // Unit is observed at confirmation, never mutated by this panel.
        public void Configure(IReadOnlyList<BattleFormationMemberDraft> source,
            int width, int height, IReadOnlyCollection<short> reserved, TopDownPlayerController controller = null)
        {
            Cancel(); owned.Clear(); ConfirmedSnapshot = null;
            useSessionRoster = false;
            placementAllows = null; requiresPlacementValidation = false; coordinateInputs.Clear();
            mapWidth = width; mapHeight = height;
            configuredPlayer = controller;
            reservedIds = reserved == null ? null : new List<short>(reserved).ToArray();
            if (source != null) foreach (var member in source)
                if (member != null) owned.Add(Copy(member));
            Message = owned.Count == 0 ? "보유 유닛 데이터가 연결되지 않았습니다." : "출전할 대장과 병사를 배정하세요.";
        }

        private static BattleFormationMemberDraft Copy(BattleFormationMemberDraft member) => new()
        {
            PersistentId = member.PersistentId, CommanderPersistentId = member.CommanderPersistentId,
            OwnedInstanceId = member.OwnedInstanceId,
            CommanderId = member.CommanderId, Unit = member.Unit, IsOwned = member.IsOwned,
            FinalStateConfirmed = member.FinalStateConfirmed, Position = member.Position,
            TeamId = member.TeamId, RecordEligible = member.RecordEligible,
            LearnedMagicIds = member.LearnedMagicIds == null ? null : new List<short>(member.LearnedMagicIds).AsReadOnly()
        };

        public bool Open()
        {
            if (FirstPlayTutorial.BlocksWorldActions) return false;
            var session = GetComponent<BattleTransitionController>();
            if (!isActiveAndEnabled || SceneManager.GetSceneByName("Battle").isLoaded ||
                (session != null && session.IsTransitioning)) return false;
            if (IsOpen) return true;
            GetComponent<TownServicesPanel>()?.Close();
            if (useSessionRoster && session != null && loadedRosterRevision != session.OwnedRosterRevision)
            {
                var source = session.CaptureOwnedRoster();
                Configure(source?.CreatePreparationDrafts(), 0, 0, null, configuredPlayer);
                useSessionRoster = true; loadedRosterRevision = session.OwnedRosterRevision;
            }
            GetComponent<ItemCatalogPanel>()?.Close(); GetComponent<MagicCatalogPanel>()?.Close();
            draft.Clear();
            if (ConfirmedSnapshot != null) foreach (var saved in ConfirmedSnapshot.Members)
            {
                var member = FindOwned(saved.PersistentId);
                if (member == null) continue;
                var copy = Copy(member); copy.CommanderId = saved.CommanderId;
                copy.CommanderPersistentId = saved.CommanderPersistentId;
                copy.OwnedInstanceId = saved.OwnedInstanceId;
                copy.Unit = CopyUnitAt(member.Unit, saved.Position); copy.Unit.Id = saved.Unit.Id;
                if (requiresPlacementValidation) copy.Position = saved.Position;
                draft.Add(copy);
            }
            player = configuredPlayer != null ? configuredPlayer : FindAnyObjectByType<TopDownPlayerController>();
            previousInputBlocked = player != null && player.UiInputBlocked;
            if (player != null) player.SetUiInputBlocked(true);
            IsOpen = true;
            Message = owned.Count == 0 ? "보유 유닛 데이터가 연결되지 않았습니다. 임의 용병은 생성하지 않습니다." : "출전할 유닛과 칸을 배정하고 확인하세요.";
            return true;
        }

        public void Cancel()
        {
            draft.Clear();
            if (!IsOpen) return;
            IsOpen = false;
            if (player != null) player.SetUiInputBlocked(previousInputBlocked);
            player = null;
        }

        private BattleFormationMemberDraft FindOwned(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            BattleFormationMemberDraft found = null;
            foreach (var member in owned) if (member.PersistentId == id)
            { if (found != null) return null; found = member; }
            return found;
        }

        public bool IsAssigned(string id) => draft.Exists(member => member.PersistentId == id);

        // Explicit encounter/map context. Never derive a native position from a
        // default zero or re-use a confirmed position from a different map.
        public bool ConfigurePlacementContext(int width, int height, IReadOnlyCollection<short> reserved,
            System.Func<int, CombatGridCell, bool> allows)
        {
            if (width <= 0 || height <= 0 || reserved == null || allows == null) return false;
            mapWidth = width; mapHeight = height; reservedIds = new List<short>(reserved).ToArray();
            placementAllows = allows; requiresPlacementValidation = true; coordinateInputs.Clear(); ConfirmedSnapshot = null;
            foreach (var member in owned) member.Position = null;
            foreach (var member in draft) member.Position = null;
            Message = "새 맵의 출전 좌표를 명시하세요. 이전 좌표는 재사용하지 않습니다."; return true;
        }

        // Native Unity Tilemaps/catalogs only; no original files or synthesized troop type.
        public bool ConfigureMapPlacement(UnityEngine.Tilemaps.Tilemap ground, UnityEngine.Tilemaps.Tilemap terrain,
            IReadOnlyList<DarkSaver.Jobs.JobCatalog> catalogs, IReadOnlyCollection<short> reserved)
        {
            if (ground == null || terrain == null || ground.layoutGrid == null || ground.layoutGrid != terrain.layoutGrid
                || ground.gameObject.scene != terrain.gameObject.scene || catalogs == null || catalogs.Count == 0) return false;
            var area = ground.cellBounds;
            if (area.xMin != 0 || area.yMin != 0 || area.zMin != 0 || area.size.z != 1
                || area.size.x <= 0 || area.size.y <= 0) return false;
            var copy = new List<DarkSaver.Jobs.JobCatalog>(); var unique = new HashSet<DarkSaver.Jobs.JobCatalog>();
            foreach (var catalog in catalogs) { if (catalog == null || !unique.Add(catalog)) return false; copy.Add(catalog); }
            return ConfigurePlacementContext(area.size.x, area.size.y, reserved, (job, cell) =>
                ground != null && terrain != null && ground.isActiveAndEnabled && terrain.isActiveAndEnabled
                && BattleMapPresentation.TryGetPlacementMovement(ground, terrain, area, copy, job, cell, out var value)
                && value != CombatTerrainTraversal.ImpassableValue);
        }

        private bool AllowsPlacement(int jobId, CombatGridCell position)
        {
            try { return placementAllows != null && placementAllows(jobId, position); }
            catch (System.Exception) { return false; }
        }

        private static CombatUnit CopyUnitAt(CombatUnit unit, CombatGridCell position) => new()
        {
            Id = unit.Id, JobId = unit.JobId, Name = unit.Name, Enemy = unit.Enemy,
            X = position.X, Y = position.Y, Hp = unit.Hp, MaxHp = unit.MaxHp, Mp = unit.Mp, MaxMp = unit.MaxMp,
            Attack = unit.Attack, Defense = unit.Defense, HasMagicAttack=unit.HasMagicAttack,HasMagicDefense=unit.HasMagicDefense,MagicAttack=unit.MagicAttack,MagicDefense=unit.MagicDefense, Experience = unit.Experience, LifeState = unit.LifeState
        };

        public bool TrySetPosition(string id, CombatGridCell position)
        {
            if (!IsOpen || !requiresPlacementValidation) { Message = "명시된 맵 배치 입력이 필요합니다."; return false; }
            var member = draft.Find(value => value.PersistentId == id);
            if (member?.Unit == null || position.X < 0 || position.Y < 0 || position.X >= mapWidth || position.Y >= mapHeight ||
                !AllowsPlacement(member.Unit.JobId, position))
            { Message = "출전 좌표 또는 직업별 통행을 확인할 수 없습니다."; return false; }
            foreach (var other in draft) if (other != member && other.Position.HasValue && other.Position.Value.Equals(position))
            { Message = "다른 아군의 출전 칸과 겹칩니다."; return false; }
            // Detached preparation body data; do not rewrite the supplier's unit.
            member.Unit = CopyUnitAt(member.Unit, position);
            member.Position = position;
            coordinateInputs[id] = new[] { position.X.ToString(System.Globalization.CultureInfo.InvariantCulture), position.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            Message = "초안 좌표를 적용했습니다. 확인 시 통행을 다시 검사합니다."; return true;
        }

        public bool Assign(string id, string commanderId)
        {
            if (!IsOpen) return false;
            if (commanderId == BattleFormation.PrimaryCommanderPersistentId) return AssignToPrimary(id);
            var member = FindOwned(id); var commander = FindOwned(commanderId);
            var roster = GetComponent<BattleTransitionController>()?.CaptureOwnedRoster();
            if (roster != null)
                foreach (var unit in roster.units)
                    if (unit.persistentId == id && unit.hireOwnerId == 100)
                    { Message = "고용 병사는 주인공 부대 배정 버튼을 이용하세요. 병사를 대장으로 승격하지 않습니다."; return false; }
            if (member == null || commander == null || !member.IsOwned || !commander.IsOwned ||
                !member.FinalStateConfirmed || !commander.FinalStateConfirmed || member.Unit == null || commander.Unit == null ||
                !member.Unit.Alive || !commander.Unit.Alive || member.Unit.Enemy || commander.Unit.Enemy ||
                member.Unit.Id < 0 || commander.Unit.Id < 0 || commander.Unit.Id % 10 != 0 || member.Unit.Id / 10 * 10 != commander.Unit.Id)
            { Message = "보유·최종 상태 또는 같은 전투 그룹의 대장 연결을 확인할 수 없습니다."; return false; }
            if (IsAssigned(id)) { Message = "이미 배정된 유닛입니다."; return false; }
            if (id != commanderId && !IsAssigned(commanderId))
            { Message = "대장을 먼저 배정하세요."; return false; }
            int groups = 0, soldiers = 0;
            foreach (var existing in draft)
            {
                if (existing.PersistentId == existing.CommanderPersistentId) groups++;
                else if (existing.CommanderPersistentId == commanderId) soldiers++;
                if (existing.Unit.Id == member.Unit.Id) { Message = "전투 인스턴스 ID가 중복됩니다."; return false; }
            }
            if ((id == commanderId && groups >= GameConstants.BattleMaximumAlliedGroups) ||
                (id != commanderId && soldiers >= GameConstants.BattleMaximumAlliedSoldiersPerGroup))
            { Message = $"대장 {GameConstants.BattleMaximumAlliedGroups}그룹 또는 그룹당 병사 {GameConstants.BattleMaximumAlliedSoldiersPerGroup}명 상한을 초과합니다."; return false; }
            var copy = Copy(member); copy.CommanderId = commander.Unit.Id;
            copy.CommanderPersistentId = commanderId; draft.Add(copy);
            Message = "초안에 배정했습니다. 확인 전에는 확정 편성이 변경되지 않습니다."; return true;
        }

        private bool IsPrimaryMercenary(string id)
            => GetComponent<BattleTransitionController>()?.IsPrimaryHiredUnit(id) ?? false;

        public bool AssignToPrimary(string id)
        {
            var member = FindOwned(id);
            if (!IsOpen || !IsPrimaryMercenary(id) || member?.Unit == null || !member.IsOwned
                || !member.FinalStateConfirmed || !member.Unit.Alive || member.Unit.Enemy || IsAssigned(id))
            { Message = "출전 가능한 주인공 소유 용병을 선택하세요."; return false; }
            var groups = new HashSet<short>(); var used = new HashSet<short>();
            foreach (var existing in draft) { groups.Add(existing.CommanderId.Value); used.Add(existing.Unit.Id); }
            groups.Add(100);
            if (groups.Count > GameConstants.BattleMaximumAlliedGroups)
            { Message = "아군 그룹 상한을 초과합니다."; return false; }
            for (short slot = 101; slot <= 100 + GameConstants.BattleMaximumAlliedSoldiersPerGroup; slot++)
            {
                if (used.Contains(slot) || (reservedIds != null && System.Array.IndexOf(reservedIds, slot) >= 0)) continue;
                var copy = Copy(member); copy.OwnedInstanceId = member.Unit.Id;
                copy.CommanderId = 100; copy.CommanderPersistentId = BattleFormation.PrimaryCommanderPersistentId;
                copy.Unit = CopyUnitAt(member.Unit, member.Position ?? new CombatGridCell(0, 0)); copy.Unit.Id = slot;
                draft.Add(copy); Message = "주인공 부대에 배정했습니다. 출전 칸을 지정하세요."; return true;
            }
            Message = "주인공 부대의 병사 슬롯이 가득 찼습니다."; return false;
        }

        public bool Unassign(string id)
        {
            if (!IsOpen || !IsAssigned(id)) return false;
            draft.RemoveAll(member => member.PersistentId == id || member.CommanderPersistentId == id);
            Message = "초안에서 해제했습니다. 대장 해제 시 소속 병사도 함께 해제됩니다."; return true;
        }

        public bool Confirm()
        {
            if (!IsOpen) return false;
            foreach (var selected in draft)
            {
                var current = FindOwned(selected.PersistentId);
                if (current?.Unit == null || !current.IsOwned || !current.FinalStateConfirmed
                    || !current.Unit.Alive || current.Unit.Enemy)
                { Message = "보유 유닛의 출전 가능 상태가 변경되었습니다. 편성을 다시 확인하세요."; return false; }
            }
            if (requiresPlacementValidation) foreach (var member in draft)
                if (member.Unit == null || !member.Position.HasValue || !AllowsPlacement(member.Unit.JobId, member.Position.Value))
                { Message = "모든 출전 칸의 직업별 통행을 다시 확인해야 합니다."; return false; }
            if (!BattleFormation.TryCreateSnapshot(draft, mapWidth, mapHeight, reservedIds, out var snapshot, out var reason,
                GetComponent<BattleTransitionController>() != null))
            { Message = reason; return false; }
            if (useSessionRoster && !GetComponent<BattleTransitionController>().MatchesOwnedFormation(snapshot))
            { Message = "보유 상태가 변경되었습니다. 편성을 다시 여세요."; return false; }
            ConfirmedSnapshot = snapshot;
            Cancel(); Message = useSessionRoster ? "확인한 편성을 다음 조우에 사용합니다." : "준비 스냅샷을 보관했습니다. 별도 공급자의 출전 연결이 필요합니다."; return true;
        }

        private void OnDisable() => Cancel();
        private void Update()
        {
            if (!IsOpen) return;
            var session = GetComponent<BattleTransitionController>();
            if (SceneManager.GetSceneByName("Battle").isLoaded || (session != null && session.IsTransitioning) || Input.GetKeyDown(KeyCode.Escape)) Cancel();
        }

        private void LateUpdate()
        {
            if (!IsOpen || SceneManager.GetSceneByName("Battle").isLoaded) { ui?.Hide(); return; }
            var width = Mathf.Min(720f, Rendering.GameScreen.Width - 24f); var height = Mathf.Min(520f, Rendering.GameScreen.Height - 24f);
            if (width < 240f || height < 180f) { ui?.Hide(); return; }
            InGameUi.Ensure(ref ui, this, 60); ui.Begin();
            var area = new Rect((Rendering.GameScreen.Width - width) / 2f, (Rendering.GameScreen.Height - height) / 2f, width, height);
            ui.Panel("formation", area, "부대 편성");
            ui.Label("count", new Rect(area.x + 12, area.y + 32, width - 24, 24), $"보유 {OwnedCount}명 / 출전 {AssignedCount}명");
            ui.Label("message", new Rect(area.x + 12, area.y + 60, width - 24, 48), Message);
            var parent = ui.Scroll("members", new Rect(area.x + 12, area.y + 114, width - 24, height - 174), owned.Count * 150);
            var y = 0f;
            foreach (var member in owned)
            {
                var id = member.PersistentId;
                ui.Label("member-" + id, new Rect(0, y, width - 48, 24), member.Unit?.Name ?? "상태 없음", parent: parent);
                y += 28;
                ui.Label("status-" + id, new Rect(0, y, width - 48, 24), MemberStatus(id), 12, parent: parent);
                y += 28;
                if (IsAssigned(member.PersistentId))
                {
                    if (requiresPlacementValidation)
                    {
                        if (!coordinateInputs.TryGetValue(member.PersistentId, out var input))
                        { input = new[] { string.Empty, string.Empty }; coordinateInputs.Add(member.PersistentId, input); }
                        ui.Label("xy-" + id, new Rect(0, y, 90, 28), "출전 칸 X/Y", parent: parent);
                        ui.TextInput("x-" + id, new Rect(94, y, 65, 28), input[0], 11, value => input[0] = value, parent);
                        ui.TextInput("y-" + id, new Rect(164, y, 65, 28), input[1], 11, value => input[1] = value, parent);
                        ui.Button("place-" + id, new Rect(234, y, 100, 28), "배치", () =>
                        {
                            if (int.TryParse(input[0], out var x) && int.TryParse(input[1], out var row)) TrySetPosition(id, new CombatGridCell(x, row));
                            else Message = "X/Y 좌표를 정수로 명시하세요.";
                        }, parent: parent);
                        y += 32;
                    }
                    ui.Button("unassign-" + id, new Rect(0, y, width - 48, 28), "출전 해제", () => Unassign(id), parent: parent);
                    y += 32;
                }
                else if (IsPrimaryMercenary(id))
                { ui.Button("assign-primary-" + id, new Rect(0, y, width - 48, 28), "주인공 부대에 배정", () => AssignToPrimary(id), parent: parent); y += 32; }
                else if (member.Unit != null && member.Unit.Id >= 0 && member.Unit.Id % 10 == 0)
                { ui.Button("assign-" + id, new Rect(0, y, width - 48, 28), "대장으로 출전", () => Assign(id, id), parent: parent); y += 32; }
                else foreach (var leader in owned)
                    if (leader.Unit != null && member.Unit != null && leader.Unit.Id >= 0 && leader.Unit.Id % 10 == 0 &&
                        member.Unit.Id / 10 * 10 == leader.Unit.Id && IsAssigned(leader.PersistentId))
                    {
                        var leaderId = leader.PersistentId;
                        ui.Button("assign-" + id + "-" + leaderId, new Rect(0, y, width - 48, 28), $"{leader.Unit.Name}의 병사로 출전", () => Assign(id, leaderId), parent: parent);
                        y += 32;
                    }
                y += 12;
            }
            parent.style.height = y;
            float footerWidth = (width - 40) / 3;
            ui.Button("confirm", new Rect(area.x + 12, area.yMax - 48, footerWidth, 36), "편성 확인", () => Confirm(), draft.Count > 0);
            ui.Button("town-return", new Rect(area.x + 20 + footerWidth, area.yMax - 48, footerWidth, 36), "대화 종료", () => ReturnToTown(), CanReturnToTown);
            ui.Button("cancel", new Rect(area.x + 28 + footerWidth * 2, area.yMax - 48, footerWidth, 36), "취소", Cancel);
            ui.End();
        }
    }
}
