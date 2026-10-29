using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class BuildingTravelTests
    {
        private static BuildingTravelRoutes Routes()=>JsonUtility.FromJson<BuildingTravelRoutes>(Resources.Load<TextAsset>("World/BuildingRoutes").text);
        [Test]public void AllBuildingRequestsValidateSourcesAndRetainCanonicalResponses()
            =>BuildingTravelChecks.Run((ok,name)=>Assert.IsTrue(ok,name),Routes());
        [Test]public void ServerManagerOwnsEntryCompletionAndScopedMovementHandlers()
        {
            var go=new GameObject("local building server fixture");
            var originalScenes=EditorBuildSettings.scenes;
            try
            {
                var manager=go.AddComponent<ServerManager>();var route=Routes().routes[0];
                string[] required={"Assets/_Game/Scenes/Maps/"+route.map+".unity",
                    "Assets/_Game/Scenes/Runtime/"+route.runtime+".unity"};
                foreach(var path in required)Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(path),path);
                EditorBuildSettings.scenes=originalScenes.Where(s=>!required.Contains(s.path))
                    .Concat(required.Select(path=>new EditorBuildSettingsScene(path,true))).ToArray();
                var context=new BuildingTravelContext(route.fromKey,route.doorX,route.doorY,true,false);
                var responses=new List<ServerPacket>();manager.ResponseReceived+=responses.Add;
                manager.BindLocalBuildingTravelService(()=>context,(x,y)=>true);
                Assert.IsTrue(manager.LocalServer.HasHandler(new Request_EnterMap()));
                Assert.IsTrue(manager.LocalServer.HasHandler(new MapEnterCompleteRequest(0,false)));
                Assert.IsTrue(manager.LocalServer.HasHandler(new WorldMoveRequest()));
                Assert.IsTrue(manager.TryRequest(new Request_EnterMap(route.toKey,route.spawnId),out var started),started?.Reason);
                var transfer=(MapEnterNotification)started;Assert.AreEqual(MapEnterStage.Started,transfer.Stage);
                Assert.AreEqual(route.x,transfer.X);Assert.AreEqual(route.y,transfer.Y);
                Assert.IsFalse(manager.TryRequest(new Request_EnterMap(route.toKey,route.spawnId),out _));
                Assert.IsTrue(manager.TryRequest(new MapEnterCompleteRequest(transfer.TransferId,true),out var done));
                var ready=(MapEnterNotification)done;Assert.AreEqual(MapEnterStage.Ready,ready.Stage);
                Assert.AreEqual(route.map,ready.Map);Assert.AreEqual(route.x,ready.X);Assert.AreEqual(route.y,ready.Y);
                Assert.IsFalse(manager.TryRequest(new MapEnterCompleteRequest(transfer.TransferId,true),out _));
                Assert.GreaterOrEqual(responses.Count,4);
            }
            finally{EditorBuildSettings.scenes=originalScenes;Object.DestroyImmediate(go);}
        }
    }
}
