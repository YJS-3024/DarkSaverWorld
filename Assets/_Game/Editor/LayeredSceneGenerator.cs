using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Player;
using DarkSaver.World;
using DarkSaver.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Editor
{
    public static class LayeredSceneGenerator
    {
        private const string KaosiaSource = "Assets/_Game/Editor/ConversionSources/Kaosia_Combined.unity";
        private const string FieldSource = "Assets/_Game/Editor/ConversionSources/World1010_Combined.unity";
        private const string KaosiaMap = "Assets/_Game/Scenes/Maps/Kaosia_Map.unity";
        private const string FieldMap = "Assets/_Game/Scenes/Maps/World1010_Map.unity";
        private const string Core = "Assets/_Game/Scenes/Runtime/GameCore.unity";
        private const string TownRuntime = "Assets/_Game/Scenes/Runtime/Town_Runtime.unity";
        private const string FieldRuntime = "Assets/_Game/Scenes/Runtime/Field_Runtime.unity";
        private const string Start = "Assets/_Game/Scenes/Entry/Start.unity";
        private const string Entry = "Assets/_Game/Scenes/Entry/Kaosia_Play.unity";
        private const string Battle = "Assets/_Game/Scenes/Battle/Battle.unity";
        private const string MonsterSymbol = "Assets/_Game/Art/Converted/Monsters/MonsterSymbol.asset";
        private const string WaterFrame0 = "Assets/_Game/Art/Converted/Effects/WaterSubmerge/frame_00.png";
        private const string WaterFrame1 = "Assets/_Game/Art/Converted/Effects/WaterSubmerge/frame_01.png";
        private const string WaterTile = "Assets/_Game/Art/Converted/World1010/Tiles/Terrain_06.asset";

        [MenuItem("DarkSaver/Scenes/Generate Layered World")]
        public static void Generate()
        {
            EnsureFolders();
            SplitKaosia();
            SplitField();
            CreateEntry();
            CreateBattle();
            EditorBuildSettings.scenes = new[] { Start, Entry, Core, KaosiaMap, TownRuntime, FieldMap, FieldRuntime, Battle }
                .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Generated layered scenes: Entry + GameCore + Map + Runtime + Battle");
        }

        public static void GenerateBatch() => Generate();

        public static void ValidateBatch()
        {
            var map = EditorSceneManager.OpenScene(KaosiaMap, OpenSceneMode.Single);
            var fieldMap = EditorSceneManager.OpenScene(FieldMap, OpenSceneMode.Additive);
            var core = EditorSceneManager.OpenScene(Core, OpenSceneMode.Additive);
            var runtime = EditorSceneManager.OpenScene(TownRuntime, OpenSceneMode.Additive);
            var fieldRuntime = EditorSceneManager.OpenScene(FieldRuntime, OpenSceneMode.Additive);
            var battle = EditorSceneManager.OpenScene(Battle, OpenSceneMode.Additive);
            if (map.GetRootGameObjects().Any(value => value.name is "Player" or "Main Camera") ||
                core.GetRootGameObjects().All(value => value.name != "Player") ||
                core.GetRootGameObjects().All(value => value.GetComponent<WaterSubmergeOverlay>() == null) ||
                core.GetRootGameObjects().All(value => value.name != "Main Camera") ||
                runtime.GetRootGameObjects().All(value => value.GetComponent<ScenePortal2D>() == null) ||
                fieldRuntime.GetRootGameObjects().All(value => value.GetComponent<BattleEncounter2D>() == null) ||
                battle.GetRootGameObjects().All(value => value.GetComponent<BattleSceneRoot>() == null) ||
                UnityEngine.Object.FindAnyObjectByType<WorldSceneCoordinator>() == null ||
                UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>() == null ||
                UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>() == null)
                throw new InvalidDataException("Additive 씬 책임 분리가 올바르지 않습니다.");
            ValidateWalkableTrigger(map, runtime, "To World Field");
            ValidateWalkableTrigger(fieldMap, fieldRuntime, "To Kaosia");
            ValidateWalkableTrigger(fieldMap, fieldRuntime, "Battle Encounter");
            var portal = fieldRuntime.GetRootGameObjects().First(value => value.name == "To Kaosia");
            var encounter = fieldRuntime.GetRootGameObjects().First(value => value.name == "Battle Encounter");
            if (portal.GetComponent<BoxCollider2D>().bounds.Intersects(encounter.GetComponent<BoxCollider2D>().bounds) ||
                encounter.GetComponent<SpriteRenderer>()?.sprite == null)
                throw new InvalidDataException("필드 포털과 몬스터 심벌 배치가 올바르지 않습니다.");
            Debug.Log("Validated layered scenes: loaded=6, walkableTriggers=3, encounterSymbol=1, corePlayer=1, battle=1");
        }

        private static void ValidateWalkableTrigger(Scene mapScene, Scene runtimeScene, string objectName)
        {
            var target = runtimeScene.GetRootGameObjects().FirstOrDefault(value => value.name == objectName);
            var tilemaps = mapScene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<Tilemap>(true)).ToArray();
            var ground = tilemaps.FirstOrDefault(value => value.name == "Ground");
            var terrain = tilemaps.FirstOrDefault(value => value.name == "TerrainAttributes");
            if (target == null || target.GetComponent<BoxCollider2D>()?.isTrigger != true ||
                ground == null || terrain == null)
                throw new InvalidDataException($"{objectName} 트리거 또는 맵 타일맵이 없습니다.");
            var cell = ground.WorldToCell(target.transform.position);
            if (!ground.HasTile(cell) || terrain.GetColliderType(cell) != Tile.ColliderType.None)
                throw new InvalidDataException($"{objectName}이 이동 불가능한 타일 {cell}에 배치됐습니다.");
        }

        private static void SplitKaosia()
        {
            var source = EditorSceneManager.OpenScene(KaosiaSource, OpenSceneMode.Single);
            var map = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            MoveRoot("카오시아 마을", source, map);
            MoveRoot("Objects", source, map);
            SetForegroundOrder(map);
            EditorSceneManager.SaveScene(map, KaosiaMap);

            var core = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            MoveRoot("Player", source, core);
            MoveRoot("Main Camera", source, core);
            ConfigureWaterOverlay(core.GetRootGameObjects().First(value => value.name == "Player"));
            var coordinator = new GameObject("World Scene Coordinator", typeof(WorldSceneCoordinator));
            SceneManager.MoveGameObjectToScene(coordinator, core);
            var battleTransition = new GameObject("Battle Transition", typeof(BattleTransitionController), typeof(WorldSessionHud), typeof(ItemCatalogPanel), typeof(MagicCatalogPanel));
            SceneManager.MoveGameObjectToScene(battleTransition, core);
            if (!battleTransition.GetComponent<BattleTransitionController>().ConfigureFormationSupport(new[] {
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/MonsterJobCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/NpcSpecialJobCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>(JobCatalogConverter.MercenaryOutput) },
                new System.Collections.Generic.Dictionary<int, DarkSaver.Player.DirectionalSpriteAnimator>()))
                throw new System.InvalidOperationException("Native formation catalogs unavailable.");
            battleTransition.GetComponent<ItemCatalogPanel>().Configure(
                AssetDatabase.LoadAssetAtPath<DarkSaver.Items.ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset"),
                core.GetRootGameObjects().First(value => value.name == "Player").GetComponent<DarkSaver.Player.TopDownPlayerController>());
            battleTransition.GetComponent<MagicCatalogPanel>().Configure(
                AssetDatabase.LoadAssetAtPath<DarkSaver.Magic.MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset"),
                core.GetRootGameObjects().First(value => value.name == "Player").GetComponent<DarkSaver.Player.TopDownPlayerController>());
            (battleTransition.GetComponent<TownServicesPanel>() ?? battleTransition.AddComponent<TownServicesPanel>()).ConfigureProgression(
                AssetDatabase.LoadAssetAtPath<DarkSaver.Combat.ExperienceProgressionTable>(ExperienceProgressionConverter.Output));
            battleTransition.GetComponent<TownServicesPanel>().ConfigureHiring(
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.MercenaryHireCatalog>(MercenaryHireConverter.Output),
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>(JobCatalogConverter.MercenaryOutput));
            MercenarySpriteConverter.ConvertInitialSheets(); MercenarySpriteConverter.InstallTemplates(battleTransition);
            battleTransition.GetComponent<BattleTransitionController>().ConfigureClassArtwork(ClassArtworkConverter.Convert());
            EditorSceneManager.SaveScene(core, Core);

            var runtime = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            MoveRoot("To World Field", source, runtime);
            MoveRoot("Spawn FromField", source, runtime);
            EditorSceneManager.SaveScene(runtime, TownRuntime);
        }

        private static void ConfigureWaterOverlay(GameObject player)
        {
            var frames = new[] { AssetDatabase.LoadAssetAtPath<Sprite>(WaterFrame0), AssetDatabase.LoadAssetAtPath<Sprite>(WaterFrame1) };
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(WaterTile);
            if (frames.Any(frame => frame == null) || tile == null)
                throw new InvalidDataException("Water overlay sprites or terrain tile are missing.");
            var overlay = player.GetComponent<WaterSubmergeOverlay>() ?? player.AddComponent<WaterSubmergeOverlay>();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Runtime/Rendering/WaterSurface.mat");
            if (material == null) throw new InvalidDataException("Native water color-key material is missing.");
            var bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Runtime/Rendering/WaterBody.mat");
            if (bodyMaterial == null) throw new InvalidDataException("Water body material is missing.");
            overlay.Configure(frames, new[] { tile }, material, bodyMaterial);
        }

        private static void SplitField()
        {
            var source = EditorSceneManager.OpenScene(FieldSource, OpenSceneMode.Single);
            var map = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            MoveRoot("월드 필드 1010", source, map);
            MoveRoot("Objects", source, map);
            SetForegroundOrder(map);
            EditorSceneManager.SaveScene(map, FieldMap);

            var runtime = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            MoveRoot("To Kaosia", source, runtime);
            ConfigureFieldTownEntrances(runtime);
            MoveRoot("Spawn FromKaosia", source, runtime);
            var encounter = new GameObject("Battle Encounter", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(BattleEncounter2D));
            encounter.transform.position = new Vector3(25.5f, 34.5f, 0f);
            var symbol = AssetDatabase.LoadAssetAtPath<Sprite>(MonsterSymbol);
            if (symbol == null) throw new InvalidDataException($"Monster symbol not found: {MonsterSymbol}");
            var renderer = encounter.GetComponent<SpriteRenderer>();
            renderer.sprite = symbol;
            renderer.sortingOrder = 20000 - Mathf.RoundToInt(encounter.transform.position.y * 10f);
            var encounterTrigger = encounter.GetComponent<BoxCollider2D>();
            encounterTrigger.size = Vector2.one;
            encounterTrigger.isTrigger = true;
            SceneManager.MoveGameObjectToScene(encounter, runtime);
            EditorSceneManager.SaveScene(runtime, FieldRuntime);
        }

        public static void ConfigureFieldTownEntrances(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Field runtime scene must be loaded.");
            var names = new[] { "To Kaosia", "To Kaosia North", "To Kaosia West", "To Kaosia East" };
            var positions = new[] { new Vector2(25f, 34.5f), new Vector2(25f, 41.5f),
                new Vector2(20.5f, 38.5f), new Vector2(29.5f, 38.5f) };
            for (int index = 0; index < names.Length; index++)
            {
                var portal = scene.GetRootGameObjects().FirstOrDefault(value => value.name == names[index]);
                if (portal == null)
                {
                    portal = new GameObject(names[index], typeof(BoxCollider2D), typeof(ScenePortal2D));
                    SceneManager.MoveGameObjectToScene(portal, scene);
                }
                portal.transform.position = positions[index];
                portal.GetComponent<BoxCollider2D>().size = index < 2 ? new Vector2(2f, 1f) : Vector2.one;
                portal.GetComponent<ScenePortal2D>().Configure("Kaosia", "FromField");
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void CreateEntry()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Kaosia Additive Bootstrap", typeof(AdditiveSceneBootstrap));
            root.GetComponent<AdditiveSceneBootstrap>().Configure("GameCore", "Kaosia_Map", "Town_Runtime");
            EditorSceneManager.SaveScene(scene, Entry);
        }

        private static void CreateBattle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Battle Runtime", typeof(BattleSceneRoot), typeof(DarkSaver.Network.BattlePacketReceiver),
                typeof(DarkSaver.Network.BattleRequestSender));
            if (!root.GetComponent<BattleSceneRoot>().ConfigureTraversalCatalogs(new[] {
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/MonsterJobCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/NpcSpecialJobCatalog.asset") }))
                throw new InvalidOperationException("Native traversal catalogs are missing.");
            EditorSceneManager.SaveScene(scene, Battle);
        }

        private static void SetForegroundOrder(Scene map)
        {
            foreach (var root in map.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.name.StartsWith("Object_", StringComparison.Ordinal) ||
                    !int.TryParse(renderer.name.Substring(7), out var sourceIndex)) continue;
                renderer.sortingOrder = 30000 + sourceIndex;
            }
        }

        private static void MoveRoot(string name, Scene source, Scene destination)
        {
            var gameObject = source.GetRootGameObjects().FirstOrDefault(value => value.name == name);
            if (gameObject == null) throw new InvalidDataException($"분리할 루트 오브젝트가 없습니다: {name}");
            SceneManager.MoveGameObjectToScene(gameObject, destination);
        }

        private static void EnsureFolders()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException();
            foreach (var folder in new[] { "Assets/_Game/Scenes/Maps", "Assets/_Game/Scenes/Runtime", "Assets/_Game/Scenes/Entry", "Assets/_Game/Scenes/Battle" })
                Directory.CreateDirectory(Path.Combine(root, folder));
        }
    }
}
