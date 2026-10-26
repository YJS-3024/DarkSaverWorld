using System.Collections.Generic;
using DarkSaver.Network;
using DarkSaver.Magic;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkSaver.Combat
{
    public enum BattleMapCommand { Automatic, Move, Attack, Magic, AttackBuff, DefenseBuff, Cancel, Rest, Special, System, Tools, Troop }
    public enum BattleMagicDirection { Up, Down, Left, Right }
    public readonly struct BattleMapVisual
    {
        public BattleMapVisual(short unitId, SpriteRenderer body) { UnitId = unitId; Body = body; }
        public short UnitId { get; }
        public SpriteRenderer Body { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleSceneRoot))]
    public sealed partial class BattleMapPresentation : MonoBehaviour
    {
        private BattleSceneRoot battle;
        private Tilemap ground, terrain;
        private Camera view;
        private BoundsInt bounds;
        private readonly List<BattleMapVisual> visuals = new();
        private readonly List<GameObject> formationBodies = new();
        public int CreatedFormationBodyCount => formationBodies.Count;
        private readonly List<bool> originalEnabled = new();
        private readonly List<DirectionalSpriteAnimator> animators = new();
        private readonly List<Vector2> originalFacing = new();
        private readonly List<bool> originalMoving = new();
        private readonly List<Rigidbody2D> bodies = new();
        private readonly List<bool> originalSimulated = new();
        private readonly List<CombatGridCell> visibleCells = new();
        private readonly HashSet<int> offsetVisuals = new();
        private bool listening;
        private bool configured;
        private MagicRequest pendingMagic;
        private bool magicTargetsPosition;
        private bool temporaryAreaTargeting;
        private MagicCatalogEntry pendingNativeMagic;
        private System.Func<short, bool?> nativeRecordEligible;
        private System.Func<short, int?> nativeTeamId;
        private int? nativeCurrentTeam;
        private IReadOnlyList<int> nativeRelations;
        private MagicCatalogEntry previewMagic;
        private GameObject previewObject;
        private Mesh previewMesh;
        private Material previewMaterial;
        private MeshRenderer previewRenderer;
        private CombatGridCell? previewCommand, previewOrigin;
        private readonly List<CombatGridCell> previewCells = new();
        private readonly List<Vector3> previewVertices = new();
        private readonly List<int> previewIndices = new();
        private readonly List<Color32> previewColors = new();
        private static readonly Vector3Int[] PreviewCorners =
            { Vector3Int.zero, Vector3Int.right, new Vector3Int(1, 1, 0), Vector3Int.up };
        public int MagicPreviewCellCount { get; private set; }
        public bool IsMagicTargeting => pendingMagic != null;
        public short? SelectedMagicId => pendingMagic?.MagicId;
        private CombatGridCell? magicDirectionCommand;
        public CombatGridCell? SelectedMagicDirectionCommand => magicDirectionCommand;
        public bool RequiresMagicDirection => IsMagicTargeting && previewMagic != null && previewMagic.Unit != 8
            && (previewMagic.Range == -1 || previewMagic.Range == -3 || previewMagic.Range == -4);
        public Rect MagicDirectionRect => battle != null && battle.IsPrototypeHudVisible
            ? new Rect(Rendering.GameScreen.Width - 380, 12, 360, 145) : default;
        public static BattleMagicDirection DirectionFromPointerDelta(Vector2 delta)
        {
            // GameCtl 0x1000A18B..A1D7: screen Y grows down; ties select vertical.
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? BattleMagicDirection.Right : BattleMagicDirection.Left;
            return delta.y <= 0 ? BattleMagicDirection.Up : BattleMagicDirection.Down;
        }
        public bool TryPreviewMagicDirectionAt(Vector3 position)
        {
            if (!isActiveAndEnabled || !IsBound || !RequiresMagicDirection || view == null || ground == null
                || !battle.TryGetSelectedUnit(out var actor)) return false;
            // Native origin is the caster tile's top-left (tile*32), not the sprite/foot pivot.
            var origin = ground.CellToWorld(new Vector3Int(actor.X, bounds.yMax - actor.Y, 0));
            var start = view.WorldToScreenPoint(origin); var end = view.WorldToScreenPoint(position);
            return TryChooseMagicDirection(DirectionFromPointerDelta(new Vector2(end.x-start.x, start.y-end.y)));
        }
        public bool TryChooseMagicDirection(BattleMagicDirection direction)
        {
            if (!isActiveAndEnabled || !IsBound || battle.IsBattleFinished || !RequiresMagicDirection
                || (pendingNativeMagic == null && !temporaryAreaTargeting)) return false;
            // Native +Y is down on the bound Unity map. No world click is converted to a sentinel.
            var x = direction switch { BattleMagicDirection.Up => -3, BattleMagicDirection.Down => -4,
                BattleMagicDirection.Left => -2, BattleMagicDirection.Right => -1, _ => 0 };
            if (x == 0) return false;
            var command = new CombatGridCell(x, 0);
            if (!TryPreviewMagicAt(command)) return false;
            magicDirectionCommand = command;
            LastInputMessage = "마법 방향 미리보기: 바닥 클릭으로 시전, 우클릭 취소.";
            return true;
        }
        public bool TryConfirmMagicDirection()
        {
            if (!isActiveAndEnabled || !IsBound || battle.IsBattleFinished || !RequiresMagicDirection || !magicDirectionCommand.HasValue) return false;
            return TrySendSelectedNativeMagic(magicDirectionCommand.Value);
        }
        public bool TryGetStandardMagicArea(MagicCatalog catalog, int magicId, Vector3 clickedPosition,
            out IReadOnlyList<CombatGridCell> cells)
            => TryGetMagicArea(catalog, magicId, clickedPosition, true, out cells);

        public bool TryGetMagicArea(MagicCatalog catalog, int magicId, Vector3 clickedPosition,
            out IReadOnlyList<CombatGridCell> cells)
            => TryGetMagicArea(catalog, magicId, clickedPosition, false, out cells);

        private bool TryGetMagicArea(MagicCatalog catalog, int magicId, Vector3 clickedPosition,
            bool standardOnly, out IReadOnlyList<CombatGridCell> cells)
        {
            cells = System.Array.Empty<CombatGridCell>();
            return TryWorldToCell(clickedPosition, out var clicked)
                && TryGetMagicArea(catalog, magicId, clicked, standardOnly, out cells);
        }

        // Native direction commands encode -4/-3/-2/-1 in X; they are not world points.
        public bool TryGetMagicArea(MagicCatalog catalog, int magicId, CombatGridCell clicked,
            out IReadOnlyList<CombatGridCell> cells)
            => TryGetMagicArea(catalog, magicId, clicked, false, out cells);

        private bool TryGetMagicArea(MagicCatalog catalog, int magicId, CombatGridCell clicked,
            bool standardOnly, out IReadOnlyList<CombatGridCell> cells)
        {
            cells = System.Array.Empty<CombatGridCell>();
            if (!isActiveAndEnabled || !IsBound || catalog == null || !catalog.TryGet(magicId, out var magic)
                || !battle.TryGetSelectedUnit(out var caster)) return false;
            var origin = new CombatGridCell(caster.X, caster.Y);
            var result = new List<CombatGridCell>();
            // Original target-list scan: X outer, Y inner. Do not reorder/deduplicate
            // unit targets here; this is only the positive geometry mask.
            for (var x = 0; x < bounds.size.x; x++)
                for (var y = 0; y < bounds.size.y; y++)
                {
                    var cell = new CombatGridCell(x, y);
                    int weight;
                    var valid = standardOnly
                        ? CombatMagicArea.TryGetStandardWeight(magic, origin, clicked, cell, bounds.size.x, bounds.size.y, out weight)
                        : CombatMagicArea.TryGetWeight(magic, origin, clicked, cell, bounds.size.x, bounds.size.y, out weight);
                    if (!valid) return false;
                    if (weight > 0) result.Add(cell);
                }
            cells = result.AsReadOnly(); return true;
        }
        public bool IsBound { get; private set; }
        public string LastInputMessage { get; private set; } = $"아군 클릭: 선택 / 빈 칸: 이동(AP{GameConstants.BattleAllyMoveApCost}) / 적: 공격 요청.";
        public Rect InputPanelRect
        {
            get
            {
                if (battle == null || !battle.IsPrototypeHudVisible) return default;
                var hud = battle.BattleHudRect;
                return new Rect(hud.x + 12, hud.y + 146, hud.width - 24, 70);
            }
        }
        public BattleMapCommand ActiveCommand { get; private set; } = BattleMapCommand.Automatic;
        private short? menuUnitId;
        private sealed class MagicChoices
        {
            public MagicCatalog Catalog; public MagicRequest[] Commands;
            public System.Func<short, bool?> Eligible; public System.Func<short, int?> TeamId;
            public int? CurrentTeam; public int[] Relations;
        }
        private readonly Dictionary<short, MagicChoices> magicChoices = new();
        private sealed class LearnedMagicDisplay
        {
            public MagicCatalog Catalog; public short[] Ids, SpecialIds;
        }
        private readonly Dictionary<short, LearnedMagicDisplay> learnedMagic = new();
        private readonly HashSet<MagicCatalog> registeredTemporaryCatalogs = new();
        private void RegisterTemporaryCatalog(MagicCatalog catalog)
        {
            if (catalog == null || !battle.TemporaryLocalRulesEnabled || registeredTemporaryCatalogs.Contains(catalog)) return;
            TemporaryMagicCatalogRules.Configure(battle, catalog); registeredTemporaryCatalogs.Add(catalog);
        }
        public int DisplayedMagicCount => specialMenuActive ? SpecialChoiceCount : NormalMagicCount;
        private int NormalMagicCount => battle != null && battle.TryGetSelectedUnit(out var unit)
            && learnedMagic.TryGetValue(unit.Id, out var known) ? known.Ids.Length : MagicChoiceCount;

        // Actual confirmed learned IDs only. Atomic across the complete formation;
        // no synthesized request cost, mode, AP cost, team relation or starting spells.
        public bool ConfigureCharacterLearnedMagic(MagicCatalog catalog, DarkSaver.Core.NativeCharacterProfile profile)
        {
            if (!IsBound || catalog == null || profile == null || !profile.IsValid || !profile.hasClassAbilities
                || !battle.TryGetUnit(100, out var player) || player.JobId != profile.jobId) return false;
            var normal = new List<short>(); var special = new List<short>(); var all = new List<short>();
            foreach (int id in profile.hasBattleMagicSelection ? profile.battleMagicIds : profile.learnedMagicIds)
            {
                if (!catalog.TryGet(id, out _)) return false;
                normal.Add((short)id); all.Add((short)id);
            }
            foreach (int id in profile.usableSkillIds)
            {
                // Hiring/passive IDs remain in the saved profile, not combat spell menus.
                if (!IsNativeSpecialMenuId(id)) continue;
                if (id > short.MaxValue || !catalog.TryGet(id, out _)) return false;
                special.Add((short)id); all.Add((short)id);
            }
            if (!battle.ConfigureUnitLearnedMagic(100, all)) return false;
            RegisterTemporaryCatalog(catalog); CancelMagicTargeting();
            learnedMagic[100] = new LearnedMagicDisplay { Catalog = catalog, Ids = normal.ToArray(), SpecialIds = special.ToArray() };
            return true;
        }

        public bool ConfigureFormationLearnedMagic(MagicCatalog catalog, BattleFormationSnapshot formation)
        {
            if (!IsBound || formation == null) return false;
            var staged = new Dictionary<short, LearnedMagicDisplay>();
            foreach (var member in formation.Members)
            {
                if (!battle.TryGetUnit(member.Unit.Id, out var unit) || unit.JobId != member.Unit.JobId) return false;
                var ids = new short[member.LearnedMagicIds.Count];
                var unique = new HashSet<short>();
                for (var i = 0; i < ids.Length; i++)
                {
                    ids[i] = member.LearnedMagicIds[i];
                    if (catalog == null || !catalog.TryGet(ids[i], out _) || !unique.Add(ids[i])) return false;
                }
                if (magicChoices.TryGetValue(unit.Id, out var choices))
                    foreach (var command in choices.Commands)
                        if (!unique.Contains(command.MagicId) || choices.Catalog != catalog) return false;
                var normalIds = new List<short>(); var specialIds = new List<short>();
                foreach (var id in ids)
                    if (IsNativeSpecialMenuId(id)) specialIds.Add(id); else normalIds.Add(id);
                staged.Add(unit.Id, new LearnedMagicDisplay { Catalog = catalog,
                    Ids = normalIds.ToArray(), SpecialIds = specialIds.ToArray() });
            }
            if (!battle.ConfigureFormationLearnedMagic(formation)) return false;
            RegisterTemporaryCatalog(catalog);
            CancelMagicTargeting();
            foreach (var entry in staged) learnedMagic[entry.Key] = entry.Value;
            return true;
        }

        public bool TryGetDisplayedMagic(int index, out MagicCatalogEntry magic)
        {
            magic = null;
            if (index < 0 || index >= DisplayedMagicCount || !battle.TryGetSelectedUnit(out var unit)) return false;
            if (specialMenuActive)
            {
                if (learnedMagic.TryGetValue(unit.Id, out var learned))
                    return learned.Catalog != null && learned.Catalog.TryGet(learned.SpecialIds[index], out magic);
                return specialChoices.TryGetValue(unit.Id, out var special)
                    && special.Catalog.TryGet(special.Commands[index].MagicId, out magic);
            }
            if (learnedMagic.TryGetValue(unit.Id, out var known))
                return known.Catalog != null && known.Catalog.TryGet(known.Ids[index], out magic);
            return TryGetMagicChoice(index, out var command)
                && magicChoices[unit.Id].Catalog.TryGet(command.MagicId, out magic);
        }

        private bool TryGetDisplayedCommandIndex(int index, out int commandIndex)
        {
            commandIndex = -1;
            if (!TryGetDisplayedMagic(index, out var magic) || !battle.TryGetSelectedUnit(out var unit)
                || !TryGetActiveMagicChoices(unit.Id, out var choices)) return false;
            for (var i = 0; i < choices.Commands.Length; i++)
                if (choices.Commands[i].MagicId == magic.Id) { commandIndex = i; return true; }
            return false;
        }

        public bool TrySelectDisplayedMagic(int index)
        {
            if (IsMagicChoiceOpen && CanUseTemporaryDisplayedMagic(index)
                && !TryGetDisplayedCommandIndex(index, out _))
            {
                battle.TryGetSelectedUnit(out var actor);
                var known = learnedMagic[actor.Id];
                TryGetDisplayedMagic(index, out var selectedMagic);
                battle.TryGetTemporaryMagicMpCost(selectedMagic.Id, out var mpCost);
                bool useArea = selectedMagic.Unit != 8;
                bool started = TryBeginMagicTargeting(known.Catalog, new MagicRequest(
                    (short)selectedMagic.Id, actor.Id, 0, 0,
                    mpCost, System.Array.Empty<short>()), useArea);
                temporaryAreaTargeting = started && useArea;
                if (started) LastInputMessage = useArea
                    ? "범위 안의 대상을 선택하세요. 우클릭 취소."
                    : "대상 부대를 선택하세요. 우클릭 취소.";
                return started;
            }
            if (!IsMagicChoiceOpen || !TryGetDisplayedCommandIndex(index, out var commandIndex))
            { LastInputMessage = "습득은 확인됐지만 시전 요청 정보·비용 연결이 필요합니다."; return false; }
            return TrySelectMagicChoice(commandIndex);
        }
        private bool CanUseTemporaryDisplayedMagic(int index)
        {
            if (battle == null || !battle.TemporaryLocalRulesEnabled || !TryGetDisplayedMagic(index, out var magic)
                || !battle.TryGetSelectedUnit(out var actor) || !learnedMagic.TryGetValue(actor.Id, out var known)) return false;
            RegisterTemporaryCatalog(known.Catalog);
            return battle.TryGetTemporaryMagicMpCost(magic.Id, out _);
        }
        private short? magicChoiceCaster;
        private int magicChoicePage;
        public int MagicChoiceCount => battle != null && battle.TryGetSelectedUnit(out var unit)
            && magicChoices.TryGetValue(unit.Id, out var choices) && choices.Catalog != null ? choices.Commands.Length : 0;
        public bool IsMagicChoiceOpen => isActiveAndEnabled && IsBound && !battle.IsBattleFinished && magicChoiceCaster.HasValue
            && battle.TryGetSelectedUnit(out var unit) && unit.Id == magicChoiceCaster && DisplayedMagicCount > 0;
        public Rect MagicChoiceRect
        {
            get
            {
                if (view != null && battle != null && battle.TryGetSelectedUnit(out var unit))
                {
                    var body = visuals.Find(v => v.UnitId == unit.Id).Body;
                    if (body != null)
                    {
                        var screen = view.WorldToScreenPoint(GetUnitMenuAnchor(body));
                        var blocked = new List<Rect> { battle.BattleHudRect, battle.AlliedRosterRect };
                        if (battle.TryGetInspectedUnit(out _)) blocked.Add(battle.UnitInfoRect);
                        if (screen.z > 0 && TryPlaceActionMenuRect(Rendering.GameScreen.ScreenToGui(screen),
                            new Vector2(Rendering.GameScreen.Width, Rendering.GameScreen.Height), blocked, out var rect)) return rect;
                    }
                }
                return new Rect(Rendering.GameScreen.Width * .5f - 48, Rendering.GameScreen.Height * .5f - 48, 96, 96);
            }
        }
        public static Rect GetMagicChoiceIconRect(int index) => NativeBattleMagicMenu.ButtonRect(index);
        public bool TryChangeMagicChoicePage(int delta)
        {
            if (!IsMagicChoiceOpen) return false;
            int next = (int)System.Math.Max(0L, System.Math.Min((long)NativeBattleMagicMenu.PageCount(DisplayedMagicCount) - 1,
                (long)magicChoicePage + delta));
            if (next == magicChoicePage) return false;
            magicChoicePage = next; return true;
        }

        // Explicit per-instance availability and native state, not the whole catalog or inferred learning rules.
        public bool ConfigureMagicChoices(MagicCatalog catalog, short casterId, IReadOnlyList<MagicRequest> commands,
            System.Func<short, bool?> eligible, System.Func<short, int?> teamId, int? currentTeam, IReadOnlyList<int> relations)
        {
            if (!IsBound || catalog == null || commands == null || eligible == null
                || (relations != null && relations.Count != 8) || !battle.TryGetUnit(casterId, out _)) return false;
            var copy = new MagicRequest[commands.Count]; var ids = new HashSet<short>();
            for (var i = 0; i < copy.Length; i++)
            {
                var command = commands[i];
                if (command == null || command.ActingUnitId != casterId || command.TargetCount != 0
                    || !catalog.TryGet(command.MagicId, out _) || !ids.Add(command.MagicId)) return false;
                if (learnedMagic.TryGetValue(casterId, out var known)
                    && (known.Catalog != catalog || System.Array.IndexOf(known.Ids, command.MagicId) < 0)) return false;
                copy[i] = command;
            }
            int[] relationCopy = null;
            if (relations != null) { relationCopy = new int[8]; for (var i = 0; i < 8; i++) relationCopy[i] = relations[i]; }
            if (magicChoiceCaster == casterId || pendingMagic?.ActingUnitId == casterId) CancelMagicTargeting();
            magicChoices[casterId] = new MagicChoices { Catalog = catalog, Commands = copy, Eligible = eligible,
                TeamId = teamId, CurrentTeam = currentTeam, Relations = relationCopy };
            return true;
        }
        public bool TryGetMagicChoice(int index, out MagicRequest command)
        {
            command = null;
            if (index < 0 || index >= MagicChoiceCount || !battle.TryGetSelectedUnit(out var unit)) return false;
            command = magicChoices[unit.Id].Commands[index]; return true;
        }
        public bool TrySelectMagicChoice(int index)
        {
            if (specialMenuActive)
            {
                if (!IsMagicChoiceOpen || index < 0
                    || !battle.TryGetSelectedUnit(out var caster) || !specialChoices.TryGetValue(caster.Id, out var special)
                    || index >= special.Commands.Length) return false;
                return TryBeginNativeMagicTargeting(special.Catalog, special.Commands[index], special.Eligible,
                    special.TeamId, special.CurrentTeam, special.Relations);
            }
            if (!IsMagicChoiceOpen || !TryGetMagicChoice(index, out var command)) return false;
            var choices = magicChoices[command.ActingUnitId];
            return TryBeginNativeMagicTargeting(choices.Catalog, command, choices.Eligible, choices.TeamId, choices.CurrentTeam, choices.Relations);
        }
        public bool IsActionMenuOpen => isActiveAndEnabled && IsBound && menuUnitId.HasValue
            && battle.TryGetSelectedUnit(out var unit) && unit.Id == menuUnitId && !battle.IsBattleFinished;
        public bool TryOpenActionMenu()
        {
            if (!isActiveAndEnabled || !IsBound || IsSystemMenuOpen || battle.IsBattleFinished || !battle.TryGetSelectedUnit(out var unit)) return false;
            menuUnitId = unit.Id; return true;
        }
        public bool TryChooseCommand(BattleMapCommand command)
        {
            if (!IsActionMenuOpen) return false;
            if (command == BattleMapCommand.System) return TryOpenSystemMenu();
            if (command == BattleMapCommand.Tools) return TryOpenBattleWindowShell(ToolsShell, true);
            if (command == BattleMapCommand.Troop) return TryOpenBattleWindowShell(TroopShell, true);
            if (command == BattleMapCommand.Magic && NormalMagicCount == 0) return TryOpenBattleWindowShell(EmptyMagicShell, true);
            if (command == BattleMapCommand.Special && SpecialChoiceCount == 0) return TryOpenBattleWindowShell(EmptySpecialShell, true);
            if (command == BattleMapCommand.Move || command == BattleMapCommand.Attack || command == BattleMapCommand.Rest)
                return TryChooseBasicCommand(command);
            if ((command == BattleMapCommand.Magic && NormalMagicCount > 0)
                || (command == BattleMapCommand.Special && SpecialChoiceCount > 0))
            {
                battle.TryGetSelectedUnit(out var caster); CancelMagicTargeting(); menuUnitId = null;
                ActiveCommand = BattleMapCommand.Automatic; magicChoiceCaster = caster.Id; magicChoicePage = 0;
                specialMenuActive = command == BattleMapCommand.Special;
                LastInputMessage = specialMenuActive ? "확인된 특수기 목록입니다." : "습득 마법 목록입니다. 요청 정보가 연결된 마법만 선택할 수 있습니다."; return true;
            }
            if (command != BattleMapCommand.Cancel)
            { LastInputMessage = "습득 마법·버프 명령의 확정 데이터 연결이 필요합니다."; return false; }
            CancelMagicTargeting(); menuUnitId = null;
            ActiveCommand = BattleMapCommand.Automatic;
            LastInputMessage = "명령 선택 취소.";
            return true;
        }

        // Persistent basic-command UI. Choosing movement/attack is not an action:
        // only the later validated map click spends AP. Rest is immediate.
        public bool CanChooseBasicCommand(BattleMapCommand command, out string reason)
        {
            reason = string.Empty;
            if (IsSystemMenuOpen) { reason = "전투 메뉴를 먼저 닫으세요."; return false; }
            if (!isActiveAndEnabled || !IsBound || battle == null || battle.IsBattleFinished
                || !battle.TryGetSelectedUnit(out var actor) || !actor.Alive)
            { reason = "행동할 아군을 먼저 선택하세요."; return false; }
            if (command != BattleMapCommand.Move && command != BattleMapCommand.Attack && command != BattleMapCommand.Rest)
            { reason = "기본 명령이 아닙니다."; return false; }
            if (battle.IsMovementInProgress(actor.Id))
            { reason = "이동이 끝난 뒤 명령을 선택하세요."; return false; }
            if (command == BattleMapCommand.Rest) return battle.CanTemporaryRestUnit(actor.Id, out reason);
            var cost = command == BattleMapCommand.Move ? GameConstants.BattleAllyMoveApCost : battle.GetUnitAttackApCost(actor.Id);
            if (cost <= 0 || cost > actor.MaxAp) { reason = "행동 AP 비용이 유효하지 않습니다."; return false; }
            if (actor.Ap != actor.MaxAp) { reason = $"AP 회복 중: {actor.Ap}/{actor.MaxAp}"; return false; }
            return true;
        }

        public bool TryChooseBasicCommand(BattleMapCommand command)
        {
            if (!CanChooseBasicCommand(command, out var reason)) { LastInputMessage = reason; return false; }
            battle.TryGetSelectedUnit(out var actor);
            if (command == BattleMapCommand.Rest && !battle.TryTemporaryRestUnit(actor.Id))
            { LastInputMessage = "현재 휴식할 수 없습니다."; return false; }
            CancelMagicTargeting(); menuUnitId = null;
            ActiveCommand = command == BattleMapCommand.Rest ? BattleMapCommand.Automatic : command;
            LastInputMessage = command == BattleMapCommand.Move ? "이동할 빈 칸을 클릭하세요. 우클릭: 취소" :
                command == BattleMapCommand.Attack ? "기본 공격할 적을 클릭하세요. 우클릭: 취소" :
                $"휴식: HP 회복 / AP {GameConstants.BattleTemporaryRestApCost} 차감 (임시 규칙)";
            return true;
        }

        public Rect GetBasicCommandButtonRect(int index)
        {
            if (index < 0 || index > 2 || battle == null || !battle.IsPrototypeHudVisible) return default;
            var panel = InputPanelRect; var width = (panel.width - 28) / 3;
            return new Rect(panel.x + 8 + index * (width + 6), panel.y + 2, width, 32);
        }
        public bool TryGetActionMenuRect(out Rect rect)
        {
            rect = default;
            if (!IsActionMenuOpen || view == null || !battle.TryGetSelectedUnit(out var unit)) return false;
            var body = visuals.Find(v => v.UnitId == unit.Id).Body;
            if (body == null) return false;
            var screen = view.WorldToScreenPoint(GetUnitMenuAnchor(body)); if (screen.z <= 0) return false;
            var blocked = new List<Rect> { battle.BattleHudRect, battle.AlliedRosterRect };
            if (battle.TryGetInspectedUnit(out _)) blocked.Add(battle.UnitInfoRect);
            if (TryPlaceActionMenuRect(Rendering.GameScreen.ScreenToGui(screen),
                new Vector2(Rendering.GameScreen.Width, Rendering.GameScreen.Height), blocked, out rect)) return true;
            LastInputMessage = "행동 메뉴를 놓을 공간이 없습니다. 화면을 넓히거나 정보창을 닫으세요. Esc/우클릭: 취소.";
            return false;
        }

        public static Vector3 GetUnitMenuAnchor(SpriteRenderer body)
        {
            if (body == null) return Vector3.zero;
            var sprite = body.sprite;
            if (sprite == null) return body.transform.position;
            // Native characters use a foot pivot. Center menus on the full sprite cell,
            // not the pivot or frame-dependent tight mesh bounds.
            var offset = (sprite.rect.size * .5f - sprite.pivot) / sprite.pixelsPerUnit;
            if (body.flipX) offset.x = -offset.x;
            if (body.flipY) offset.y = -offset.y;
            return body.transform.TransformPoint(new Vector3(offset.x, offset.y, 0));
        }

        public static bool TryPlaceActionMenuRect(Vector2 anchor, Vector2 screenSize, IReadOnlyList<Rect> blocked, out Rect rect)
        {
            rect = default;
            var size = GameConstants.BattleActionMenuRadius * 2 + GameConstants.BattleActionMenuButtonSize;
            if (blocked == null || size <= 0 || float.IsNaN(size) || float.IsInfinity(size)
                || float.IsNaN(anchor.x) || float.IsNaN(anchor.y) || float.IsInfinity(anchor.x) || float.IsInfinity(anchor.y)
                || float.IsNaN(screenSize.x) || float.IsNaN(screenSize.y) || float.IsInfinity(screenSize.x) || float.IsInfinity(screenSize.y)
                || screenSize.x < size || screenSize.y < size) return false;
            var maxX = screenSize.x - size; var maxY = screenSize.y - size;
            var xs = new List<float> { Mathf.Clamp(anchor.x - size * .5f, 0, maxX), 0, maxX };
            var ys = new List<float> { Mathf.Clamp(anchor.y - size * .5f, 0, maxY), 0, maxY };
            foreach (var panel in blocked)
            {
                if (float.IsNaN(panel.x) || float.IsNaN(panel.y) || float.IsNaN(panel.width) || float.IsNaN(panel.height)
                    || float.IsInfinity(panel.x) || float.IsInfinity(panel.y) || float.IsInfinity(panel.width) || float.IsInfinity(panel.height)
                    || panel.width < 0 || panel.height < 0) return false;
                xs.Add(Mathf.Clamp(panel.xMin - size, 0, maxX)); xs.Add(Mathf.Clamp(panel.xMax, 0, maxX));
                ys.Add(Mathf.Clamp(panel.yMin - size, 0, maxY)); ys.Add(Mathf.Clamp(panel.yMax, 0, maxY));
            }
            var found = false; var distance = float.PositiveInfinity;
            foreach (var x in xs) foreach (var y in ys)
            {
                var candidate = new Rect(x, y, size, size); var overlaps = false;
                foreach (var panel in blocked)
                    if (panel.width > 0 && panel.height > 0 && candidate.Overlaps(panel)) { overlaps = true; break; }
                if (overlaps) continue;
                var next = (candidate.center - anchor).sqrMagnitude;
                if (found && next >= distance) continue;
                rect = candidate; distance = next; found = true;
            }
            return found;
        }

        // Explicit native command metadata only. Do not derive opcode/mode/cost or
        // learned spells from the raw catalog. Multi-target generation is not resolved.
        public bool TryBeginMagicTargeting(MagicCatalog catalog, MagicRequest command, bool targetsPosition)
        {
            if (!isActiveAndEnabled || !IsBound || catalog == null || command == null
                || command.TargetCount != 0 || !catalog.TryGet(command.MagicId, out var magic)
                || !battle.TryGetSelectedUnit(out var actor) || actor.Id != command.ActingUnitId
                || !battle.IsLearnedMagicAllowed(actor.Id, command.MagicId)) return false;
            CancelMagicTargeting();
            pendingMagic = command; magicTargetsPosition = targetsPosition;
            menuUnitId = null; ActiveCommand = BattleMapCommand.Automatic;
            previewMagic = magic;
            LastInputMessage = targetsPosition ? "마법 위치를 클릭하세요. Esc/우클릭: 취소."
                : "마법 대상 유닛을 클릭하세요. Esc/우클릭: 취소.";
            return true;
        }

        // Explicit native state only. No Enemy-to-team or Alive-to-record fallback.
        public bool TryBeginNativeMagicTargeting(MagicCatalog catalog, MagicRequest command,
            System.Func<short, bool?> recordEligible, System.Func<short, int?> teamId,
            int? currentTeam, IReadOnlyList<int> relations)
        {
            if (recordEligible == null || catalog == null || command == null
                || !catalog.TryGet(command.MagicId, out var magic)
                || (relations != null && relations.Count != 8)) return false;
            int[] ownedRelations = null;
            if (relations != null)
            {
                ownedRelations = new int[8];
                for (var i = 0; i < 8; i++) ownedRelations[i] = relations[i];
            }
            if (!TryBeginMagicTargeting(catalog, command, true)) return false;
            pendingNativeMagic = magic; nativeRecordEligible = recordEligible; nativeTeamId = teamId;
            nativeCurrentTeam = currentTeam; nativeRelations = ownedRelations;
            LastInputMessage = "원본 규칙 마법 대상을 클릭하세요. 미확정 팀/유효성 정보는 요청을 차단합니다.";
            return true;
        }

        public bool TrySendSelectedNativeMagic(CombatGridCell nativeCommand)
            => isActiveAndEnabled && IsBound && pendingMagic != null && (pendingNativeMagic != null || temporaryAreaTargeting)
                && TrySendSelectedMagic(nativeCommand);

        public void CancelMagicTargeting()
        {
            pendingRecoveryItem=null;
            specialMenuActive = false;
            magicDirectionCommand = null;
            magicChoiceCaster = null;
            ClearMagicPreview(); previewMagic = null;
            pendingMagic = null;
            temporaryAreaTargeting = false;
            pendingNativeMagic = null; nativeRecordEligible = null; nativeTeamId = null;
            nativeCurrentTeam = null; nativeRelations = null;
        }

        public void ClearMagicPreview()
        {
            MagicPreviewCellCount = 0; previewCommand = null; previewOrigin = null;
            if (previewRenderer != null) previewRenderer.enabled = false;
            if (previewMesh != null) previewMesh.Clear();
        }

        // Geometry display only. It never approves range/team/cost or sends packets.
        // Directional callers supply the original X sentinel explicitly.
        public bool TryPreviewMagicAt(CombatGridCell clicked)
        {
            if (!isActiveAndEnabled || !IsBound || pendingMagic == null || previewMagic == null
                || !battle.TryGetSelectedUnit(out var actor) || actor.Id != pendingMagic.ActingUnitId)
            { ClearMagicPreview(); return false; }
            var origin = new CombatGridCell(actor.X, actor.Y);
            if (previewCommand.HasValue && previewCommand.Value.Equals(clicked)
                && previewOrigin.HasValue && previewOrigin.Value.Equals(origin)) return true;
            var cells = previewCells; cells.Clear();
            for (var x = 0; x < bounds.size.x; x++)
                for (var y = 0; y < bounds.size.y; y++)
                {
                    var cell = new CombatGridCell(x, y);
                    if (!CombatMagicArea.TryGetWeight(previewMagic, origin, clicked, cell,
                        bounds.size.x, bounds.size.y, out var weight)) { ClearMagicPreview(); return false; }
                    if (weight > 0) cells.Add(cell);
                }
            if (cells.Count == 0)
            {
                ClearMagicPreview();
                // Empty edge directions are valid geometry too. Cache them instead
                // of scanning the complete map again on every stationary frame.
                previewCommand = clicked; previewOrigin = origin; return true;
            }
            if (previewObject == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) { ClearMagicPreview(); return false; }
                previewObject = new GameObject("MagicGeometryPreview") { hideFlags = HideFlags.DontSave };
                previewObject.layer = ground.gameObject.layer;
                previewObject.transform.SetParent(ground.transform, false);
                previewMesh = new Mesh { name = "MagicGeometryPreview", hideFlags = HideFlags.DontSave };
                previewMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                previewMaterial.mainTexture = Texture2D.whiteTexture;
                previewObject.AddComponent<MeshFilter>().sharedMesh = previewMesh;
                previewRenderer = previewObject.AddComponent<MeshRenderer>();
                previewRenderer.sharedMaterial = previewMaterial;
                previewRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                previewRenderer.receiveShadows = false;
            }
            var vertices = previewVertices; var indices = previewIndices; var colors = previewColors;
            vertices.Clear(); indices.Clear(); colors.Clear();
            for (var i = 0; i < cells.Count; i++)
            {
                var native = new Vector3Int(cells[i].X, bounds.yMax - 1 - cells[i].Y, 0);
                for (var corner = 0; corner < 4; corner++)
                {
                    vertices.Add(ground.transform.InverseTransformPoint(ground.CellToWorld(native + PreviewCorners[corner])));
                    colors.Add(Color.white);
                }
                var v = i * 4;
                indices.Add(v); indices.Add(v + 1); indices.Add(v + 2);
                indices.Add(v); indices.Add(v + 2); indices.Add(v + 3);
            }
            previewMesh.Clear(); previewMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            previewMesh.SetVertices(vertices); previewMesh.SetColors(colors); previewMesh.SetTriangles(indices, 0);
            previewMesh.RecalculateBounds(); previewMaterial.color = GameConstants.BattleMagicPreviewColor;
            var floorRenderer = ground.GetComponent<TilemapRenderer>();
            previewRenderer.sortingLayerID = floorRenderer != null ? floorRenderer.sortingLayerID : 0;
            previewRenderer.sortingOrder = (floorRenderer != null ? floorRenderer.sortingOrder : 0) + GameConstants.BattleMagicPreviewSortOffset;
            previewRenderer.enabled = true;
            MagicPreviewCellCount = cells.Count; previewCommand = clicked; previewOrigin = origin;
            return true;
        }

        private bool TrySendSelectedMagic(CombatGridCell cell)
        {
            if (!battle.TryGetSelectedUnit(out var actor) || actor.Id != pendingMagic.ActingUnitId)
            { CancelMagicTargeting(); LastInputMessage = "시전자 선택이 변경되어 마법 명령을 취소했습니다."; return false; }
            if (cell.X > short.MaxValue || cell.Y > short.MaxValue)
            { LastInputMessage = "마법 패킷 좌표 범위를 벗어났습니다."; return false; }
            var command = pendingMagic;
            MagicRequest packet;
            if (pendingNativeMagic != null)
            {
                short? Occupant(CombatGridCell position) => battle.TryGetGridOccupant(position, out var id) ? id : null;
                if (!CombatMagicTargets.TryBuildRequest(pendingNativeMagic, command, new CombatGridCell(actor.X, actor.Y),
                    cell, bounds.size.x, bounds.size.y, Occupant, nativeRecordEligible, nativeTeamId,
                    nativeCurrentTeam, nativeRelations, out packet, out var failure))
                { LastInputMessage = failure; return false; }
            }
            else if (temporaryAreaTargeting)
            {
                if (!battle.TemporaryLocalRulesEnabled || previewMagic == null)
                { LastInputMessage = "임시 마법 판정이 비활성 상태입니다."; return false; }
                if (actor.Ap != actor.MaxAp)
                { LastInputMessage = $"AP 회복 중: {actor.Ap}/{actor.MaxAp}"; return false; }
                if (battle.TryGetTemporaryMagicMpCost(command.MagicId, out int mpCost) && actor.Mp < mpCost)
                { LastInputMessage = "MP가 부족합니다."; return false; }
                var targets = new List<short>(); var seen = new HashSet<short>();
                var origin = new CombatGridCell(actor.X, actor.Y);
                for (int x = 0; x < bounds.size.x; x++)
                    for (int y = 0; y < bounds.size.y; y++)
                    {
                        var targetCell = new CombatGridCell(x, y);
                        if (!CombatMagicArea.TryGetWeight(previewMagic, origin, cell, targetCell,
                            bounds.size.x, bounds.size.y, out int weight))
                        { LastInputMessage = "마법 범위 정보를 확인할 수 없습니다."; return false; }
                        if (weight <= 0 || !battle.TryGetMagicTargetAt(command.MagicId,targetCell, out short id) || !seen.Add(id)) continue;
                        // Temporary authority supplies effect/team/range validity. Native team IDs are not guessed.
                        if (!battle.CanStartMagicRequest(command.MagicId, actor.Id, new[] { id }, out _)) continue;
                        if (targets.Count == byte.MaxValue)
                        { LastInputMessage = "마법 대상 수가 로컬 패킷 한도를 초과했습니다."; return false; }
                        targets.Add(id);
                    }
                if (targets.Count == 0) { LastInputMessage = "범위 안에 시전 가능한 대상이 없습니다."; return false; }
                packet = new MagicRequest(command.MagicId, actor.Id, (short)cell.X, (short)cell.Y,
                    command.ComputedCost, targets.ToArray(), command.OriginalOpcode, command.Mode);
            }
            else
            {
                short[] targets = System.Array.Empty<short>();
                if (!magicTargetsPosition)
                {
                    if (!battle.TryGetMagicTargetAt(command.MagicId,cell, out var id) || !battle.TryGetUnit(id, out var target) || !target.Alive && !battle.IsRevivalMagic(command.MagicId))
                    { LastInputMessage = "유효한 대상 유닛 또는 부활할 아군의 위치를 클릭하세요."; return false; }
                    if(battle.TemporaryLocalRulesEnabled && previewMagic!=null && previewMagic.Unit==8)
                    {
                        var group=new List<short>();int commander=id/10*10;
                        for(int offset=0;offset<9;offset++)
                        {short member=(short)(commander+offset);if(battle.CanStartMagicRequest(command.MagicId,actor.Id,new[]{member},out _))group.Add(member);}
                        targets=group.ToArray();
                    }
                    else targets = new[] { id };
                }
                packet = new MagicRequest(command.MagicId, actor.Id, (short)cell.X, (short)cell.Y,
                    command.ComputedCost, targets, command.OriginalOpcode, command.Mode);
            }
            var sender = GetComponent<BattleRequestSender>();
            if (sender == null) { LastInputMessage = "마법 요청 연결이 없습니다."; return false; }
            var delivered = sender.TrySendMagic(packet, out var reason);
            LastInputMessage = delivered ? (battle.TemporaryLocalRulesEnabled
                ? "시전했습니다." : "마법 요청 전달됨: 승인·효과·MP는 독립 응답을 기다립니다.") : reason;
            if (delivered && ReferenceEquals(pendingMagic, command)) CancelMagicTargeting();
            return delivered;
        }

        private static bool AllowsEightDirections(CombatGridCell from, CombatGridCell to)
        {
            var dx = System.Math.Abs((long)to.X - from.X); var dy = System.Math.Abs((long)to.Y - from.Y);
            return dx <= 1 && dy <= 1 && (dx != 0 || dy != 0);
        }

        public bool TryFindWorldMovementPath(Vector3 destination, out IReadOnlyList<CombatGridCell> path)
        {
            path = System.Array.Empty<CombatGridCell>();
            return isActiveAndEnabled && IsBound && TryWorldToCell(destination, out var cell)
                && battle.TryFindSelectedMovementPath(cell, out path);
        }

        public bool TryGetReachableMovementDestinations(out IReadOnlyList<CombatGridCell> cells)
        {
            cells = System.Array.Empty<CombatGridCell>();
            return isActiveAndEnabled && IsBound && battle.TryGetSelectedUnit(out var unit)
                && battle.TryGetTroopColumn(unit.Id, out _)
                && battle.TryGetSelectedReachableDestinations(out cells);
        }

        // Registered preparation units plus explicitly supplied native job artwork.
        // This does not enter a battle or consume/return any owned roster state.
        public bool TryBindPreparedFormation(BattleFormationSnapshot formation, Tilemap groundMap, Tilemap terrainMap,
            Camera camera, IReadOnlyList<BattleMapVisual> existing,
            IReadOnlyDictionary<int, DirectionalSpriteAnimator> jobArtwork)
        {
            battle = GetComponent<BattleSceneRoot>();
            if (!isActiveAndEnabled || IsBound || battle == null || battle.GridConfigured || formation == null
                || groundMap == null || terrainMap == null || camera == null || existing == null || jobArtwork == null) return false;
            var area = groundMap.cellBounds;
            var ids = new HashSet<short>(); var input = new List<BattleMapVisual>(existing);
            foreach (var visual in existing) if (!ids.Add(visual.UnitId)) return false;
            foreach (var member in formation.Members)
            {
                if (!ids.Add(member.Unit.Id) || !battle.TryGetUnit(member.Unit.Id, out var unit) || !unit.Alive || unit.Enemy
                    || unit.JobId != member.Unit.JobId || unit.X != member.Position.X || unit.Y != member.Position.Y
                    || !jobArtwork.TryGetValue(unit.JobId, out var art) || art == null || !art.HasCompleteDirections
                    || art.GetComponent<SpriteRenderer>() == null
                    || !TryGetPlacementMovement(groundMap, terrainMap, area, battle.TraversalCatalogsForPreparation,
                        unit.JobId, member.Position, out var cost) || cost == CombatTerrainTraversal.ImpassableValue) return false;
            }
            var pending = new List<GameObject>(); var success = false;
            try
            {
                foreach (var member in formation.Members)
                {
                    var art = jobArtwork[member.Unit.JobId]; var source = art.GetComponent<SpriteRenderer>();
                    var go = new GameObject("Formation unit " + member.Unit.Id); pending.Add(go);
                    go.transform.position = groundMap.GetCellCenterWorld(new Vector3Int(member.Position.X, area.yMax - 1 - member.Position.Y, 0));
                    go.transform.rotation = art.transform.rotation; go.transform.localScale = art.transform.lossyScale;
                    go.transform.SetParent(transform, true);
                    var body = go.AddComponent<SpriteRenderer>(); body.color = source.color;
                    body.sortingLayerID = source.sortingLayerID; body.sortingOrder = source.sortingOrder;
                    body.flipX = source.flipX; body.flipY = source.flipY; body.sharedMaterial = source.sharedMaterial;
                    if (!art.TryCopyConfigurationTo(go.AddComponent<DirectionalSpriteAnimator>())) return false;
                    input.Add(new BattleMapVisual(member.Unit.Id, body));
                }
                if (!TryBind(groundMap, terrainMap, camera, input)) return false;
                formationBodies.AddRange(pending); success = true; return true;
            }
            finally
            {
                if (!success) foreach (var go in pending) DestroyFormationBody(go);
            }
        }

        private static void DestroyFormationBody(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
        }

        public bool TryBind(Tilemap groundMap, Tilemap terrainMap, Camera camera,
            IReadOnlyList<BattleMapVisual> input,
            System.Func<CombatGridCell, CombatGridCell, bool> movementDirection = null)
        {
            battle = GetComponent<BattleSceneRoot>();
            if (!isActiveAndEnabled || IsBound || battle == null || battle.GridConfigured
                || groundMap == null || terrainMap == null || camera == null || input == null || input.Count == 0
                || groundMap.layoutGrid == null || terrainMap.layoutGrid != groundMap.layoutGrid
                || groundMap.gameObject.scene != terrainMap.gameObject.scene) return false;
            var area = groundMap.cellBounds;
            if (area.xMin != 0 || area.yMin != 0 || area.zMin != 0 || area.size.z != 1 || area.size.x <= 0 || area.size.y <= 0) return false;
            var placements = new List<CombatGridPlacement>(); var pending = new List<BattleMapVisual>();
            var uniqueBodies = new HashSet<SpriteRenderer>();
            foreach (var visual in input)
            {
                if (visual.Body == null || visual.Body.sprite == null || !uniqueBodies.Add(visual.Body)
                    || !battle.TryGetUnit(visual.UnitId, out _)) return false;
                var cell = groundMap.WorldToCell(visual.Body.transform.position);
                if (!groundMap.HasTile(cell)) return false;
                placements.Add(new CombatGridPlacement(visual.UnitId, new CombatGridCell(cell.x, area.yMax - 1 - cell.y)));
                pending.Add(visual);
            }
            // Existing bodies supply native initial positions only. Do not turn world
            // colliders into class-specific combat traversal. Eight directions are user-approved.
            if (!battle.TryConfigureGrid(area.size.x, area.size.y, placements,
                (unit, cell) => TryGetMovement(groundMap, terrainMap, area, unit.Id, cell, out var value)
                    && value != CombatTerrainTraversal.ImpassableValue, movementDirection ?? AllowsEightDirections,
                (_, cell) => groundMap.HasTile(new Vector3Int(cell.X, area.yMax - 1 - cell.Y, 0)),
                GameConstants.BattlePreventDiagonalCornerCutting,
                movementCost: (unit, cell) => TryGetMovement(groundMap, terrainMap, area, unit.Id, cell, out var cost)
                    ? cost : (int?)null)) return false;
            audioLifeStates.Clear();
            ground = groundMap; terrain = terrainMap; view = camera; bounds = area;
            foreach (var visual in pending)
            {
                visuals.Add(visual); originalEnabled.Add(visual.Body.enabled);
                var animator = visual.Body.GetComponent<DirectionalSpriteAnimator>();
                animators.Add(animator); originalFacing.Add(animator != null ? animator.Facing : Vector2.zero);
                originalMoving.Add(animator != null && animator.IsMoving);
                var body = visual.Body.GetComponent<Rigidbody2D>();
                bodies.Add(body); originalSimulated.Add(body != null && body.simulated);
                var cell = groundMap.WorldToCell(visual.Body.transform.position);
                visibleCells.Add(new CombatGridCell(cell.x, area.yMax - 1 - cell.y));
            }
            configured = true; IsBound = true; battle.SetMapPresentationActive(true);
            // A free-walking player can enter combat between cell centres. Snap on bind,
            // rather than waiting for the first grid step to align the visual and body.
            for(int i=0;i<visuals.Count;i++)SynchronizePosition(i,visibleCells[i]);
            Listen(); RefreshVisualState(); return true;
        }

        private void Listen()
        {
            if (listening) return;
            battle.AttackPresentationStarted += OnAttackPresentation;
            battle.MovementStepApplied += OnMovementStep;
            battle.CombatResultApplied += OnCombatResult;
            battle.VitalsDisplayChanged += OnVitalsDisplay;
            ListenMagicEffects();
            listening = true;
        }

        private sealed class CombatFeedback
        {
            public Vector3 Position;
            public string Text;
            public int ColorRow;
            public float Age;
        }
        private readonly List<CombatFeedback> combatFeedback = new();
        public int CombatFeedbackCount => combatFeedback.Count;
        public string GetCombatFeedbackText(int index) => index >= 0 && index < combatFeedback.Count ? combatFeedback[index].Text : null;
        public int GetCombatFeedbackColorRow(int index) => index >= 0 && index < combatFeedback.Count ? combatFeedback[index].ColorRow : -1;
        private void OnVitalsDisplay(short unitId, int delta, bool mp)
        {
            if (delta == 0) return;
            var text = delta.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AddCombatFeedback(unitId, delta > 0 ? "+" + text : text, mp ? 3 : delta > 0 ? 1 : 0);
        }
        private void OnCombatResult(CombatResult result)
        {
            if (!result.Hit) AddCombatFeedback(result.TargetId, "MISS", 3);
        }
        private void AddCombatFeedback(short unitId, string text, int colorRow)
        {
            if (!isActiveAndEnabled || !IsBound) return;
            var visual = visuals.Find(v => v.UnitId == unitId);
            if (visual.Body == null) return;
            if (combatFeedback.Count >= GameConstants.BattleFeedbackMaximum) combatFeedback.RemoveAt(0);
            combatFeedback.Add(new CombatFeedback { Position = visual.Body.transform.position,
                Text = text, ColorRow = colorRow });
        }
        public void AdvanceCombatFeedback(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            for (int i = combatFeedback.Count - 1; i >= 0; i--)
            {
                combatFeedback[i].Age += seconds;
                if (combatFeedback[i].Age >= GameConstants.BattleFeedbackSeconds) combatFeedback.RemoveAt(i);
            }
        }

        private void OnAttackPresentation(BattleAttackPresentation presentation)
        {
            if (!isActiveAndEnabled || !IsBound || !presentation.HasDirection) return;
            Face(presentation.AttackerId, presentation.AttackerFacing);
            Face(presentation.TargetId, presentation.TargetFacing);
            StartAttackEffect(presentation.AttackerId, presentation.TargetId, true);
            StartAttackEffect(presentation.TargetId, presentation.AttackerId, false);
        }

        private sealed class AttackEffect
        {
            public short UnitId;
            public SpriteRenderer Renderer;
            public Vector3 Offset;
            public float Age;
            public bool IsImpact;
        }
        private readonly Dictionary<short, AttackEffect> attackEffects = new();
        private Sprite[] attackFrames;
        public int ActiveAttackEffectCount
        {
            get { var count = 0; foreach (var effect in attackEffects.Values) if (effect.Renderer != null && effect.Renderer.enabled) count++; return count; }
        }
        private void StartAttackEffect(short unitId, short otherId, bool attacker)
        {
            if (!battle.TryGetUnit(unitId, out var unit) || !battle.TryGetUnit(otherId, out var other)) return;
            var visual = visuals.Find(v => v.UnitId == unitId);
            if (visual.Body == null) return;
            attackFrames ??= BattleAttackEffectFrames.Load();
            if (attackFrames.Length != 4 || System.Array.Exists(attackFrames, f => f == null)) return;
            if (!attackEffects.TryGetValue(unitId, out var effect))
            {
                var go = new GameObject("Native Attack Fx2 " + unitId); go.transform.SetParent(transform, false);
                effect = new AttackEffect { UnitId = unitId, Renderer = go.AddComponent<SpriteRenderer>() };
                attackEffects.Add(unitId, effect);
            }
            // AttackChar supplies half-tile offsets only for Manhattan distance < 3.
            var dx = other.X - unit.X; var dy = other.Y - unit.Y;
            if (attacker && System.Math.Abs(dx) + System.Math.Abs(dy) >= 3)
            { effect.Renderer.enabled = false; return; }
            effect.Offset = attacker && System.Math.Abs(dx) + System.Math.Abs(dy) < 3
                ? ground.transform.TransformVector(new Vector3(dx * ground.layoutGrid.cellSize.x * .5f,
                    -dy * ground.layoutGrid.cellSize.y * .5f, 0)) : Vector3.zero;
            effect.Age = 0;
            effect.IsImpact = !attacker;
            effect.Renderer.gameObject.layer = visual.Body.gameObject.layer;
            effect.Renderer.sortingLayerID = visual.Body.sortingLayerID;
            effect.Renderer.sortingOrder = visual.Body.sortingOrder + 1;
            effect.Renderer.sprite = attackFrames[0]; effect.Renderer.enabled = unit.Alive;
            effect.Renderer.transform.position = (effect.IsImpact ? GetUnitMenuAnchor(visual.Body) : visual.Body.transform.position) + effect.Offset;
        }
        public void AdvanceAttackEffects(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            foreach (var effect in attackEffects.Values)
            {
                if (effect.Renderer == null || !effect.Renderer.enabled) continue;
                effect.Age += seconds; var frame = BattleAttackEffectFrames.FrameAt(effect.Age);
                if (frame < 0 || !battle.TryGetUnit(effect.UnitId, out var unit) || !unit.Alive)
                { effect.Renderer.enabled = false; continue; }
                var visual = visuals.Find(v => v.UnitId == effect.UnitId);
                if (visual.Body == null) { effect.Renderer.enabled = false; continue; }
                effect.Renderer.sprite = attackFrames[frame];
                effect.Renderer.transform.position = (effect.IsImpact ? GetUnitMenuAnchor(visual.Body) : visual.Body.transform.position) + effect.Offset;
            }
        }

        private void OnMovementStep(CombatMovementStep step)
        {
            if (!isActiveAndEnabled || !IsBound || ground == null) return;
            for (var i = 0; i < visuals.Count; i++)
            {
                var visual = visuals[i];
                if (visual.UnitId != step.UnitId || visual.Body == null) continue;
                SynchronizePosition(i, step.To);
                Face(step.UnitId, new Vector2(step.To.X.CompareTo(step.From.X), step.From.Y.CompareTo(step.To.Y)));
                return;
            }
        }

        private void SynchronizePosition(int index, CombatGridCell cell)
        {
            var visual = visuals[index];
            var position = ground.GetCellCenterWorld(new Vector3Int(cell.X, bounds.yMax - 1 - cell.Y, 0));
            position.z = visual.Body.transform.position.z;
            if (bodies[index] != null) bodies[index].position = new Vector2(position.x, position.y);
            visual.Body.transform.position = position; visibleCells[index] = cell;
            offsetVisuals.Remove(index);
        }

        private void PresentMovement(int index, CombatMovementStep step, float progress)
        {
            Face(step.UnitId, new Vector2(step.To.X.CompareTo(step.From.X), step.From.Y.CompareTo(step.To.Y)));
            if (progress <= 0) return;
            var from = ground.GetCellCenterWorld(new Vector3Int(step.From.X, bounds.yMax - 1 - step.From.Y, 0));
            var to = ground.GetCellCenterWorld(new Vector3Int(step.To.X, bounds.yMax - 1 - step.To.Y, 0));
            var position = Vector3.LerpUnclamped(from, to, progress);
            position.z = visuals[index].Body.transform.position.z;
            if (bodies[index] != null) bodies[index].position = new Vector2(position.x, position.y);
            visuals[index].Body.transform.position = position; offsetVisuals.Add(index);
        }

        private void Face(short id, Vector2 direction)
        {
            if (!battle.TryGetUnit(id, out var unit) || !unit.Alive) return;
            for (var i = 0; i < visuals.Count; i++)
                if (visuals[i].UnitId == id && animators[i] != null && animators[i].isActiveAndEnabled)
                    animators[i].TrySetFacing(direction);
        }

        public bool TryWorldToCell(Vector3 position, out CombatGridCell cell)
        {
            cell = default;
            if (!IsBound || ground == null) return false;
            var native = ground.WorldToCell(position);
            if (!bounds.Contains(native) || !ground.HasTile(native)) return false;
            cell = new CombatGridCell(native.x, bounds.yMax - 1 - native.y); return true;
        }

        private bool TryGetAttribute(CombatUnitSnapshot unit, out short attribute)
            => TryGetAttribute(terrain, bounds, new CombatGridCell(unit.X, unit.Y), out attribute);

        public static bool TryGetPlacementMovement(Tilemap floor, Tilemap attributes, BoundsInt expectedBounds,
            IReadOnlyList<DarkSaver.Jobs.JobCatalog> catalogs, int jobId, CombatGridCell cell, out int value)
        {
            value = 0;
            return floor != null && attributes != null && floor.layoutGrid != null
                && floor.layoutGrid == attributes.layoutGrid && floor.gameObject.scene == attributes.gameObject.scene
                && floor.cellBounds.Equals(expectedBounds) && expectedBounds.xMin == 0 && expectedBounds.yMin == 0
                && expectedBounds.zMin == 0 && expectedBounds.size.z == 1
                && cell.X >= 0 && cell.X < expectedBounds.size.x && cell.Y >= 0 && cell.Y < expectedBounds.size.y
                && floor.HasTile(new Vector3Int(cell.X, expectedBounds.yMax - 1 - cell.Y, 0))
                && TryGetAttribute(attributes, expectedBounds, cell, out var attribute)
                && CombatTerrainTraversal.TryGetTroopColumn(jobId, catalogs, out var column)
                && CombatTerrainTraversal.TryGetMovementValue(attribute, column, out value);
        }

        private static bool TryGetAttribute(Tilemap attributes, BoundsInt area, CombatGridCell cell, out short attribute)
        {
            attribute = 0;
            if (attributes == null) return false;
            var tile = attributes.GetTile(new Vector3Int(cell.X, area.yMax - 1 - cell.Y, 0));
            if (tile == null) return true; // Native converter omits normalized terrain zero.
            var name = tile.name;
            var value = name.StartsWith("Terrain_") ? name.Substring(8)
                : name.StartsWith("Terrain Attribute ") ? name.Substring(18) : null;
            return value != null && short.TryParse(value, out attribute) && attribute >= 0 && attribute <= 0x1B;
        }

        private bool TryGetMovement(Tilemap floor, Tilemap attributes, BoundsInt area,
            short unitId, CombatGridCell cell, out int value)
        {
            value = 0;
            return floor != null && cell.X >= 0 && cell.X < area.size.x && cell.Y >= 0 && cell.Y < area.size.y
                && floor.HasTile(new Vector3Int(cell.X, area.yMax - 1 - cell.Y, 0))
                && TryGetAttribute(attributes, area, cell, out var attribute)
                && battle.TryGetTerrainMovement(unitId, attribute, out value);
        }

        public bool TryGetMovementAtCell(short unitId, CombatGridCell cell, out int value)
        {
            value = 0;
            return isActiveAndEnabled && IsBound && TryGetMovement(ground, terrain, bounds, unitId, cell, out value);
        }

        public bool TryHandleWorldClick(Vector3 position)
        {
            if (!isActiveAndEnabled || !IsBound || battle.IsBattleFinished || SystemMenuBlocksMapPointer || IsMagicChoiceOpen) return false;
            // Direction commands depend on the pointer axis, not on a floor tile.
            // Keep confirmation consistent with the direction preview at map edges.
            if (RequiresMagicDirection) return TryPreviewMagicDirectionAt(position) && TryConfirmMagicDirection();
            if (!TryWorldToCell(position, out var cell)) return false;
            if (IsToolTargeting)
            {
                if(!battle.TryGetGridOccupant(cell,out var toolTarget)) {LastInputMessage="사용할 대상을 선택하세요.";return false;}
                return TryUseSelectedRecoveryTool(toolTarget);
            }
            if (IsMagicTargeting) return TrySendSelectedMagic(cell);
            if (!battle.TryGetGridOccupant(cell, out var id))
            {
                if (ActiveCommand == BattleMapCommand.Attack) { LastInputMessage = "공격할 적을 클릭하세요."; return false; }
                if (!battle.TryGetSelectedUnit(out var selected))
                { LastInputMessage = "이동할 아군을 먼저 선택하세요."; return false; }
                var started = battle.TryMoveTo(selected.Id, cell, out var moveReason);
                LastInputMessage = started ? $"이동 시작: 행동 한 번에 AP{GameConstants.BattleAllyMoveApCost} 차감." : moveReason;
                return started;
            }
            if (!battle.TryGetUnit(id, out var target)) return false;
            battle.TryInspectUnit(id);
            if (!target.Enemy)
            {
                if (!battle.TrySelectUnit(id)) return false;
                TryOpenActionMenu(); return true;
            }
            if (ActiveCommand == BattleMapCommand.Move) { LastInputMessage = "이동할 빈 칸을 클릭하세요."; return false; }
            if (!battle.TryGetSelectedUnit(out var actor))
            { LastInputMessage = "행동할 아군을 먼저 선택하세요."; return false; }
            if (!TryGetAttribute(actor, out var actorAttribute) || !TryGetAttribute(target, out var targetAttribute))
            { LastInputMessage = "지형 속성 데이터가 유효하지 않습니다."; return false; }
            var sender = GetComponent<BattleRequestSender>();
            if (sender == null) { LastInputMessage = "공격 요청 연결이 없습니다."; return false; }
            var delivered = sender.TrySendAttack(new AttackRequest(target.Id, actor.Id, targetAttribute, actorAttribute), out var reason);
            LastInputMessage = delivered ? (battle.TemporaryLocalRulesEnabled
                ? battle.Status : "공격 요청 전달됨: 피해 결과는 독립 응답을 기다립니다.") : reason;
            return delivered;
        }

        // Use the same pointer gate for real input and isolated UI regression checks.
        public bool TryBlockMapPointer(Vector2 screenPoint)
        {
            if (!isActiveAndEnabled || !IsBound || battle == null) return false;
            if (IsMagicChoiceOpen || SystemMenuBlocksMapPointer) return true;
            if (!Rendering.GameScreen.TryScreenToGui(screenPoint, out var guiPoint))
            { if (!RequiresMagicDirection || !magicDirectionCommand.HasValue) ClearMagicPreview(); return true; }
            if (!InputPanelRect.Contains(guiPoint) && !battle.IsPointerOverBattleHud(screenPoint)) return false;
            // A chosen direction is a fixed command, not a hover target. UI hover
            // must neither hide its geometry nor send it to the map below the UI.
            if (!RequiresMagicDirection || !magicDirectionCommand.HasValue) ClearMagicPreview();
            return true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { TryHandleCancelInput(true); return; }
            if (Input.GetMouseButtonDown(1)) { TryHandleCancelInput(false); return; }
            if (!IsBound || view == null || battle.IsBattleFinished) return;
            if (IsMagicChoiceOpen)
            {
                if (Input.GetKeyDown(KeyCode.PageDown) || Input.mouseScrollDelta.y < 0) TryChangeMagicChoicePage(1);
                if (Input.GetKeyDown(KeyCode.PageUp) || Input.mouseScrollDelta.y > 0) TryChangeMagicChoicePage(-1);
            }
            var pointer = (Vector2)Input.mousePosition;
            if (TryBlockMapPointer(pointer)) return;
            var ray = view.ScreenPointToRay(pointer); var plane = new Plane(Vector3.forward, ground.transform.position);
            if (!plane.Raycast(ray, out var distance)) { ClearMagicPreview(); return; }
            var position = ray.GetPoint(distance);
            if (RequiresMagicDirection) TryPreviewMagicDirectionAt(position);
            else if (IsMagicTargeting && previewMagic != null
                && previewMagic.Range != -1 && previewMagic.Range != -3 && previewMagic.Range != -4)
            {
                if (TryWorldToCell(position, out var hovered)) TryPreviewMagicAt(hovered);
                else ClearMagicPreview();
            }
            if (Input.GetMouseButtonDown(0)) TryHandleWorldClick(position);
        }

        private readonly Dictionary<short,UnitLifeState> audioLifeStates=new();
        public void RefreshVisualState()
        {
            if (!IsBound) return;
            if (magicChoiceCaster.HasValue && !IsMagicChoiceOpen) { magicChoiceCaster = null; specialMenuActive = false; }
            if (battle.IsBattleFinished) { CloseSystemMenu(); CancelMagicTargeting(); menuUnitId = null; ActiveCommand = BattleMapCommand.Automatic; }
            if (IsMagicTargeting && (!battle.TryGetSelectedUnit(out var caster) || caster.Id != pendingMagic.ActingUnitId))
                CancelMagicTargeting();
            // Selection does not stop the movement clock. Rebase a fixed direction
            // on confirmed grid coordinates, never on interpolated sprite positions.
            if (RequiresMagicDirection && magicDirectionCommand.HasValue)
                TryPreviewMagicAt(magicDirectionCommand.Value);
            for (var i = 0; i < visuals.Count; i++)
                if (visuals[i].Body != null && battle.TryGetUnit(visuals[i].UnitId, out var unit))
                {
                    if(audioLifeStates.TryGetValue(unit.Id,out var before) && before==UnitLifeState.Active && unit.LifeState!=UnitLifeState.Active)
                        DarkSaver.World.WorldAudioService.PlayDefeat(unit.JobId);
                    audioLifeStates[unit.Id]=unit.LifeState;
                    visuals[i].Body.enabled = originalEnabled[i] && unit.LifeState == UnitLifeState.Active;
                    if (animators[i] != null) animators[i].SetMovingPresentation(unit.Alive && battle.IsMovementInProgress(unit.Id));
                    // World infantry colliders must not displace confirmed combat
                    // positions (e.g. a flying unit). Combat grid owns occupancy.
                    if (bodies[i] != null) bodies[i].simulated = false;
                    var cell = new CombatGridCell(unit.X, unit.Y);
                    if (unit.LifeState != UnitLifeState.Removed && !visibleCells[i].Equals(cell)) SynchronizePosition(i, cell);
                    if (battle.TryGetMovementPresentation(unit.Id, out var step, out var progress)) PresentMovement(i, step, progress);
                    else if (offsetVisuals.Contains(i)) SynchronizePosition(i, cell);
                }
        }

        private InGameUi ui;
        private void LateUpdate() { AdvanceCombatFeedback(Time.unscaledDeltaTime); RefreshVisualState(); AdvanceAttackEffects(Time.unscaledDeltaTime); AdvanceMagicEffects(Time.unscaledDeltaTime); DrawInGameUi(); }

        public Sprite GetUnitPortrait(short unitId)
        {
            foreach (var visual in visuals)
                if (visual.UnitId == unitId && visual.Body != null)
                    return NativeBattleStatus.UnitPicture(visual.Body,
                        battle != null && battle.TryGetUnit(unitId, out var unit) ? unit.JobId : 0);
            return null;
        }

        private void DrawInGameUi()
        {
            if (!IsBound || view == null) { ui?.Hide(); return; }
            InGameUi.Ensure(ref ui, this, 40);
            ui.Begin();
            for (int i = 0; i < combatFeedback.Count; i++)
            {
                var feedback = combatFeedback[i];
                var screen = view.WorldToScreenPoint(feedback.Position);
                if (screen.z <= 0) continue;
                if (!BattleFeedbackMotion.TryGetOffset(feedback.Age, out var offset)) continue;
                var scale = GameConstants.BattleFeedbackPixelScale;
                var logical = Rendering.GameScreen.ScreenToGui(screen);
                logical.x += offset.x * scale;
                var top = logical.y - 40 + offset.y * scale;
                if (feedback.Text == "MISS")
                {
                    var sprite = BattleFeedbackIcons.Miss;
                    if (sprite != null) ui.Portrait("combat-feedback-" + i, new Rect(logical.x - 18 * scale, top, 36 * scale, 11 * scale), sprite);
                }
                else
                {
                    var left = logical.x - feedback.Text.Length * 4 * scale;
                    for (var glyph = 0; glyph < feedback.Text.Length; glyph++)
                    {
                        var sprite = BattleFeedbackIcons.Number(feedback.Text[glyph], feedback.ColorRow);
                        if (sprite != null) ui.Portrait("combat-feedback-" + i + "-" + glyph,
                            new Rect(left + glyph * 8 * scale, top, 8 * scale, 14 * scale), sprite);
                    }
                }
            }
            if (RequiresMagicDirection && battle.IsPrototypeHudVisible)
            {
                var panel = MagicDirectionRect; ui.BattlePanel("direction", panel, "마법 방향 선택");
                var directions = new[] { BattleMagicDirection.Up, BattleMagicDirection.Down, BattleMagicDirection.Left, BattleMagicDirection.Right };
                var labels = new[] { "위", "아래", "왼쪽", "오른쪽" };
                for (var i = 0; i < 4; i++)
                {
                    var direction = directions[i];
                    ui.Button("direction-" + i, new Rect(panel.x + 12 + i * 84, panel.y + 36, 80, 32), labels[i],
                        () => TryChooseMagicDirection(direction), pendingNativeMagic != null || temporaryAreaTargeting);
                }
                ui.Button("direction-confirm", new Rect(panel.x + 12, panel.y + 86, 164, 32), "시전 확인", () => TryConfirmMagicDirection(), (pendingNativeMagic != null || temporaryAreaTargeting) && magicDirectionCommand.HasValue);
                ui.Button("direction-cancel", new Rect(panel.x + 184, panel.y + 86, 164, 32), "취소", CancelMagicTargeting);
            }
            if (TryGetActionMenuRect(out var menu))
            {
                var commands = new BattleMapCommand?[] { BattleMapCommand.Move, BattleMapCommand.Attack, BattleMapCommand.System,
                    BattleMapCommand.Rest, BattleMapCommand.Magic, BattleMapCommand.Special, BattleMapCommand.Troop, BattleMapCommand.Tools };
                var labels = new[] { "이동", "공격", "게임설정", "휴식(HP/MP 회복)", "마법", "특수기", "용병들에게 명령", "도구" };
                for (var i = 0; i < commands.Length; i++)
                {
                    var command = commands[i]; var reason = "기능 미연결";
                    var allowed = command.HasValue && (command.Value == BattleMapCommand.Magic
                        || command.Value == BattleMapCommand.Special || command.Value == BattleMapCommand.System || command.Value == BattleMapCommand.Tools
                            || command.Value == BattleMapCommand.Troop || CanChooseBasicCommand(command.Value, out reason));
                    var emptySkill = command == BattleMapCommand.Magic && NormalMagicCount == 0
                        || command == BattleMapCommand.Special && SpecialChoiceCount == 0;
                    ui.ActionButton("radial-" + i, NativeBattleActionMenu.ButtonRect(menu, i), labels[i],
                        () => { if (command.HasValue) TryChooseCommand(command.Value); }, allowed,
                        allowed ? labels[i] + (emptySkill ? " / 빈 습득 목록·UI 틀" : command == BattleMapCommand.Tools ? " / 소지품 조회" : "")
                            : labels[i] + " / " + reason,
                        BattleIconFrames.Action(NativeBattleActionMenu.IconColumn(i), Time.unscaledTimeAsDouble));
                }
                // Native center is the character cell, not an invented cancel button. Esc/right click remain available.
            }
            if (IsMagicChoiceOpen)
            {
                ui.ModalBlocker("magic-modal-input");
                var panel = MagicChoiceRect;
                var count = DisplayedMagicCount;
                magicChoicePage = Mathf.Clamp(magicChoicePage, 0, NativeBattleMagicMenu.PageCount(count) - 1);
                for (var slot = 0; slot < NativeBattleMagicMenu.Capacity; slot++)
                {
                    int i = NativeBattleMagicMenu.DisplayIndex(count, magicChoicePage, slot);
                    if (i < 0) continue;
                    TryGetDisplayedMagic(i, out var magic);
                    var nativeCommand = TryGetDisplayedCommandIndex(i, out _);
                    var temporaryCommand = !nativeCommand && CanUseTemporaryDisplayedMagic(i);
                    var canSelect = nativeCommand || temporaryCommand;
                    var index = i;
                    var name = magic?.Name ?? "알 수 없는 마법";
                    var info = magic == null ? name : $"{name} / 원본 MP {magic.Mp}";
                    if (temporaryCommand && battle.TryGetTemporaryMagicMpCost(magic.Id, out var temporaryMp)) info += $" / 임시 소모 MP {temporaryMp}";
                    if (!canSelect) info += " / 시전 정보 미연결";
                    if (specialMenuActive && magic != null && TryGetSpecialVisualMapping(
                        learnedMagic.TryGetValue(magicChoiceCaster.Value, out var learned) ? learned.Catalog
                            : specialChoices.TryGetValue(magicChoiceCaster.Value, out var special) ? special.Catalog : null,
                        magic.Id, out var nativePattern, out var visualMapped))
                        info += $" / 원본 패턴 {nativePattern} · 연출 {(visualMapped ? "연결" : "미연결")} · 실제 효과 판정은 별도";
                    var iconRect = GetMagicChoiceIconRect(slot); iconRect.position += panel.position;
                    ui.ActionButton("spell-" + slot, iconRect, "?", () => TrySelectDisplayedMagic(index),
                        canSelect && magic != null, info,
                        magic == null ? null : BattleIconFrames.FromMagicCoordinates(magic.IconX, magic.IconY, Time.unscaledTimeAsDouble));
                }
                if (NativeBattleMagicMenu.PageCount(count) > 1)
                    ui.Label("magic-overflow", new Rect(panel.x - 36, panel.yMax + 2, 180, 20),
                        $"{magicChoicePage + 1}/{NativeBattleMagicMenu.PageCount(count)} PgUp/PgDn · 휠 (임시)", size: 10);
            }
            if (battle.IsPrototypeHudVisible)
            {
            var inputPanel = InputPanelRect;
            // Duplicate controls belong to the optional Unity diagnostics, not the native HUD.
            var basicCommands = new[] { BattleMapCommand.Move, BattleMapCommand.Attack, BattleMapCommand.Rest };
            battle.TryGetSelectedUnit(out var commandActor);
            var basicLabels = new[] { $"이동 AP{GameConstants.BattleAllyMoveApCost}",
                $"기본 공격 AP{battle.GetUnitAttackApCost(commandActor.Id)}", $"휴식 AP{GameConstants.BattleTemporaryRestApCost}" };
            var basicIcons = new[] { GameConstants.BattleMoveIconColumn, GameConstants.BattleAttackIconColumn, GameConstants.BattleRestIconColumn };
            for (var i = 0; i < basicCommands.Length; i++)
            {
                var cancelMagic = i == 2 && IsMagicTargeting;
                var blockedReason = string.Empty;
                var allowed = cancelMagic || CanChooseBasicCommand(basicCommands[i], out blockedReason);
                var command = basicCommands[i];
                ui.ActionButton("basic-" + i, GetBasicCommandButtonRect(i), cancelMagic ? "마법 취소" : basicLabels[i], () =>
                {
                    if (cancelMagic) CancelMagicTargeting();
                    else TryChooseBasicCommand(command);
                }, allowed, cancelMagic ? "마법 대상 지정 취소" : string.IsNullOrEmpty(blockedReason) ? basicLabels[i] : blockedReason,
                    cancelMagic ? null : BattleIconFrames.Action(basicIcons[i], Time.unscaledTimeAsDouble));
            }
            ui.BattleLabel("feedback", new Rect(inputPanel.x + 8, inputPanel.y + 35, inputPanel.width - 16, 32), LastInputMessage, singleLine: true);
            }
            foreach (var visual in visuals)
            {
                if (visual.Body == null || !visual.Body.enabled || !battle.TryGetUnit(visual.UnitId, out var actor) || actor.LifeState != UnitLifeState.Active) continue;
                var bodyBounds = visual.Body.bounds;
                var screen = view.WorldToScreenPoint(new Vector3(bodyBounds.center.x, bodyBounds.max.y, bodyBounds.center.z));
                if (screen.z <= 0) continue;
                var logicalName = Rendering.GameScreen.ScreenToGui(screen);
                ui.NativeWorldName("name-" + actor.Id, new Vector2(logicalName.x, logicalName.y - 2), actor.Name);
            }
            if (IsMagicChoiceOpen)
            {
                ui.Raise("magic-modal-input");
                for (int slot = 0; slot < NativeBattleMagicMenu.Capacity; slot++)
                { ui.Raise("spell-" + slot); ui.Raise("spell-" + slot + "-icon"); }
                ui.Raise("magic-overflow");
            }
            if(IsToolTargeting)
            {
                if(!battle.TryGetSelectedUnit(out var itemActor) || itemActor.Id!=100 || !itemActor.Alive)pendingRecoveryItem=null;
                else ui.Label("tool-target-prompt",new Rect(0,Rendering.GameScreen.Height-22,Rendering.GameScreen.Width,20),LastInputMessage,12);
            }
            DrawSystemMenu();
            ui.End();
        }

        public void SuspendPresentation()
        {
            CloseSystemMenu();
            toolDisplays.Clear();
            SuspendMagicEffects();
            foreach (var effect in attackEffects.Values) if (effect.Renderer != null) effect.Renderer.enabled = false;
            combatFeedback.Clear();
            menuUnitId = null; ActiveCommand = BattleMapCommand.Automatic;
            if (battle != null) battle.ClearUnitInspection();
            CancelMagicTargeting();
            if (battle != null) battle.CancelMovementRoutes();
            if (battle != null && listening) battle.AttackPresentationStarted -= OnAttackPresentation;
            if (battle != null && listening) battle.MovementStepApplied -= OnMovementStep;
            if (battle != null && listening) battle.CombatResultApplied -= OnCombatResult;
            if (battle != null && listening) battle.VitalsDisplayChanged -= OnVitalsDisplay;
            listening = false;
            if (battle != null) battle.SetMapPresentationActive(false);
            for (var i = 0; i < visuals.Count; i++)
            {
                if (offsetVisuals.Contains(i) && visuals[i].Body != null && ground != null
                    && battle.TryGetUnit(visuals[i].UnitId, out var unit))
                    SynchronizePosition(i, new CombatGridCell(unit.X, unit.Y));
                if (visuals[i].Body != null)
                    visuals[i].Body.enabled = originalEnabled[i]
                        && (battle == null || !battle.TryGetUnit(visuals[i].UnitId, out var state)
                            || state.LifeState == UnitLifeState.Active);
                if (bodies[i] != null) bodies[i].simulated = originalSimulated[i];
                if (animators[i] != null)
                {
                    animators[i].TrySetFacing(originalFacing[i]);
                    animators[i].SetMovingPresentation(originalMoving[i]);
                }
            }
            IsBound = false;
        }

        public bool ResumePresentation()
        {
            if (!isActiveAndEnabled || !configured || battle == null || ground == null || terrain == null || view == null) return false;
            IsBound = true; battle.SetMapPresentationActive(true); Listen(); RefreshVisualState();
            return true;
        }

        private void OnDisable() => SuspendPresentation();
        private void OnEnable() => ResumePresentation();
        private void OnDestroy()
        {
            ReleaseMagicPreviewResources();
            ReleaseCreatedFormationBodies();
            ReleaseAttackEffectResources();
            ReleaseMagicEffectResources();
        }

        public void ReleaseAttackEffectResources()
        {
            foreach (var effect in attackEffects.Values)
                if (effect.Renderer != null) ReleasePreview(effect.Renderer.gameObject);
            attackEffects.Clear();
        }

        // Explicit release for preview fixtures and scene-owned teardown.
        public void ReleaseCreatedFormationBodies()
        {
            if (formationBodies.Count == 0) return;
            if (IsBound) SuspendPresentation();
            configured = false;
            foreach (var go in formationBodies) DestroyFormationBody(go);
            formationBodies.Clear();
        }
        public void ReleaseMagicPreviewResources()
        {
            ClearMagicPreview();
            ReleasePreview(previewObject); ReleasePreview(previewMesh); ReleasePreview(previewMaterial);
            previewObject = null; previewMesh = null; previewMaterial = null; previewRenderer = null;
        }
        private static void ReleasePreview(Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
        }
    }
}
