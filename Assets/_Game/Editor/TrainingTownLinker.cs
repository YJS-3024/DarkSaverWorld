using System;
using System.Linq;
using DarkSaver.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.EditorTools
{
    // Connection only: preserve existing map tiles, encounters and native data.
    public static class TrainingTownLinker
    {
        [MenuItem("DarkSaver/Scenes/Connect Kaosia Training Entrance")]
        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Editor must be idle.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save dirty scenes first.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var townMap = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Maps/Kaosia_Map.unity");
                Vector3 entrance, returned;
                try
                {
                    entrance = FindWalkable(townMap, new Vector3(8.5f, 24.5f), default, 0);
                    returned = FindWalkable(townMap, entrance + Vector3.down * 2, entrance, 1.8f);
                }
                finally { EditorSceneManager.ClosePreviewScene(townMap); }
                var town = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Runtime/Town_Runtime.unity", OpenSceneMode.Additive);
                try
                {
                    var gate = Root(town, "Training Entrance"); gate.transform.position = entrance;
                    var entry = gate.GetComponent<TrainingEntrance2D>() ?? gate.AddComponent<TrainingEntrance2D>();
                    entry.Configure(AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset"));
                    var box = gate.GetComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(.9f, .9f);
                    var spawn = Root(town, "Spawn FromTraining"); spawn.transform.position = returned;
                    (spawn.GetComponent<SceneSpawnPoint>() ?? spawn.AddComponent<SceneSpawnPoint>()).Configure("FromTraining");
                    EditorSceneManager.SaveScene(town);
                    Debug.Log($"Kaosia training entrance={entrance}, return={returned}; western building approach, not recovered portal coordinates.");
                }
                finally { EditorSceneManager.CloseScene(town, true); }
                for (int id = 101; id <= 107; id++)
                {
                    var runtime = EditorSceneManager.OpenScene(TrainingSceneGenerator.RuntimePath(id), OpenSceneMode.Additive);
                    try
                    {
                        var entry = runtime.GetRootGameObjects().Select(go => go.GetComponent<SceneSpawnPoint>())
                            .FirstOrDefault(spawn => spawn != null && spawn.Matches("TrainingEntry"));
                        if (entry == null) throw new InvalidOperationException("Missing training entry " + id);
                        var map = EditorSceneManager.OpenPreviewScene($"Assets/_Game/Scenes/Maps/Map_{id}_;.unity");
                        Vector3 exit;
                        try { exit = FindWalkable(map, entry.transform.position + Vector3.down * 2, entry.transform.position, 1.8f); }
                        finally { EditorSceneManager.ClosePreviewScene(map); }
                        var gate = Root(runtime, "To Kaosia From Training"); gate.transform.position = exit;
                        var portal = gate.GetComponent<ScenePortal2D>() ?? gate.AddComponent<ScenePortal2D>();
                        portal.Configure("Kaosia", "FromTraining"); gate.GetComponent<BoxCollider2D>().size = new Vector2(.9f, .9f);
                        EditorSceneManager.SaveScene(runtime);
                        Debug.Log($"Training{id} return portal={exit}");
                    }
                    finally { EditorSceneManager.CloseScene(runtime, true); }
                }
                var builds = EditorBuildSettings.scenes.ToList();
                for (int id = 101; id <= 107; id++)
                    foreach (var path in new[] { $"Assets/_Game/Scenes/Maps/Map_{id}_;.unity", TrainingSceneGenerator.RuntimePath(id) })
                    {
                        var existing = builds.FirstOrDefault(s => s.path == path);
                        if (existing == null) builds.Add(new EditorBuildSettingsScene(path, true)); else existing.enabled = true;
                    }
                EditorBuildSettings.scenes = builds.ToArray();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static GameObject Root(Scene scene, string name)
        {
            var existing = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
            if (existing != null) return existing;
            var created = new GameObject(name); SceneManager.MoveGameObjectToScene(created, scene); return created;
        }
        private static Vector3 FindWalkable(Scene scene, Vector3 near, Vector3 avoid, float minimumDistance)
        {
            var maps = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Tilemap>(true)).ToArray();
            var ground = maps.First(m => m.name == "Ground"); var terrain = maps.First(m => m.name == "TerrainAttributes");
            var origin = ground.WorldToCell(near); float best = float.MaxValue; Vector3 result = default;
            for (int y = -4; y <= 4; y++) for (int x = -4; x <= 4; x++)
            {
                var cell = origin + new Vector3Int(x, y, 0);
                if (!ground.HasTile(cell) || terrain.GetColliderType(cell) != Tile.ColliderType.None) continue;
                var point = ground.GetCellCenterWorld(cell);
                if (Vector3.Distance(point, avoid) < minimumDistance) continue;
                float distance = (point - near).sqrMagnitude;
                if (distance < best) { best = distance; result = point; }
            }
            if (best == float.MaxValue) throw new InvalidOperationException("No walkable portal approach: " + scene.name);
            return result;
        }
    }
}
