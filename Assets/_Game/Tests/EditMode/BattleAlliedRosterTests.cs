using DarkSaver.Combat;
using DarkSaver.Network;
using DarkSaver.Magic;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class BattleAlliedRosterTests
    {
        [Test] public void NativeRosterListsCommandersNotTheirSoldiers()
        {
            var go = new GameObject("Native commander roster");
            try
            {
                var battle = go.AddComponent<BattleSceneRoot>();
                Assert.That(battle.TryRegisterGroup(new[] { new CombatUnit { Id = 101, Hp = 50, MaxHp = 50 } }), Is.True);
                Assert.That(battle.TryRegisterGroup(new[] {
                    new CombatUnit { Id = 200, Hp = 50, MaxHp = 50 },
                    new CombatUnit { Id = 201, Hp = 50, MaxHp = 50 } }), Is.True);
                var commanders = battle.GetAlliedRosterCommanders();
                Assert.That(commanders.Count, Is.EqualTo(2));
                Assert.That(commanders[0].Id, Is.EqualTo(100));
                Assert.That(commanders[1].Id, Is.EqualTo(200));
                Assert.That(battle.AlliedRosterRect.size, Is.EqualTo(new Vector2(120,126)));
                Assert.That(battle.ApplyDeath(200, false), Is.True);
                commanders = battle.GetAlliedRosterCommanders();
                Assert.That(commanders.Count, Is.EqualTo(1));
                Assert.That(commanders[0].Id, Is.EqualTo(100));
                Assert.That(battle.TryGetUnit(201, out var soldier), Is.True);
                Assert.That(soldier.Alive, Is.True);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void PrimaryDeathDoesNotFinishBattleWhileAnotherCommanderLives()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Multiple commanders fixture"); SceneManager.MoveGameObjectToScene(root, scene);
                var battle = root.AddComponent<BattleSceneRoot>();
                Assert.That(battle.TryRegisterGroup(new[] {
                    new CombatUnit { Id = 200, Hp = 50, MaxHp = 50 },
                    new CombatUnit { Id = 201, Hp = 50, MaxHp = 50 } }), Is.True);
                Assert.That(battle.ApplyDeath(100, false), Is.True);
                Assert.That(battle.DefeatAchieved, Is.False);
                Assert.That(battle.IsBattleFinished, Is.False);
                Assert.That(battle.TryAcknowledgeOutcome(), Is.False);
                Assert.That(battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp), Is.True);
                Assert.That(battle.TryGetUnit(200, out var commander), Is.True);
                Assert.That(commander.Ap, Is.EqualTo(commander.MaxAp));
                Assert.That(battle.ApplyHp(200, 0), Is.True);
                Assert.That(battle.DefeatAchieved, Is.True);
                Assert.That(battle.IsBattleFinished, Is.True);
                Assert.That(battle.TryGetUnit(201, out var soldier), Is.True);
                Assert.That(soldier.Alive, Is.True); // Do not invent a forced subordinate death.
                Assert.That(battle.TryAcknowledgeOutcome(), Is.True);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void AllCommanderDefeatStopsNewActionsButStillAcceptsAbsoluteResults()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Commander defeat gate fixture"); SceneManager.MoveGameObjectToScene(root, scene);
                var battle = root.AddComponent<BattleSceneRoot>();
                Assert.That(battle.TryRegisterGroup(new[] { new CombatUnit { Id = 101, Hp = 50, MaxHp = 50 } }), Is.True);
                Assert.That(battle.ApplyDeath(100, true), Is.True);
                Assert.That(battle.IsBattleFinished, Is.True);
                Assert.That(battle.CanStartAttackRequest(800, 101, out var reason), Is.False);
                Assert.That(reason, Does.Contain("종료"));
                Assert.That(battle.TryStartAttackRequest(800, 101, out _), Is.False);
                Assert.That(battle.CanStartMagicRequest(1, 101, new short[] { 800 }, out _), Is.False);
                Assert.That(battle.TryStartMagicRequest(1, 101, new short[] { 800 }, out _), Is.False);
                Assert.That(battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp), Is.True);
                Assert.That(battle.TryGetUnit(101, out var soldier), Is.True);
                Assert.That(soldier.Ap, Is.EqualTo(6));
                Assert.That(battle.ApplyMp(101, 0), Is.True);
                Assert.That(battle.ApplyExperience(new CombatExperienceResult(101, 42)), Is.True);
                Assert.That(battle.TryGetUnit(101, out soldier), Is.True);
                Assert.That(soldier.Experience, Is.EqualTo(42));
                Assert.That(soldier.Alive, Is.True);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void ActionMenuPlacementAvoidsPanelsAndRejectsInsufficientSpace()
        {
            var screen = new Vector2(1280, 720);
            var panels = new[] { new Rect(12, 12, 220, 145), new Rect(12, 440, 220, 260), new Rect(900, 410, 360, 290) };
            foreach (var anchor in new[] { new Vector2(100, 80), new Vector2(100, 600), new Vector2(1100, 600),
                new Vector2(-200, -100), new Vector2(1500, 900), new Vector2(600, 350) })
            {
                Assert.That(BattleMapPresentation.TryPlaceActionMenuRect(anchor, screen, panels, out var rect), Is.True);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(screen.x)); Assert.That(rect.yMax, Is.LessThanOrEqualTo(screen.y));
                foreach (var panel in panels) Assert.That(rect.Overlaps(panel), Is.False);
                Assert.That(BattleMapPresentation.TryPlaceActionMenuRect(anchor, screen, panels, out var again), Is.True);
                Assert.That(again, Is.EqualTo(rect));
            }
            Assert.That(BattleMapPresentation.TryPlaceActionMenuRect(new Vector2(600, 350), screen, panels, out var unchanged), Is.True);
            Assert.That(unchanged.center, Is.EqualTo(new Vector2(600, 350)));
            Assert.That(BattleMapPresentation.TryPlaceActionMenuRect(Vector2.zero, new Vector2(80, 80), panels, out _), Is.False);
            Assert.That(BattleMapPresentation.TryPlaceActionMenuRect(Vector2.zero, screen,
                new[] { new Rect(Vector2.zero, screen) }, out _), Is.False);
            Assert.That(BattleMapPresentation.TryPlaceActionMenuRect(new Vector2(float.NaN, 0), screen, panels, out _), Is.False);
            Assert.That(BattleMapPresentation.TryPlaceActionMenuRect(Vector2.zero, screen, null, out _), Is.False);
        }

        [Test]
        public void CommanderRemovalKeepsOtherGroupSoldierAndApRecovery()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Commander removal fixture"); SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<BattleRequestSender>();
                var receiver = root.AddComponent<BattlePacketReceiver>(); Assert.That(receiver.Bind(), Is.True);
                var battle = root.GetComponent<BattleSceneRoot>();
                Assert.That(battle.TryRegisterGroup(new[] {
                    new CombatUnit { Id = 200, JobId = 100, Hp = 50, MaxHp = 50 },
                    new CombatUnit { Id = 201, JobId = 100, Hp = 50, MaxHp = 50 } }), Is.True);
                var before = battle.GetAlliedGroups();
                Assert.That(before.Count, Is.EqualTo(2)); Assert.That(before[0].CommanderId, Is.EqualTo(100));
                Assert.That(before[1].CommanderId, Is.EqualTo(200)); Assert.That(before[1].AliveCount, Is.EqualTo(2));
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(200)), Is.True);
                var after = battle.GetAlliedGroups();
                Assert.That(after.Count, Is.EqualTo(2)); Assert.That(after[1].CommanderPresent, Is.True);
                Assert.That(after[1].CommanderAlive, Is.False); Assert.That(after[1].AliveCount, Is.EqualTo(1));
                Assert.That(before[1].CommanderAlive, Is.True); Assert.That(battle.IsBattleFinished, Is.False);
                Assert.That(battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp), Is.True);
                Assert.That(battle.TryGetUnit(200, out var commander), Is.True);
                Assert.That(commander.LifeState, Is.EqualTo(UnitLifeState.Removed)); Assert.That(commander.Ap, Is.EqualTo(6));
                Assert.That(battle.TryGetUnit(201, out var soldier), Is.True);
                Assert.That(soldier.Alive, Is.True); Assert.That(soldier.Hp, Is.EqualTo(50)); Assert.That(soldier.Ap, Is.EqualTo(soldier.MaxAp));
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(201)), Is.True);
                Assert.That(battle.GetAlliedGroups()[1].AliveCount, Is.Zero);
                Assert.That(battle.IsBattleFinished, Is.False); // Do not invent a new whole-group defeat rule.
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RosterSelectionIsFreeAndRejectsUnavailableUnits()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var floor = ScriptableObject.CreateInstance<Tile>();
            var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
            GameObject Create(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
            try
            {
                var grid = Create("Grid").AddComponent<Grid>();
                var ground = Create("Ground").AddComponent<Tilemap>(); ground.transform.SetParent(grid.transform);
                for (var x = 0; x < 4; x++) for (var y = 0; y < 4; y++) ground.SetTile(new Vector3Int(x, y, 0), floor);
                var terrain = Create("Terrain").AddComponent<Tilemap>(); terrain.transform.SetParent(grid.transform);
                var camera = Create("Camera").AddComponent<Camera>(); camera.orthographic = true;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Down_1.png");
                Assert.That(sprite, Is.Not.Null);
                SpriteRenderer Body(string name, Vector3 position) { var body = Create(name).AddComponent<SpriteRenderer>(); body.sprite = sprite; body.transform.position = position; return body; }
                var ally = Body("Ally", new Vector3(1.5f, 2.5f));
                var enemy = Body("Enemy", new Vector3(2.5f, 2.5f));
                var soldier = Body("Soldier", new Vector3(.5f, 2.5f));
                var root = Create("Battle"); root.AddComponent<BattleRequestSender>();
                var receiver = root.AddComponent<BattlePacketReceiver>(); Assert.That(receiver.Bind(), Is.True);
                var view = root.AddComponent<BattleMapPresentation>(); var battle = root.GetComponent<BattleSceneRoot>();
                Assert.That(battle.TryRegisterGroup(new[] { new CombatUnit {
                    Id = 101, JobId = 100, Name = "soldier", Hp = 50, MaxHp = 50, Mp = 10, MaxMp = 10 } }), Is.True);
                var groups = battle.GetAlliedGroups();
                Assert.That(groups.Count, Is.EqualTo(1)); Assert.That(groups[0].CommanderId, Is.EqualTo(100));
                Assert.That(groups[0].Members.Count, Is.EqualTo(2));
                Assert.That(battle.TrySelectRosterUnit(100), Is.False);
                var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                Assert.That(jobs, Is.Not.Null); Assert.That(battle.ConfigureTraversalCatalogs(new[] { jobs }), Is.True);
                Assert.That(view.TryBind(ground, terrain, camera, new[] {
                    new BattleMapVisual(100, ally), new BattleMapVisual(800, enemy), new BattleMapVisual(101, soldier) }), Is.True);
                var packets = 0; root.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                battle.SetPrototypeHudVisible(true);
                var inputPanel = view.InputPanelRect;
                Assert.That(inputPanel.Overlaps(battle.AlliedRosterRect), Is.False);
                Assert.That(inputPanel.Overlaps(view.MagicDirectionRect), Is.False);
                Assert.That(battle.BattleHudRect.Contains(inputPanel.min), Is.True);
                Assert.That(battle.BattleHudRect.Contains(inputPanel.max), Is.True);
                Assert.That(inputPanel.yMin, Is.GreaterThanOrEqualTo(battle.BattleHudRect.y + 145));
                Assert.That(inputPanel.yMax, Is.LessThan(battle.BattleHudRect.y + 220)); // Return button remains unobstructed.
                Assert.That(view.TryBlockMapPointer(Rendering.GameScreen.GuiToScreen(inputPanel.center)), Is.True);
                Assert.That(battle.TrySelectRosterUnit(100), Is.True);
                // Selecting the roster is free, but choosing attack requires the confirmed full-AP gate.
                Assert.That(battle.ApplyTickMenu(100, GameConstants.BattleAllyMaximumAp, 0), Is.True);
                // Edit-mode fixtures do not run Start(), which enables the approved local cost profile in play.
                battle.SetTemporaryLocalRules(true);
                Assert.That(view.TryChooseCommand(BattleMapCommand.Attack), Is.True);
                battle.SetTemporaryLocalRules(false);
                Assert.That(battle.ApplyTickMenu(100, GameConstants.BattleStartingAp, 0), Is.True);
                Assert.That(battle.TrySelectRosterUnit(101), Is.True);
                Assert.That(battle.SelectedUnitId, Is.EqualTo(101));
                Assert.That(view.ActiveCommand, Is.EqualTo(BattleMapCommand.Automatic)); Assert.That(view.IsActionMenuOpen, Is.True);
                Assert.That(battle.TryGetInspectedUnit(out var shown), Is.True); Assert.That(shown.Id, Is.EqualTo(101));
                Assert.That(shown.Ap, Is.EqualTo(6)); Assert.That(shown.Mp, Is.EqualTo(10)); Assert.That(packets, Is.Zero);
                var hudText = battle.GetBattleHudText();
                Assert.That(hudText, Does.Contain("선택: soldier [101]"));
                Assert.That(hudText, Does.Contain("HP 50/50  MP 10/10  AP 6/10"));
                Assert.That(hudText, Does.Not.Contain("HP 120/120"));
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(101, 37)), Is.True);
                Assert.That(receiver.TryApplyPacket(new MpSetNotification(101, 4)), Is.True);
                Assert.That(battle.GetBattleHudText(), Does.Contain("HP 37/50  MP 4/10"));
                Assert.That(battle.PlayerHp, Is.EqualTo(120)); Assert.That(battle.PlayerMp, Is.EqualTo(30));
                receiver.TryApplyPacket(new HpSetNotification(101, 50)); receiver.TryApplyPacket(new MpSetNotification(101, 10));
                battle.ClearUnitSelection();
                Assert.That(battle.GetBattleHudText(), Does.StartWith("아군을 선택하세요."));
                Assert.That(battle.GetBattleHudText(), Does.Not.Contain("HP 120/120"));
                Assert.That(battle.TrySelectRosterUnit(101), Is.True);
                Assert.That(battle.TrySelectRosterUnit(800), Is.False); Assert.That(battle.SelectedUnitId, Is.EqualTo(101));
                Assert.That(battle.CanSelectRosterUnit(800), Is.False);
                Assert.That(battle.CanSelectRosterUnit(101), Is.True);
                var values = new long[MagicCatalogEntry.NumericColumnCount];
                values[0] = 701; values[2] = -1; values[3] = 2; values[6] = 1; values[7] = 1;
                var pointValues = new long[MagicCatalogEntry.NumericColumnCount];
                pointValues[0] = 702; pointValues[2] = 0; pointValues[3] = 1; pointValues[6] = 1; pointValues[7] = 1;
                catalog.Initialize(new[] { new MagicCatalogEntry(values, "Explicit roster fixture"),
                    new MagicCatalogEntry(pointValues, "Explicit point fixture") });
                var command = new MagicRequest(701, 101, 0, 0, 0, System.Array.Empty<short>());
                Assert.That(view.TryBeginNativeMagicTargeting(catalog, command, _ => true, _ => 1, null, null), Is.True);
                Assert.That(view.TryChooseMagicDirection(BattleMagicDirection.Right), Is.True);
                Assert.That(view.MagicPreviewCellCount, Is.GreaterThan(0));
                var previewCount = view.MagicPreviewCellCount;
                foreach (var panel in new[] { view.MagicDirectionRect, battle.AlliedRosterRect, battle.UnitInfoRect, battle.BattleHudRect })
                {
                    if (panel.width <= 0 || panel.height <= 0) continue; // Hidden views have no input area.
                    Assert.That(view.TryBlockMapPointer(Rendering.GameScreen.GuiToScreen(panel.center)), Is.True);
                    Assert.That(view.MagicPreviewCellCount, Is.EqualTo(previewCount));
                    Assert.That(view.SelectedMagicDirectionCommand, Is.EqualTo(new CombatGridCell(-1, 0)));
                }
                Assert.That(view.TryBlockMapPointer(new Vector2(-100, -100)), Is.True); // Outside the logical viewport is not map input.
                Assert.That(view.MagicPreviewCellCount, Is.EqualTo(previewCount)); Assert.That(packets, Is.Zero);
                Assert.That(view.TryChooseMagicDirection(BattleMagicDirection.Down), Is.True);
                Assert.That(view.MagicPreviewCellCount, Is.EqualTo(2));
                Assert.That(battle.TryReserveMovementStep(101, new CombatGridCell(0, 2), out var step), Is.True);
                Assert.That(battle.TryApplyReservedMovementStep(step), Is.True);
                view.RefreshVisualState();
                Assert.That(view.SelectedMagicDirectionCommand, Is.EqualTo(new CombatGridCell(-4, 0)));
                Assert.That(view.MagicPreviewCellCount, Is.EqualTo(1));
                view.RefreshVisualState(); Assert.That(view.MagicPreviewCellCount, Is.EqualTo(1));
                Assert.That(battle.TryGetUnit(101, out var moved), Is.True);
                Assert.That(moved.X, Is.Zero); Assert.That(moved.Y, Is.EqualTo(2));
                Assert.That(moved.Ap, Is.EqualTo(6)); Assert.That(moved.Mp, Is.EqualTo(10)); Assert.That(packets, Is.Zero);
                Assert.That(battle.TryReserveMovementStep(101, new CombatGridCell(0, 1), out step), Is.True);
                Assert.That(battle.TryApplyReservedMovementStep(step), Is.True);
                view.RefreshVisualState(); Assert.That(view.MagicPreviewCellCount, Is.EqualTo(2));
                Assert.That(view.TryChooseMagicDirection(BattleMagicDirection.Right), Is.True);
                ally.sprite = null;
                Assert.That(battle.CanSelectRosterUnit(100), Is.False);
                Assert.That(battle.TrySelectRosterUnit(100), Is.False);
                Assert.That(battle.SelectedUnitId, Is.EqualTo(101)); Assert.That(view.SelectedMagicDirectionCommand, Is.Not.Null);
                Assert.That(view.IsMagicTargeting, Is.True); Assert.That(view.MagicPreviewCellCount, Is.GreaterThan(0));
                ally.sprite = sprite;
                Assert.That(battle.TrySelectRosterUnit(100), Is.True);
                Assert.That(view.IsMagicTargeting, Is.False); Assert.That(view.SelectedMagicDirectionCommand, Is.Null);
                Assert.That(view.MagicPreviewCellCount, Is.Zero); Assert.That(view.TryConfirmMagicDirection(), Is.False);
                Assert.That(view.TryBeginMagicTargeting(catalog,
                    new MagicRequest(702, 100, 0, 0, 0, System.Array.Empty<short>()), true), Is.True);
                Assert.That(view.TryPreviewMagicAt(new CombatGridCell(2, 1)), Is.True);
                Assert.That(view.MagicPreviewCellCount, Is.GreaterThan(0));
                var hud = battle.BattleHudRect;
                Assert.That(view.TryBlockMapPointer(Rendering.GameScreen.GuiToScreen(hud.center)), Is.True);
                Assert.That(view.MagicPreviewCellCount, Is.Zero); Assert.That(view.IsMagicTargeting, Is.True);
                view.CancelMagicTargeting();
                Assert.That(view.ConfigureMagicChoices(catalog, 101, new[] { command }, _ => true, _ => 1, null, null), Is.True);
                Assert.That(battle.TrySelectRosterUnit(101), Is.True);
                Assert.That(view.TryChooseCommand(BattleMapCommand.Magic), Is.True); Assert.That(view.IsMagicChoiceOpen, Is.True);
                Assert.That(view.TryBlockMapPointer(new Vector2(-100, -100)), Is.True); // Modal list blocks the whole map.
                Assert.That(battle.TrySelectRosterUnit(100), Is.True);
                Assert.That(view.IsMagicChoiceOpen, Is.False); Assert.That(view.TrySelectMagicChoice(0), Is.False);
                Assert.That(battle.TrySelectRosterUnit(101), Is.True); Assert.That(view.MagicChoiceCount, Is.EqualTo(1));
                Assert.That(view.IsMagicChoiceOpen, Is.False); Assert.That(packets, Is.Zero);
                Assert.That(battle.TryGetUnit(101, out var unchanged), Is.True);
                Assert.That(unchanged.Ap, Is.EqualTo(6)); Assert.That(unchanged.Mp, Is.EqualTo(10));
                Assert.That(battle.PlayerAp, Is.EqualTo(6));
                var rect = battle.UnitInfoRect; // Roster selection switches to the same information window.
                Assert.That(battle.IsPointerOverBattleHud(Rendering.GameScreen.GuiToScreen(rect.center)), Is.True);
                view.SuspendPresentation(); Assert.That(battle.TrySelectRosterUnit(101), Is.False);
                Assert.That(battle.GetBattleHudText(), Does.Contain("HP 120/120  MP 30/30")); // Legacy non-map display only.
                Assert.That(view.ResumePresentation(), Is.True);
                receiver.TryApplyPacket(new UnitCorpseNotification(101)); Assert.That(battle.TrySelectRosterUnit(101), Is.False);
                Assert.That(battle.GetAlliedGroups()[0].AliveCount, Is.EqualTo(1));
                Assert.That(groups[0].AliveCount, Is.EqualTo(2)); // Returned snapshots are not a live mutable roster.
                receiver.TryApplyPacket(new UnitCorpseNotification(800)); Assert.That(battle.TrySelectRosterUnit(100), Is.False);
            }
            finally
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var view in root.GetComponentsInChildren<BattleMapPresentation>(true)) view.ReleaseMagicPreviewResources();
                EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(floor); Object.DestroyImmediate(catalog);
            }
        }
    }
}
