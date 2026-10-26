using System;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class TownServiceNpcTests
    {
        [TestCase("Kaosia")][TestCase("Belpueus")][TestCase("Sisirio")][TestCase("Castlelim")]
        public void ServiceActorsMatchManifestAndInventoryOpensOnlyFromNearbyLoadedTown(string key)
        {
            Assert.IsTrue(TownCatalog.TryGet(key,out var town));
            var map=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);map.name=town.MapScene;
            var runtime=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);runtime.name=town.RuntimeScene;
            try
            {
                var root=new GameObject("service actors test");SceneManager.MoveGameObjectToScene(root,runtime);
                var installer=root.AddComponent<TownServiceNpcInstaller>();installer.Build(key);
                var data=JsonUtility.FromJson<TownServiceNpcData>(Resources.Load<TextAsset>("Town/ServiceNpcs").text);
                var actors=root.GetComponentsInChildren<TownServiceNpc>();
                Assert.AreEqual(data.entries.Count(r=>r.townKey==key),actors.Length);
                Assert.AreEqual(key=="Kaosia"?8:6,actors.Length);
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
                        root.SendMessage("LateUpdate");
                        var ui=root.GetComponentsInChildren<UIDocument>().First(d=>d.rootVisualElement.Q("quest-status")!=null).rootVisualElement;
                        Assert.IsNull(ui.Q("quest-1"));Assert.IsNull(ui.Q("quest-2"));Assert.IsNull(ui.Q("town-save"));
                        panel.Close();
                    }
                }
                actor.enabled=false;Assert.IsFalse(actor.TryInteract(player,panel));
            }
            finally{EditorSceneManager.CloseScene(runtime,true);EditorSceneManager.CloseScene(map,true);}
        }
    }
}
