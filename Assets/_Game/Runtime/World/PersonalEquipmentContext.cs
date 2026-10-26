using DarkSaver.Core;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    public static class PersonalEquipmentContext
    {
        public static bool IsReady
        {
            get
            {
                int regions=0;
                if(TownSceneContext.IsReady)regions++;
                var field=FieldSceneContext.Current;
                if(field!=null && SceneManager.GetSceneByName(field.MapScene).isLoaded)regions++;
                foreach(var scenario in ScenarioCatalog.All)
                    if(SceneManager.GetSceneByName(scenario.MapScene).isLoaded && SceneManager.GetSceneByName(scenario.RuntimeScene).isLoaded)regions++;
                for(int id=101;id<=107;id++)
                    if(SceneManager.GetSceneByName("Map_"+id+"_;").isLoaded && SceneManager.GetSceneByName("Training"+id+"_Runtime").isLoaded)regions++;
                return regions==1;
            }
        }
    }
}
