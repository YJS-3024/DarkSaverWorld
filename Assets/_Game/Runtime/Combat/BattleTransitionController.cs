using System.Collections;
using System.Collections.Generic;
using DarkSaver.Player;
using DarkSaver.Core;
using DarkSaver.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController : MonoBehaviour
    {
        private const string BattleSceneName = "Battle";
        private bool transitioning;
        private BattleSceneRoot activeBattle;
        private uint playerExperience;
        private bool hasPlayerVitals;
        private int playerHp, playerMaxHp, playerMp, playerMaxMp;
        private UnitLifeState playerLifeState;
        private NativeSaveStore saveStore;
        private string lastSaveMessage;
        private NativeCharacterProfile character;
        private NativeOwnedRoster ownedRoster;
        [SerializeField] private DarkSaver.Jobs.JobCatalog[] formationCatalogs = System.Array.Empty<DarkSaver.Jobs.JobCatalog>();
        private readonly Dictionary<int, DirectionalSpriteAnimator> formationArtwork = new();
        [System.Serializable] private struct FormationArtworkReference
        { public int jobId; public DirectionalSpriteAnimator template; }
        [SerializeField] private FormationArtworkReference[] persistedFormationArtwork = System.Array.Empty<FormationArtworkReference>();
        private BattleFormationSnapshot activeFormation;
        private Tilemap preparedGround;
        private int preparedRosterRevision = -1;
        public string LastTransitionMessage { get; private set; }

        public bool ConfigureFormationSupport(IReadOnlyList<DarkSaver.Jobs.JobCatalog> catalogs,
            IReadOnlyDictionary<int, DirectionalSpriteAnimator> artwork)
        {
            if (transitioning || activeBattle != null || catalogs == null || catalogs.Count == 0 || artwork == null
                || GetComponent<DarkSaver.UI.BattleFormationPanel>()?.IsOpen == true) return false;
            var unique = new HashSet<DarkSaver.Jobs.JobCatalog>(); var copy = new List<DarkSaver.Jobs.JobCatalog>();
            foreach (var catalog in catalogs) { if (catalog == null || !unique.Add(catalog)) return false; copy.Add(catalog); }
            foreach (var art in artwork) if (art.Key <= 0 || art.Value == null || !art.Value.HasCompleteDirections) return false;
            formationCatalogs = copy.ToArray(); formationArtwork.Clear();
            foreach (var art in artwork) formationArtwork.Add(art.Key, art.Value);
            preparedGround = null; preparedRosterRevision = -1;
            GetComponent<DarkSaver.UI.BattleFormationPanel>()?.UseSessionRoster(); return true;
        }

        public bool OpenFormationPreparation()
        {
            var panel = GetComponent<DarkSaver.UI.BattleFormationPanel>();
            if (panel == null || !panel.Open()) return false;
            if (!TryFindBattleMap(out var ground, out var terrain) || formationCatalogs == null || formationCatalogs.Length == 0)
            { LastTransitionMessage = "편성 맵 또는 직업 통행 데이터가 연결되지 않았습니다."; return true; }
            if (preparedGround != ground || preparedRosterRevision != OwnedRosterRevision || panel.ConfirmedSnapshot == null)
            {
                if (!panel.ConfigureMapPlacement(ground, terrain, formationCatalogs, new short[] { 100 }))
                { LastTransitionMessage = "현재 맵의 출전 배치 문맥을 확인할 수 없습니다."; return true; }
                preparedGround = ground; preparedRosterRevision = OwnedRosterRevision;
            }
            LastTransitionMessage = string.Empty; return true;
        }

        // Compare against current ownership, not merely an old UI snapshot.
        public bool MatchesOwnedFormation(BattleFormationSnapshot formation)
        {
            if (formation == null || ownedRoster == null || !ownedRoster.IsValid) return false;
            var byId = new Dictionary<string, NativeOwnedUnitData>(System.StringComparer.Ordinal);
            foreach (var unit in ownedRoster.units) byId.Add(unit.persistentId, unit);
            foreach (var member in formation.Members)
            {
                var observed = member.Unit;
                if (!byId.TryGetValue(member.PersistentId, out var unit) || !BattleFormation.MatchesOwnedIdentity(
                    unit.instanceId, unit.hireOwnerId, observed.Id, member.OwnedInstanceId, member.CommanderId, member.CommanderPersistentId) || unit.jobId != observed.JobId
                    || unit.name != observed.Name || unit.hp != observed.Hp || unit.maxHp != observed.MaxHp
                    || unit.mp != observed.Mp || unit.maxMp != observed.MaxMp || unit.attack != observed.Attack
                    || unit.defense != observed.Defense || unit.experience != observed.Experience || unit.lifeState != (int)observed.LifeState
                    || unit.teamId != member.TeamId || unit.recordEligible != member.RecordEligible
                    || unit.learnedMagicIds.Length != member.LearnedMagicIds.Count) return false;
                var learned = new HashSet<short>(unit.learnedMagicIds);
                foreach (var spell in member.LearnedMagicIds) if (!learned.Contains(spell)) return false;
            }
            return true;
        }

        public bool TryCaptureBattleReturn(BattleSceneRoot battle, BattleFormationSnapshot formation, out string reason)
        {
            reason = "전투 결과를 확인할 수 없습니다.";
            if (battle == null || !battle.TryGetUnit(100, out var primary) || primary.MaxHp <= 0
                || primary.Hp < 0 || primary.Hp > primary.MaxHp || primary.MaxMp < 0 || primary.Mp < 0 || primary.Mp > primary.MaxMp) return false;
            NativeOwnedRoster updated = null;
            if (formation != null && (ownedRoster == null || !battle.TryCaptureFormationReturn(formation, out var returned, out reason)
                || !ownedRoster.TryApplyBattleReturn(returned, out updated, out reason))) return false;
            var progressed = character; uint remainingExperience = primary.Experience;
            if (character != null && character.hasLevels && !character.TryAdvanceLevels(
                GetComponent<DarkSaver.UI.TownServicesPanel>()?.Progression, primary.Experience, out progressed, out remainingExperience))
            { reason = "확인된 레벨의 원본 성장 행을 검증할 수 없습니다. 복귀 상태를 보존합니다."; return false; }
            var scenarioReturn = scenario01?.Clone();
            if ((DarkSaver.Events.Scenario01Controller.ActiveKey == "01" || string.IsNullOrEmpty(DarkSaver.Events.Scenario01Controller.ActiveKey)) && scenarioReturn != null && scenarioReturn.pendingBattle != -1)
            {
                var content = DarkSaver.Events.Scenario01Controller.Definition;
                if (content == null || !battle.TryGetUnit((short)scenarioReturn.pendingBattle, out var opponent)
                    || !DarkSaver.Events.Scenario01Rules.RecordBattleReturn(content, scenarioReturn, scenarioReturn.pendingBattle,
                        opponent.LifeState != UnitLifeState.Active,
                        new DarkSaver.Events.Scenario01EnemyState { id = opponent.Id, hp = opponent.Hp, mp = opponent.Mp, x = opponent.X, y = opponent.Y }))
                { reason = "시나리오 조우 결과를 확인할 수 없습니다."; return false; }
            }
            var campaignKey = DarkSaver.Events.Scenario01Controller.ActiveKey;
            var campaignReturn = CaptureCampaign(campaignKey);
            if (campaignReturn != null && campaignReturn.pendingBattle != -1)
            {
                var definition = DarkSaver.Events.Scenario01Controller.LoadDefinition(campaignKey);
                if (!battle.TryGetUnit((short)campaignReturn.pendingBattle,out var target)
                    || !DarkSaver.Events.Scenario01Rules.RecordBattleReturn(definition,campaignReturn,campaignReturn.pendingBattle,target.LifeState != UnitLifeState.Active,
                        new DarkSaver.Events.Scenario01EnemyState {id=target.Id,hp=target.Hp,mp=target.Mp,x=target.X,y=target.Y}))
                {reason="시나리오 전투 복귀 상태가 올바르지 않습니다.";return false;}
            }
            // Publish only after both primary and additional results validate.
            playerHp = primary.Hp; playerMaxHp = primary.MaxHp; playerMp = primary.MaxMp; playerMaxMp = primary.MaxMp;
            playerExperience = remainingExperience; character = progressed;
            playerLifeState = primary.LifeState; hasPlayerVitals = true;
            if (scenarioReturn != null) { scenario01 = scenarioReturn; Scenario01Revision++; }
            if (campaignReturn != null) { campaign[campaignKey] = campaignReturn; CampaignRevision++; }
            if (updated != null)
            {
                ownedRoster = updated; OwnedRosterRevision++;
                preparedGround = null; preparedRosterRevision = -1;
                GetComponent<DarkSaver.UI.BattleFormationPanel>()?.UseSessionRoster();
            }
            CaptureScenarioCompletion(battle); reason = string.Empty; return true;
        }
        public int OwnedRosterRevision { get; private set; }
        public int OwnedUnitCount => ownedRoster?.units.Length ?? 0;
        public NativeOwnedRoster CaptureOwnedRoster() => ownedRoster?.Clone();
        public bool TrySetOwnedRoster(NativeOwnedRoster source, out string reason)
        {
            if(!processingSessionRequest)return SubmitSession(new DarkSaver.Network.LocalFormationRequest(source),out reason);
            reason = "보유 목록을 변경할 준비가 되지 않았습니다.";
            if (source == null || !source.IsValid || transitioning || SceneManager.GetSceneByName(BattleSceneName).isLoaded ||
                GetComponent<DarkSaver.UI.BattleFormationPanel>()?.IsOpen == true) return false;
            ownedRoster = source.Clone(); OwnedRosterRevision++; reason = string.Empty; return true;
        }
        private readonly HashSet<string> clearedScenarioKeys = new();

        // Live GameCore session; persisted only through the native save snapshot.
        public uint PlayerExperience => playerExperience;
        public bool HasCreatedCharacter => character != null;
        public string PlayerName => character?.name;
        public int PlayerJobId => character?.jobId ?? 0;
        public bool TryGetPlayerTroopType(out int type)
        {
            type = -1;
            if (character == null) return false;
            DarkSaver.Jobs.JobCatalogEntry found = null;
            foreach (var catalog in formationCatalogs) {
                if (catalog == null || !catalog.TryGet(character.jobId, out var entry)) continue;
                if (found != null) return false; // Do not guess across ambiguous catalogs.
                found = entry;
            }
            if (found == null) return false;
            long raw = found.GetNumericValue(22);
            if (raw < 0 || raw > 9) return false;
            type = (int)raw; return true;
        }
        public NativeCharacterProfile CaptureCharacterProfile() => character?.Clone();
        public bool InitializeCharacter(NativeCharacterProfile profile)
        {
            if (profile == null || !profile.IsValid || character != null || transitioning ||
                playerExperience != 0 || hasPlayerVitals || ownedRoster != null || inventory != null || clearedScenarioKeys.Count != 0 ||
                SceneManager.GetSceneByName(BattleSceneName).isLoaded) return false;
            character = profile.Clone(); inventory = new NativeInventory(); return true;
        }
        public int ClearedScenarioCount => clearedScenarioKeys.Count;
        public bool HasPlayerVitals => hasPlayerVitals;
        public int PlayerHp => playerHp;
        public int PlayerMaxHp => playerMaxHp;
        public int PlayerMp => playerMp;
        public int PlayerMaxMp => playerMaxMp;
        public bool CanEnterBattle => !hasPlayerVitals || (playerHp > 0 && playerLifeState == UnitLifeState.Active);
        public bool IsTransitioning => transitioning;
        public string LastSaveMessage => lastSaveMessage;
        public void ConfigureSaveFile(string path) => saveStore = new NativeSaveStore(path);

        public NativeSaveData CaptureSaveState(string map, string runtime, Vector2 position)
        {
            var keys = new List<string>(clearedScenarioKeys); keys.Sort(System.StringComparer.Ordinal);
            return new NativeSaveData
            {
                format = NativeSaveStore.Format, version = campaign.Count > 0 ? NativeSaveStore.CampaignVersion : scenario01 != null ? NativeSaveStore.ScenarioVersion
                    : ownedRoster == null ? NativeSaveStore.Version : NativeSaveStore.RosterVersion,
                mapScene = map, runtimeScene = runtime, worldX = position.x, worldY = position.y,
                experience = playerExperience, hasVitals = hasPlayerVitals,
                hp = playerHp, maxHp = playerMaxHp, mp = playerMp, maxMp = playerMaxMp,
                lifeState = (int)playerLifeState, clearedScenarios = keys.ToArray(), character = character?.Clone(), hasCharacter = character != null,
                ownedRoster = ownedRoster?.Clone(), hasOwnedRoster = ownedRoster != null,
                inventory = inventory?.Clone(), hasInventory = inventory != null,
                hasScenario01 = scenario01 != null, scenario01 = scenario01?.Clone(),
                campaign = CaptureCampaignStates()
            };
        }

        public bool RestoreSaveState(NativeSaveData data)
        {
            if (transitioning || SceneManager.GetSceneByName(BattleSceneName).isLoaded || !NativeSaveStore.IsValid(data)) return false;
            var keys = new HashSet<string>(data.clearedScenarios, System.StringComparer.Ordinal);
            var restoredRoster = data.hasOwnedRoster ? data.ownedRoster.Clone() : null;
            playerExperience = data.experience; hasPlayerVitals = data.hasVitals;
            playerHp = data.hp; playerMaxHp = data.maxHp; playerMp = data.mp; playerMaxMp = data.maxMp;
            playerLifeState = (UnitLifeState)data.lifeState;
            character = data.hasCharacter ? data.character.Clone() : null;
            ownedRoster = restoredRoster; OwnedRosterRevision++;
            inventory = data.hasInventory ? data.inventory.Clone() : null;
            scenario01 = data.hasScenario01 ? data.scenario01.Clone() : null; Scenario01Revision++;
            RestoreCampaignStates(data.campaign);
            GetComponent<DarkSaver.UI.BattleFormationPanel>()?.UseSessionRoster();
            clearedScenarioKeys.Clear(); clearedScenarioKeys.UnionWith(keys);
            return true;
        }

        public bool SaveWorld()
        {
            if(!processingSessionRequest)return SubmitSession(new DarkSaver.Network.SaveWorldRequest(),out lastSaveMessage);
            var player = FindAnyObjectByType<TopDownPlayerController>();
            var coordinator = FindAnyObjectByType<WorldSceneCoordinator>();
            if (transitioning || SceneManager.GetSceneByName(BattleSceneName).isLoaded || player == null ||
                !player.isActiveAndEnabled || (coordinator != null && coordinator.IsTravelling))
            { lastSaveMessage = "전투 또는 맵 이동 중에는 저장할 수 없습니다."; return false; }
            var townDefinition = DarkSaver.World.TownSceneContext.Current;
            var town = townDefinition != null && SceneManager.GetSceneByName(townDefinition.MapScene).isLoaded;
            var fieldDefinition = DarkSaver.World.FieldSceneContext.Current;
            var field = fieldDefinition != null && SceneManager.GetSceneByName(fieldDefinition.MapScene).isLoaded;
            var scenarioDefinition = DarkSaver.Events.Scenario01Controller.CurrentScenario;
            var scenario = scenarioDefinition != null && SceneManager.GetSceneByName(scenarioDefinition.MapScene).isLoaded;
            string trainingMap = null, trainingRuntime = null; int trainingCount = 0;
            for (int id = 101; id <= 107; id++)
                if (SceneManager.GetSceneByName("Map_" + id + "_;").isLoaded && SceneManager.GetSceneByName("Training" + id + "_Runtime").isLoaded)
                { trainingMap = "Map_" + id + "_;"; trainingRuntime = "Training" + id + "_Runtime"; trainingCount++; }
            if ((town ? 1 : 0) + (field ? 1 : 0) + (scenario ? 1 : 0) + trainingCount != 1)
            { lastSaveMessage = "현재 맵의 저장 준비가 되지 않았습니다."; return false; }
            if (saveStore == null) saveStore = new NativeSaveStore(NativeSaveStore.DefaultPath);
            return saveStore.TrySave(CaptureSaveState(town ? townDefinition.MapScene : field ? fieldDefinition.MapScene : scenario ? scenarioDefinition.MapScene : trainingMap,
                town ? townDefinition.RuntimeScene : field ? fieldDefinition.RuntimeScene : scenario ? scenarioDefinition.RuntimeScene : trainingRuntime, player.transform.position), out lastSaveMessage);
        }

        public bool IsScenarioCleared(string mapKey) =>
            !string.IsNullOrWhiteSpace(mapKey) && clearedScenarioKeys.Contains(mapKey);

        public bool CaptureScenarioCompletion(BattleSceneRoot battle)
        {
            if (battle == null) return false;
            var key = battle.ClearedScenarioKey;
            if (key != null && campaign.ContainsKey(key) || key == "01" && scenario01 != null) return false; // DEE and its reward must complete first.
            return !string.IsNullOrWhiteSpace(key) && clearedScenarioKeys.Add(key);
        }

        public bool CapturePlayerExperience(BattleSceneRoot battle)
        {
            if (battle == null) return false;
            playerExperience = battle.PlayerExperience;
            return true;
        }

        public bool RestorePlayerExperience(BattleSceneRoot battle) =>
            battle != null && battle.RestorePlayerExperience(playerExperience);

        public bool CapturePlayerVitals(BattleSceneRoot battle)
        {
            if (battle == null) return false;
            var hp = battle.PlayerHp; var maxHp = battle.PlayerMaxHp;
            var mp = battle.PlayerMp; var maxMp = battle.PlayerMaxMp;
            if (maxHp <= 0 || hp < 0 || hp > maxHp || maxMp < 0 || mp < 0 || mp > maxMp) return false;
            playerHp = hp; playerMaxHp = maxHp;
            // Confirmed user rule: retain remaining HP, recover MP to maximum on return.
            playerMp = maxMp; playerMaxMp = maxMp;
            playerLifeState = battle.PlayerLifeState;
            hasPlayerVitals = true;
            return true;
        }

        public bool RestorePlayerVitals(BattleSceneRoot battle) =>
            battle != null && (!hasPlayerVitals || (CanEnterBattle && battle.TryRestorePlayerVitals(playerHp, playerMp)));

        public void EnterBattle(BattleObjectiveDefinition objective = null, BattleEnemyDefinition enemy = null)
        {
            TryEnterBattle(objective, enemy);
        }

        // Acceptance of a transition request, not confirmation of victory or load completion.
        public bool TryEnterBattle(BattleObjectiveDefinition objective = null, BattleEnemyDefinition enemy = null,
            SpriteRenderer enemyVisual = null)
        {
            if(!processingSessionRequest)
            {
                serverEncounterObjective=objective;serverEncounterEnemy=enemy;serverEncounterVisual=enemyVisual;
                try{return SubmitSession(new DarkSaver.Network.LocalBattleTransitionRequest(true),out _);}
                finally{serverEncounterObjective=null;serverEncounterEnemy=null;serverEncounterVisual=null;}
            }
            if (HasPendingTutorial || DarkSaver.Events.Scenario01Controller.BlocksWorldActions || !isActiveAndEnabled || !CanEnterBattle || transitioning ||
                GetComponent<DarkSaver.UI.BattleFormationPanel>()?.IsOpen == true ||
                SceneManager.GetSceneByName(BattleSceneName).isLoaded ||
                !Application.CanStreamedLevelBeLoaded(BattleSceneName)) return false;
            var coordinator = FindAnyObjectByType<WorldSceneCoordinator>();
            var player = FindFirstObjectByType<TopDownPlayerController>();
            if ((coordinator != null && coordinator.IsTravelling) || player == null || !player.isActiveAndEnabled) return false;
            var formation = GetComponent<DarkSaver.UI.BattleFormationPanel>()?.ConfirmedSnapshot;
            var art = new Dictionary<int, DirectionalSpriteAnimator>(formationArtwork);
            foreach (var binding in persistedFormationArtwork)
                if (!art.ContainsKey(binding.jobId) && binding.template != null && binding.template.HasCompleteDirections)
                    art.Add(binding.jobId, binding.template);
            if (formation != null)
            {
                if (!MatchesOwnedFormation(formation) || !TryFindBattleMap(out var ground, out var terrain)
                    || ground != preparedGround || preparedRosterRevision != OwnedRosterRevision || enemyVisual == null || Camera.main == null)
                { LastTransitionMessage = "편성·맵·보유 상태가 바뀌었습니다. 출전 준비를 다시 확인하세요."; return false; }
                // Existing Fighter100 renderer is a known native job-100 template only.
                var animator = player.GetComponent<DirectionalSpriteAnimator>(); var renderer = player.GetComponent<SpriteRenderer>();
                if (!art.ContainsKey(100) && animator != null && animator.HasCompleteDirections && renderer != null
                    && renderer.sprite != null && (renderer.sprite.name.StartsWith("Fighter100_") || renderer.sprite.name.StartsWith("Class100_"))) art.Add(100, animator);
                if (character != null && classArtwork != null && classArtwork.TryGet(character.jobId, out _)
                    && !art.ContainsKey(character.jobId) && animator != null && animator.HasCompleteDirections
                    && renderer != null && renderer.sprite != null && renderer.sprite.name.StartsWith("Class" + character.jobId + "_"))
                    art.Add(character.jobId, animator);
                foreach (var member in formation.Members)
                    if (!art.TryGetValue(member.Unit.JobId, out var template) || template == null || !template.HasCompleteDirections
                        || !BattleMapPresentation.TryGetPlacementMovement(ground, terrain, ground.cellBounds, formationCatalogs,
                            member.Unit.JobId, member.Position, out var cost) || cost == CombatTerrainTraversal.ImpassableValue)
                    { LastTransitionMessage = "출전 칩 또는 직업별 통행을 확인할 수 없습니다."; return false; }
            }
            LastTransitionMessage = string.Empty;
            // Keep the world encounter location before disabling the player/loading battle.
            if(HasCreatedCharacter && !SaveWorld()){LastTransitionMessage=lastSaveMessage;return false;}
            StartCoroutine(EnterRoutine(objective, enemy, player.GetComponent<SpriteRenderer>(), enemyVisual, formation, art));
            return true;
        }

        public void ExitBattle()
        {
            if(!processingSessionRequest){SubmitSession(new DarkSaver.Network.LocalBattleTransitionRequest(false),out _);return;}
            if (!transitioning && SceneManager.GetSceneByName(BattleSceneName).isLoaded)
                StartCoroutine(ExitRoutine());
        }

        private IEnumerator EnterRoutine(BattleObjectiveDefinition objective, BattleEnemyDefinition enemy,
            SpriteRenderer playerVisual, SpriteRenderer enemyVisual, BattleFormationSnapshot formation,
            IReadOnlyDictionary<int, DirectionalSpriteAnimator> artwork)
        {
            transitioning = true;
            SetPlayerEnabled(false);
            yield return SceneManager.LoadSceneAsync(BattleSceneName, LoadSceneMode.Additive);
            BattleSceneRoot battle = null;
            var scene = SceneManager.GetSceneByName(BattleSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    battle = root.GetComponentInChildren<BattleSceneRoot>(true);
                    if (battle != null) break;
                }
            }
            var resolvedObjective = objective != null ? objective : enemy != null ? enemy.VictoryObjective : null;
            if (battle == null || !battle.ConfigureEnemy(enemy) || !battle.ConfigureObjective(resolvedObjective) ||
                (character != null && (!battle.TrySetPlayerName(character.name) || !battle.ConfigurePlayerJob(character.jobId)
                    || !battle.ConfigurePlayerJobBaseStats(character,GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog))) ||
                !RestorePlayerVitals(battle) || !RestorePlayerExperience(battle))
            {
                Debug.LogError("Battle scene has no valid encounter/objective configuration. Returning to world.");
                if (scene.IsValid() && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
                SetPlayerEnabled(true);
            }
            else if (formation != null)
            {
                var bound = false;
                if (MatchesOwnedFormation(formation) && battle.ConfigureTraversalCatalogs(formationCatalogs)
                    && battle.TryRegisterAlliedFormation(formation, out _) && TryFindBattleMap(out var ground, out var terrain)
                    && ground == preparedGround && playerVisual != null && enemyVisual != null)
                    bound = battle.gameObject.AddComponent<BattleMapPresentation>().TryBindPreparedFormation(formation, ground, terrain,
                        Camera.main, new[] { new BattleMapVisual(100, playerVisual), new BattleMapVisual(battle.PrimaryEnemyId, enemyVisual) }, artwork);
                if (!bound)
                {
                    LastTransitionMessage = "편성 출전에 실패했습니다. 보유 상태와 저장은 변경하지 않았습니다.";
                    if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
                    SetPlayerEnabled(true);
                }
                else
                {
                    var catalog = GetComponent<DarkSaver.UI.MagicCatalogPanel>()?.Catalog;
                    var view = battle.GetComponent<BattleMapPresentation>();
                    if (!view.ConfigureFormationLearnedMagic(catalog, formation))
                    {
                        LastTransitionMessage = "습득 마법의 네이티브 카탈로그 연결에 실패했습니다. 출전과 저장을 보류합니다.";
                        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
                        SetPlayerEnabled(true);
                    }
                    else { activeBattle = battle; activeFormation = formation; }
                }
            }
            else
            {
                activeBattle = battle;
                if (playerVisual != null && enemyVisual != null && TryFindBattleMap(out var ground, out var terrain))
                {
                    var display = battle.gameObject.AddComponent<BattleMapPresentation>();
                    if (!display.TryBind(ground, terrain, Camera.main, new[] {
                        new BattleMapVisual(100, playerVisual), new BattleMapVisual(battle.PrimaryEnemyId, enemyVisual) }))
                        Debug.LogWarning("Native battle map binding rejected; retaining legacy presentation without guessed placement.");
                }
            }
            if (activeBattle != null && character != null && character.hasClassAbilities)
            {
                var display = activeBattle.GetComponent<BattleMapPresentation>();
                var catalog = GetComponent<DarkSaver.UI.MagicCatalogPanel>()?.Catalog;
                if (display == null || !display.ConfigureCharacterLearnedMagic(catalog, character))
                {
                    LastTransitionMessage = "캐릭터의 습득 기술 연결에 실패했습니다. 출전을 보류합니다.";
                    activeBattle = null; activeFormation = null;
                    if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
                    SetPlayerEnabled(true);
                }
            }
            if(activeBattle!=null) activeBattle.GetComponent<BattleMapPresentation>()?.TryBindSessionToolDisplay(this);
            transitioning = false;
        }

        private IEnumerator ExitRoutine()
        {
            transitioning = true;
            if (!TryCaptureBattleReturn(activeBattle, activeFormation, out var reason))
            {
                LastTransitionMessage = "복귀 결과 반영 실패: " + reason;
                transitioning = false; yield break; // Retain live battle for a safe retry; never save partial state.
            }
            LastTransitionMessage = string.Empty;
            yield return SceneManager.UnloadSceneAsync(BattleSceneName);
            activeBattle = null;
            activeFormation = null;
            SetPlayerEnabled(true);
            transitioning = false;
            SaveWorld();
        }

        private DarkSaver.UI.InGameUi noticeUi;
        private void LateUpdate()
        {
            if (!string.IsNullOrEmpty(LastTransitionMessage) && SceneManager.GetSceneByName(BattleSceneName).isLoaded)
            {
                DarkSaver.UI.InGameUi.Ensure(ref noticeUi, this, 70); noticeUi.Begin();
                var area = new Rect(Mathf.Max(12f, (Rendering.GameScreen.Width - 440f) / 2f), 12f, Mathf.Min(440f, Rendering.GameScreen.Width - 24f), 56f);
                noticeUi.Panel("notice", area);
                noticeUi.Label("message", new Rect(area.x + 12, area.y + 4, area.width - 24, 48), LastTransitionMessage);
                noticeUi.End();
            }
            else noticeUi?.Hide();
        }

        private static void SetPlayerEnabled(bool value)
        {
            var player = FindFirstObjectByType<TopDownPlayerController>();
            if (player != null)
            {
                player.enabled = value;
                var body = player.GetComponent<Rigidbody2D>();
                if (!value) body.linearVelocity = Vector2.zero;
            }
        }

        private static bool TryFindBattleMap(out Tilemap ground, out Tilemap terrain)
        {
            ground = null; terrain = null;
            var count = 0; var mapScene = default(Scene);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var candidate = SceneManager.GetSceneAt(i);
                if (candidate.isLoaded && IsSupportedBattleMapScene(candidate.name)) { count++; mapScene = candidate; }
            }
            if (count != 1) return false;
            foreach (var root in mapScene.GetRootGameObjects())
            foreach (var map in root.GetComponentsInChildren<Tilemap>(false))
            {
                if (map.name == "Ground") { if (ground != null) return false; ground = map; }
                if (map.name == "TerrainAttributes") { if (terrain != null) return false; terrain = map; }
            }
            return ground != null && terrain != null;
        }

        public static bool IsSupportedBattleMapScene(string name)
        {
            if (DarkSaver.Core.ScenarioCatalog.IsMap(name) || DarkSaver.Core.TownCatalog.IsMap(name) || DarkSaver.Core.FieldCatalog.IsMap(name) || name == "Brugos01_Map") return true;
            for (int id = 101; id <= 107; id++) if (name == "Map_" + id + "_;") return true;
            return false;
        }
    }
}
