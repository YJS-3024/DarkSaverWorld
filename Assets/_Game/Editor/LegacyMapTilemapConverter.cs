using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DarkSaver.CameraSystem;
using DarkSaver.Maps;
using DarkSaver.Player;
using DarkSaver.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Editor
{
    public static class LegacyMapTilemapConverter
    {
        private const string KaosiaSpriteFolder = "Assets/_Game/Art/Converted/Kaosia/Sprites";
        private const string CharacterSpriteFolder = "Assets/_Game/Art/Converted/Characters/Fighter100";
        private const string KaosiaTileFolder = "Assets/_Game/Art/Converted/Kaosia/Tiles";
        private const string KaosiaScenePath = "Assets/_Game/Scenes/Towns/Kaosia.unity";
        private const string WorldSpriteFolder = "Assets/_Game/Art/Converted/World1010/Sprites";
        private const string WorldTileFolder = "Assets/_Game/Art/Converted/World1010/Tiles";
        private const string WorldScenePath = "Assets/_Game/Scenes/Fields/WorldField1010.unity";
        private const string BrugosSpriteFolder = "Assets/_Game/Art/Converted/Map01/Sprites";
        private const string BrugosTileFolder = "Assets/_Game/Art/Converted/Map01/Tiles";
        private const string BrugosScenePath = "Assets/_Game/Scenes/Maps/Brugos01_Map.unity";
        private static string activeSpriteFolder;
        private static string activeTileFolder;
        private static HashSet<byte> activeBlockedAttributes;

        [MenuItem("DarkSaver/Convert/Kaosia To Unity Tilemap")]
        public static void ConvertKaosia()
        {
            Convert("1000", "카오시아 마을", KaosiaScenePath, KaosiaSpriteFolder, KaosiaTileFolder,
                new Vector2(21.5f, 2.5f), new HashSet<byte> { 9 });
        }

        public static void ConvertKaosiaBatch() => ConvertKaosia();

        [MenuItem("DarkSaver/Convert/World Field 1010 To Unity Tilemap")]
        public static void ConvertWorldField1010()
        {
            Convert("1010", "월드 필드 1010", WorldScenePath, WorldSpriteFolder, WorldTileFolder,
                new Vector2(23f, 34f), new HashSet<byte> { 3, 6, 9 });
        }

        public static void ConvertWorldField1010Batch() => ConvertWorldField1010();

        [MenuItem("DarkSaver/Convert/Brugos 01 To Unity Tilemap")]
        public static void ConvertBrugos01()
        {
            AssetDatabase.Refresh();
            Convert("01", "브르고스궁", BrugosScenePath, BrugosSpriteFolder, BrugosTileFolder,
                Vector2.zero, new HashSet<byte> { 3, 6, 9, 24 }, mapOnly: true);
        }

        public static void ConvertBrugos01Batch() => ConvertBrugos01();

        private const string RestoredSpriteFolder = "Assets/_Game/Art/Converted/RestoredMaps/Sprites";
        private const string RestoredTileFolder = "Assets/_Game/Art/Converted/RestoredMaps/Tiles";

        // Editor-only conversion. Saved scenes reference native sprites/tiles, never MRC/GRS files.
        public static int PopulateRestoredMapsBatch(int start, int count)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("편집 상태에서만 맵 타일을 변환할 수 있습니다.");
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes/Maps" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith("Map_", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (start < 0 || count < 0 || start + count > paths.Length) throw new ArgumentOutOfRangeException();
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var analysisRoot = Path.Combine(projectRoot, "분석", "Saver_1.2");
            var catalog = GrsCatalog.Load(analysisRoot);
            activeSpriteFolder = RestoredSpriteFolder;
            activeTileFolder = RestoredTileFolder;
            activeBlockedAttributes = new HashSet<byte> { 3, 6, 9, 24 };
            if (!AssetDatabase.IsValidFolder(activeTileFolder))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Converted/RestoredMaps", "Tiles");
            var previousActive = SceneManager.GetActiveScene();
            var converted = 0;
            var imageTiles = new Dictionary<string, Tile>();
            var attributeTiles = new Dictionary<byte, Tile>();
            Tile ImageTile(string key)
            {
                if (!imageTiles.TryGetValue(key, out var tile)) imageTiles[key] = tile = GetOrCreateTile(key);
                return tile;
            }
            Tile AttributeTile(byte key)
            {
                if (!attributeTiles.TryGetValue(key, out var tile)) attributeTiles[key] = tile = GetOrCreateAttributeTile(key);
                return tile;
            }
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths.Skip(start).Take(count))
                {
                    if (SceneManager.GetSceneByPath(path).isLoaded)
                        throw new InvalidOperationException("변환 대상 씬을 먼저 닫으세요: " + path);
                    var parts = Path.GetFileNameWithoutExtension(path).Split(new[] { '_' }, 3);
                    var mapKey = parts[1]; var title = parts[2];
                    var map = MrcMapReader.Read(File.ReadAllBytes(Path.Combine(analysisRoot,
                        "extracted", "Saver_Files", "Saver", "MAP", mapKey + ".mrc")));
                    // Resolve all asset references before replacing the scene file.
                    var groundTiles = new TileBase[map.Width * map.Height];
                    var detailTiles = new TileBase[groundTiles.Length];
                    var terrainTiles = new TileBase[groundTiles.Length];
                    for (var index = 0; index < groundTiles.Length; index++)
                    {
                        var destination = (map.Height - 1 - index / map.Width) * map.Width + index % map.Width;
                        groundTiles[destination] = ImageTile(catalog.Primary(map.BaseResourceLayer[index], map.BaseTileLayer[index], 0));
                        var cell = map.Tiles[index];
                        if (cell.Group != byte.MaxValue)
                            detailTiles[destination] = ImageTile(catalog.CellPrimary(cell.Resource, cell.Group, cell.Frame));
                        if (cell.TerrainAttribute != 0) terrainTiles[destination] = AttributeTile(cell.TerrainAttribute);
                    }
                    var objectSprites = map.Sprites.Select(value => AssetDatabase.LoadAssetAtPath<Sprite>(
                        SpriteAssetPath(catalog.Secondary(value.Raw[0], value.Raw[12], value.Raw[13])))).ToArray();
                    if (objectSprites.Any(value => value == null)) throw new InvalidDataException("오브젝트 이미지 누락: " + mapKey);
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    try
                    {
                        SceneManager.SetActiveScene(scene);
                        var grid = new GameObject(title, typeof(Grid));
                        grid.AddComponent<MapSceneDefinition>().Configure(mapKey, title, "분석 기반 네이티브 타일맵");
                        var bounds = new BoundsInt(0, 0, 0, map.Width, map.Height, 1);
                        CreateTilemap("Ground", grid.transform, 0).SetTilesBlock(bounds, groundTiles);
                        CreateTilemap("Detail", grid.transform, 10).SetTilesBlock(bounds, detailTiles);
                        var terrain = CreateTilemap("TerrainAttributes", grid.transform, 0);
                        terrain.SetTilesBlock(bounds, terrainTiles);
                        terrain.GetComponent<TilemapRenderer>().enabled = false;
                        terrain.gameObject.AddComponent<TilemapCollider2D>();
                        var objects = new GameObject("Objects"); objects.transform.SetParent(grid.transform, false);
                        for (var i = 0; i < map.Sprites.Count; i++)
                        {
                            var source = map.Sprites[i];
                            var instance = new GameObject($"Object_{source.SourceIndex:000}", typeof(SpriteRenderer));
                            instance.transform.SetParent(objects.transform, false);
                            instance.transform.position = new Vector3(source.X, map.Height - source.Y, 0);
                            var renderer = instance.GetComponent<SpriteRenderer>();
                            renderer.sprite = objectSprites[i]; renderer.sortingOrder = 30000 + source.SourceIndex;
                        }
                        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("씬 저장 실패: " + path);
                        converted++;
                    }
                    finally
                    {
                        if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
                AssetDatabase.SaveAssets();
                return converted;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            }
        }

        public static void PrepareRestoredSpriteImports()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { RestoredSpriteFolder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    var secondary = path.Contains("_secondary_");
                    var pivot = secondary ? new Vector2(0, 1) : new Vector2(.5f, .5f);
                    var alignment = (int)(secondary ? SpriteAlignment.Custom : SpriteAlignment.Center);
                    if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                        importer.spritePixelsPerUnit == 32 && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                        importer.textureCompression == TextureImporterCompression.Uncompressed && importer.alphaIsTransparency &&
                        settings.spriteAlignment == alignment && settings.spritePivot == pivot && settings.spriteMeshType == SpriteMeshType.FullRect) continue;
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    settings.spriteAlignment = alignment; settings.spritePivot = pivot; settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings); importer.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }

        public static int ValidateRestoredMapsBatch(int start, int count)
        {
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes/Maps" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(path => Path.GetFileName(path).StartsWith("Map_", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal).Skip(start).Take(count).ToArray();
            var analysisRoot = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "분석", "Saver_1.2");
            var catalog = GrsCatalog.Load(analysisRoot);
            var validated = 0;
            foreach (var path in paths)
            {
                if (SceneManager.GetSceneByPath(path).isLoaded) throw new InvalidOperationException("검증 대상 씬이 이미 열려 있습니다: " + path);
                var key = Path.GetFileNameWithoutExtension(path).Split(new[] { '_' }, 3)[1];
                var map = MrcMapReader.Read(File.ReadAllBytes(Path.Combine(analysisRoot, "extracted", "Saver_Files", "Saver", "MAP", key + ".mrc")));
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var tilemaps = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true)).ToArray();
                    var ground = tilemaps.Single(value => value.name == "Ground");
                    var detail = tilemaps.Single(value => value.name == "Detail");
                    var terrain = tilemaps.Single(value => value.name == "TerrainAttributes");
                    for (var index = 0; index < map.Tiles.Length; index++)
                    {
                        var position = new Vector3Int(index % map.Width, map.Height - 1 - index / map.Width, 0);
                        var cell = map.Tiles[index];
                        var back = ground.GetTile<Tile>(position);
                        if (back?.sprite == null || AssetDatabase.GetAssetPath(back.sprite) != RestoredSpriteFolder + "/" + SafeKey(catalog.Primary(map.BaseResourceLayer[index], map.BaseTileLayer[index], 0)) + ".png")
                            throw new InvalidDataException("바닥 불일치: " + key + "/" + index);
                        var shown = detail.GetTile<Tile>(position);
                        if (cell.Group == byte.MaxValue ? shown != null : shown?.sprite == null || AssetDatabase.GetAssetPath(shown.sprite) != RestoredSpriteFolder + "/" + SafeKey(catalog.CellPrimary(cell.Resource, cell.Group, cell.Frame)) + ".png")
                            throw new InvalidDataException("세부 타일 불일치: " + key + "/" + index);
                        var attribute = terrain.GetTile<Tile>(position);
                        if (cell.TerrainAttribute == 0 ? attribute != null : attribute == null ||
                            AssetDatabase.GetAssetPath(attribute) != $"{RestoredTileFolder}/Terrain_{cell.TerrainAttribute:00}.asset" ||
                            attribute.colliderType != (new byte[] { 3, 6, 9, 24 }.Contains(cell.TerrainAttribute) ? Tile.ColliderType.Grid : Tile.ColliderType.None))
                            throw new InvalidDataException("통행 속성 불일치: " + key + "/" + index);
                    }
                    var sprites = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                        .OrderBy(value => int.Parse(value.name.Substring(7), CultureInfo.InvariantCulture)).ToArray();
                    if (sprites.Length != map.Sprites.Count) throw new InvalidDataException("오브젝트 수 불일치: " + key);
                    for (var i = 0; i < sprites.Length; i++)
                    {
                        var source = map.Sprites[i]; var shown = sprites[i];
                        if (shown.sprite == null || AssetDatabase.GetAssetPath(shown.sprite) != RestoredSpriteFolder + "/" + SafeKey(catalog.Secondary(source.Raw[0], source.Raw[12], source.Raw[13])) + ".png" ||
                            shown.transform.position != new Vector3(source.X, map.Height - source.Y, 0) || shown.sortingOrder != 30000 + source.SourceIndex)
                            throw new InvalidDataException("오브젝트 배치 불일치: " + key + "/" + i);
                    }
                    if (roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Any(value => value == null))
                        throw new InvalidDataException("미싱 스크립트: " + key);
                    if (AssetDatabase.GetDependencies(path, true).Any(value => value.EndsWith(".mrc", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".grs", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException("원본 파일 의존성: " + key);
                    validated++;
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            return validated;
        }

        public static void ValidateBrugos01Batch()
        {
            var scene = EditorSceneManager.OpenScene(BrugosScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var ground = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(tilemap => tilemap.name == "Ground");
            var objects = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                .Where(renderer => renderer.name.StartsWith("Object_", StringComparison.Ordinal)).ToArray();
            var cages = objects.Where(renderer => renderer.name is "Object_024" or "Object_025").ToArray();
            if (ground == null || CountTiles(ground) != 1600 || objects.Length != 159 || cages.Length != 2 ||
                cages.Any(renderer => renderer.sprite == null || renderer.sprite.name != "mdsr101_secondary_31_0") ||
                roots.Any(root => root.name is "Player" or "Main Camera") ||
                EditorBuildSettings.scenes.Any(buildScene => buildScene.path == BrugosScenePath))
                throw new InvalidDataException("브르고스궁 01 맵 변환 결과가 예상과 다릅니다.");
            Debug.Log("Validated native Brugos 01 map: ground=1600, objects=159, embeddedCages=2, mapOnly=true");
        }

        public static void ValidateKaosiaBatch()
        {
            ValidateScene(KaosiaScenePath, 225, 536, "카오시아");
        }

        public static void ValidateWorldField1010Batch()
        {
            ValidateScene(WorldScenePath, 734, 17233, "월드 필드 1010");
        }

        private static void ValidateScene(string scenePath, int expectedObjects, int expectedTerrainCells, string label)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            var ground = tilemaps.FirstOrDefault(value => value.name == "Ground");
            var detail = tilemaps.FirstOrDefault(value => value.name == "Detail");
            var terrain = tilemaps.FirstOrDefault(value => value.name == "TerrainAttributes");
            var objectCount = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Count(value => value.name.StartsWith("Object_", StringComparison.Ordinal));
            var player = UnityEngine.Object.FindFirstObjectByType<TopDownPlayerController>();
            var collisionCells = terrain == null ? 0 : CountTiles(terrain);
            if (ground == null || detail == null || terrain == null || objectCount != expectedObjects || player == null ||
                terrain.GetComponent<TilemapCollider2D>() == null || collisionCells != expectedTerrainCells ||
                UnityEngine.Object.FindFirstObjectByType<CameraFollow2D>() == null ||
                player.GetComponent<DirectionalSpriteAnimator>() == null ||
                player.GetComponent<SpriteRenderer>().sprite == null ||
                UnityEngine.Object.FindFirstObjectByType<ScenePortal2D>() == null ||
                UnityEngine.Object.FindFirstObjectByType<SceneSpawnPoint>() == null ||
                UnityEngine.Object.FindFirstObjectByType<DarkSaver.Core.DarkSaverGame>() != null)
                throw new InvalidDataException("카오시아 Tilemap 씬 계층 또는 오브젝트 수가 올바르지 않습니다.");
            if (ground.GetUsedTilesCount() == 0 || detail.GetUsedTilesCount() == 0)
                throw new InvalidDataException("카오시아 Tilemap에 변환된 타일이 없습니다.");
            if (AssetDatabase.GetDependencies(scenePath, true).Any(path => path.EndsWith(".mrc", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException($"{label} 씬에 원본 MRC 의존성이 남아 있습니다.");
            Debug.Log($"Validated {label}: tilemaps={tilemaps.Length}, groundTiles={ground.GetUsedTilesCount()}, " +
                      $"detailTiles={detail.GetUsedTilesCount()}, collisionCells={collisionCells}, " +
                      $"objects={objectCount}, player=1, legacyDependencies=0");
        }

        private static int CountTiles(Tilemap tilemap)
        {
            var count = 0;
            foreach (var position in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(position)) count++;
            return count;
        }

        private static void Convert(string mapKey, string displayName, string scenePath, string spriteFolder,
            string tileFolder, Vector2 playerSpawn, HashSet<byte> blockedAttributes, bool mapOnly = false)
        {
            activeSpriteFolder = spriteFolder;
            activeTileFolder = tileFolder;
            activeBlockedAttributes = blockedAttributes;
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("프로젝트 루트를 찾을 수 없습니다.");
            var analysisRoot = Path.Combine(projectRoot, "분석", "Saver_1.2");
            var sourcePath = Path.Combine(analysisRoot, "extracted", "Saver_Files", "Saver", "MAP", mapKey + ".mrc");
            var map = MrcMapReader.Read(File.ReadAllBytes(sourcePath));
            var catalog = GrsCatalog.Load(analysisRoot);

            Directory.CreateDirectory(Path.Combine(projectRoot, activeTileFolder));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(projectRoot, scenePath))!);
            ConfigureSpriteImports();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gridObject = new GameObject(displayName, typeof(Grid));
            var ground = CreateTilemap("Ground", gridObject.transform, 0);
            var detail = CreateTilemap("Detail", gridObject.transform, 10);
            var terrain = CreateTilemap("TerrainAttributes", gridObject.transform, 0);
            terrain.GetComponent<TilemapRenderer>().enabled = false;
            terrain.gameObject.AddComponent<TilemapCollider2D>();
            var objects = new GameObject("Objects");

            for (var index = 0; index < map.Width * map.Height; index++)
            {
                var x = index % map.Width;
                var sourceY = index / map.Width;
                var position = new Vector3Int(x, map.Height - 1 - sourceY, 0);
                var back = catalog.Primary(map.BaseResourceLayer[index], map.BaseTileLayer[index], 0);
                ground.SetTile(position, GetOrCreateTile(back));

                var tile = map.Tiles[index];
                if (tile.Group != byte.MaxValue)
                {
                    var image = catalog.CellPrimary(tile.Resource, tile.Group, tile.Frame);
                    detail.SetTile(position, GetOrCreateTile(image));
                }

                if (tile.TerrainAttribute != 0)
                    terrain.SetTile(position, GetOrCreateAttributeTile(tile.TerrainAttribute));
            }

            foreach (var spriteData in map.Sprites)
            {
                var raw = spriteData.Raw;
                var image = catalog.Secondary(raw[0], raw[12], raw[13]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteAssetPath(image));
                if (sprite == null) throw new InvalidDataException($"변환 스프라이트가 없습니다: {image}");
                var instance = new GameObject($"Object_{spriteData.SourceIndex:000}", typeof(SpriteRenderer));
                instance.transform.SetParent(objects.transform, false);
                instance.transform.position = new Vector3(spriteData.X, map.Height - spriteData.Y, 0f);
                var renderer = instance.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                // MRC sprites are drawn after unit bodies, in source record order.
                renderer.sortingOrder = 30000 + spriteData.SourceIndex;
            }

            if (!mapOnly)
            {
                var player = CreatePlayer(playerSpawn);
                CreateCamera(map.Width, map.Height, player.transform);
                CreateSceneLinks(mapKey);
            }
            EditorSceneManager.SaveScene(scene, scenePath);
            if (!mapOnly) AddToBuildSettings(scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Converted {displayName}: {map.Width}x{map.Height}, objects={map.Sprites.Count}, scene={scenePath}");
        }

        private static Tilemap CreateTilemap(string name, Transform parent, int order)
        {
            var gameObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<TilemapRenderer>().sortingOrder = order;
            return gameObject.GetComponent<Tilemap>();
        }

        private static GameObject CreatePlayer(Vector2 spawn)
        {
            var player = new GameObject("Player", typeof(SpriteRenderer), typeof(Rigidbody2D),
                typeof(CapsuleCollider2D), typeof(DirectionalSpriteAnimator), typeof(TopDownPlayerController));
            player.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            var collider = player.GetComponent<CapsuleCollider2D>();
            collider.size = new Vector2(.45f, .55f);
            collider.offset = new Vector2(0f, .25f);
            var up = LoadDirection("Up");
            var down = LoadDirection("Down");
            var left = LoadDirection("Left");
            var right = LoadDirection("Right");
            player.GetComponent<SpriteRenderer>().sprite = down[1];
            player.GetComponent<DirectionalSpriteAnimator>().Configure(up, down, left, right);
            return player;
        }

        private static Sprite[] LoadDirection(string direction)
        {
            return Enumerable.Range(0, 3)
                .Select(frame => AssetDatabase.LoadAssetAtPath<Sprite>(
                    $"{CharacterSpriteFolder}/Fighter100_{direction}_{frame}.png"))
                .ToArray();
        }

        private static void CreateCamera(int width, int height, Transform target)
        {
            var gameObject = new GameObject("Main Camera", typeof(Camera), typeof(CameraFollow2D));
            gameObject.tag = "MainCamera";
            gameObject.transform.position = new Vector3(width * .5f, height * .5f, -10f);
            var camera = gameObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = GameConstants.WorldCameraOrthographicSize;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;
            gameObject.GetComponent<CameraFollow2D>().Configure(target, new Vector2(width, height));
        }

        private static void CreateSceneLinks(string mapKey)
        {
            if (mapKey == "1000")
            {
                CreatePortal("To World Field", new Vector2(21.5f, 41.5f), new Vector2(2f, 1f),
                    "WorldField1010", "FromKaosia");
                CreateSpawnPoint("FromField", new Vector2(21.5f, 39f));
            }
            else if (mapKey == "1010")
            {
                CreatePortal("To Kaosia", new Vector2(25f, 34.5f), new Vector2(2f, 1f),
                    "Kaosia", "FromField");
                CreatePortal("To Kaosia North", new Vector2(25f, 41.5f), new Vector2(2f, 1f), "Kaosia", "FromField");
                CreatePortal("To Kaosia West", new Vector2(20.5f, 38.5f), Vector2.one, "Kaosia", "FromField");
                CreatePortal("To Kaosia East", new Vector2(29.5f, 38.5f), Vector2.one, "Kaosia", "FromField");
                CreateSpawnPoint("FromKaosia", new Vector2(23f, 34f));
            }
        }

        private static void CreatePortal(string name, Vector2 position, Vector2 size, string targetScene, string spawnId)
        {
            var portal = new GameObject(name, typeof(BoxCollider2D), typeof(ScenePortal2D));
            portal.transform.position = position;
            portal.GetComponent<BoxCollider2D>().size = size;
            portal.GetComponent<ScenePortal2D>().Configure(targetScene, spawnId);
        }

        private static void CreateSpawnPoint(string id, Vector2 position)
        {
            var spawn = new GameObject("Spawn " + id, typeof(SceneSpawnPoint));
            spawn.transform.position = position;
            spawn.GetComponent<SceneSpawnPoint>().Configure(id);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var paths = EditorBuildSettings.scenes.Select(scene => scene.path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Append(scenePath)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        private static void ConfigureSpriteImports()
        {
            AssetDatabase.Refresh();
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { activeSpriteFolder, CharacterSpriteFolder })
                         .Select(AssetDatabase.GUIDToAssetPath))
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                var customPivot = path.Contains("_secondary_") || path.Contains("/Fighter100_");
                settings.spriteAlignment = customPivot ? (int)SpriteAlignment.Custom : (int)SpriteAlignment.Center;
                if (path.Contains("_secondary_")) settings.spritePivot = new Vector2(0f, 1f);
                else if (path.Contains("/Fighter100_")) settings.spritePivot = new Vector2(.5f, .1f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static Tile GetOrCreateTile(string imageKey)
        {
            var safeName = SafeKey(imageKey);
            var path = $"{activeTileFolder}/{safeName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteAssetPath(imageKey));
            if (sprite == null) throw new InvalidDataException($"변환 스프라이트가 없습니다: {imageKey}");
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static Tile GetOrCreateAttributeTile(byte attribute)
        {
            var path = $"{activeTileFolder}/Terrain_{attribute:00}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = $"Terrain Attribute {attribute}";
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.colliderType = activeBlockedAttributes.Contains(attribute) ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static string SpriteAssetPath(string key) => $"{activeSpriteFolder}/{SafeKey(key)}.png";
        private static string SafeKey(string value) => value.Replace(".grs|", "_").Replace('|', '_');

        private sealed class GrsCatalog
        {
            private readonly Dictionary<string, string> images = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, int> cells = new(StringComparer.OrdinalIgnoreCase);

            public static GrsCatalog Load(string analysisRoot)
            {
                var catalog = new GrsCatalog();
                foreach (var row in ReadCsv(Path.Combine(analysisRoot, "catalogs", "grs_images.csv")))
                    catalog.images[$"{row[0]}|{row[1]}|{row[2]}|{row[3]}"] = row[0] + "|" + row[1] + "|" + row[2] + "|" + row[3];
                foreach (var row in ReadCsv(Path.Combine(analysisRoot, "catalogs", "grs_cells.csv")))
                {
                    var first = row[6].Split(';').FirstOrDefault(value => value.Length > 0);
                    if (first == null) continue;
                    catalog.cells[$"{row[0]}|{row[1]}|{row[2]}"] = int.Parse(first, CultureInfo.InvariantCulture);
                }
                return catalog;
            }

            public string Primary(byte resource, byte group, int image) => Find($"mdsr{resource}.grs|primary|{group}|{image}");
            public string Secondary(byte resource, byte group, byte image) => Find($"mdsr{resource}.grs|secondary|{group}|{image}");
            public string CellPrimary(byte resource, byte group, byte cell)
            {
                var source = $"mdsr{resource}.grs";
                if (!cells.TryGetValue($"{source}|{group}|{cell}", out var image))
                    throw new InvalidDataException($"GRS cell을 찾을 수 없습니다: {source}/{group}/{cell}");
                return Find($"{source}|primary|{group}|{image}");
            }
            private string Find(string key) => images.TryGetValue(key, out var value)
                ? value : throw new InvalidDataException($"GRS 이미지를 찾을 수 없습니다: {key}");
        }

        private static IEnumerable<string[]> ReadCsv(string path)
        {
            foreach (var line in File.ReadLines(path, Encoding.UTF8).Skip(1))
            {
                var values = new List<string>();
                var value = new StringBuilder();
                var quoted = false;
                for (var i = 0; i < line.Length; i++)
                {
                    var character = line[i];
                    if (character == '"') quoted = !quoted;
                    else if (character == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
                    else value.Append(character);
                }
                values.Add(value.ToString());
                yield return values.ToArray();
            }
        }
    }
}
