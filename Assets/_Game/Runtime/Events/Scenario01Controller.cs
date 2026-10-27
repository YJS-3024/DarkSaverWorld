using DarkSaver.Network;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using DarkSaver.CameraSystem;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Events
{
    // A local scenario director. Battle adjudication stays in the existing combat
    // authority; scripted ATTACK/MAGIC are presentation and never award kills.
    public sealed class Scenario01Controller : MonoBehaviour
    {
        private static Scenario01Controller current;
        private static Scenario01Definition definition;
        public static Scenario01Definition Definition
        {
            get
            {
                if (definition == null)
                {
                    var asset = Resources.Load<TextAsset>("Scenarios/01/Content");
                    if (asset != null) definition = JsonUtility.FromJson<Scenario01Definition>(asset.text);
                }
                return definition;
            }
        }
        [SerializeField] private string scenarioKey = "01";
        private static readonly Dictionary<string, Scenario01Definition> definitions = new();
        public static string ActiveKey => current != null ? current.scenarioKey : null;
        public static ScenarioDefinition CurrentScenario => current != null && ScenarioCatalog.TryGet(current.scenarioKey, out var metadata) ? metadata : null;
        public static Scenario01Definition LoadDefinition(string key)
        {
            if (key == "01") return Definition;
            if (!ScenarioCatalog.TryGet(key, out var metadata)) return null;
            if (!definitions.TryGetValue(metadata.Key, out var content))
            {
                var asset = Resources.Load<TextAsset>("Scenarios/" + metadata.Key + "/Content");
                content = asset != null ? JsonUtility.FromJson<Scenario01Definition>(asset.text) : null;
                if (content != null) definitions[metadata.Key] = content;
            }
            return content;
        }
        private Scenario01Definition Content => LoadDefinition(scenarioKey);
        private ScenarioDefinition Metadata => ScenarioCatalog.TryGet(scenarioKey, out var row) ? row : null;
        private Scenario01Progress CaptureProgress() => scenarioKey == "01" ? session.CaptureScenario01() : session.CaptureCampaign(scenarioKey);
        private int ProgressRevision => scenarioKey == "01" ? session.Scenario01Revision : session.CampaignRevision;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { current = null; definition = null; definitions.Clear(); }

        public static bool BlocksWorldActions => current != null && !current.aborting && current.player != null
            && (current.progress == null || current.ScriptActive || current.completionResultPending || !string.IsNullOrEmpty(current.error));
        public bool ScriptActive => progress != null && (progress.phase == Scenario01Progress.Intro
            || progress.phase == Scenario01Progress.Outro || progress.activeEvent != 0);
        private bool CinematicActive => progress != null && progress.activeEvent == 0
            && (progress.phase == Scenario01Progress.Intro || progress.phase == Scenario01Progress.Outro);
        private bool completionResultPending;
        public bool CompletionResultPending=>completionResultPending;
        public bool AcknowledgeCompletionResult()
        {
            if(!completionResultPending || progress==null || progress.phase!=Scenario01Progress.Complete)return false;
            completionResultPending=false;player?.SetScenarioInputBlocked(false);return true;
        }
        public string Error => error;
        public static void EnsureForLoadedScene()
        {
            if(!OriginalRestorationPolicy.AllowSinglePlayerRules)return;
            foreach (var scenario in ScenarioCatalog.All)
            {
                var scene = SceneManager.GetSceneByName(scenario.RuntimeScene);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                bool exists = scene.GetRootGameObjects().Any(root => root.GetComponent<Scenario01Controller>() != null);
                if (exists) continue;
                var instance = new GameObject("Scenario " + scenario.Key + " Director");
                SceneManager.MoveGameObjectToScene(instance, scene);
                instance.AddComponent<Scenario01Controller>().scenarioKey = scenario.Key;
            }
        }

        private BattleTransitionController session;
        private TopDownPlayerController player;
        private SpriteRenderer playerBody;
        private CameraFollow2D cameraFollow;
        private Transform cameraTarget;
        private Tilemap ground, terrain, detail, presentationGround;
        private string displayedMapKey;
        private bool swappingView;
        private bool? savedBodySimulation;
        private readonly List<(Renderer Renderer,bool Enabled)> hiddenMapRenderers=new();
        private ServerManager protocol;
        private IDisposable scenarioBinding;
        private ScenarioStateNotification lastResponse;
        private ScenarioCommandRequest pendingSaveRequest;
        public bool CanRetrySave => pendingSaveRequest!=null;
        private bool Request(ScenarioOperation operation,int x=0,int y=0,double seconds=0)
        {
            lastResponse=null;
            var request=new ScenarioCommandRequest(scenarioKey,revision,operation,x,y,seconds);
            bool accepted=protocol.TryRequest(request,out var response);
            if(!accepted)
            {
                if(response is ScenarioStateNotification failed && failed.RetryableSaveFailure)
                {
                    pendingSaveRequest=request;error=failed.Reason;
                    if(player!=null)player.SetScenarioInputBlocked(true);
                    Debug.LogWarning("Scenario "+scenarioKey+" save postponed: "+session.ScenarioSaveDiagnostic);
                }
                else {pendingSaveRequest=null;Fail(response?.Reason ?? "로컬 시나리오 서버가 없습니다.");}
                return false;
            }
            pendingSaveRequest=null;
            if(lastResponse==null) { Fail("시나리오 응답을 받지 못했습니다."); return false; }
            return true;
        }
        public bool RetrySave()
        {
            var request=pendingSaveRequest;
            if(request==null || session==null || session.IsTransitioning || swappingView
                || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling==true
                || SceneManager.GetSceneByName("Battle").isLoaded)return false;
            string previousError=error;
            error=null;
            if(request.Operation==ScenarioOperation.Step)Step();
            else if(request.Operation==ScenarioOperation.BeginEncounter)
            {
                if(!views.TryGetValue(request.X,out var view)) {pendingSaveRequest=null;Fail("조우 대상을 확인할 수 없습니다.");return false;}
                player.SetScenarioInputBlocked(false);
                TryEncounter(request.X,view.GetComponent<SpriteRenderer>());
            }
            else if(Request(request.Operation,request.X,request.Y,request.Seconds))
            {
                if(!initialized)
                {
                    if(progress.pendingBattle!=-1 && !Request(ScenarioOperation.CancelEncounter))return false;
                    CompleteInitialization();
                }
                else Reconcile();
            }
            if(pendingSaveRequest!=null && string.IsNullOrEmpty(error))
            {error=previousError;if(player!=null)player.SetScenarioInputBlocked(true);}
            return pendingSaveRequest==null && string.IsNullOrEmpty(error);
        }
        private void ReceiveScenarioPacket(ServerPacket packet)
        {
            if(packet is not ScenarioStateNotification state || state.Key!=scenarioKey)return;
            lastResponse=state;
            if(!state.Accepted)return;
            progress=state.CaptureState();revision=state.Revision;
        }
        private Scenario01Progress progress;
        private int revision = -1;
        private bool initialized, aborting;
        private bool previousPlayerVisible;
        private string error;
        private InGameUi ui;
        private readonly Dictionary<int, GameObject> views = new();
        private readonly List<BattleEnemyDefinition> enemyDefinitions = new();
        private readonly HashSet<int> openedVisuals = new();
        [Serializable] private sealed class ChestBinding
        { public int eventId, sourceIndex, x, y, widthTiles, heightTiles; }
        [Serializable] private sealed class VisualBindings
        { public string contentHash; public ChestBinding[] chests; public MapOperation[] operations; }
        [Serializable] private sealed class MapOperation
        { public int line,x,y,sourceIndex,attribute,widthTiles,heightTiles; public string kind,texture; }
        private readonly List<UnityEngine.Object> visualAssets = new();
        private int[] appliedChanges = Array.Empty<int>();
        private VisualBindings visualBindings;
        private CharacterPortraitCatalog playerPortraits;
        private AudioSource music;
        private AudioListener createdListener;
        private string playingMusic;
        private readonly HashSet<string> missingMusic = new();
        private readonly HashSet<int> approximateEffects = new();
        private readonly List<(SpriteRenderer Body, SpriteRenderer Anchor, float Age, bool Magic)> effects = new();
        private Sprite[] attackFrames;

        private void Start()
        {
            if(!OriginalRestorationPolicy.AllowSinglePlayerRules){enabled=false;return;}
            current = this;
            foreach (var metadata in ScenarioCatalog.All) if (metadata.RuntimeScene == gameObject.scene.name) scenarioKey = metadata.Key;
            session = FindFirstObjectByType<BattleTransitionController>();
            player = FindFirstObjectByType<TopDownPlayerController>();
            if (player != null) { playerBody = player.GetComponent<SpriteRenderer>(); previousPlayerVisible = playerBody.enabled; }
            cameraFollow = FindFirstObjectByType<CameraFollow2D>();
            foreach (var root in SceneManager.GetSceneByName(Metadata.MapScene).GetRootGameObjects())
                foreach (var tilemap in root.GetComponentsInChildren<Tilemap>(true))
                { if (tilemap.name == "Ground") ground = tilemap; if (tilemap.name == "TerrainAttributes") terrain = tilemap; if (tilemap.name == "Detail") detail = tilemap; }
            if (Content == null || !Content.IsValid || session == null || player == null || ground == null)
            { Fail("시나리오 자산 또는 실행 문맥을 확인할 수 없습니다."); return; }
            if (!session.HasCreatedCharacter)
            { Fail("시작 화면에서 캐릭터를 생성하거나 저장된 게임을 불러와 주세요."); return; }
            // The earlier single-boss demo is replaced for this runtime scene only.
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var encounter in root.GetComponentsInChildren<BattleEncounter2D>(true)) encounter.gameObject.SetActive(false);
            cameraTarget = new GameObject("Scenario Camera Target").transform;
            cameraTarget.SetParent(transform, false);
            music = gameObject.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true;
            if (FindAnyObjectByType<AudioListener>() == null && cameraFollow != null)
                createdListener = cameraFollow.gameObject.AddComponent<AudioListener>();
            attackFrames = BattleAttackEffectFrames.Load();
            revision=ProgressRevision;
            protocol=session.GetComponent<ServerManager>();
            if(protocol==null)protocol=session.gameObject.AddComponent<ServerManager>();
            var local=new ScenarioServerBoundary(Content,CaptureProgress,session.CaptureInventory,()=>ProgressRevision,
                ()=>session.PlayerJobId,()=>session.IsScenarioCleared(scenarioKey),
                (Scenario01Progress candidate,NativeInventory items,out string reason)=>scenarioKey=="01"
                    ? session.TryCommitScenario01(ProgressRevision,candidate,items,out reason)
                    : session.TryCommitCampaign(scenarioKey,ProgressRevision,candidate,items,out reason),()=>session.CanRetryScenarioSave);
            scenarioBinding=protocol.RegisterRequestHandler<ScenarioCommandRequest>(local.Handle);
            protocol.ResponseReceived+=ReceiveScenarioPacket;
            if(!Request(ScenarioOperation.Initialize))return;
            // An interrupted battle does not manufacture a victory when resumed.
            if(progress.pendingBattle!=-1 && !Request(ScenarioOperation.CancelEncounter))return;
            CompleteInitialization();
        }

        private void CompleteInitialization()
        {
            var visuals = Resources.Load<TextAsset>("Scenarios/" + scenarioKey + "/Visuals");
            visualBindings = visuals == null ? null : JsonUtility.FromJson<VisualBindings>(visuals.text);
            if (visualBindings == null || visualBindings.contentHash != Content.sourceHash || (scenarioKey == "01" ? visualBindings.chests == null : visualBindings.operations == null))
            { Fail("시나리오 연출 자산 버전이 다릅니다."); return; }
            playerPortraits = Resources.Load<CharacterPortraitCatalog>("UI/CharacterPortraits/CharacterPortraits");
            displayedMapKey=scenarioKey; presentationGround=ground;
            initialized = true; Reconcile();
        }

        private void Fail(string message)
        { pendingSaveRequest=null;error = message; if (player != null) player.SetScenarioInputBlocked(true); Debug.LogError("Scenario01: " + message); }

        private void Update()
        {
            if (!initialized || aborting || swappingView || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling == true || !string.IsNullOrEmpty(error) || Time.timeScale == 0) return;
            if (session.IsTransitioning || SceneManager.GetSceneByName("Battle").isLoaded)
            {
                foreach (var pair in views)
                    if (pair.Key != progress.pendingBattle) pair.Value.GetComponent<SpriteRenderer>().enabled = false;
                return;
            }
            if (revision != ProgressRevision)
            {
                progress = CaptureProgress(); revision = ProgressRevision;
                if (progress == null || !progress.Matches(Content)) { Fail("시나리오 복원 상태를 확인할 수 없습니다."); return; }
                Reconcile();
            }
            // Keep the completion modal locked on every frame, including unchanged revisions.
            if(completionResultPending)
            {player.SetScenarioInputBlocked(true);TickActors(Time.unscaledDeltaTime);return;}
            // A rejected asynchronous battle load must not leave this encounter locked.
            if (progress.pendingBattle != -1)
            {
                if (!Request(ScenarioOperation.CancelEncounter)) return;
                Reconcile();
            }
            if (scenarioKey != "01" && progress.phase == Scenario01Progress.Complete)
            { AbortToTown(); return; }
            player.SetScenarioInputBlocked(ScriptActive);
            if (playerBody != null) playerBody.enabled = CinematicActive ? false : previousPlayerVisible;
            TickActors(Time.unscaledDeltaTime); TickEffects(Time.unscaledDeltaTime);
            if (ScriptActive)
            {
                if (progress.wait > 0) return;
                // Execute immediate commands in source order in this update. Blocking
                // commands yield; there is no invented one-command-per-frame cadence.
                for (int budget = 0; budget < 128 && ScriptActive && string.IsNullOrEmpty(error); budget++)
                    if (!Step()) break;
            }
            else if (progress.phase == Scenario01Progress.Playing && session.CanEnterBattle)
            {
                var cell = ground.WorldToCell(player.transform.position);
                if(Request(ScenarioOperation.Probe,cell.x,Content.height-1-cell.y) && lastResponse.Changed)Reconcile();
            }
        }

        private bool Step()
        {
            var commands = Scenario01Rules.CurrentCommands(Content, progress);
            var command = progress.cursor < commands.Length ? commands[progress.cursor] : null;
            bool wasIntro = progress.phase == Scenario01Progress.Intro && progress.activeEvent == 0;
            bool wasOutro = progress.phase == Scenario01Progress.Outro && progress.activeEvent == 0;
            try
            {
                var previous=progress;
                if(!Request(ScenarioOperation.Step))return false;
                var result=lastResponse.StepResult;
                if(result==Scenario01Rules.StepResult.Waiting)return false;
                if (result == Scenario01Rules.StepResult.Finished)
                {
                    if(wasOutro && progress.phase==Scenario01Progress.Complete)completionResultPending=true;
                    if (wasIntro) SetPlayerPosition(World(Content.entryX, Content.entryY));
                    else if (wasOutro && scenarioKey != "01") SetPlayerPosition(ground.GetCellCenterWorld(new Vector3Int(Content.entryX,Content.height-1-Content.entryY,0)));
                    else if (wasOutro)
                    {
                        var actor = previous.actors.Find(a => a.id == 301);
                        if (actor != null) SetPlayerPosition(World(actor.x, actor.y));
                    }
                }
                if (command != null && result == Scenario01Rules.StepResult.Advanced)
                {
                    if (command.name == "ATTACK" || command.name == "MAGIC") PresentAction(command);
                }
                Reconcile(); return !swappingView && result == Scenario01Rules.StepResult.Advanced && command?.name != "DO";
            }
            catch (Exception ex) { Fail("연출 " + command?.line + "행: " + ex.Message); return false; }
        }

        public bool AdvanceDialogue()
        {
            if (!initialized || swappingView || !string.IsNullOrEmpty(error) || !ScriptActive || session.IsTransitioning || Time.timeScale == 0) return false;
            return Request(ScenarioOperation.Dialogue) && lastResponse.Changed;
        }

        private int ViewHeight => ScenarioCatalog.TryGet(displayedMapKey ?? scenarioKey,out var view) ? view.Height : Content.height;
        private Vector3 World(float x, float y) => (presentationGround ?? ground).CellToWorld(new Vector3Int((int)x, ViewHeight - 1 - (int)y, 0))
            + new Vector3(.5f + x - (int)x, .5f - (y - (int)y), 0);
        private void SetPlayerPosition(Vector3 position)
        {
            player.transform.position = position;
            var body = player.GetComponent<Rigidbody2D>(); if (body != null) { body.position = player.transform.position; body.linearVelocity = Vector2.zero; }
        }
        private static Vector2 Facing(int value) => value == 0 ? Vector2.up : value == 1 ? Vector2.down : value == 2 ? Vector2.left : Vector2.right;
        private void TickActors(float seconds)
        {
            if(!Request(ScenarioOperation.Clock,seconds:seconds))return;
            foreach (var actor in progress.actors)
                if (views.TryGetValue(actor.id, out var view))
                {
                    view.transform.position = World(actor.x, actor.y);
                    var animator = view.GetComponent<DirectionalSpriteAnimator>(); animator.TrySetFacing(Facing(actor.facing)); animator.SetMovingPresentation(actor.moving);
                    view.GetComponent<SpriteRenderer>().sortingOrder = 20000 - Mathf.RoundToInt(view.transform.position.y * 10f);
                }
        }

        private GameObject CreateView(int id, int job, float x, float y, bool encounter)
        {
            var view = new GameObject("Scenario actor " + id, typeof(SpriteRenderer), typeof(DirectionalSpriteAnimator));
            view.transform.SetParent(transform, false); view.transform.position = World(x, y);
            var animator = view.GetComponent<DirectionalSpriteAnimator>();
            if (job == session.PlayerJobId)
            {
                if (!player.GetComponent<DirectionalSpriteAnimator>().TryCopyConfigurationTo(animator)) throw new InvalidOperationException("Missing player artwork.");
            }
            else
            {
                Sprite[] Frames(string direction) => Enumerable.Range(0, 3).Select(i => Resources.Load<Sprite>((scenarioKey == "01" ? "Scenarios/01/Characters/" : "Scenarios/Shared/Characters/") + job + "_" + direction + "_" + i)).ToArray();
                var up = Frames("Up"); var down = Frames("Down"); var left = Frames("Left"); var right = Frames("Right");
                if (up.Concat(down).Concat(left).Concat(right).Any(s => s == null)) throw new InvalidOperationException("Missing scenario character artwork: " + job);
                animator.Configure(up, down, left, right);
            }
            animator.PresentFrame(0);
            view.GetComponent<SpriteRenderer>().sortingOrder = 20000 - Mathf.RoundToInt(view.transform.position.y * 10f);
            if (encounter)
            {
                // Encounter before the player enters the occupied tile. A .7-tile
                // trigger could first overlap inside the same cell, rejecting the combat grid.
                var trigger = view.AddComponent<BoxCollider2D>(); trigger.size = new Vector2(1.4f, 1.4f); trigger.isTrigger = true;
                view.AddComponent<Scenario01Encounter>().Configure(this, id);
            }
            return view;
        }
        private void Reconcile()
        {
            if (progress == null || ground == null) return;
            try
            {
                if (!EnsurePresentationMap()) return;
                var desired = new Dictionary<int, (int Job, float X, float Y, bool Encounter)>();
                if (CinematicActive || progress.phase == Scenario01Progress.Complete)
                    foreach (var actor in progress.actors) desired[actor.id] = (actor.job, actor.x, actor.y, false);
                else if (progress.phase == Scenario01Progress.Playing)
                {
                    foreach (var unit in Content.units.Where(u => u.wave == progress.wave && !progress.defeated.Contains(u.id)))
                    {
                        var saved = progress.enemies.Find(e => e.id == unit.id);
                        desired[unit.id] = (unit.job, saved?.x ?? unit.x, saved?.y ?? unit.y, true);
                    }
                    // Authored NPC bindings to the two source rescue sites; not recovered server placement.
                    if (scenarioKey == "01" && !progress.removedActors.Contains(800)) desired[800] = (801, 15, 5, false);
                    if (scenarioKey == "01" && !progress.removedActors.Contains(810)) desired[810] = (802, 24, 5, false);
                }
                foreach (var id in views.Keys.ToArray())
                    if (!desired.ContainsKey(id) || views[id].GetComponent<ScenarioActorIdentity>()?.Job != desired[id].Job
                        || (views[id].GetComponent<Scenario01Encounter>() != null) != desired[id].Encounter)
                    { Destroy(views[id]); views.Remove(id); }
                foreach (var pair in desired)
                    if (!views.ContainsKey(pair.Key))
                    {
                        var data = pair.Value; var view = CreateView(pair.Key, data.Job, data.X, data.Y, data.Encounter);
                        view.AddComponent<ScenarioActorIdentity>().Job = data.Job; views.Add(pair.Key, view);
                    }
                foreach (var view in views.Values) view.GetComponent<SpriteRenderer>().enabled = true;
                if (scenarioKey == "01") foreach (var id in progress.changedEvents) ApplyOpenedChest(id);
                else ApplyMapChanges();
                if (!string.IsNullOrEmpty(progress.musicSource)) PlayMusic(progress.musicSource, progress.musicMode);
                if (cameraFollow != null)
                {
                    if (ScriptActive)
                    {
                        cameraTarget.position = World(progress.cameraX, progress.cameraY);
                        cameraFollow.Configure(cameraTarget, new Vector2(ScenarioCatalog.TryGet(displayedMapKey ?? scenarioKey,out var view) ? view.Width : Content.width, ViewHeight));
                        cameraFollow.transform.position = new Vector3(cameraTarget.position.x, cameraTarget.position.y, cameraFollow.transform.position.z);
                    }
                    else cameraFollow.Configure(player.transform, new Vector2(Content.width, Content.height));
                }
                player.SetScenarioInputBlocked(ScriptActive || completionResultPending);
                if (playerBody != null) playerBody.enabled = CinematicActive ? false : previousPlayerVisible;
            }
            catch (Exception ex) { Fail(ex.Message); }
        }

        private bool EnsurePresentationMap()
        {
            string wanted=CinematicActive && !string.IsNullOrEmpty(progress.presentationMap) ? progress.presentationMap : scenarioKey;
            if(displayedMapKey==wanted)return true;
            if(!swappingView)StartCoroutine(SwapPresentationMap(wanted));
            return false;
        }
        private IEnumerator SwapPresentationMap(string wanted)
        {
            swappingView=true;
            if(wanted==scenarioKey)
            {
                foreach(var pair in hiddenMapRenderers)if(pair.Renderer!=null)pair.Renderer.enabled=pair.Enabled;
                hiddenMapRenderers.Clear();
                var old=ScenarioCatalog.TryGet(displayedMapKey,out var oldMetadata)?oldMetadata:null;
                presentationGround=ground;displayedMapKey=scenarioKey;
                if(old!=null && old.MapScene!=Metadata.MapScene && SceneManager.GetSceneByName(old.MapScene).isLoaded)
                    yield return SceneManager.UnloadSceneAsync(old.MapScene);
                var body=player.GetComponent<Rigidbody2D>();if(body!=null && savedBodySimulation.HasValue)body.simulated=savedBodySimulation.Value;
                savedBodySimulation=null;
            }
            else
            {
                if(!ScenarioCatalog.TryGet(wanted,out var view)){Fail("연출 맵을 확인할 수 없습니다.");swappingView=false;yield break;}
                var body=player.GetComponent<Rigidbody2D>();if(body!=null){savedBodySimulation=body.simulated;body.simulated=false;}
                if(!SceneManager.GetSceneByName(view.MapScene).isLoaded)yield return SceneManager.LoadSceneAsync(view.MapScene,LoadSceneMode.Additive);
                var map=SceneManager.GetSceneByName(view.MapScene);
                if(!map.isLoaded){Fail("연출 맵을 불러올 수 없습니다.");swappingView=false;yield break;}
                presentationGround=map.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)).FirstOrDefault(t=>t.name=="Ground");
                if(presentationGround==null){Fail("연출 맵 바닥이 없습니다.");swappingView=false;yield break;}
                foreach(var root in SceneManager.GetSceneByName(Metadata.MapScene).GetRootGameObjects())
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){hiddenMapRenderers.Add((renderer,renderer.enabled));renderer.enabled=false;}
                displayedMapKey=wanted;
            }
            swappingView=false;Reconcile();
        }

        private void ApplyMapChanges()
        {
            if (progress.mapChanges.SequenceEqual(appliedChanges)) return;
            foreach (int line in progress.mapChanges)
            {
                var operation = Array.Find(visualBindings.operations, o => o.line == line);
                if (operation == null) throw new InvalidOperationException("Missing map change binding: " + line);
                if (operation.kind == "CHANGESPRITE" && operation.sourceIndex < 0) continue; // Native first-origin miss is inert.
                var texture = Resources.Load<Texture2D>("Scenarios/" + scenarioKey + "/" + operation.texture);
                if (texture == null) throw new InvalidOperationException("Missing map change artwork.");
                var sprite = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),operation.kind == "CHANGESPRITE" ? new Vector2(0,1) : new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
                visualAssets.Add(sprite);
                if (operation.kind == "CHANGESPRITE")
                {
                    var target = SceneManager.GetSceneByName(Metadata.MapScene).GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true)).FirstOrDefault(r => r.name == "Object_" + operation.sourceIndex.ToString("000"));
                    if (target == null) throw new InvalidOperationException("Missing source map object.");
                    target.sprite = sprite; target.enabled = true;
                }
                else
                {
                    if (detail == null) throw new InvalidOperationException("Missing detail tilemap.");
                    var tile = ScriptableObject.CreateInstance<Tile>();visualAssets.Add(tile);tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;
                    detail.SetTile(new Vector3Int(operation.x,Content.height-1-operation.y,0),tile);
                }
                var attribute = ScriptableObject.CreateInstance<Tile>(); visualAssets.Add(attribute);
                attribute.name="Terrain_"+operation.attribute;
                attribute.colliderType=operation.attribute==3 || operation.attribute==6 || operation.attribute==9 ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                for(int y=0;y<operation.heightTiles;y++)for(int x=0;x<operation.widthTiles;x++)
                    terrain.SetTile(new Vector3Int(operation.x+x,Content.height-1-operation.y-y,0),attribute);
            }
            // Unity's incremental tile collider can retain the old bridge shapes even
            // when GetColliderType already reports None. Rebuild after the whole batch,
            // including restoration of saved map changes, before allowing world travel.
            var terrainCollider = terrain.GetComponent<TilemapCollider2D>();
            if (terrainCollider != null && terrainCollider.enabled)
            {
                terrainCollider.enabled = false;
                terrainCollider.enabled = true;
            }
            appliedChanges=progress.mapChanges.ToArray();
        }

        private void ApplyOpenedChest(int eventId)
        {
            if (openedVisuals.Contains(eventId)) return;
            var binding = Array.Find(visualBindings.chests, c => c.eventId == eventId);
            if (binding == null) return;
            var sprite = Resources.Load<Sprite>("Scenarios/01/OpenedChest");
            var attribute = Resources.Load<TileBase>("Scenarios/01/ChestTerrain22");
            if (sprite == null || attribute == null || terrain == null) throw new InvalidOperationException("Missing chest presentation resources.");
            SpriteRenderer target = null;
            foreach (var root in SceneManager.GetSceneByName(Metadata.MapScene).GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.name == "Object_" + binding.sourceIndex.ToString("000")) target = renderer;
            if (target == null) throw new InvalidOperationException("Missing source chest object: " + binding.sourceIndex);
            // Native SpriteChange replaces only the first MRC origin match and
            // writes terrain attribute 22 over the replacement's complete footprint.
            target.sprite = sprite; target.enabled = true;
            for (int y = 0; y < binding.heightTiles; y++)
                for (int x = 0; x < binding.widthTiles; x++)
                    terrain.SetTile(new Vector3Int(binding.x + x, Content.height - 1 - binding.y - y, 0), attribute);
            openedVisuals.Add(eventId);
        }

        public bool TryEncounter(int id, SpriteRenderer visual)
        {
            if (!initialized || ScriptActive || !string.IsNullOrEmpty(error) || progress.phase != Scenario01Progress.Playing
                || session.IsTransitioning || player.UiInputBlocked || progress.pendingBattle != -1 || SceneManager.GetSceneByName("Battle").isLoaded) return false;
            var unit = Array.Find(Content.units, u => u.id == id && u.wave == progress.wave);
            if (unit == null || progress.defeated.Contains(id)) return false;
            if(!Request(ScenarioOperation.BeginEncounter,id))return false;
            var enemy = ScriptableObject.CreateInstance<BattleEnemyDefinition>();
            enemy.InitializeScenarioUnit(unit, progress.enemies.Find(e => e.id == id));
            enemyDefinitions.Add(enemy);
            if (session.TryEnterBattle(null, enemy, visual)) return true;
            Request(ScenarioOperation.CancelEncounter); return false;
        }

        private void PlayMusic(string source, int mode)
        {
            var key = System.IO.Path.GetFileNameWithoutExtension(source.Replace('\\', '/')).ToLowerInvariant();
            var clip = Resources.Load<AudioClip>((scenarioKey == "01" ? "Scenarios/01/Music/" : "Scenarios/Shared/Music/") + key);
            if (clip == null)
            {
                music.Stop();music.clip=null;playingMusic=null;
                if (missingMusic.Add(source)) Debug.LogWarning("Scenario01: original MIDI is not a Unity AudioClip; music binding unavailable: " + source);
                return;
            }
            music.loop=mode==1;
            if (playingMusic == key) return;
            music.Stop(); music.clip = clip; music.loop = mode == 1; music.Play(); playingMusic = key;
        }
        private void PresentAction(Scenario01Command command)
        {
            if (command.name == "ATTACK")
            {
                if (views.TryGetValue(command.Int(1), out var target)) StartEffect(target.GetComponent<SpriteRenderer>(), false);
                return;
            }
            int magicId = command.Int(0);
            if(scenarioKey != "01")
            {
                SpriteRenderer Target(int id) => views.TryGetValue(id,out var actor) ? actor.GetComponent<SpriteRenderer>() : id==301 ? playerBody : null;
                var caster=Target(command.Int(1));var source=caster!=null?caster.bounds.center:World(Mathf.Max(0,command.Int(2)),Mathf.Max(0,command.Int(3)));
                bool shown=false;
                for(int index=4;index<command.args.Length;index++)
                {
                    var target=Target(command.Int(index));if(target==null)continue;
                    shown |= ScenarioSpellVisual.Spawn(transform,magicId,source,target.bounds.center,target,target.sortingOrder+1);
                }
                if(command.Int(2)>=0 && command.Int(3)>=0)
                {var point=World(command.Int(2),command.Int(3));shown |= ScenarioSpellVisual.Spawn(transform,magicId,source,point,null,20001-Mathf.RoundToInt(point.y*10));}
                if(!shown && approximateEffects.Add(magicId))Debug.LogWarning("Scenario "+scenarioKey+": no native effect binding/valid target for MAGIC "+magicId+"; no substitute applied.");
                return;
            }

            if (magicId == 2901)
            {
                var anchor = new GameObject("Scenario thunder target", typeof(SpriteRenderer)); anchor.transform.SetParent(transform, false);
                anchor.transform.position = World(command.Int(2), command.Int(3));
                anchor.GetComponent<SpriteRenderer>().sortingOrder = 20000 - Mathf.RoundToInt(anchor.transform.position.y * 10f);
                StartEffect(anchor.GetComponent<SpriteRenderer>(), true);
            }
            else
            {
                // Preserve the distinct scripted token 1; never silently turn it into magic 101.
                if (approximateEffects.Add(magicId)) Debug.LogWarning(magicId == 1
                    ? "Scenario01: original MAGIC 1 has no magictbl.atr entry. Native lookup returns -1 with stale cached fields; no substitute effect is played."
                    : "Scenario01: scripted MAGIC has no confirmed effect binding: " + magicId);
            }
        }
        private void StartEffect(SpriteRenderer anchor, bool magicEffect)
        {
            if(magicEffect)
            {
                var sound=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");
                if(sound!=null && sound.TryGet(2901,out var entry))DarkSaver.World.WorldAudioService.PlayEffect(entry.Sound1);
            }
            var go = new GameObject("Scripted action effect", typeof(SpriteRenderer)); go.transform.SetParent(transform, false);
            var body = go.GetComponent<SpriteRenderer>(); body.sortingOrder = anchor.sortingOrder + 1;
            effects.Add((body, anchor, 0, magicEffect));
        }
        private void TickEffects(float seconds)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i]; effect.Age += seconds;
                int frame = BattleAttackEffectFrames.FrameAt(effect.Age);
                if (effect.Anchor == null || effect.Age > 1 || (!effect.Magic && frame < 0))
                { Destroy(effect.Body.gameObject); if (effect.Magic && effect.Anchor != null) Destroy(effect.Anchor.gameObject); effects.RemoveAt(i); continue; }
                effect.Body.transform.position = effect.Anchor.transform.position;
                if (effect.Magic)
                {
                    var thunder = Resources.Load<NativeThunderEffects>("Effects/NativeThunderEffects");
                    effect.Body.sprite = thunder != null ? thunder.Bolt(NativeTargetEffects.CounterAt(effect.Age)) : null;
                }
                else if (attackFrames != null && frame >= 0 && frame < attackFrames.Length) effect.Body.sprite = attackFrames[frame];
                effects[i] = effect;
            }
        }

        private void DrawNotice()
        {
            if(progress==null || progress.noticeSeconds<=0 || string.IsNullOrEmpty(progress.noticeText))return;
            ui.NativeTexturePanel("notice",new Rect(40,130,561,65),Resources.Load<Texture2D>("UI/ScenarioNotice"));
            ui.NativeBitmapText("notice-text",new Rect(52,153,537,18),progress.noticeText.Replace("%s",session.PlayerName ?? ""),Color.white,
                alignment:TextAnchor.MiddleCenter);
        }
        private void LateUpdate()
        {
            if (aborting || player == null || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling == true || SceneManager.GetSceneByName("Battle").isLoaded) { ui?.Hide(); return; }
            if (!string.IsNullOrEmpty(error))
            {
                InGameUi.Ensure(ref ui, this, 120); ui.Begin(); ui.ModalBlocker("block");
                ui.Panel("error", new Rect(60, 120, 520, 200)); ui.Label("error-message", new Rect(80, 140, 480, 100), error);
                if(CanRetrySave)
                {
                    ui.Button("retry-save",new Rect(80,260,220,32),"저장 다시 시도",()=>RetrySave());
                    ui.Button("leave",new Rect(320,260,220,32),"필드로 돌아가기",AbortToTown);
                }
                else ui.Button("leave", new Rect(180, 260, 280, 32), "필드로 돌아가기", AbortToTown);
                ui.End(); return;
            }
            if (!initialized) { ui?.Hide(); return; }
            if(completionResultPending)
            {
                InGameUi.Ensure(ref ui,this,120);ui.Begin();
                ui.DungeonResult("scenario-result",default,session.PlayerName,()=>AcknowledgeCompletionResult(),
                    "처치한 몬스터: 총 "+progress.defeated.Count+"명",progress.hasBattleExperienceLedger?
                        progress.battleExperienceEarned.ToString(System.Globalization.CultureInfo.InvariantCulture):string.Empty,"승리");
                DrawNotice();ui.End();return;
            }
            var commands = Scenario01Rules.CurrentCommands(Content, progress);
            if (progress.cursor >= commands.Length || commands[progress.cursor].name != "MESSAGE" || commands[progress.cursor].args[0] == "0")
            {
                if (progress.noticeSeconds > 0 && !string.IsNullOrEmpty(progress.noticeText))
                {
                    InGameUi.Ensure(ref ui, this, 120); ui.Begin();DrawNotice();ui.End();
                }
                else ui?.Hide();
                return;
            }
            var command = commands[progress.cursor]; int.TryParse(command.args[0], out int mode);
            InGameUi.Ensure(ref ui, this, 120); ui.Begin(); ui.ModalBlocker("block");
            var rect = new Rect(40, ScenarioDialogueLayout.Top(mode), 561, ScenarioDialogueLayout.Height(mode)); ui.Panel("dialogue", rect);
            var text = ScenarioDialogueLayout.Page(mode, command.text, progress.page);
            // Only the confirmed lowercase name substitution occurs in 01. Raw
            // literal text and uppercase %S remain untouched rather than guessed.
            text = text.Replace("%s", session.PlayerName ?? "");
            var portraitId = Scenario01Rules.ScenarioPortraitId(command.args[1], session.PlayerJobId, session.PlayerName);
            Texture2D portrait = Resources.Load<Texture2D>((scenarioKey == "01" ? "Scenarios/01/Portraits/" : "Scenarios/Shared/Portraits/") + portraitId);
            if (portrait == null && playerPortraits != null) playerPortraits.TryGet(portraitId, out portrait);
            if (portrait == null) portrait = Resources.Load<Texture2D>("Scenarios/01/Portraits/NoFace");
            ui.PortraitTexture("face", new Rect(rect.x + 20, rect.y + 20, 64, 64), portrait);
            var lines = text.Split('\n');
            for (int row = 0; row < lines.Length && row < 3; row++)
                ui.NativeBitmapText("line-" + row, new Rect(150, rect.y + 23 + row * 20, 439, 14), lines[row], Color.white);
            ui.Button("advance", new Rect(rect.xMax - 80, rect.yMax + 4, 68, 24), "다음", () => AdvanceDialogue()); ui.End();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) AdvanceDialogue();
        }
        private void AbortToTown()
        {
            aborting = true; player.SetScenarioInputBlocked(false);
            if (playerBody != null) playerBody.enabled = previousPlayerVisible;
            var coordinator = FindFirstObjectByType<DarkSaver.World.WorldSceneCoordinator>();
            var entrance=DarkSaver.World.CampaignGateway.Find(scenarioKey);
            if (coordinator == null || !coordinator.TryTravelTo(entrance?.fieldKey ?? "Kaosia", entrance!=null ? "ScenarioReturn_"+scenarioKey : "FromField")) { aborting = false; player.SetScenarioInputBlocked(true); }
        }
        private void OnDisable()
        {
            scenarioBinding?.Dispose();scenarioBinding=null;
            if(protocol!=null)protocol.ResponseReceived-=ReceiveScenarioPacket;
            if (current == this) current = null;
            foreach(var pair in hiddenMapRenderers)if(pair.Renderer!=null)pair.Renderer.enabled=pair.Enabled;
            if(player!=null && savedBodySimulation.HasValue){var body=player.GetComponent<Rigidbody2D>();if(body!=null)body.simulated=savedBodySimulation.Value;}
            if (player != null) player.SetScenarioInputBlocked(false);
            if (playerBody != null) playerBody.enabled = previousPlayerVisible;
            if (cameraFollow != null && player != null) cameraFollow.Configure(player.transform, new Vector2(40, 40));
            foreach (var asset in visualAssets) if (asset != null) Destroy(asset);
            foreach (var enemy in enemyDefinitions) if (enemy != null) Destroy(enemy);
            if (createdListener != null) Destroy(createdListener);
            ui?.Hide();
        }
    }

}
