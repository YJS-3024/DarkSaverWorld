using UnityEngine;
namespace DarkSaver.World
{
    // Serialized scene compatibility. Native map entry must come from server state;
    // no synthesized reciprocal exits, scenario portals or guide NPCs are generated.
    public sealed class WorldRouteInstaller:MonoBehaviour
    {
        public static void EnsureForLoadedScenes(){}
        private void Build(string key){}
    }
}
