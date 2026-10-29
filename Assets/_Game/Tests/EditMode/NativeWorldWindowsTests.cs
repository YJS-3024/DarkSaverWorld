using System.Reflection;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DarkSaver.Tests
{
    public sealed class NativeWorldWindowsTests
    {
        private static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        [TestCase(2,263,363,18)] [TestCase(3,594,84,5)] [TestCase(15,146,199,8)]
        [TestCase(19,628,466,3)] [TestCase(27,285,83,2)] [TestCase(42,255,282,11)] [TestCase(303,594,377,5)]
        public void OriginalWorldLayoutsHaveArtworkAndThreeDistinctStateReferences(int id,int width,int height,int controls)
        {
            var catalog=Resources.Load<NativeBattleWindows>("UI/WorldWindows/NativeWorldWindows");Assert.NotNull(catalog);
            var window=catalog.Find(id);Assert.NotNull(window);Assert.AreEqual(new Vector2Int(width,height),window.size);
            Assert.AreEqual(width,window.background.width);Assert.AreEqual(height,window.background.height);
            Assert.AreEqual(controls,window.controls.Length);
            foreach(var c in window.controls)
            {
                Assert.AreEqual(3,c.frames.Length);
                foreach(var frame in c.frames){Assert.NotNull(frame);Assert.AreEqual(c.rect.width,frame.width);Assert.AreEqual(c.rect.height,frame.height);}
                Assert.AreNotSame(c.frames[0],c.frames[1]);Assert.AreNotSame(c.frames[1],c.frames[2]);
            }
        }
        [Test]
        public void NativeWorldShortcutsToggleMinimizeRestoreAndRejectUnsupportedOrMissingServer()
        {
            var active=SceneManager.GetActiveScene();var dirty=active.isDirty;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);scene.name="Town_Runtime";
            try
            {
                var root=new GameObject("isolated world UI");SceneManager.MoveGameObjectToScene(root,scene);
                var hud=root.AddComponent<WorldSessionHud>();var session=root.GetComponent<BattleTransitionController>();
                typeof(BattleTransitionController).GetField("character",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(session,new NativeCharacterProfile{name="검증",jobId=100});
                var playerGo=new GameObject("fixture player");SceneManager.MoveGameObjectToScene(playerGo,scene);var player=playerGo.AddComponent<TopDownPlayerController>();
                var panel=root.GetComponent<TownServicesPanel>() ?? root.AddComponent<TownServicesPanel>();
                var server=root.AddComponent<ServerManager>();var windows=root.GetComponent<NativeWorldWindows>() ?? root.AddComponent<NativeWorldWindows>();
                Assert.IsTrue(windows.IsAvailable);
                foreach(var pair in new[]{(1,19),(2,2),(5,3),(8,15)})
                {
                    Assert.IsTrue(windows.HandleFunctionKey(pair.Item1));Assert.IsTrue(windows.IsWindowOpen(pair.Item2));
                    Assert.IsTrue(windows.HandleFunctionKey(pair.Item1));Assert.IsFalse(windows.IsWindowOpen(pair.Item2));
                }
                Assert.IsTrue(windows.HandleFunctionKey(2));Assert.IsTrue(windows.Minimize(2));
                Assert.IsTrue(windows.HandleFunctionKey(2));Assert.IsTrue(windows.IsWindowOpen(2));Assert.IsFalse(windows.IsWindowMinimized(2));
                foreach(int key in new[]{0,6,7,10,11,13})Assert.IsFalse(windows.HandleFunctionKey(key));
                Assert.IsFalse(windows.HandleFunctionKey(12));Assert.That(windows.LastMessage,Does.Contain("명령"));
                int requests=0;server.ConfigureLocalWorldCommandHandler((p,c)=>{requests++;Assert.AreEqual("/공간이동",p.Text);return new Response_WorldText(false,"허용되지 않은 이동");});
                Assert.IsFalse(windows.HandleFunctionKey(12));Assert.AreEqual(1,requests);Assert.AreEqual("허용되지 않은 이동",windows.LastMessage);
                Assert.IsTrue(windows.HandleFunctionKey(9));Assert.IsTrue(player.UiInputBlocked);Assert.IsTrue(windows.ExitConfirmationOpen);
                Assert.IsFalse(windows.HandleFunctionKey(2));windows.CloseWindow(27);Assert.IsFalse(player.UiInputBlocked);
                player.SetScenarioInputBlocked(true);Assert.IsFalse(windows.HandleFunctionKey(9));Assert.IsTrue(player.UiInputBlocked);player.SetScenarioInputBlocked(false);
                Assert.IsTrue(windows.HandleFunctionKey(3));Assert.AreEqual(TownService.CharacterInfo,root.GetComponent<TownServicesPanel>().Service);
                Assert.IsTrue(windows.HandleFunctionKey(4));Assert.AreEqual(TownService.Inventory,root.GetComponent<TownServicesPanel>().Service);
                Assert.IsTrue(windows.HandleFunctionKey(4));Assert.IsFalse(root.GetComponent<TownServicesPanel>().IsOpen);Assert.IsFalse(player.UiInputBlocked);
                windows.CloseWindow(3);Assert.IsTrue(windows.HandleFunctionKey(5));windows.ToggleChatExpansion();
                Assert.IsTrue(windows.IsWindowOpen(3));windows.ToggleChatExpansion();
                Assert.IsFalse(windows.SubmitText("\n"));Assert.IsFalse(windows.SubmitText(new string('a',256)));
                Assert.IsTrue(windows.HandleFunctionKey(9));windows.enabled=false;Assert.IsFalse(player.UiInputBlocked);
            }
            finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);}
            Assert.AreEqual(dirty,active.isDirty);
        }
        [Test]
        public void RepeatedNativePaintingKeepsWindowBackgroundBehindControlsAndChatInputBlack()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("world window paint fixture");SceneManager.MoveGameObjectToScene(root,scene);
                var window=root.AddComponent<NativeWorldWindows>();InGameUi ui=null;InGameUi.Ensure(ref ui,window);
                typeof(NativeWorldWindows).GetField("ui",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,ui);
                typeof(NativeWorldWindows).GetField("catalog",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,Resources.Load<NativeBattleWindows>("UI/WorldWindows/NativeWorldWindows"));
                for(int frame=0;frame<3;frame++)
                {
                    ui.Begin();Call(window,"DrawWindow",3);Call(window,"DrawWindow",15);Call(window,"DrawWindow",19);ui.End();
                    Assert.AreEqual(new Rect(10,458,489,16),ui.GetRect("world-chat-input"));Assert.AreEqual(Color.black,ui.Find<RawImage>("world-chat-input").color);
                    foreach(int id in new[]{3,15,19})Assert.Greater(ui.Find<Button>("world-"+id+"-control-2").transform.GetSiblingIndex(),ui.Find<RawImage>("world-"+id+"-background").transform.GetSiblingIndex());
                    Assert.NotNull(ui.Find<InputField>("world-chat-input"));
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]
        public void NotificationChannelsRespectWorldPreferencesAndUnsubscribeWhenDisabled()
        {
            const string all="DarkSaver.WorldUI.all-chat",whisper="DarkSaver.WorldUI.whisper";
            bool hadAll=PlayerPrefs.HasKey(all),hadWhisper=PlayerPrefs.HasKey(whisper);int a=PlayerPrefs.GetInt(all),b=PlayerPrefs.GetInt(whisper);
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("isolated text receipt");SceneManager.MoveGameObjectToScene(root,scene);
                var server=root.AddComponent<ServerManager>();var window=root.AddComponent<NativeWorldWindows>();Call(window,"BindServer");
                NativeWorldPreferences.Set(2,false);NativeWorldPreferences.Set(3,false);
                server.SendToClient(new Notify_WorldText("전체",WorldTextChannel.General));Assert.IsNull(window.LastMessage);
                server.SendToClient(new Notify_WorldText("귓속말",WorldTextChannel.Whisper));Assert.IsNull(window.LastMessage);
                server.SendToClient(new Notify_WorldText("시스템"));Assert.AreEqual("시스템",window.LastMessage);
                NativeWorldPreferences.Set(2,true);server.SendToClient(new Notify_WorldText("대화",WorldTextChannel.General));Assert.AreEqual("대화",window.LastMessage);
                window.enabled=false;server.SendToClient(new Notify_WorldText("종료 후"));Assert.AreEqual("대화",window.LastMessage);
            }
            finally
            {
                if(hadAll)PlayerPrefs.SetInt(all,a);else PlayerPrefs.DeleteKey(all);
                if(hadWhisper)PlayerPrefs.SetInt(whisper,b);else PlayerPrefs.DeleteKey(whisper);PlayerPrefs.Save();
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
