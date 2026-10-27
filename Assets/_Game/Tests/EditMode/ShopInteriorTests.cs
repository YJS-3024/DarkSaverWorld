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
    [NUnit.Framework.Category("LegacySinglePlayer")]
    [NUnit.Framework.Ignore("싱글판 구현 삭제: 원작 서버 기반 검사로 교체 필요")]
    public sealed class ShopInteriorTests
    {
        // Archived single-player behavior is tested only inside an explicit fixture.

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
        [TestCase("KaosiaBank")][TestCase("KaosiaWarehouse")][TestCase("KaosiaTemple")][TestCase("KaosiaMagicTools")][TestCase("Interior2800")]
        public void ConnectedInteriorUsesConfirmedStaffWithoutAuthoredFallback(string key)
        {
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject(key);SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                var loader=go.AddComponent<NativeTownScene>();Assert.That(loader.Configure(key,true),Is.True);
                Assert.That(loader.Build(),Is.True);
                var staff=go.GetComponentsInChildren<OriginalTownStaff>();
                int expected=(key=="KaosiaArmorShop" || key=="KaosiaWeaponShop" || key=="KaosiaAccessoryShop" || key=="KaosiaMagicTools" || key=="Interior2800")?2:key=="KaosiaBank" || key=="KaosiaWarehouse" || key=="KaosiaTemple"?1:0;
                Assert.That(staff.Length,Is.EqualTo(expected));
                Assert.IsEmpty(go.GetComponentsInChildren<IndoorNpcLabel>(),"No invented staff name labels");
                foreach(var actor in staff)
                {
                    var source=actor.Source;var renderer=actor.GetComponent<SpriteRenderer>();
                    Assert.IsNotNull(renderer.sprite);
                    Assert.That(renderer.bounds.min.x*32,Is.EqualTo(source.pixelX).Within(.001));
                    var height=(key=="KaosiaTemple" || key=="Interior2800")?20:15;
                    Assert.That((height-renderer.bounds.max.y)*32,Is.EqualTo(source.pixelY).Within(.001));
                    Assert.That(actor.GetComponent<ShopCounter2D>()!=null,Is.EqualTo(key=="KaosiaTemple" || ShopInteriorCatalog.TryGet(key,out _)));
                    Assert.IsNotEmpty(source.reference);
                    Assert.AreEqual(key=="Interior2800"?"user-authored":(key=="KaosiaWeaponShop" || key=="KaosiaAccessoryShop" || key=="KaosiaMagicTools")?"user-confirmed":"screenshot",source.evidence);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [TestCase("KaosiaWeaponShop")][TestCase("KaosiaArmorShop")][TestCase("KaosiaAccessoryShop")]
        [TestCase("KaosiaMagicTools")][TestCase("Interior2800")]
        public void BothShopStaffOpenSameNativeWindowAndStockWithoutDuplicateRequests(string key)
        {
            Assert.IsTrue(ShopInteriorCatalog.TryGet(key,out var shop));
            var map=EditModeSceneFixture.Create(shop.Region.MapScene);var runtime=EditModeSceneFixture.Create(shop.Region.RuntimeScene);
            try
            {
                var root=new GameObject("paired counter fixture");SceneManager.MoveGameObjectToScene(root,runtime);
                Assert.IsTrue(root.AddComponent<NativeTownScene>().Configure(key,true));
                var session=root.AddComponent<DarkSaver.Combat.BattleTransitionController>();
                var playerObject=new GameObject("shop visitor");playerObject.transform.SetParent(root.transform);
                var player=playerObject.AddComponent<DarkSaver.Player.TopDownPlayerController>();
                var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Items.ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
                Assert.IsNotNull(catalog);root.AddComponent<DarkSaver.UI.ItemCatalogPanel>().Configure(catalog,player);
                var panel=root.AddComponent<DarkSaver.UI.TownServicesPanel>();
                var counters=root.GetComponentsInChildren<ShopCounter2D>();Assert.AreEqual(2,counters.Length);
                Assert.IsTrue(PersonalEquipmentContext.IsReady,"shop is counted as one loaded region");
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                Texture firstTexture=null;int[] firstStock=null;DarkSaver.UI.InGameUi firstUi=null;
                foreach(var counter in counters)
                {
                    player.transform.position=counter.transform.position+Vector3.down*counter.InteractionDistance;
                    Assert.IsTrue(counter.TryOpen(player));Assert.AreEqual(DarkSaver.UI.TownService.Shop,panel.Service);
                    Assert.IsFalse(counter.TryOpen(player),"open window locks repeated NPC requests");
                    typeof(DarkSaver.UI.TownServicesPanel).GetMethod("LateUpdate",flags).Invoke(panel,null);
                    var ui=root.GetComponentsInChildren<DarkSaver.UI.InGameUi>().Single(u=>u.Find<UnityEngine.UI.RawImage>("town-service-native")!=null);
                    var texture=ui.Find<UnityEngine.UI.RawImage>("town-service-native").texture;
                    var offers=(DarkSaver.Items.NativeShopOffer[])typeof(DarkSaver.UI.TownServicesPanel).GetField("shopOffers",flags).GetValue(panel);
                    var ids=offers.Select(o=>o.itemId).ToArray();Assert.Greater(ids.Length,0);
                    if(firstStock==null){firstStock=ids;firstTexture=texture;firstUi=ui;}
                    else {CollectionAssert.AreEqual(firstStock,ids);Assert.AreSame(firstTexture,texture);Assert.AreSame(firstUi,ui);}
                    if(key==ShopInteriorCatalog.MageHouseKey)CollectionAssert.AreEquivalent(new[]{361,362,363,324,325,326},ids);
                    panel.Close();Assert.IsFalse(player.UiInputBlocked);
                    player.transform.position=counter.transform.position+Vector3.down*(counter.InteractionDistance+1);
                    Assert.IsFalse(counter.TryOpen(player),"distance restriction remains active");
                }
            }
            finally{EditModeSceneFixture.Close(runtime);EditModeSceneFixture.Close(map);}
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
