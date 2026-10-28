using DarkSaver.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.World
{
    public sealed class BuildingPortalInstaller:MonoBehaviour
    {
        private static BuildingPortalInstaller instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset()=>instance=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Initialize()
        {
            if(instance!=null)return;var go=new GameObject("Local building door installer");DontDestroyOnLoad(go);
            instance=go.AddComponent<BuildingPortalInstaller>();SceneManager.sceneLoaded+=instance.Loaded;EnsureForLoadedScenes();
        }
        private void Loaded(Scene scene,LoadSceneMode mode)=>EnsureForLoadedScenes();
        private void OnDestroy(){SceneManager.sceneLoaded-=Loaded;if(instance==this)instance=null;}
        public static void EnsureForLoadedScenes()
        {
            var asset=Resources.Load<TextAsset>("World/BuildingRoutes");if(asset==null)return;
            var table=JsonUtility.FromJson<BuildingTravelRoutes>(asset.text);if(table==null || !table.IsValid)return;
            foreach(var route in table.routes)
            {
                var scene=SceneManager.GetSceneByName(route.sourceRuntime);
                if(!scene.IsValid() || !scene.isLoaded || !SceneManager.GetSceneByName(route.sourceMap).isLoaded)continue;
                string name="Local building door "+route.toKey+" "+route.spawnId;
                bool found=false;foreach(var root in scene.GetRootGameObjects())if(root.name==name){found=true;break;}
                if(found)continue;
                var door=new GameObject(name);SceneManager.MoveGameObjectToScene(door,scene);
                door.transform.position=new Vector3(route.doorX,route.doorY,0);door.AddComponent<BuildingDoor2D>().Configure(route.toKey,route.spawnId);
            }
        }
    }
}
