using System;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class ShopInteriorTests
    {
        [TestCase("KaosiaWeaponShop")][TestCase("KaosiaArmorShop")]
        public void ShopDestinationUsesIndependentPersistableScenePair(string key)
        {
            Assert.That(ShopInteriorCatalog.TryGet(key,out var shop),Is.True);
            Assert.That(WorldSceneCoordinator.TryResolveDestination(key,out var map,out var runtime,out var size),Is.True);
            Assert.That(map,Is.EqualTo(shop.Region.MapScene));Assert.That(runtime,Is.EqualTo(shop.Region.RuntimeScene));
            Assert.That(size,Is.EqualTo(new Vector2(20,15)));
            Assert.That(NativeSaveStore.IsSupportedMap(map,runtime),Is.True);
            Assert.That(DarkSaver.Combat.BattleTransitionController.IsSupportedBattleMapScene(map),Is.False);
            Assert.That(ShopInteriorCatalog.RoutesFrom(key).Single().targetSpawnId,Is.EqualTo(shop.ReturnSpawn));
        }
        [TestCase("KaosiaWeaponShop")][TestCase("KaosiaArmorShop")]
        public void LoadedRoomBuildsGroundAndRealCollisionForWallsCounterAndExit(string key)
        {
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("interior tilemap fixture");go.SetActive(false);SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                var loader=go.AddComponent<ShopInteriorScene>();JsonUtility.FromJsonOverwrite("{\"shopKey\":\""+key+"\",\"runtimeOnly\":false}",loader);go.SetActive(true);Assert.That(loader.Build(),Is.True);
                var layers=go.GetComponentsInChildren<Tilemap>();
                var ground=layers.Single(t=>t.name=="Ground");var terrain=layers.Single(t=>t.name=="TerrainAttributes");
                Assert.That(ground.HasTile(new Vector3Int(10,3,0)),Is.True);
                Assert.That(terrain.GetColliderType(new Vector3Int(10,3,0)),Is.EqualTo(Tile.ColliderType.None));
                Assert.That(terrain.GetColliderType(new Vector3Int(10,0,0)),Is.EqualTo(Tile.ColliderType.None));
                Assert.That(terrain.GetColliderType(new Vector3Int(10,11,0)),Is.EqualTo(Tile.ColliderType.Grid));
                Assert.That(terrain.GetColliderType(new Vector3Int(0,14,0)),Is.EqualTo(Tile.ColliderType.Grid));
                Assert.That(terrain.GetComponent<TilemapCollider2D>(),Is.Not.Null);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static string[] NativeInteriorKeys=>NativeInteriorCatalog.All.Select(r=>r.Key).Concat(KaosiaFacilityCatalog.All.Select(r=>r.Region.Key)).ToArray();
        [TestCaseSource(nameof(NativeInteriorKeys))]
        public void EveryNativeInteriorBuildsItsOriginalSizeAndCollision(string key)
        {
            if(!NativeInteriorCatalog.TryGet(key,out var region))
            {Assert.That(KaosiaFacilityCatalog.TryGet(key,out var facility),Is.True);region=facility.Region;}
            Assert.That(WorldSceneCoordinator.TryResolveDestination(key,out var map,out var runtime,out var size),Is.True);
            Assert.That(NativeSaveStore.IsSupportedMap(map,runtime),Is.True);
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject(key);SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                var loader=go.AddComponent<NativeTownScene>();Assert.That(loader.Configure(key,false),Is.True);
                var layers=go.GetComponentsInChildren<Tilemap>();var ground=layers.Single(t=>t.name=="Ground");
                Assert.That(ground.cellBounds.size.x,Is.EqualTo(region.Width));Assert.That(ground.cellBounds.size.y,Is.EqualTo(region.Height));
                Assert.That(layers.Single(t=>t.name=="TerrainAttributes").GetComponent<TilemapCollider2D>(),Is.Not.Null);
                Assert.That(size,Is.EqualTo(new Vector2(region.Width,region.Height)));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [TestCase("KaosiaWeaponShop")][TestCase("KaosiaArmorShop")][TestCase("KaosiaAccessoryShop")]
        [TestCase("KaosiaBank")][TestCase("KaosiaWarehouse")][TestCase("KaosiaTemple")][TestCase("KaosiaMagicTools")]
        public void ConnectedInteriorCreatesExactlyOneVisibleStaffMember(string key)
        {
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject(key);SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                var loader=go.AddComponent<NativeTownScene>();Assert.That(loader.Configure(key,true),Is.True);
                Assert.That(loader.Build(),Is.True);
                var staff=go.GetComponentsInChildren<IndoorNpcLabel>();Assert.That(staff.Length,Is.EqualTo(1));
                var renderer=staff[0].GetComponent<SpriteRenderer>();Assert.That(renderer.sprite,Is.Not.Null);
                Assert.That(renderer.sortingOrder,Is.LessThan(30000));
                Assert.That(staff[0].GetComponent<ShopCounter2D>()!=null,Is.EqualTo(key=="KaosiaTemple" || ShopInteriorCatalog.TryGet(key,out _)));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]
        public void EnteringInteriorImmediatelyRepositionsCameraAndKeepsDepth()
        {
            var go=new GameObject("transition camera",typeof(Camera));
            var target=new GameObject("arriving player");
            try
            {
                go.transform.position=new Vector3(40,35,-10);
                target.transform.position=new Vector3(10.5f,3.5f,0);
                var follow=go.AddComponent<DarkSaver.CameraSystem.CameraFollow2D>();
                follow.Configure(target.transform,new Vector2(20,15));
                var view=go.GetComponent<Camera>();float size=view.orthographicSize;
                Assert.That(go.transform.position.x,Is.EqualTo(Mathf.Clamp(10.5f,size*view.aspect,20-size*view.aspect)).Within(.001f));
                Assert.That(go.transform.position.y,Is.EqualTo(Mathf.Clamp(3.5f,size,15-size)).Within(.001f));
                Assert.AreEqual(-10,go.transform.position.z);
                // Returning outside must also snap before any LateUpdate frame.
                target.transform.position=new Vector3(34.5f,10.5f,0);
                follow.Configure(target.transform,new Vector2(43,43));
                Assert.That(go.transform.position.x,Is.GreaterThan(20));
                Assert.AreEqual(-10,go.transform.position.z);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(target);}
        }
        [Test]
        public void CameraFitsSmallIndoorMap()
        {
            var go=new GameObject("interior camera",typeof(Camera));var target=new GameObject("indoor player");
            try
            {
                var follow=go.AddComponent<DarkSaver.CameraSystem.CameraFollow2D>();follow.Configure(target.transform,new Vector2(20,15));
                var camera=go.GetComponent<Camera>();Assert.That(camera.orthographicSize*2,Is.LessThanOrEqualTo(15.001f));
                Assert.That(camera.orthographicSize*2*camera.aspect,Is.LessThanOrEqualTo(20.001f));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
