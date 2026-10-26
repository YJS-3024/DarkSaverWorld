using DarkSaver.UI;
using System.Linq;
using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.Magic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.EditorTools
{
    // Editor-only smoke run: real scene entry and command APIs, no HP/AP injection.
    // Uses a separate save and restores the editor's scene setup after leaving Play.
    [InitializeOnLoad]
    public static partial class BattlePlayVerification
    {
        const string Key = "DarkSaver.BattlePlayVerification.";
        [Serializable] sealed class SavedScene { public string path; public bool loaded, active; }
        [Serializable] sealed class Setup { public SavedScene[] scenes; }
        static double nextAction;
        static int moves, attacks, rests;
        static int menuCaptures;
        static int rosterCaptures;
        static int magicUiStep;
        static MagicCatalog uiMagicCatalog;
        static BattleFormationSnapshot uiRestoreKnowledge;
        static int combatMagicStep;
        static short combatMagicId;
        static string output;
        static BattleSceneRoot battle;
        static BattleMapPresentation presentation;

        static BattlePlayVerification()
        {
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Tick;
        }

        public static string Status => SessionState.GetString(Key + "status", "Not started");

        [MenuItem("DarkSaver/Verification/Run learned magic combat smoke test (isolated fixture)")]
        public static void BeginMagicCombat()
        {
            SessionState.SetBool(Key + "combatMagic", true);
            try { BeginWide(); }
            catch { SessionState.SetBool(Key + "combatMagic", false); throw; }
        }

        static int terrainUiStep;
        [MenuItem("DarkSaver/Verification/Run training terrain UI smoke test")]
        public static void BeginTerrainUi()
        {
            terrainUiStep = 0;
            SessionState.SetBool(Key + "previewTerrain", true);
            try { Begin(); }
            catch { SessionState.SetBool(Key + "previewTerrain", false); throw; }
        }

        [MenuItem("DarkSaver/Verification/Run confirmed training retreat smoke test")]
        public static void BeginRetreat()
        {
            SessionState.SetBool(Key + "verifyRetreat",true);
            try { Begin(); }
            catch { SessionState.SetBool(Key + "verifyRetreat",false); throw; }
        }

        [MenuItem("DarkSaver/Verification/Run native commander roster preview (isolated fixture)")]
        public static void BeginRosterUi()
        {
            SessionState.SetBool(Key + "previewRoster", true);
            try { Begin(); }
            catch { SessionState.SetBool(Key + "previewRoster", false); throw; }
        }

        [MenuItem("DarkSaver/Verification/Run native target effect preview (isolated fixture)")]
        public static void BeginNativeTargetEffectsUi()
        {
            NativeRosterVisualFixture.TargetEffectsOnly = true;
            try { BeginRosterUi(); }
            catch { NativeRosterVisualFixture.TargetEffectsOnly = false; throw; }
        }

        [MenuItem("DarkSaver/Verification/Run battle UI preview (Full HD, isolated fixture)")]
        public static void BeginWideUi()
        {
            SessionState.SetBool(Key + "previewMagic", true);
            try { BeginWide(); }
            catch { SessionState.SetBool(Key + "previewMagic", false); throw; }
        }

        [MenuItem("DarkSaver/Verification/Run battle play smoke test (Full HD)")]
        public static void BeginWide()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Editor must be idle in Edit mode.");
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var windows = Resources.FindObjectsOfTypeAll(type);
            if (windows.Length == 0) throw new InvalidOperationException("Open a Game View before verification.");
            var sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = sizesType.BaseType.GetProperty("instance").GetValue(null);
            var group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            int wideIndex = Array.FindIndex(labels, x => x.Contains("1920x1080"));
            if (wideIndex < 0) throw new InvalidOperationException("Existing Full HD preset is required; no presets were added.");
            var property = type.GetProperty("selectedSizeIndex");
            SessionState.SetInt(Key + "originalGameSize", (int)property.GetValue(windows[0]));
            try { property.SetValue(windows[0], wideIndex); Begin(); }
            catch { RestoreGameViewSize(); throw; }
        }

        static void RestoreGameViewSize()
        {
            int original = SessionState.GetInt(Key + "originalGameSize", -1);
            if (original < 0) return;
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var windows = Resources.FindObjectsOfTypeAll(type);
            if (windows.Length > 0) type.GetProperty("selectedSizeIndex").SetValue(windows[0], original);
            SessionState.EraseInt(Key + "originalGameSize");
        }

        static bool PreviewTerrainUi(BattleSceneRoot current, BattleMapPresentation view)
        {
            if (terrainUiStep == 0)
            {
                if (!view.TryGetSelectedTerrain(out var attribute, out var movement, out var sprite)
                    || !movement.HasValue || sprite == null)
                    throw new InvalidOperationException("Training terrain data/artwork missing.");
                current.TryGetUnit(100, out var before);
                if (!view.TryOpenBattleWindowShell(6)) throw new InvalidOperationException("Terrain window failed to open.");
                current.TryGetUnit(100, out var after);
                if (before.Ap != after.Ap || before.Hp != after.Hp || before.Mp != after.Mp)
                    throw new InvalidOperationException("Terrain inspection changed resources.");
                Note($"Terrain preview: attribute={attribute}, movement={movement.Value}, converted tile sprite present");
                terrainUiStep = 1; return true;
            }
            if (terrainUiStep == 1)
            {
                if (!view.IsBattleWindowShellOpen || view.BattleWindowShellId != 6)
                    throw new InvalidOperationException("Terrain window disappeared before capture.");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "battle-terrain-live.png"));
                terrainUiStep = 2; return true;
            }
            current.TryGetUnit(100, out var closing);
            if (!view.TryCloseBattleWindowShell()) throw new InvalidOperationException("Terrain window close failed.");
            view.CloseSystemMenu();
            current.TryGetUnit(100, out var closed);
            if (closing.Ap != closed.Ap || closing.Hp != closed.Hp || closing.Mp != closed.Mp)
                throw new InvalidOperationException("Terrain close changed resources.");
            SessionState.SetBool(Key + "previewTerrain", false);
            Note("PASS: real training terrain artwork/movement shown and closed without resource changes");
            return true;
        }

        [MenuItem("DarkSaver/Verification/Run battle play smoke test")]
        public static void Begin() => BeginTraining(TrainingBattleTestMenu.SelectedMap);

        public static void BeginTraining(int mapId)
        {
            if (mapId < 101 || mapId > 107) throw new ArgumentOutOfRangeException(nameof(mapId));
            BeginCore(mapId);
        }

        [MenuItem("DarkSaver/Verification/Run scenario 01 battle smoke test")]
        public static void BeginScenario() => BeginCore(0);

        private static void BeginCore(int trainingMap)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Editor must be idle in Edit mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard dirty scenes yourself before verification.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            SessionState.SetInt(Key + "trainingMap", trainingMap);
            var setup = new Setup { scenes = new SavedScene[previous.Length] };
            for (var i = 0; i < previous.Length; i++)
                setup.scenes[i] = new SavedScene { path = previous[i].path, loaded = previous[i].isLoaded, active = previous[i].isActive };
            SessionState.SetString(Key + "setup", JsonUtility.ToJson(setup));
            output = Path.GetFullPath("Tools/CombatRulesChecks/Play-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output);
            SessionState.SetString(Key + "output", output);
            SessionState.SetString(Key + "status", "Entering Play");
            SessionState.SetInt(Key + "stage", 1);
            EditorApplication.isPlaying = true;
        }

        static void Changed(PlayModeStateChange state)
        {
            if (SessionState.GetInt(Key + "stage", 0) == 0) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                output = SessionState.GetString(Key + "output", "");
                moves = attacks = rests = menuCaptures = magicUiStep = rosterCaptures = nativeMenuStep = 0; battle = null; presentation = null;
                uiMagicCatalog = null; uiRestoreKnowledge = null; combatMagicStep = 0;
                if (!NativeCharacterProfile.TryQueue("전투검증", 100, Path.Combine(output, "verification-save.json"), out var error))
                { Finish("FAIL: " + error); return; }
                SessionState.SetFloat(Key + "deadline", Time.realtimeSinceStartup + 360);
                if (SessionState.GetBool(Key + "townTraining", false))
                {
                    SessionState.SetInt(Key + "stage", 20);
                    SceneManager.LoadSceneAsync("Kaosia_Play", LoadSceneMode.Single);
                    Note("Loading isolated Kaosia training connection verification");
                }
                else if (SessionState.GetInt(Key + "trainingMap", 101) != 0)
                {
                    SessionState.SetInt(Key + "stage", 7);
                    SceneManager.LoadSceneAsync("GameCore", LoadSceneMode.Single);
                    Note("Loading training verification core; no scenario objective or original training spawn assumed");
                }
                else
                {
                    SessionState.SetInt(Key + "stage", 2);
                    SceneManager.LoadSceneAsync("Brugos01_Play", LoadSceneMode.Single);
                    Note("Loading original scenario 01 entry (scenario-specific verification)");
                }
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                NativeRosterVisualFixture.Dispose();
                SessionState.SetBool(Key + "previewRoster", false);
                SessionState.SetBool(Key + "previewMagic", false);
                SessionState.SetBool(Key + "combatMagic", false);
                SessionState.SetBool(Key + "verifyRetreat", false);
                SessionState.SetBool(Key + "previewTerrain", false);
                SessionState.SetBool(Key + "townTraining", false);
                SessionState.SetBool(Key + "mercenaryDeployment", false);
                SessionState.SetBool(Key + "classChange", false);
                SessionState.SetBool(Key + "characterInfo", false);
                SessionState.SetBool(Key + "townItems", false);
                SessionState.SetBool(Key + "townHiring", false);
                townTrainingSession = null;
                RestoreGameViewSize();
                var saved = JsonUtility.FromJson<Setup>(SessionState.GetString(Key + "setup", ""));
                SessionState.SetInt(Key + "stage", 0);
                if (saved?.scenes != null)
                {
                    var previous = new SceneSetup[saved.scenes.Length];
                    for (var i = 0; i < previous.Length; i++)
                        previous[i] = new SceneSetup { path = saved.scenes[i].path, isLoaded = saved.scenes[i].loaded, isActive = saved.scenes[i].active };
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                }
                WorkBatchMemoryMaintenance.ScheduleAfterBatch();
            }
        }

        static void Note(string message)
        {
            SessionState.SetString(Key + "status", message);
            if (!string.IsNullOrEmpty(output)) File.AppendAllText(Path.Combine(output, "play-verification.txt"), message + Environment.NewLine);
            Debug.Log("BattlePlayVerification: " + message);
        }
        static void Finish(string message)
        {
            NativeRosterVisualFixture.Dispose();
            Note(message + $"; move={moves}, attack={attacks}, rest={rests}");
            SessionState.SetInt(Key + "stage", 5);
            EditorApplication.isPlaying = false;
        }

        static void Tick()
        {
            var stage = SessionState.GetInt(Key + "stage", 0);
            if (!EditorApplication.isPlaying || stage < 2 || stage == 5 || EditorApplication.isPaused) return;
            if (Time.realtimeSinceStartup > SessionState.GetFloat(Key + "deadline", 0))
            { Finish("FAIL: timed out without a complete combat outcome"); return; }
            try
            {
                if (stage >= 1000) { TickCharacterInfo(stage); return; }
                if (stage >= 50) { TickTownItems(stage); return; }
                if (stage >= 40) { TickClassChange(stage); return; }
                if (stage >= 30) { TickMercenaryDeployment(stage); return; }
                if (stage >= 20) { TickTownTraining(stage); return; }
                if (stage == 7)
                {
                    if (!SceneManager.GetSceneByName("GameCore").isLoaded) return;
                    var id = SessionState.GetInt(Key + "trainingMap",101);
                    EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/Maps/Map_" + id + "_;.unity",
                        new LoadSceneParameters(LoadSceneMode.Additive));
                    SessionState.SetInt(Key + "stage",8); return;
                }
                if (stage == 8)
                {
                    var id = SessionState.GetInt(Key + "trainingMap",101);
                    if (!SceneManager.GetSceneByName("Map_"+id+"_;").isLoaded) return;
                    EditorSceneManager.LoadSceneInPlayMode(TrainingSceneGenerator.RuntimePath(id),new LoadSceneParameters(LoadSceneMode.Additive));
                    SessionState.SetInt(Key + "stage",9); return;
                }
                if (stage == 9)
                {
                    var id = SessionState.GetInt(Key + "trainingMap",101);
                    if (!SceneManager.GetSceneByName("Training"+id+"_Runtime").isLoaded) return;
                    TrainingBattleVerificationSetup.Prepare(id,Path.Combine(output,"verification-save.json"));
                    SessionState.SetInt(Key + "stage",2);
                    Note("Training map " + id + " prepared: user-approved themed monster " + TrainingSceneGenerator.MonsterJob(id)
                        + ", native base stats, no scenario objective");
                    return;
                }
                if (stage == 2)
                {
                    var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
                    var encounter = UnityEngine.Object.FindAnyObjectByType<BattleEncounter2D>();
                    var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
                    if (player == null || encounter == null || session == null || !player.isActiveAndEnabled) return;
                    session.ConfigureSaveFile(Path.Combine(output, "verification-save.json"));
                    if (!encounter.TryEncounter(player)) return;
                    SessionState.SetInt(Key + "stage", 3); Note("Encounter accepted");
                    nextAction = EditorApplication.timeSinceStartup + 1;
                    return;
                }
                if (stage == 4)
                {
                    if (SceneManager.GetSceneByName("Battle").isLoaded) return;
                    var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
                    if (player == null || !player.isActiveAndEnabled) return;
                    if (SessionState.GetInt(Key + "trainingMap",101) != 0)
                    {
                        var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
                        if (session == null || session.ClearedScenarioCount != 0 || session.PlayerMp != session.PlayerMaxMp)
                        { Finish("FAIL: training return changed scenario progress or did not restore maximum MP"); return; }
                        Note("PASS: training return preserves zero scenario clears and restores maximum MP");
                    }
                    Finish(SessionState.GetBool(Key + "verifyRetreat",false)
                        ? "PASS: immediate retreat, Battle unloaded, world input restored"
                        : "PASS: real combat outcome acknowledged, Battle unloaded, world input restored"); return;
                }
                if (stage == 6)
                {
                    if (EditorApplication.timeSinceStartup < nextAction) return;
                    if (battle == null || !battle.TryAcknowledgeOutcome()) { Finish("FAIL: outcome acknowledgement rejected"); return; }
                    UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>().ExitBattle();
                    SessionState.SetInt(Key + "stage", 4); return;
                }
                if (EditorApplication.timeSinceStartup < nextAction) return;
                nextAction = EditorApplication.timeSinceStartup + .5;
                if (SessionState.GetBool(Key + "previewRoster", false) && NativeRosterVisualFixture.IsActive)
                {
                    if (!NativeRosterVisualFixture.Tick(null, null, output))
                    {
                        SessionState.SetBool(Key + "previewRoster", false);
                        Note("PASS: isolated 2/3/4 commander rows; pointer down/up minimized/restored/closed/reopened and selected commanders without AP cost");
                    }
                    return;
                }
                battle = UnityEngine.Object.FindAnyObjectByType<BattleSceneRoot>();
                presentation = UnityEngine.Object.FindAnyObjectByType<BattleMapPresentation>();
                if (battle == null) return;
                if (battle.IsBattleFinished)
                {
                    if (SessionState.GetBool(Key + "combatMagic", false))
                    { Finish("FAIL: combat ended before the learned spell check completed"); return; }
                    Note($"Outcome: victory={battle.VictoryAchieved}, HP={battle.PlayerHp}, MP={battle.PlayerMp}");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "battle-outcome.png"));
                    nextAction = EditorApplication.timeSinceStartup + 2;
                    SessionState.SetInt(Key + "stage", 6); return;
                }
                if (presentation == null || !presentation.IsBound) { Finish("FAIL: scene entered but map combat is unbound"); return; }
                if (SessionState.GetBool(Key + "previewRoster", false))
                {
                    NativeRosterVisualFixture.Tick(battle, presentation, output); return;
                }
                if ((battle.SelectedUnitId != 100 && !battle.TrySelectUnit(100)) || battle.IsMovementInProgress(100)) return;
                if (SessionState.GetBool(Key + "previewTerrain", false) && PreviewTerrainUi(battle, presentation)) return;
                if (battle.PlayerAp < GameConstants.GetBattleMaximumAp(false)) return;
                if (SessionState.GetBool(Key + "previewMagic", false) && PreviewMagicUi()) return;
                if (menuCaptures < 2)
                {
                    if (menuCaptures == 0)
                    {
                        var viewport = Rendering.GameScreen.Viewport;
                        var camera = Camera.main;
                        if (camera == null || Mathf.Abs(camera.aspect - (float)Rendering.GameScreen.Width / Rendering.GameScreen.Height) > .001f)
                        { Finish("FAIL: camera does not match logical aspect ratio"); return; }
                        if ((viewport.x > 1 && !presentation.TryBlockMapPointer(new Vector2(0, Screen.height * .5f)))
                            || (viewport.y > 1 && !presentation.TryBlockMapPointer(new Vector2(Screen.width * .5f, 0))))
                        { Finish("FAIL: letterbox pointer reached the map"); return; }
                        Note($"Display {Screen.width}x{Screen.height}; viewport={viewport}; camera={camera.rect}; size={camera.orthographicSize}");
                        float worldCellPixels = camera.pixelHeight / (2 * camera.orthographicSize);
                        float uiCellPixels = viewport.height / Rendering.GameScreen.Height * 32;
                        if (Mathf.Abs(worldCellPixels - uiCellPixels) > .1f)
                        { Finish("FAIL: 32PPU world cell and 32px UI cell use different pixel scales"); return; }
                        Note($"PASS: native 32px world/UI scale matches: {worldCellPixels:F3}/{uiCellPixels:F3} screen pixels");
                    }
                    if (menuCaptures == 0 && !presentation.TryOpenActionMenu())
                    { Finish("FAIL: selected allied unit cannot open action menu"); return; }
                    // The first capture requests the menu; the next tick must see
                    // retained controls attached to a live document after the fixture.
                    if (menuCaptures == 1)
                    {
                        Button moveButton = null;
                        foreach (var document in presentation.GetComponentsInChildren<InGameUi>().Select(CanvasProbe.For))
                            if (document.rootVisualElement.Q<Button>("radial-0") is Button candidate) moveButton = candidate;
                        if (moveButton == null || moveButton.panel == null
                            || moveButton.resolvedStyle.display == DisplayStyle.None || moveButton.worldBound.width <= 0)
                        { Finish("FAIL: action menu is detached or invisible after reactivation"); return; }
                        Note("PASS: action menu attached and visible after fixture reactivation");
                    }
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "battle-menu-" + menuCaptures + ".png"));
                    Note("Action menu captured: frame " + menuCaptures);
                    menuCaptures++;
                    nextAction = EditorApplication.timeSinceStartup + GameConstants.BattleIconFrameSeconds;
                    return;
                }
                if (rosterCaptures < 3)
                {
                    if (presentation.IsActionMenuOpen) presentation.TryChooseCommand(BattleMapCommand.Cancel);
                    if (rosterCaptures == 0) battle.MinimizeAlliedRoster();
                    else if (rosterCaptures == 1) battle.CloseAlliedRoster();
                    else battle.OpenAlliedRoster();
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "battle-roster-" + rosterCaptures + ".png"));
                    Note("Roster state captured: " + rosterCaptures++);
                    nextAction = EditorApplication.timeSinceStartup + .5;
                    return;
                }
                if (SessionState.GetBool(Key + "verifyRetreat",false))
                {
                    if (VerifyNativeBattleMenu()) return;
                    ClickBattleMenu("system-retreat");
                    SessionState.SetInt(Key+"stage",4);
                    Note("PASS: menu retreat immediately requested battle exit without confirmation");
                    return;
                }
                if (presentation.IsActionMenuOpen) presentation.TryChooseCommand(BattleMapCommand.Cancel);
                if (!battle.TryGetUnit(battle.PrimaryEnemyId, out var enemy)) return;
                var ground = FindGround();
                if (ground == null) { Finish("FAIL: Ground missing"); return; }
                Vector3 World(CombatGridCell cell) => ground.GetCellCenterWorld(new Vector3Int(cell.X, ground.cellBounds.yMax - 1 - cell.Y));
                if (SessionState.GetBool(Key + "combatMagic", false) && VerifyMagicCombat(ground, World(new CombatGridCell(enemy.X, enemy.Y)))) return;
                if (battle.PlayerHp < battle.PlayerMaxHp * .6f && rests < 3
                    && presentation.TryChooseBasicCommand(BattleMapCommand.Rest)) { rests++; Note("Rest accepted"); return; }
                if (presentation.TryChooseBasicCommand(BattleMapCommand.Attack)
                    && presentation.TryHandleWorldClick(World(new CombatGridCell(enemy.X, enemy.Y))))
                {
                    attacks++; Note($"Attack accepted: enemy HP={battle.EnemyHp}, player AP={battle.PlayerAp}");
                    if (attacks == 1) ScreenCapture.CaptureScreenshot(Path.Combine(output, "battle-actions.png"));
                    return;
                }
                if (!presentation.TryChooseBasicCommand(BattleMapCommand.Move)
                    || !presentation.TryGetReachableMovementDestinations(out var cells)) return;
                battle.TryGetUnit(100, out var actor);
                var best = new CombatGridCell(actor.X, actor.Y);
                int distance = Math.Abs(actor.X - enemy.X) + Math.Abs(actor.Y - enemy.Y);
                foreach (var cell in cells)
                {
                    int candidate = Math.Abs(cell.X - enemy.X) + Math.Abs(cell.Y - enemy.Y);
                    if (candidate < distance) { best = cell; distance = candidate; }
                }
                if ((best.X != actor.X || best.Y != actor.Y) && presentation.TryHandleWorldClick(World(best)))
                { moves++; Note($"Movement accepted: {best.X},{best.Y}"); }
            }
            catch (Exception error) { Finish("FAIL: " + error); }
        }
        static UnityEngine.Tilemaps.Tilemap FindGround()
        {
            var id = SessionState.GetInt(Key + "trainingMap",101);
            var name = id == 0 ? "Brugos01_Map" : "Map_"+id+"_;";
            foreach (var map in UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>())
                if (map.name == "Ground" && map.gameObject.scene.name == name) return map;
            return null;
        }

        static bool VerifyMagicCombat(UnityEngine.Tilemaps.Tilemap ground, Vector3 target)
        {
            if (!battle.TryGetUnit(100, out var actor)) throw new InvalidOperationException("Missing cast fixture actor.");
            if (combatMagicStep == 0)
            {
                if (presentation.DisplayedMagicCount != 0) throw new InvalidOperationException("Cast fixture requires empty knowledge.");
                uiMagicCatalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                combatMagicId = 0;
                for (int i = 0; i < uiMagicCatalog.Count; i++) {
                    var spell = uiMagicCatalog.GetAt(i);
                    if (spell.Range > 0 && spell.Unit != 8 && TemporaryMagicCatalogRules.TryClassify(spell, out var effect, out _)
                        && effect == TemporaryMagicEffect.Damage) { combatMagicId = (short)spell.Id; break; }
                }
                if (combatMagicId == 0) throw new InvalidOperationException("No supported point spell in converted catalog.");
                BattleFormationSnapshot Knowledge(short[] ids) {
                    var draft = new BattleFormationMemberDraft { PersistentId = "isolated-cast-100", CommanderPersistentId = "isolated-cast-100",
                        CommanderId = 100, Unit = new CombatUnit { Id = 100, JobId = actor.JobId, Name = actor.Name,
                            X = actor.X, Y = actor.Y, Hp = actor.Hp, MaxHp = actor.MaxHp, Mp = actor.Mp, MaxMp = actor.MaxMp },
                        IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(actor.X, actor.Y),
                        TeamId = 1, RecordEligible = true, LearnedMagicIds = ids };
                    if (!BattleFormation.TryCreateSnapshot(new[] { draft }, ground.cellBounds.size.x, ground.cellBounds.size.y,
                        Array.Empty<short>(), out var snapshot, out var reason)) throw new InvalidOperationException(reason);
                    return snapshot;
                }
                uiRestoreKnowledge = Knowledge(Array.Empty<short>());
                if (!presentation.ConfigureFormationLearnedMagic(uiMagicCatalog, Knowledge(new[] { combatMagicId })))
                    throw new InvalidOperationException("Cannot configure isolated cast knowledge.");
                combatMagicStep = 1; Note("Isolated cast fixture configured: " + combatMagicId + "; profile unchanged.");
            }
            if (combatMagicStep == 1)
            {
                if (!battle.CanStartMagicRequest(combatMagicId, 100, new[] { battle.PrimaryEnemyId }, out _)) return false;
                int ap = actor.Ap, mp = actor.Mp;
                if (!presentation.TryOpenActionMenu() || !presentation.TryChooseCommand(BattleMapCommand.Magic)
                    || !presentation.TrySelectDisplayedMagic(0) || !presentation.TryHandleWorldClick(target))
                    throw new InvalidOperationException("Learned point cast failed: " + presentation.LastInputMessage);
                if (presentation.ActiveMagicEffectCount <= 0 || battle.PlayerAp >= ap || battle.PlayerMp >= mp)
                    throw new InvalidOperationException("Cast did not start visuals and spend AP/MP.");
                Note($"PASS: real Play learned spell {combatMagicId}, AP {ap}->{battle.PlayerAp}, MP {mp}->{battle.PlayerMp}; effect active.");
                combatMagicStep = 2; nextAction = EditorApplication.timeSinceStartup + .16; return true;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "learned-magic-cast.png"));
            if (!presentation.ConfigureFormationLearnedMagic(uiMagicCatalog, uiRestoreKnowledge) || presentation.DisplayedMagicCount != 0)
                throw new InvalidOperationException("Cannot restore empty cast fixture knowledge.");
            SessionState.SetBool(Key + "combatMagic", false);
            Note("Cast knowledge restored; continuing basic combat and result/return verification."); return true;
        }

        // UI fixture only: no spell is cast; knowledge is restored before combat/save return.
        static bool PreviewMagicUi()
        {
            if (magicUiStep == 0)
            {
                if (presentation.DisplayedMagicCount != 0 || presentation.MagicChoiceCount != 0)
                    throw new InvalidOperationException("UI fixture requires the isolated new fighter's empty knowledge.");
                if (!battle.TryGetUnit(100, out var actor) || actor.JobId != 100)
                    throw new InvalidOperationException("Unexpected UI fixture actor.");
                uiMagicCatalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                var ground = FindGround();
                if (uiMagicCatalog == null || ground == null) throw new InvalidOperationException("Missing UI fixture resources.");
                BattleFormationSnapshot Knowledge(short[] ids)
                {
                    var draft = new BattleFormationMemberDraft { PersistentId = "isolated-ui-100", CommanderPersistentId = "isolated-ui-100",
                        CommanderId = 100, Unit = new CombatUnit { Id = 100, JobId = 100, Name = actor.Name, X = actor.X, Y = actor.Y, Hp = actor.Hp, MaxHp = actor.MaxHp, Mp = actor.Mp, MaxMp = actor.MaxMp },
                        IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(actor.X, actor.Y), TeamId = 1,
                        RecordEligible = true, LearnedMagicIds = ids };
                    if (!BattleFormation.TryCreateSnapshot(new[] { draft }, ground.cellBounds.size.x, ground.cellBounds.size.y,
                        Array.Empty<short>(), out var snapshot, out var reason)) throw new InvalidOperationException(reason);
                    return snapshot;
                }
                uiRestoreKnowledge = Knowledge(Array.Empty<short>());
                var fixture = Knowledge(new short[] { 101, 102, 103, 301, 302, 501, 502, 701, 702, 801, 802, 2701 });
                if (!presentation.ConfigureFormationLearnedMagic(uiMagicCatalog, fixture)
                    || !presentation.TryOpenActionMenu() || !presentation.TryChooseCommand(BattleMapCommand.Magic))
                    throw new InvalidOperationException("Cannot open the UI-only magic fixture.");
                Note("UI-only magic fixture opened: 12 catalog entries; no spell cast or permanent learning.");
                magicUiStep = 1; nextAction = EditorApplication.timeSinceStartup + .5; return true;
            }
            if (magicUiStep == 1)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "magic-menu-ui-fixture.png"));
                magicUiStep = 2; nextAction = EditorApplication.timeSinceStartup + .5; return true;
            }
            presentation.CancelMagicTargeting();
            if (!presentation.ConfigureFormationLearnedMagic(uiMagicCatalog, uiRestoreKnowledge) || presentation.DisplayedMagicCount != 0)
                throw new InvalidOperationException("Cannot restore empty fixture knowledge.");
            SessionState.SetBool(Key + "previewMagic", false);
            Note("UI fixture knowledge restored; continuing real combat with no added spells.");
            return false;
        }
    }
}
