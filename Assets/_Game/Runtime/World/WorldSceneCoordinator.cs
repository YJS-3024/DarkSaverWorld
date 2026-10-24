using DarkSaver.Network;
using System.Collections;
using DarkSaver.CameraSystem;
using DarkSaver.Player;
using DarkSaver.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    public sealed class WorldSceneCoordinator : MonoBehaviour
    {
        private ServerManager protocol;
        private System.IDisposable mapBinding;
        private bool travelling;
        private void BindServer()
        {
            if(protocol!=null)return;
            protocol=GetComponent<ServerManager>();if(protocol==null)protocol=gameObject.AddComponent<ServerManager>();
            mapBinding=protocol.RegisterRequestHandler<MapEnterRequest>(HandleMapEnter);
            protocol.ResponseReceived+=ReceiveMapPacket;
        }
        private LocalCommandResponse HandleMapEnter(MapEnterRequest request)
        {
            if(!CanBeginTravel(request.Target,request.SpawnId,out var map,out var runtime,out var size))
                return new MapEnterNotification(reason:"현재 목적지로 이동할 수 없습니다.");
            travelling=true;
            return new MapEnterNotification(true,MapEnterStage.Started,map,runtime,request.SpawnId,size.x,size.y);
        }
        private void ReceiveMapPacket(ServerPacket packet)
        {
            if(packet is not MapEnterNotification response || !response.Accepted)return;
            var player=FindFirstObjectByType<TopDownPlayerController>();
            if(response.Stage==MapEnterStage.Started)
            {
                if(player==null){travelling=false;return;}
                StartCoroutine(TravelRoutine(player,response.Map,response.Runtime,new Vector2(response.Width,response.Height),response.SpawnId));
            }
            else if(response.Stage==MapEnterStage.Ready && player!=null)
            {
                var position=new Vector3(response.X,response.Y,0);player.transform.position=position;
                var body=player.GetComponent<Rigidbody2D>();if(body!=null){body.position=position;body.linearVelocity=Vector2.zero;}
                var camera=FindFirstObjectByType<CameraFollow2D>();if(camera!=null)camera.Configure(player.transform,new Vector2(response.Width,response.Height));
                ScenePortal2D.SetRequestedSpawn(response.SpawnId);
            }
        }
        private void OnDestroy()
        {mapBinding?.Dispose();if(protocol!=null)protocol.ResponseReceived-=ReceiveMapPacket;}

        public bool IsTravelling => travelling;

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
            BindServer();return protocol.TryRequest(new MapEnterRequest(target,spawnId),out _);
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

        private IEnumerator TravelRoutine(TopDownPlayerController player, string map, string runtime, Vector2 size, string spawnId)
        {
            travelling = true;
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
                var destination = SceneManager.GetSceneByName(runtime);
                if (!TryFindSpawn(destination, spawnId, out var position)
                    || !HasWalkableDestination(SceneManager.GetSceneByName(map), position))
                {
                    Debug.LogWarning($"Spawn '{spawnId}' or walkable map data was not found in {runtime}; current world retained.");
                    if (!runtimeWasLoaded) yield return UnloadIfLoaded(runtime);
                    if (!mapWasLoaded) yield return UnloadIfLoaded(map);
                    yield break;
                }
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
                protocol.SendToClient(new MapEnterNotification(true,MapEnterStage.Ready,map,runtime,spawnId,size.x,size.y,position.x,position.y));
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    ScenePortal2D.SetRequestedSpawn(oldSpawn);
                    protocol.SendToClient(new MapEnterNotification(reason:"맵/출현 좌표 확인에 실패해 기존 지역을 유지했습니다."));
                }
                if (player != null) player.enabled = true;
                travelling = false;
                if (committed)
                {
                    DarkSaver.Events.Scenario01Controller.EnsureForLoadedScene();
                    if (sessionForSave != null && sessionForSave.HasCreatedCharacter) sessionForSave.SaveWorld();
                }
            }
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

        private static bool TryFindSpawn(Scene scene, string spawnId, out Vector3 position)
        {
            position = default;
            if (!scene.IsValid() || !scene.isLoaded) return false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var spawn in root.GetComponentsInChildren<SceneSpawnPoint>(true))
                {
                    if (!spawn.Matches(spawnId)) continue;
                    position = spawn.transform.position;
                    return true;
                }
            }
            return false;
        }

        private static IEnumerator UnloadIfLoaded(string sceneName)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
                yield return SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
