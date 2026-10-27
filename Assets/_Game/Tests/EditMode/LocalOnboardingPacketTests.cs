using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditorSceneManagement=UnityEditor.SceneManagement.EditorSceneManager;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Tests
{
    public sealed class LocalOnboardingPacketTests
    {
        [Test] public void SharedOnboardingServiceChecks()=>LocalOnboardingChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [Test] public void NewAccountSelectionTransfersToTutorialSessionAndSavesEachStep()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-onboarding-ui-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var scene=UnityEditorSceneManagement.NewPreviewScene();
            try
            {
                GameObject Object(string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
                var paths=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray();
                var menu=Object("entry fixture").AddComponent<StartMenuController>();menu.ConfigureLocalLogin(Path.Combine(directory,"history.json"),paths);menu.StartGame();
                Assert.IsTrue(menu.IsRegistering);Assert.IsFalse(menu.IsLoggingIn);
                Assert.IsTrue(menu.RegisterAccount("alpha_01","onboarding-password","onboarding-password"));
                Assert.IsTrue(menu.Login("alpha_01","onboarding-password"));Assert.IsTrue(menu.SelectServer(1));
                Assert.IsTrue(menu.SelectSlot(0));Assert.IsTrue(menu.BeginSelectedCharacterCreation());Assert.IsTrue(menu.SelectStartingJob(103));
                Assert.IsTrue(menu.GetComponent<ServerManager>().TryRequest(new CharacterCreateRequest("NewMage",menu.SelectedStartingJob,paths[0]),out _));
                menu.CancelCharacterCreation();Assert.IsTrue(menu.TryPrepareCharacterEntry(paths[0],out _));
                Assert.IsFalse(menu.TryPrepareCharacterEntry(paths[0],out _));
                Assert.IsTrue(NativeSaveStore.TakeResume(out var state,out var path));Assert.AreEqual(paths[0],path);
                Assert.AreEqual(103,state.character.jobId);Assert.AreEqual(0,state.character.tutorial.step);
                Assert.AreEqual((21f,21f),(state.worldX,state.worldY));
                var session=Object("gameplay session fixture").AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.RestoreSaveState(state));session.ConfigureSaveFile(path);
                var player=Object("tutorial player fixture").AddComponent<TopDownPlayerController>();
                FirstPlayTutorial.Ensure(session,player);var tutorial=session.GetComponent<FirstPlayTutorial>();
                Assert.IsNotNull(tutorial);Assert.IsTrue(player.UiInputBlocked);Assert.IsTrue(session.HasPendingTutorial);
                Assert.IsFalse(tutorial.Advance(0)); // EditMode is not a played tutorial scene.
                Assert.IsTrue(session.TrySaveTutorial(0,false,false,true,out _));
                var script=new DarkSaver.Events.OriginalTutorialScript(LocalOnboardingChecks.Source,103);
                int guard=0;
                while(session.HasPendingTutorial && guard++<400)
                {
                    var current=session.CaptureCharacterProfile().tutorial;
                    Assert.IsTrue(session.TryTutorialInput(LocalOnboardingChecks.RequiredInput(current,script),out var reason),reason);
                }
                Assert.IsFalse(session.HasPendingTutorial);Assert.Less(guard,400);
                Assert.IsTrue(new NativeSaveStore(path).TryLoad(out var completed,out _));Assert.IsTrue(completed.character.tutorial.IsComplete);
                Assert.IsFalse(completed.hasInventory);Assert.IsFalse(completed.hasVitals);
            }
            finally {NativeSaveStore.ClearResume();UnityEditorSceneManagement.ClosePreviewScene(scene);Directory.Delete(directory,true);}
        }
        [Test] public void TutorialManagerOwnsScopedHandlerAndRejectsStaleOrDisabledRequests()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-tutorial-manager-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var scene=UnityEditorSceneManagement.NewPreviewScene();
            try
            {
                string path=Path.Combine(directory,"slot.json");
                var profile=new NativeCharacterProfile {name="Fixture",jobId=100,tutorial=new NativeTutorialProgress()};
                Assert.IsTrue(new NativeSaveStore(path).TryCreateNew(new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,
                    mapScene="Kaosia_Map",runtimeScene="Town_Runtime",clearedScenarios=Array.Empty<string>(),hasCharacter=true,character=profile},out _));
                var go=new GameObject("tutorial server fixture");SceneManager.MoveGameObjectToScene(go,scene);var server=go.AddComponent<ServerManager>();
                server.BindLocalTutorialService(path,profile);server.BindLocalTutorialService(path,profile);
                Assert.Throws<InvalidOperationException>(()=>server.BindLocalTutorialService(path+"other",profile));
                int count=0;server.ResponseReceived+=p=>{Assert.IsInstanceOf<TutorialProgressResponse>(p);count++;};
                Assert.IsTrue(server.TryRequest(new TutorialProgressRequest(0,false,false,true),out var first));
                int cursor=((TutorialProgressResponse)first).CaptureState().character.tutorial.step;Assert.Greater(cursor,4);
                Assert.IsFalse(server.TryRequest(new TutorialProgressRequest(0,false,false,false),out _));Assert.AreEqual(2,count);
                server.enabled=false;Assert.IsFalse(server.TryRequest(new TutorialProgressRequest(1,false,false,false),out _));Assert.AreEqual(2,count);
                Assert.IsTrue(new NativeSaveStore(path).TryLoad(out var saved,out _));Assert.AreEqual(cursor,saved.character.tutorial.step);
            }
            finally {UnityEditorSceneManagement.ClosePreviewScene(scene);Directory.Delete(directory,true);}
        }
    }
}
