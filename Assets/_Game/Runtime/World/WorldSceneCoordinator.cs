using DarkSaver.Network;
using System.Collections;
using DarkSaver.CameraSystem;
using DarkSaver.Player;
using DarkSaver.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    public sealed partial class WorldSceneCoordinator : MonoBehaviour
    {
        private ServerManager protocol;
        private int activeTransfer;
        private bool trainingTransfer;
        private BuildingTravelRoutes buildingRoutes;
        private bool travelling;
        private void BindServer()
        {
            if(protocol!=null)return;
            protocol=GetComponent<ServerManager>();if(protocol==null)protocol=gameObject.AddComponent<ServerManager>();
            protocol.BindLocalTrainingTravelService(CaptureTrainingContext);
            protocol.BindLocalBuildingTravelService(CaptureBuildingContext,IsBuildingPositionWalkable);
            protocol.ResponseReceived+=ReceiveMapPacket;
        }
        private BuildingTravelContext CaptureBuildingContext()
        {
            if(buildingRoutes==null)
            {
                var asset=Resources.Load<TextAsset>("World/BuildingRoutes");
                buildingRoutes=asset==null?null:JsonUtility.FromJson<BuildingTravelRoutes>(asset.text);
            }
            string region=null;var found=new System.Collections.Generic.HashSet<string>();
            if(buildingRoutes?.routes!=null)foreach(var route in buildingRoutes.routes)
                if(SceneManager.GetSceneByName(route.sourceMap).isLoaded && SceneManager.GetSceneByName(route.sourceRuntime).isLoaded)found.Add(route.fromKey);
            if(found.Count==1)foreach(var key in found)region=key;
            var session=FindAnyObjectByType<BattleTransitionController>();var player=FindFirstObjectByType<TopDownPlayerController>();
            bool authenticated=session!=null && session.HasCreatedCharacter;
            bool blocked=travelling || region==null || session==null || session.IsTransitioning || session.HasPendingTutorial
                || player==null || !player.isActiveAndEnabled || player.UiInputBlocked
                || DarkSaver.UI.FirstPlayTutorial.BlocksWorldActions || DarkSaver.Events.Scenario01Controller.BlocksWorldActions
                || SceneManager.GetSceneByName("Battle").isLoaded;
            return new BuildingTravelContext(region,player==null?0:player.transform.position.x,player==null?0:player.transform.position.y,authenticated,blocked);
        }
        private bool IsBuildingPositionWalkable(float x,float y)
        {
            var context=CaptureBuildingContext();
            var route=buildingRoutes?.routes==null?null:System.Array.Find(buildingRoutes.routes,r=>r.fromKey==context.Region);
            return route!=null && HasWalkableDestination(SceneManager.GetSceneByName(route.sourceMap),new Vector3(x,y,0));
        }
        public bool TryMoveWorld(Vector2 position)
        {BindServer();return protocol.TryRequest(new WorldMoveRequest(position.x,position.y),out _);}
        private DarkSaver.UI.InGameUi travelCover;
        private void LateUpdate()
        {
            if(!travelling){travelCover?.Hide();return;}
            DarkSaver.UI.InGameUi.Ensure(ref travelCover,this,1000);travelCover.Begin();
            travelCover.SolidRect("building-transition",new Rect(0,0,640,480),Color.black);travelCover.End();
        }
        private void ReceiveMapPacket(ServerPacket packet)
        {
            if(ReceiveTrainingPacket(packet))return;
            if(packet is WorldMoveNotification movement && movement.Accepted)
            {
                var actor=FindFirstObjectByType<TopDownPlayerController>();
                if(actor!=null && actor.isActiveAndEnabled && !travelling)
                    actor.GetComponent<Rigidbody2D>().MovePosition(new Vector2(movement.X,movement.Y));
                return;
            }
            if(packet is not MapEnterNotification response)return;
            if(!response.Accepted){LastTravelMessage=response.Reason;return;}
            var player=FindFirstObjectByType<TopDownPlayerController>();
            if(response.Stage==MapEnterStage.Started)
            {
                if(response.TransferId<=0 || travelling)return;
                if(player==null){protocol.TryRequest(new MapEnterCompleteRequest(response.TransferId,false),out _);return;}
                activeTransfer=response.TransferId;travelling=true;
                StartCoroutine(TravelRoutine(player,response));
            }
            else if(response.Stage==MapEnterStage.Ready && !trainingTransfer && player!=null && travelling && response.TransferId==activeTransfer)
            {
                ApplyArrival(player,response);
                LastTravelMessage="맵 이동이 완료되었습니다.";
            }
        }
        private void OnDestroy()
        {ReleaseTrainingView();travelCover?.Hide();if(protocol!=null){protocol.ResponseReceived-=ReceiveMapPacket;if(activeTransfer>0){if(trainingTransfer)protocol.TryRequest(new Request_ReadyTraining(activeTransfer,false),out _);else protocol.TryRequest(new MapEnterCompleteRequest(activeTransfer,false),out _);}protocol.UnbindLocalBuildingTravelService(CaptureBuildingContext);protocol.UnbindLocalTrainingService();}}

        public bool IsTravelling => travelling;
        public string LastTravelMessage {get;private set;}

        public void TravelTo(string legacyTarget, string spawnId)
        {
            TryTravelTo(legacyTarget, spawnId);
        }

        public static bool TryResolveDestination(string target, out string map, out string runtime, out Vector2 size)
        {
            if(DarkSaver.Core.ShopInteriorCatalog.TryGet(target,out var shop))
            {map=shop.Region.MapScene;runtime=shop.Region.RuntimeScene;size=new Vector2(shop.Region.Width,shop.Region.Height);return true;}
            if(DarkSaver.Core.KaosiaFacilityCatalog.TryGet(target,out var facility))
            {map=facility.Region.MapScene;runtime=facility.Region.RuntimeScene;size=new Vector2(facility.Region.Width,facility.Region.Height);return true;}
            if(DarkSaver.Core.NativeInteriorCatalog.TryGet(target,out var indoor))
            {map=indoor.MapScene;runtime=indoor.RuntimeScene;size=new Vector2(indoor.Width,indoor.Height);return true;}
            if (DarkSaver.Core.ScenarioCatalog.TryGet(target, out var scenario))
            { map=scenario.MapScene; runtime=scenario.RuntimeScene; size=new Vector2(scenario.Width,scenario.Height);return true; }
            if (DarkSaver.Core.TownCatalog.TryGet(target, out var town))
            { map = town.MapScene; runtime = town.RuntimeScene; size = new Vector2(town.Width, town.Height); return true; }
            if (DarkSaver.Core.FieldCatalog.TryGet(target, out var field))
            { map = field.MapScene; runtime = field.RuntimeScene; size = new Vector2(field.Width, field.Height); return true; }
            for (int id = 101; id <= 107; id++)
                if (target == "Training" + id)
                { map = "Map_" + id + "_;"; runtime = "Training" + id + "_Runtime"; size = new Vector2(20, 15); return true; }
            switch (target)
            {
                case "Brugos01": map = "Brugos01_Map"; runtime = "Brugos01_Runtime"; size = new Vector2(40f, 40f); return true;
                default: map = runtime = null; size = Vector2.zero; return false;
            }
        }

        public bool TryTravelTo(string target,string spawnId)
        {
            if(!CanBeginTravel(target,spawnId,out _,out _,out _)){LastTravelMessage="현재 건물로 이동할 수 없습니다.";return false;}
            BindServer();return protocol.TryRequest(new Request_EnterMap(target,spawnId),out _);
        }
        private bool CanBeginTravel(string target,string spawnId,out string map,out string runtime,out Vector2 size)
        {
            map=runtime=null;size=Vector2.zero;
            if (DarkSaver.UI.FirstPlayTutorial.BlocksWorldActions || DarkSaver.Events.Scenario01Controller.BlocksWorldActions || travelling || string.IsNullOrWhiteSpace(spawnId) ||
                !TryResolveDestination(target, out map, out runtime, out size) ||
                !Application.CanStreamedLevelBeLoaded(map) || !Application.CanStreamedLevelBeLoaded(runtime) ||
                SceneManager.GetSceneByName("Battle").isLoaded) return false;
            var session = FindAnyObjectByType<BattleTransitionController>();
            if (DarkSaver.Core.ScenarioCatalog.TryGet(target,out var campaignScenario) && (session==null || !session.CanEnterScenario(campaignScenario.Key))) return false;
            var player = FindFirstObjectByType<TopDownPlayerController>();
            if ((session != null && session.IsTransitioning) || player == null || !player.isActiveAndEnabled) return false;
            return true;
        }

        private IEnumerator TravelRoutine(TopDownPlayerController player, MapEnterNotification approval,
            System.Func<bool,bool> complete=null, System.Func<bool> prepare=null,
            System.Action commitAction=null, System.Action<bool> finish=null, bool save=true, System.Func<bool> commitCheck=null)
        {
            string map=approval.Map,runtime=approval.Runtime,spawnId=approval.SpawnId;
            var size=new Vector2(approval.Width,approval.Height);
            var position=new Vector3(approval.X,approval.Y,0);
            travelling = true;
            var previousPosition=player.transform.position;
            player.StopForSceneTravel();
            player.enabled = false;
            var oldSpawn = ScenePortal2D.RequestedSpawnId;
            var mapWasLoaded = SceneManager.GetSceneByName(map).isLoaded;
            var runtimeWasLoaded = SceneManager.GetSceneByName(runtime).isLoaded;
            var committed = false;
            var sessionForSave = FindAnyObjectByType<BattleTransitionController>();
            // Load/validate the destination before removing the current world.
            ScenePortal2D.SetRequestedSpawn(null);
            try
            {
                yield return LoadIfNeeded(map);
                yield return LoadIfNeeded(runtime);
                WorldRouteInstaller.EnsureForLoadedScenes();
                if (!HasWalkableDestination(SceneManager.GetSceneByName(map), position))
                {
                    Debug.LogWarning($"Spawn '{spawnId}' or walkable map data was not found in {runtime}; current world retained.");
                    if (!runtimeWasLoaded) yield return UnloadIfLoaded(runtime);
                    if (!mapWasLoaded) yield return UnloadIfLoaded(map);
                    yield break;
                }
                // Client readiness contains only the transfer token. The server retains
                // destination coordinates; callers cannot rewrite the arrival position.
                if(prepare!=null && !prepare()){LastTravelMessage="훈련장 화면을 준비하지 못했습니다. 현재 맵을 유지합니다.";yield break;}
                if(complete!=null)
                {
                    if(!complete(true))yield break;
                }
                else if(!protocol.TryRequest(new MapEnterCompleteRequest(approval.TransferId,true),out var completed)
                    || completed is not MapEnterNotification ready || ready.Stage!=MapEnterStage.Ready)yield break;
                if(commitCheck!=null && !commitCheck())yield break;
                committed=true;
                commitAction?.Invoke();
                ApplyArrival(player,approval);
                LastTravelMessage="맵 이동이 완료되었습니다.";
                foreach (var town in DarkSaver.Core.TownCatalog.All)
                {
                    if (town.MapScene != map) yield return UnloadIfLoaded(town.MapScene);
                    if (town.RuntimeScene != runtime) yield return UnloadIfLoaded(town.RuntimeScene);
                }
                foreach(var facility in DarkSaver.Core.KaosiaFacilityCatalog.All)
                {
                    if(facility.Region.MapScene!=map)yield return UnloadIfLoaded(facility.Region.MapScene);
                    if(facility.Region.RuntimeScene!=runtime)yield return UnloadIfLoaded(facility.Region.RuntimeScene);
                }
                foreach(var indoor in DarkSaver.Core.NativeInteriorCatalog.All)
                {
                    if(indoor.MapScene!=map)yield return UnloadIfLoaded(indoor.MapScene);
                    if(indoor.RuntimeScene!=runtime)yield return UnloadIfLoaded(indoor.RuntimeScene);
                }
                foreach(var shop in DarkSaver.Core.ShopInteriorCatalog.All)
                {
                    if(shop.Region.MapScene!=map)yield return UnloadIfLoaded(shop.Region.MapScene);
                    if(shop.Region.RuntimeScene!=runtime)yield return UnloadIfLoaded(shop.Region.RuntimeScene);
                }
                foreach (var field in DarkSaver.Core.FieldCatalog.All)
                {
                    if (field.MapScene != map) yield return UnloadIfLoaded(field.MapScene);
                    if (field.RuntimeScene != runtime) yield return UnloadIfLoaded(field.RuntimeScene);
                }
                foreach (var scenario in DarkSaver.Core.ScenarioCatalog.All)
                {
                    if (scenario.MapScene != map) yield return UnloadIfLoaded(scenario.MapScene);
                    if (scenario.RuntimeScene != runtime) yield return UnloadIfLoaded(scenario.RuntimeScene);
                }
                foreach (var sceneName in new[] { "Kaosia_Map", "Town_Runtime", "World1010_Map", "Field_Runtime", "Brugos01_Map", "Brugos01_Runtime" })
                    if (sceneName != map && sceneName != runtime) yield return UnloadIfLoaded(sceneName);
                for (int id = 101; id <= 107; id++)
                {
                    var trainingMap = "Map_" + id + "_;"; var trainingRuntime = "Training" + id + "_Runtime";
                    if (trainingRuntime != runtime) yield return UnloadIfLoaded(trainingRuntime);
                    if (trainingMap != map) yield return UnloadIfLoaded(trainingMap);
                }

            }
            finally
            {
                if (!committed)
                {
                    ScenePortal2D.SetRequestedSpawn(oldSpawn);
                    if(complete!=null)complete(false);
                    else protocol.TryRequest(new MapEnterCompleteRequest(approval.TransferId,false),out _);
                    if(!runtimeWasLoaded && SceneManager.GetSceneByName(runtime).isLoaded)SceneManager.UnloadSceneAsync(runtime);
                    if(!mapWasLoaded && SceneManager.GetSceneByName(map).isLoaded)SceneManager.UnloadSceneAsync(map);
                }
                finish?.Invoke(committed);
                if(player!=null)
                {
                    if(!committed)
                    {
                        player.transform.position=previousPosition;
                        var body=player.GetComponent<Rigidbody2D>();if(body!=null){body.position=previousPosition;body.linearVelocity=Vector2.zero;}
                        var camera=FindFirstObjectByType<CameraFollow2D>();
                        if(camera!=null)
                        {
                            var town=TownSceneContext.Current;
                            if(IsLocalTraining)camera.Configure(player.transform,new Vector2(20,15));
                            else if(town!=null)camera.Configure(player.transform,new Vector2(town.Width,town.Height));
                        }
                    }
                    player.enabled = trainingView==null;
                }
                travelling = false;activeTransfer=0;trainingTransfer=false;
                if (committed)
                {
                    DarkSaver.Events.Scenario01Controller.EnsureForLoadedScene();
                    if (save && sessionForSave != null && sessionForSave.HasCreatedCharacter && !sessionForSave.SaveWorld())
                        LastTravelMessage="건물 이동은 완료됐으나 저장하지 못했습니다. "+sessionForSave.LastSaveMessage;
                }
            }
        }

        private static void ApplyArrival(TopDownPlayerController player,MapEnterNotification response)
        {
            var position=new Vector3(response.X,response.Y,0);player.transform.position=position;
            var body=player.GetComponent<Rigidbody2D>();if(body!=null){body.position=position;body.linearVelocity=Vector2.zero;}
            var camera=FindFirstObjectByType<CameraFollow2D>();
            if(camera!=null)camera.Configure(player.transform,new Vector2(response.Width,response.Height));
            ScenePortal2D.SetRequestedSpawn(response.SpawnId);
        }

        private static IEnumerator LoadIfNeeded(string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        private static bool HasWalkableDestination(Scene scene, Vector3 position)
        {
            UnityEngine.Tilemaps.Tilemap ground = null, terrain = null;
            if (!scene.IsValid() || !scene.isLoaded) return false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tilemap in root.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true))
                {
                    if (tilemap.name == "Ground") { if (ground != null) return false; ground = tilemap; }
                    else if (tilemap.name == "TerrainAttributes") { if (terrain != null) return false; terrain = tilemap; }
                }
            return ground != null && terrain != null && ground.HasTile(ground.WorldToCell(position))
                && terrain.GetColliderType(terrain.WorldToCell(position)) == UnityEngine.Tilemaps.Tile.ColliderType.None;
        }

        private static IEnumerator UnloadIfLoaded(string sceneName)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
                yield return SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
