using System;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkSaver.EditorTools.CanvasTesting;
using UnityEditor.SceneManagement;

namespace DarkSaver.Tests
{
    [NUnit.Framework.Category("LegacySinglePlayer")]
    [NUnit.Framework.Ignore("싱글판 구현 삭제: 원작 서버 기반 검사로 교체 필요")]
    public sealed class TownServiceNpcTests
    {
        // Archived single-player behavior is tested only inside an explicit fixture.

        [Test]
        public void OriginalKaosiaGatesKeepTravelTriggersWithoutAuthoredMarkersOrLabels()
        {
            TownCatalog.TryGet("Kaosia",out var town);
            var map=EditModeSceneFixture.Create(town.MapScene);var runtime=EditModeSceneFixture.Create(town.RuntimeScene);
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var contentField=typeof(WorldRouteInstaller).GetField("content",flags);var previous=contentField.GetValue(null);
            try
            {
                var content=JsonUtility.FromJson<WorldRouteContent>(Resources.Load<TextAsset>("World/Routes").text);
                contentField.SetValue(null,content);
                var root=new GameObject("original Kaosia gates");SceneManager.MoveGameObjectToScene(root,runtime);
                var legacy=new GameObject("legacy marker",typeof(SpriteRenderer),typeof(ScenePortal2D));legacy.transform.SetParent(root.transform);
                legacy.GetComponent<ScenePortal2D>().Configure("Field1010","FromKaosia");
                var installer=root.AddComponent<WorldRouteInstaller>();
                typeof(WorldRouteInstaller).GetMethod("Build",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(installer,new object[]{"Kaosia"});
                Assert.IsEmpty(root.GetComponentsInChildren<TownServiceNpc>());
                var portals=root.GetComponentsInChildren<ScenePortal2D>();Assert.GreaterOrEqual(portals.Length,8);
                foreach(var portal in portals)
                {
                    Assert.IsTrue(portal.GetComponent<BoxCollider2D>().isTrigger);
                    Assert.IsFalse(portal.GetComponent<SpriteRenderer>().enabled);
                }
                var labels=(System.Collections.ICollection)typeof(WorldRouteInstaller).GetField("labels",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(installer);
                Assert.AreEqual(0,labels.Count);
            }
            finally{EditModeSceneFixture.Close(runtime);EditModeSceneFixture.Close(map);contentField.SetValue(null,previous);}
        }
        [TestCase("Kaosia")][TestCase("Belpueus")][TestCase("Sisirio")][TestCase("Castlelim")]
        public void ServiceActorsMatchManifestAndInventoryOpensOnlyFromNearbyLoadedTown(string key)
        {
            Assert.IsTrue(TownCatalog.TryGet(key,out var town));
            var map=EditModeSceneFixture.Create(town.MapScene);
            var runtime=EditModeSceneFixture.Create(town.RuntimeScene);
            try
            {
                var root=new GameObject("service actors test");SceneManager.MoveGameObjectToScene(root,runtime);
                var installer=root.AddComponent<TownServiceNpcInstaller>();installer.Build(key);
                var data=JsonUtility.FromJson<TownServiceNpcData>(Resources.Load<TextAsset>("Town/ServiceNpcs").text);
                var actors=root.GetComponentsInChildren<TownServiceNpc>();
                if(key=="Kaosia")
                {
                    Assert.AreEqual(8,data.entries.Count(r=>r.townKey==key),"Authored data preserved for later expansion");
                    Assert.IsEmpty(actors,"Original Kaosia must not spawn single-player service NPCs");
                    var stale=new GameObject("previously serialized authored NPC");stale.transform.SetParent(root.transform);
                    var npc=stale.AddComponent<TownServiceNpc>();npc.Configure(data.entries.First(r=>r.townKey==key));
                    var playerObject=new GameObject("player");playerObject.transform.SetParent(root.transform);
                    var originalPlayer=playerObject.AddComponent<TopDownPlayerController>();var originalPanel=root.AddComponent<TownServicesPanel>();
                    originalPlayer.transform.position=npc.transform.position;
                    Assert.IsFalse(npc.TryInteract(originalPlayer,originalPanel));Assert.IsFalse(originalPanel.IsOpen);
                    return;
                }
                Assert.AreEqual(data.entries.Count(r=>r.townKey==key),actors.Length);
                Assert.AreEqual(6,actors.Length);
                var actor=actors.Single(n=>n.Definition.action=="Inventory");
                Assert.IsNotNull(actor.GetComponent<SpriteRenderer>().sprite);Assert.IsTrue(actor.GetComponent<BoxCollider2D>().isTrigger);
                var go=new GameObject("player");go.transform.SetParent(root.transform);var player=go.AddComponent<TopDownPlayerController>();
                var panel=root.AddComponent<TownServicesPanel>();
                Assert.IsFalse(panel.Open(TownService.Overview,player));
                player.transform.position=actor.transform.position+Vector3.right*10;
                Assert.IsFalse(actor.TryInteract(player,panel));Assert.IsFalse(panel.IsOpen);
                player.transform.position=actor.transform.position;
                Assert.IsTrue(actor.TryInteract(player,panel));Assert.AreEqual(TownService.Inventory,panel.Service);Assert.IsTrue(player.UiInputBlocked);
                Assert.IsFalse(actor.TryInteract(player,panel));
                panel.Close();Assert.IsFalse(player.UiInputBlocked);
                if(key=="Kaosia")
                {
                    foreach(var quest in actors.Where(n=>n.Definition.action=="Quests"))
                    {
                        player.transform.position=quest.transform.position;
                        Assert.IsTrue(quest.TryInteract(player,panel));Assert.AreEqual(quest.Definition.name,panel.Speaker);
                        typeof(TownServicesPanel).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(panel,null);
                        var ui=root.GetComponentsInChildren<InGameUi>().Select(CanvasProbe.For).First(d=>d.rootVisualElement.Q("quest-status")!=null).rootVisualElement;
                        Assert.IsNull(ui.Q("quest-1"));Assert.IsNull(ui.Q("quest-2"));Assert.IsNull(ui.Q("town-save"));
                        panel.Close();
                    }
                }
                actor.enabled=false;Assert.IsFalse(actor.TryInteract(player,panel));
            }
            finally{EditModeSceneFixture.Close(runtime);EditModeSceneFixture.Close(map);}
        }
    }
}
