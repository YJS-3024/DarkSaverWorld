using System;
using System.IO;
using System.Linq;
using DarkSaver.Network;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Tests
{
    public sealed class LocalAccountPacketTests
    {
        [Test] public void LocalAuthenticationServiceChecks()=>LocalAuthenticationChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [Test] public void LocalServerLobbyChecks()=>LocalServerSelectionChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        private static void WithServer(Action<GameObject,string,string[]> test)
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-auth-manager-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("local account manager fixture");SceneManager.MoveGameObjectToScene(go,scene);
                test(go,Path.Combine(directory,"history.json"),Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray());
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);Directory.Delete(directory,true);}
        }
        [Test] public void ServerManagerPublishesOneTypedResponseAndOwnsBindings()
        {
            WithServer((go,history,slots)=>{
                var server=go.AddComponent<ServerManager>();var service=server.BindLocalAccountService(history,slots);
                Assert.AreSame(service,server.BindLocalAccountService(history,slots));
                Assert.Throws<InvalidOperationException>(()=>server.BindLocalAccountService(history+"other",slots));
                int responses=0;ServerPacket last=null;server.ResponseReceived+=p=>{responses++;last=p;};
                Assert.IsTrue(server.TryRequest(new AccountRegisterRequest("alpha_01","auth-password-a","auth-password-a"),out var registered));
                Assert.IsInstanceOf<AccountRegisterResponse>(registered);Assert.AreSame(registered,last);Assert.AreEqual(1,responses);
                Assert.IsTrue(server.TryRequest(new LoginRequest("alpha_01","auth-password-a"),out var logged));
                Assert.IsTrue(((LoginResponse)logged).Authenticated);Assert.AreSame(logged,last);Assert.AreEqual(2,responses);
                Assert.IsFalse(server.TryRequest(new LoginRequest("alpha_01","wrong-password"),out var rejected));
                Assert.IsFalse(((LoginResponse)rejected).Authenticated);Assert.AreSame(rejected,last);Assert.AreEqual(3,responses);
                server.enabled=false;
                Assert.IsFalse(server.TryRequest(new AccountRegisterRequest("beta_02","auth-password-b","auth-password-b"),out _));
                Assert.AreEqual(3,responses);
            });
        }
        [Test] public void EntrySignupAndLoginUseManagerAndStopBeforeCharacterOrWorldEntry()
        {
            WithServer((go,history,slots)=>{
                var menu=go.AddComponent<StartMenuController>();Assert.IsTrue(menu.ConfigureLocalLogin(history,slots));menu.StartGame();
                Assert.IsTrue(menu.OpenRegistration());
                Assert.IsFalse(menu.RegisterAccount("alpha_01","auth-password-a","mismatch"));Assert.IsTrue(menu.IsRegistering);
                Assert.IsTrue(menu.RegisterAccount("alpha_01","auth-password-a","auth-password-a"));
                Assert.IsTrue(menu.IsLoggingIn);Assert.IsFalse(menu.IsRegistering);
                Assert.IsTrue(go.GetComponent<ServerManager>().HasLocalAccountService);
                Assert.IsTrue(menu.Login("alpha_01","auth-password-a"));
                Assert.AreEqual("alpha_01",menu.CurrentAccountId);Assert.IsTrue(menu.IsAwaitingOriginalServerSelection);
                Assert.IsFalse(menu.IsSelectingCharacter);Assert.IsFalse(menu.IsCreatingCharacter);
                Assert.IsFalse(File.Exists(history));Assert.IsFalse(File.Exists(slots[0]));
                menu.StartGame();
                Assert.IsFalse(menu.Login("alpha_01","wrong-password"));Assert.IsNull(menu.CurrentAccountId);
                Assert.IsFalse(menu.IsAwaitingOriginalServerSelection);
            });
        }
        [Test] public void ServerSelectionAdvancesOnlyAfterManagerApprovalAndCharacterList()
        {
            WithServer((go,history,slots)=>{
                var menu=go.AddComponent<StartMenuController>();menu.ConfigureLocalLogin(history,slots);menu.StartGame();
                menu.OpenRegistration();Assert.IsTrue(menu.RegisterAccount("alpha_01","auth-password-a","auth-password-a"));
                Assert.IsTrue(menu.Login("alpha_01","auth-password-a"));
                Assert.AreEqual(1,menu.DisplayedServerCount);Assert.IsTrue(menu.IsAwaitingOriginalServerSelection);
                Assert.IsFalse(menu.IsLoggingIn);Assert.IsFalse(menu.SelectServer(999));Assert.IsFalse(menu.IsSelectingCharacter);
                var server=go.GetComponent<ServerManager>();server.enabled=false;
                Assert.IsFalse(menu.SelectServer(1));Assert.IsTrue(menu.IsAwaitingOriginalServerSelection);Assert.AreEqual(0,menu.ConnectedServerId);
                server.enabled=true;Assert.IsTrue(menu.ChooseServer(1));Assert.IsTrue(menu.IsAwaitingOriginalServerSelection);
                Assert.IsTrue(menu.SelectServer(1));Assert.AreEqual(1,menu.ConnectedServerId);
                Assert.IsFalse(menu.IsAwaitingOriginalServerSelection);Assert.IsTrue(menu.IsSelectingCharacter);
                Assert.IsFalse(menu.IsCreatingCharacter);Assert.IsFalse(menu.SelectServer(1));
                Assert.IsTrue(menu.SelectSlot(9));Assert.IsFalse(File.Exists(slots[0]));Assert.IsFalse(File.Exists(history));
                Assert.IsTrue(menu.BeginSelectedCharacterCreation());menu.CancelCharacterCreation();
                Assert.IsFalse(menu.ContinueGame());Assert.IsFalse(menu.CreateCharacter("test"));
                menu.StartGame();Assert.AreEqual(0,menu.DisplayedServerCount);Assert.AreEqual(0,menu.ConnectedServerId);Assert.IsNull(menu.CurrentAccountId);
            });
        }
    }
}
