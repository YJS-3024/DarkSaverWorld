using System.Collections;
using DarkSaver.CameraSystem;
using DarkSaver.Player;
using DarkSaver.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    public sealed class WorldSceneCoordinator : MonoBehaviour
    {
        private bool travelling;
        public bool IsTravelling => travelling;

        public void TravelTo(string legacyTarget, string spawnId)
        {
            TryTravelTo(legacyTarget, spawnId);
        }

        public static bool TryResolveDestination(string target, out string map, out string runtime, out Vector2 size)
        {
            for (int id = 101; id <= 107; id++)
                if (target == "Training" + id)
                { map = "Map_" + id + "_;"; runtime = "Training" + id + "_Runtime"; size = new Vector2(20, 15); return true; }
            switch (target)
            {
                case "Kaosia": map = "Kaosia_Map"; runtime = "Town_Runtime"; size = new Vector2(43f, 43f); return true;
                case "WorldField1010": map = "World1010_Map"; runtime = "Field_Runtime"; size = new Vector2(160f, 160f); return true;
                case "Brugos01": map = "Brugos01_Map"; runtime = "Brugos01_Runtime"; size = new Vector2(40f, 40f); return true;
                default: map = runtime = null; size = Vector2.zero; return false;
            }
        }

        public bool TryTravelTo(string target, string spawnId)
        {
            if (travelling || string.IsNullOrWhiteSpace(spawnId) ||
                !TryResolveDestination(target, out var map, out var runtime, out var size) ||
                !Application.CanStreamedLevelBeLoaded(map) || !Application.CanStreamedLevelBeLoaded(runtime) ||
                SceneManager.GetSceneByName("Battle").isLoaded) return false;
            var session = FindAnyObjectByType<BattleTransitionController>();
            var player = FindFirstObjectByType<TopDownPlayerController>();
            if ((session != null && session.IsTransitioning) || player == null || !player.isActiveAndEnabled) return false;
            StartCoroutine(TravelRoutine(player, map, runtime, size, spawnId));
            return true;
        }

        private IEnumerator TravelRoutine(TopDownPlayerController player, string map, string runtime, Vector2 size, string spawnId)
        {
            travelling = true;
            player.enabled = false;
            var oldSpawn = ScenePortal2D.RequestedSpawnId;
            var mapWasLoaded = SceneManager.GetSceneByName(map).isLoaded;
            var runtimeWasLoaded = SceneManager.GetSceneByName(runtime).isLoaded;
            var committed = false;
            // Load/validate the destination before removing the current world.
            ScenePortal2D.SetRequestedSpawn(null);
            try
            {
                yield return LoadIfNeeded(map);
                yield return LoadIfNeeded(runtime);
                var destination = SceneManager.GetSceneByName(runtime);
                if (!TryFindSpawn(destination, spawnId, out var position))
                {
                    Debug.LogWarning($"Spawn '{spawnId}' was not found in {runtime}; current world retained.");
                    if (!runtimeWasLoaded) yield return UnloadIfLoaded(runtime);
                    if (!mapWasLoaded) yield return UnloadIfLoaded(map);
                    yield break;
                }
                foreach (var sceneName in new[] { "Kaosia_Map", "Town_Runtime", "World1010_Map", "Field_Runtime", "Brugos01_Map", "Brugos01_Runtime" })
                    if (sceneName != map && sceneName != runtime) yield return UnloadIfLoaded(sceneName);
                for (int id = 101; id <= 107; id++)
                {
                    var trainingMap = "Map_" + id + "_;"; var trainingRuntime = "Training" + id + "_Runtime";
                    if (trainingRuntime != runtime) yield return UnloadIfLoaded(trainingRuntime);
                    if (trainingMap != map) yield return UnloadIfLoaded(trainingMap);
                }
                player.transform.position = position;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
                var camera = FindFirstObjectByType<CameraFollow2D>();
                if (camera != null) camera.Configure(player.transform, size);
                ScenePortal2D.SetRequestedSpawn(spawnId);
                committed = true;
            }
            finally
            {
                if (!committed) ScenePortal2D.SetRequestedSpawn(oldSpawn);
                if (player != null) player.enabled = true;
                travelling = false;
            }
        }

        private static IEnumerator LoadIfNeeded(string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        private static bool TryFindSpawn(Scene scene, string spawnId, out Vector3 position)
        {
            position = default;
            if (!scene.IsValid() || !scene.isLoaded) return false;
            foreach (var root in scene.GetRootGameObjects())
            {
                var spawn = root.GetComponent<SceneSpawnPoint>();
                if (spawn == null || !spawn.Matches(spawnId)) continue;
                position = spawn.transform.position;
                return true;
            }
            return false;
        }

        private static IEnumerator UnloadIfLoaded(string sceneName)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
                yield return SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
