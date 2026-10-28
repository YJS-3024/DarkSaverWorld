using UnityEngine;
namespace DarkSaver.World
{
    // Serialized scene compatibility. Native map entry must come from server state;
    // Only the explicitly authorized local building doors are installed.
    // Field/scenario portals and guide NPCs are not generated.
    public sealed class WorldRouteInstaller:MonoBehaviour
    {
        public static void EnsureForLoadedScenes()=>BuildingPortalInstaller.EnsureForLoadedScenes();
        private void Build(string key){}
    }
}
