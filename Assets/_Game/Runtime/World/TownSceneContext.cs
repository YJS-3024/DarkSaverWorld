using DarkSaver.Core;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    public static class TownSceneContext
    {
        // Ambiguous additive loading is never treated as a usable town context.
        public static TownDefinition Current
        {
            get
            {
                TownDefinition result=null;
                foreach(var row in TownCatalog.All)
                    if(SceneManager.GetSceneByName(row.RuntimeScene).isLoaded)
                    { if(result!=null)return null;result=row; }
                var interior=ShopInteriorContext.Current;
                if(interior!=null){if(result!=null)return null;return interior.TownContext;}
                return result;
            }
        }
        public static bool IsReady => Current != null;
    }
    public static class FieldSceneContext
    {
        public static TownDefinition Current
        {
            get
            {
                TownDefinition result=null;
                foreach(var row in FieldCatalog.All)
                    if(SceneManager.GetSceneByName(row.RuntimeScene).isLoaded)
                    {if(result!=null)return null;result=row;}
                foreach(var facility in KaosiaFacilityCatalog.All)
                    if(!ShopInteriorCatalog.TryGet(facility.Region.Key,out _) && SceneManager.GetSceneByName(facility.Region.RuntimeScene).isLoaded)
                    {if(result!=null)return null;result=facility.Region;}
                foreach(var row in NativeInteriorCatalog.All)
                    if(!ShopInteriorCatalog.TryGet(row.Key,out _) && SceneManager.GetSceneByName(row.RuntimeScene).isLoaded)
                    {if(result!=null)return null;result=row;}
                return result;
            }
        }
    }

}
