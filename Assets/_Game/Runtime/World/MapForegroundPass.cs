using DarkSaver.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    // Keep flat decorations below actors and depth-sort tall objects at ground contact.
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
                if(renderer.sprite!=null && renderer.sprite.rect.width==64 && renderer.sprite.rect.height==64
                    && (renderer.sprite.texture.name=="1_8_0" || renderer.sprite.texture.name=="1_11_0"))
                {
                    if(renderer.GetComponent<MapTreeSlices>()==null)
                        renderer.gameObject.AddComponent<MapTreeSlices>().Configure(renderer,order-OriginalRenderRules.ForegroundSortingBase);
                    continue;
                }
                renderer.sortingOrder = renderer.sprite==null ? order : OriginalRenderRules.WorldObjectSortingOrder(
                    order-OriginalRenderRules.ForegroundSortingBase,renderer.sprite.rect.height,renderer.bounds.min.y);
            }
        }
    }
}
