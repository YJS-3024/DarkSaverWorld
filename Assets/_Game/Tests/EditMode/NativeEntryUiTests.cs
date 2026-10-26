using System;
using System.IO;
using System.Linq;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.Tests
{
    public sealed class NativeEntryUiTests
    {
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
                var root=go.GetComponentInChildren<UIDocument>().rootVisualElement;
                var background=Resources.Load<Texture2D>("UI/Entry/Background");
                Assert.IsNotNull(background);Assert.AreEqual(640,background.width);Assert.AreEqual(480,background.height);
                Assert.AreSame(background,root.Q("entry-background").style.backgroundImage.value.texture);
                Assert.IsNull(root.Q("menu"));Assert.IsNotNull(root.Q<Button>("new"));
                controller.StartGame();typeof(StartMenuController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                Assert.IsTrue(controller.IsLoggingIn);
                Assert.AreEqual(DisplayStyle.None,root.Q("new").style.display.value);
                Assert.AreSame(Resources.Load<Texture2D>("UI/Entry/LoginPanel"),root.Q("entry-login-panel").style.backgroundImage.value.texture);
                var nativePanel=root.Q("entry-login-panel");
                Assert.AreEqual(226,nativePanel.style.left.value.value);Assert.AreEqual(279,nativePanel.style.top.value.value);
                Assert.AreEqual(197,nativePanel.style.width.value.value);Assert.AreEqual(87,nativePanel.style.height.value.value);
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
