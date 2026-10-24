using DarkSaver.Rendering;
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
            if (!OriginalRenderRules.IsNativeMapScene(scene.name)) return;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!OriginalRenderRules.TryGetForegroundSortingOrder(renderer.name, out var order)) continue;
                renderer.sortingOrder = order;
            }
        }
    }
}
