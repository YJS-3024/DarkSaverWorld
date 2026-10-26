using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class WorldFieldTests
    {
        [Test]
        public void GeneratedOutdoorRuntimeDoesNotInventIndoorNpcsFromMissingJsonFields()
        {
            foreach (var region in TownCatalog.All.Concat(FieldCatalog.All))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
                try
                {
                    var root = new GameObject("outdoor runtime " + region.Key);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                    var loader = root.AddComponent<NativeTownScene>();
                    Assert.IsTrue(loader.Configure(region.Key,true),region.Key);
                    Assert.IsEmpty(root.GetComponentsInChildren<IndoorNpcLabel>(true),region.Key);
                    Assert.IsNotEmpty(root.GetComponentsInChildren<SceneSpawnPoint>(true),region.Key);
                }
                finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        [Test]
        public void AllFieldDestinationsResolveAndTheNetworkIsReciprocal()
        {
            Assert.AreEqual(18,FieldCatalog.All.Count);
            foreach(var field in FieldCatalog.All)
            {
                Assert.IsTrue(WorldSceneCoordinator.TryResolveDestination(field.Key,out var map,out var runtime,out var size));
                Assert.AreEqual(field.MapScene,map);Assert.AreEqual(field.RuntimeScene,runtime);
                Assert.AreEqual(new Vector2(field.Width,field.Height),size);
                Assert.IsTrue(NativeSaveStore.IsSupportedMap(map,runtime));
                foreach(var other in FieldCatalog.All)Assert.AreEqual(field==other,NativeSaveStore.IsSupportedMap(map,other.RuntimeScene));
            }
            var content=JsonUtility.FromJson<WorldRouteContent>(Resources.Load<TextAsset>("World/Routes").text);
            Assert.IsTrue(content.IsValid);Assert.AreEqual(42,content.routes.Length);
            content.routes=content.routes.Skip(1).ToArray();Assert.IsFalse(content.IsValid);
        }
        [Test]
        public void DefeatedFieldVisualCannotLeaveAnInvisibleEncounterTrigger()
        {
            var playerObject=new GameObject("field result player fixture");
            var enemyObject=new GameObject("field result enemy fixture");
            try
            {
                var player=playerObject.AddComponent<DarkSaver.Player.TopDownPlayerController>();
                var encounter=enemyObject.AddComponent<DarkSaver.Combat.BattleEncounter2D>();
                encounter.ConfigureFieldLifecycle();
                // Explicit post-battle fixture: native battle presentation hid the
                // dead body, and the encounter already interacted with this player.
                typeof(DarkSaver.Combat.BattleEncounter2D).GetField("encounteredPlayer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(encounter,player);
                enemyObject.GetComponent<SpriteRenderer>().enabled=true;
                Assert.IsFalse(encounter.TryRearm());Assert.IsTrue(enemyObject.GetComponent<BoxCollider2D>().enabled);
                enemyObject.GetComponent<SpriteRenderer>().enabled=false;
                Assert.IsFalse(encounter.TryRearm());Assert.IsFalse(enemyObject.GetComponent<BoxCollider2D>().enabled);Assert.IsFalse(encounter.enabled);
            }
            finally {UnityEngine.Object.DestroyImmediate(enemyObject);UnityEngine.Object.DestroyImmediate(playerObject);}
        }
        [Test]
        public void FieldSaveAndActualJsonPreserveLocationAndInventory()
        {
            var directory=Path.Combine(Path.GetTempPath(),"ds-fields-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                foreach(var field in FieldCatalog.All)
                {
                    var snapshot=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,
                        mapScene=field.MapScene,runtimeScene=field.RuntimeScene,worldX=5.5f,worldY=6.5f,clearedScenarios=Array.Empty<string>(),
                        hasCharacter=true,character=new NativeCharacterProfile {name="들판",jobId=100},hasInventory=true,inventory=new NativeInventory {gold=123}};
                    var store=new NativeSaveStore(Path.Combine(directory,field.Key+".json"));
                    Assert.IsTrue(store.TrySave(snapshot,out _));Assert.IsTrue(store.TryLoad(out var loaded,out _));
                    Assert.AreEqual(field.MapScene,loaded.mapScene);Assert.AreEqual(field.RuntimeScene,loaded.runtimeScene);
                    Assert.AreEqual(5.5f,loaded.worldX);Assert.AreEqual(6.5f,loaded.worldY);Assert.AreEqual(123,loaded.inventory.gold);
                }
            }
            finally {Directory.Delete(directory,true);}
        }
    }
}
