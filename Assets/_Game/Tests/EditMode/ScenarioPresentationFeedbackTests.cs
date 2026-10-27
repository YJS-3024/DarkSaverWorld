using System;
using System.Reflection;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DarkSaver.Tests
{
    public sealed class ScenarioPresentationFeedbackTests
    {
        [Test]
        public void PlayerFootPivotIsCenteredWithoutMovingLogicalRootOrChangingSource()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var texture=new Texture2D(32,32);var source=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.1f),32);
            try
            {
                var go=new GameObject("grid aligned character",typeof(SpriteRenderer));SceneManager.MoveGameObjectToScene(go,scene);
                go.transform.position=new Vector3(10.5f,12.5f,0);var animator=go.AddComponent<DirectionalSpriteAnimator>();
                var frames=new[]{source,source,source};animator.Configure(frames,frames,frames,frames);animator.PresentFrame(0);
                var shown=go.GetComponent<SpriteRenderer>().sprite;
                Assert.That(shown.bounds.center.sqrMagnitude,Is.LessThan(.00001));
                Assert.AreSame(texture,shown.texture);Assert.AreSame(source,animator.StatusPicture);
                Assert.AreEqual(new Vector3(10.5f,12.5f,0),go.transform.position);
                Assert.That(source.pivot.y,Is.EqualTo(3.2f).Within(.001f));
                animator.PresentFrame(100);Assert.AreSame(shown,go.GetComponent<SpriteRenderer>().sprite);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(texture);}
        }
        [Test]
        public void WorldHudNeverDrawsCombatStatusEvenAfterRestoreOrMinimize()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("world status hidden");SceneManager.MoveGameObjectToScene(go,scene);
                var hud=go.AddComponent<WorldSessionHud>();
                var draw=typeof(WorldSessionHud).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
                foreach(var action in new Action[]{hud.RestoreStatus,hud.MinimizeStatus,hud.CloseStatus,hud.RestoreStatus})
                {
                    action();draw.Invoke(hud,null);
                    var ui=go.GetComponentInChildren<InGameUi>();Assert.IsNotNull(ui);
                    Assert.IsNull(ui.Find<UnityEngine.UI.RawImage>("world"));
                    Assert.IsNull(ui.Find<UnityEngine.UI.Button>("world-minimized"));
                    Assert.IsNull(ui.Find<UnityEngine.UI.Button>("world-inventory-open"));
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]
        public void CompletionModalKeepsMovementLockedWithoutAProgressRevisionChange()
        {
            var scene=EditorSceneManager.NewPreviewScene();float previousScale=Time.timeScale;
            try
            {
                Time.timeScale=1;
                var go=new GameObject("completion lock fixture");SceneManager.MoveGameObjectToScene(go,scene);
                var session=go.AddComponent<DarkSaver.Combat.BattleTransitionController>();
                var player=go.AddComponent<TopDownPlayerController>();
                var director=go.AddComponent<Scenario01Controller>();var protocol=go.AddComponent<ServerManager>();
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                void Set(string name,object value)=>typeof(Scenario01Controller).GetField(name,flags).SetValue(director,value);
                var state=new Scenario01Progress{phase=Scenario01Progress.Complete};
                Set("session",session);Set("player",player);Set("protocol",protocol);Set("progress",state);
                Set("revision",session.Scenario01Revision);Set("initialized",true);Set("completionResultPending",true);
                var receive=typeof(Scenario01Controller).GetMethod("ReceiveScenarioPacket",flags);
                using(var binding=protocol.RegisterRequestHandler<ScenarioCommandRequest>(request=>
                {
                    var response=new ScenarioStateNotification("01",session.Scenario01Revision,true,null,state);
                    receive.Invoke(director,new object[]{response});return response;
                }))
                {
                    var update=typeof(Scenario01Controller).GetMethod("Update",flags);
                    for(int frame=0;frame<3;frame++)
                    {
                        player.SetScenarioInputBlocked(false);update.Invoke(director,null);
                        Assert.IsTrue(player.UiInputBlocked,"completion modal frame "+frame);
                        Assert.IsTrue(director.CompletionResultPending);
                    }
                    Assert.IsTrue(director.AcknowledgeCompletionResult());
                    Assert.IsFalse(player.UiInputBlocked);Assert.IsFalse(director.AcknowledgeCompletionResult());
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);Time.timeScale=previousScale;}
        }
        [Test]
        public void ResultAggregateIsDetachedSavedAndNotAnAdditionalExperienceGrant()
        {
            var content=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/01/Content").text);
            var progress=Scenario01Progress.Create(content);progress.battleExperienceEarned=1234;
            Assert.IsTrue(progress.hasBattleExperienceLedger);
            Assert.IsFalse(new Scenario01Progress().hasBattleExperienceLedger);
            var copy=progress.Clone();copy.battleExperienceEarned=777;
            Assert.AreEqual(1234,progress.battleExperienceEarned);
            var loaded=JsonUtility.FromJson<Scenario01Progress>(JsonUtility.ToJson(progress));
            Assert.AreEqual(1234,loaded.battleExperienceEarned);Assert.IsTrue(loaded.Matches(content));
            Assert.IsTrue(loaded.hasBattleExperienceLedger);
            loaded.battleExperienceEarned=-1;Assert.IsFalse(loaded.Matches(content));
        }
        [Test]
        public void RoomListingDoesNotCallCompletedSavesRunningRooms()
        {
            Assert.IsFalse(ScenarioRoomRules.IsOngoing(null));
            foreach(int phase in new[]{Scenario01Progress.Intro,Scenario01Progress.Playing,Scenario01Progress.Outro})
                Assert.IsTrue(ScenarioRoomRules.IsOngoing(new Scenario01Progress{phase=phase}));
            Assert.IsFalse(ScenarioRoomRules.IsOngoing(new Scenario01Progress{phase=Scenario01Progress.Complete}));
            Assert.IsFalse(ScenarioRoomRules.IsOngoing(new Scenario01Progress{phase=-1}));
        }
    }
}
