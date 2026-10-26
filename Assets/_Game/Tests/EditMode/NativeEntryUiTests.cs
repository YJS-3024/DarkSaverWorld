using System;
using System.IO;
using System.Linq;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.Tests
{
    public sealed class NativeEntryUiTests
    {
        [Test]
        public void RegistrationAndAccountSwitchReturnToIndependentCharacterSelection()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-menu-multi-account-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var root=new GameObject("multi-account menu");
            try
            {
                var menu=root.AddComponent<StartMenuController>();
                Assert.IsTrue(menu.ConfigureLocalLogin(Path.Combine(directory,"history.json"),Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray()));
                menu.StartGame();Assert.IsTrue(menu.OpenRegistration());Assert.IsTrue(menu.RegisterAccount("alpha_01","alpha-fixture-pass","alpha-fixture-pass"));
                Assert.IsTrue(menu.Login("ALPHA_01","alpha-fixture-pass"));Assert.AreEqual("alpha_01",menu.CurrentAccountId);Assert.IsTrue(menu.IsSelectingCharacter);
                menu.StartGame();Assert.IsNull(menu.CurrentAccountId);Assert.IsFalse(menu.IsSelectingCharacter);
                Assert.IsTrue(menu.OpenRegistration());Assert.IsTrue(menu.RegisterAccount("beta_02","beta-fixture-pass","beta-fixture-pass"));
                Assert.IsTrue(menu.Login("beta_02","beta-fixture-pass"));Assert.AreEqual("beta_02",menu.CurrentAccountId);Assert.IsTrue(menu.SelectSlot(9));
                menu.StartGame();Assert.IsFalse(menu.Login("alpha_01","beta-fixture-pass"));Assert.IsNull(menu.CurrentAccountId);Assert.IsFalse(menu.IsSelectingCharacter);
                Assert.IsTrue(menu.Login("alpha_01","alpha-fixture-pass"));Assert.AreEqual("alpha_01",menu.CurrentAccountId);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);Directory.Delete(directory,true);}
        }
        [Test]
        public void ExistingAccountDoesNotDisableRegistrationNavigationOrAllowOverwrite()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-registration-navigation-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var go=new GameObject("existing account navigation");
            try
            {
                string accountPath=Path.Combine(directory,"native-account.json");
                var account=new DarkSaver.Core.NativeAccountStore(accountPath);
                Assert.IsTrue(account.TryRegister("existing_user","fixture-password","fixture-password",out var reason),reason);
                var preserved=File.ReadAllBytes(accountPath);
                var controller=go.AddComponent<StartMenuController>();
                Assert.IsTrue(controller.ConfigureLocalLogin(Path.Combine(directory,"history.json"),Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray()));
                controller.StartGame();
                var draw=typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                draw.Invoke(controller,null);
                var ui=go.GetComponentInChildren<InGameUi>();
                var button=ui.Find<UnityEngine.UI.Button>("open-register");Assert.IsTrue(button.interactable);
                button.onClick.Invoke();Assert.IsTrue(controller.IsRegistering);draw.Invoke(controller,null);
                Assert.That(ui.Find<UnityEngine.UI.Text>("register-message").text,Does.Contain("계정마다 캐릭터 10슬롯"));
                Assert.IsFalse(controller.RegisterAccount("EXISTING_USER","other-fixture-pass","other-fixture-pass"));
                CollectionAssert.AreEqual(preserved,File.ReadAllBytes(accountPath));
                controller.CancelRegistration();Assert.IsTrue(controller.IsLoggingIn);
                Assert.IsFalse(File.Exists(Path.Combine(directory,"history.json")));
                Assert.IsEmpty(Directory.GetFiles(directory,"slot*.json"));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);Directory.Delete(directory,true);}
        }
        [Test]
        public void NativeEntryArtworkKeepsPasswordMaskingAndRegistrationNavigation()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-entry-ui-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var go=new GameObject("isolated native entry");
            try
            {
                var controller=go.AddComponent<StartMenuController>();
                Assert.IsTrue(controller.ConfigureLocalLogin(Path.Combine(directory,"history.json"),Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray()));
                typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                var root=CanvasProbe.For(go.GetComponentInChildren<InGameUi>());
                var background=Resources.Load<Texture2D>("UI/Entry/Background");
                Assert.IsNotNull(background);Assert.AreEqual(640,background.width);Assert.AreEqual(480,background.height);
                Assert.AreSame(background,root.Q("entry-background").style.backgroundImage.value.texture);
                Assert.IsNull(root.Q("menu"));Assert.IsNull(root.Q<Button>("new"));Assert.IsTrue(controller.IsLoggingIn);
                controller.StartGame();typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                Assert.IsTrue(controller.IsLoggingIn);
                Assert.IsNull(root.Q("new"));Assert.IsNull(root.Q("entry-login-action-fill"));Assert.IsNull(root.Q("login-back"));
                Assert.IsNotNull(root.Q<Button>("login-options"));Assert.IsNotNull(root.Q<Button>("quit"));
                Assert.AreSame(Resources.Load<Texture2D>("UI/Entry/LoginPanel"),root.Q("entry-login-panel").style.backgroundImage.value.texture);
                var nativePanel=root.Q("entry-login-panel");
                Assert.AreEqual(226,nativePanel.style.left.value.value);Assert.AreEqual(279,nativePanel.style.top.value.value);
                Assert.AreEqual(197,nativePanel.style.width.value.value);Assert.AreEqual(87,nativePanel.style.height.value.value);
                var canvasUi=go.GetComponentInChildren<InGameUi>();
                Assert.AreEqual(new Rect(239,339,55,19),canvasUi.GetRect("login"));
                Assert.AreEqual(new Rect(294,339,38,19),canvasUi.GetRect("open-register"));
                Assert.AreEqual(new Rect(334,339,38,19),canvasUi.GetRect("login-options"));
                Assert.AreEqual(new Rect(374,339,38,19),canvasUi.GetRect("quit"));
                Assert.AreEqual(new Rect(298,296,110,20),canvasUi.GetRect("login-id"));
                Assert.AreEqual(new Rect(298,320,110,18),canvasUi.GetRect("login-password"));
                Assert.AreEqual(Color.clear,canvasUi.Find<UnityEngine.UI.RawImage>("login-id").color);
                canvasUi.Find<UnityEngine.UI.Button>("login-options").onClick.Invoke();
                typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                Assert.AreEqual(new Rect(226,360,197,45),canvasUi.GetRect("entry-options"));
                Assert.IsTrue(canvasUi.Find<UnityEngine.UI.RawImage>("entry-options").gameObject.activeSelf);
                canvasUi.Find<UnityEngine.UI.Button>("login-options").onClick.Invoke();
                typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                Assert.IsFalse(canvasUi.Find<UnityEngine.UI.RawImage>("entry-options").gameObject.activeSelf);
                var password=root.Q<TextField>("login-password");Assert.IsTrue(password.isPasswordField);Assert.AreEqual('*',password.maskChar);Assert.AreEqual(64,password.maxLength);
                Assert.AreEqual(20,root.Q<TextField>("login-id").maxLength);
                Assert.IsTrue(root.Q<Button>("open-register").enabledSelf);
                Assert.IsTrue(controller.OpenRegistration());typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                Assert.AreEqual(DisplayStyle.None,password.style.display.value);Assert.IsNotNull(root.Q<TextField>("register-id"));
                controller.CancelRegistration();typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                Assert.IsTrue(controller.IsLoggingIn);Assert.AreEqual(DisplayStyle.Flex,password.style.display.value);
                Assert.IsTrue(controller.Login());Assert.IsTrue(controller.BeginSelectedCharacterCreation());
                Assert.IsTrue(controller.SelectStartingJob(101));Assert.IsTrue(controller.SelectStartingJob(103));
                Assert.IsFalse(controller.SelectStartingJob(102));Assert.AreEqual(103,controller.SelectedStartingJob);
                typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                var logical=root.Q("LogicalScreen");var creation=root.Q("creation-native");
                Assert.IsNotNull(creation);Assert.AreSame(Resources.Load<Texture2D>("UI/Entry/CharacterCreation"),creation.style.backgroundImage.value.texture);
                foreach(int job in new[]{100,101,103})
                {
                    var face=root.Q("creation-face-"+job);Assert.IsNotNull(face);
                    Assert.Greater(logical.Children().ToList().IndexOf(face),logical.Children().ToList().IndexOf(creation),"face stays above reused background after redraw");
                    Assert.IsNull(root.Q("creation-job-"+job).style.backgroundImage.value.texture,"hit area cannot cover original art with paper");
                }
                Assert.IsEmpty(Directory.GetFiles(directory,"*",SearchOption.AllDirectories),"drawing/navigation must not create or overwrite saves");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);Directory.Delete(directory,true);}
        }
    }
}
