using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    public sealed class WorldRouteInstaller : MonoBehaviour
    {
        private static WorldRouteContent content;
        private readonly List<(WorldRoute Route,Transform Marker)> labels=new();
        private Texture2D markerTexture;
        private Sprite markerSprite;
        private InGameUi ui;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetContent()=>content=null;
        public static void EnsureForLoadedScenes()
        {
            if(content==null)
            {
                var asset=Resources.Load<TextAsset>("World/Routes");
                content=asset==null?null:JsonUtility.FromJson<WorldRouteContent>(asset.text);
                if(content==null || !content.IsValid){content=null;Debug.LogError("World route resources are invalid.");return;}
            }
            foreach(var region in TownCatalog.All)Ensure(region);
            foreach(var facility in KaosiaFacilityCatalog.All)Ensure(facility.Region);
            foreach(var shop in ShopInteriorCatalog.All)Ensure(shop.Region);
            foreach(var region in FieldCatalog.All)Ensure(region);
            foreach(var scenario in ScenarioCatalog.All)Ensure(new TownDefinition("Scenario"+scenario.Key,scenario.Title,scenario.MapScene,scenario.RuntimeScene,scenario.Key,scenario.Width,scenario.Height,false));
        }
        private static void Ensure(TownDefinition region)
        {
            var scene=SceneManager.GetSceneByName(region.RuntimeScene);
            if(!scene.isLoaded || !SceneManager.GetSceneByName(region.MapScene).isLoaded)return;
            foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<WorldRouteInstaller>()!=null)return;
            var go=new GameObject("World route gates");SceneManager.MoveGameObjectToScene(go,scene);
            go.AddComponent<WorldRouteInstaller>().Build(region.Key);
        }
        private void Build(string key)
        {
            if (FieldCatalog.TryGet(key,out _))
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    foreach (var encounter in root.GetComponentsInChildren<DarkSaver.Combat.BattleEncounter2D>(true)) encounter.ConfigureFieldLifecycle();
            markerTexture=new Texture2D(16,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            var pixels=new Color[256];
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)
            {int distance=Mathf.Abs(x-7)+Mathf.Abs(y-7);pixels[y*16+x]=distance>=4 && distance<=7?new Color(1,.8f,.15f,1):Color.clear;}
            markerTexture.SetPixels(pixels);markerTexture.Apply();
            markerSprite=Sprite.Create(markerTexture,new Rect(0,0,16,16),new Vector2(.5f,.5f),16);
            var routes=new List<WorldRoute>(content.routes);routes.AddRange(CampaignGateway.RoutesFrom(key));routes.AddRange(ShopInteriorCatalog.RoutesFrom(key));routes.AddRange(KaosiaFacilityCatalog.RoutesFrom(key));
            foreach(var route in routes)
            {
                if(route.fromKey!=key || route.legacy)continue;
                bool foundSpawn=false;
                foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var existing in root.GetComponentsInChildren<SceneSpawnPoint>(true))if(existing.Matches(route.spawnId))foundSpawn=true;
                if(!foundSpawn)
                {
                    var spawn=new GameObject("Spawn "+route.spawnId);spawn.transform.SetParent(transform,false);
                    spawn.transform.position=new Vector3(route.spawnX,route.spawnY,0);spawn.AddComponent<SceneSpawnPoint>().Configure(route.spawnId);
                }
                var gate=new GameObject("Route to "+route.toKey,typeof(SpriteRenderer),typeof(BoxCollider2D));gate.transform.SetParent(transform,false);
                gate.transform.position=new Vector3(route.portalX,route.portalY,0);
                var renderer=gate.GetComponent<SpriteRenderer>();renderer.sprite=markerSprite;renderer.sortingOrder=31000;
                gate.GetComponent<BoxCollider2D>().size=new Vector2(1.1f,1.1f);
                gate.AddComponent<ScenePortal2D>().Configure(route.toKey,route.targetSpawnId);
                labels.Add((route,gate.transform));
            }
        }
        private void LateUpdate()
        {
            bool hidden=SceneManager.GetSceneByName("Battle").isLoaded || DarkSaver.Events.Scenario01Controller.BlocksWorldActions;
            foreach(var pair in labels)if(pair.Marker!=null)pair.Marker.GetComponent<SpriteRenderer>().enabled=!hidden;
            var player=FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>();
            if(player==null || !player.isActiveAndEnabled || player.UiInputBlocked || Camera.main==null || SceneManager.GetSceneByName("Battle").isLoaded)
            {ui?.Hide();return;}
            InGameUi.Ensure(ref ui,this,15);ui.Begin();
            foreach(var pair in labels)
            {
                if(Vector2.Distance(player.transform.position,pair.Marker.position)>8)continue;
                string title;
                if(WorldRouteContent.Resolve(pair.Route.toKey,out var target))title=target.Title;
                else if(ScenarioCatalog.TryGet(pair.Route.toKey,out var scenario))
                {
                    var session=FindAnyObjectByType<DarkSaver.Combat.BattleTransitionController>();
                    title="시나리오 "+scenario.Key+" · "+scenario.Title+(session!=null && session.IsScenarioCleared(scenario.Key)?" · 완료":session!=null && !session.CanEnterScenario(scenario.Key)?" · 잠김":"");
                }
                else continue;
                var screen=Camera.main.WorldToScreenPoint(pair.Marker.position);
                if(screen.z<=0 || !DarkSaver.Rendering.GameScreen.TryScreenToGui(screen,out var point))continue;
                ui.NativeBitmapText("route-"+pair.Route.toKey,new Rect(point.x-150,point.y-42,300,34),title+" 입구",Color.yellow,maximumLines:2,alignment:TextAnchor.MiddleCenter);
            }
            ui.End();
        }
        private void OnDestroy(){ui?.Hide();if(markerSprite!=null)Destroy(markerSprite);if(markerTexture!=null)Destroy(markerTexture);}
    }
}
