using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    // Original MRC object sprites are drawn after unit bodies, in record order.
    internal static class MapForegroundPass
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (var index = 0; index < SceneManager.sceneCount; index++)
                Apply(SceneManager.GetSceneAt(index));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

        private static void Apply(Scene scene)
        {
            if (scene.name != "Kaosia_Map" && scene.name != "World1010_Map") return;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.name.StartsWith("Object_", StringComparison.Ordinal) ||
                    !int.TryParse(renderer.name.Substring(7), out var sourceIndex)) continue;
                renderer.sortingOrder = 30000 + sourceIndex;
            }
        }
    }
}
