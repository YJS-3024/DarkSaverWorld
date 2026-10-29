using System;
using System.Reflection;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace DarkSaver.Tests
{
    public sealed class WorldFunctionIntegrationTests
    {
        [Test] public void F1ReusesBattleHelpArtworkAndControls()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("world help reuse");SceneManager.MoveGameObjectToScene(go,scene);var ui=go.AddComponent<NativeWorldWindows>();
                var window=(NativeBattleWindows.Window)typeof(NativeWorldWindows).GetMethod("WindowFor",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,new object[]{19});
                var original=Resources.Load<NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows").Find(7);
                Assert.AreSame(original,window);Assert.AreEqual(new Vector2Int(614,452),window.size);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void LocalManagerPublishesOneChatReceiptAndRejectsInactiveContext()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("world text server");SceneManager.MoveGameObjectToScene(go,scene);var server=go.AddComponent<ServerManager>();
                bool ready=true;Func<WorldTextContext> context=()=>new WorldTextContext("검증",ready);server.BindLocalWorldTextService(context);server.BindLocalWorldTextService(context);
                var packets=new List<ServerPacket>();server.ResponseReceived+=packets.Add;
                Assert.IsTrue(server.TryRequest(new Request_WorldText("안녕G"),out _));Assert.AreEqual(1,server.OriginalFrameCount);
                Assert.AreEqual(2,packets.Count);Assert.IsInstanceOf<Notify_WorldText>(packets[0]);Assert.IsInstanceOf<Response_WorldText>(packets[1]);
                ready=false;Assert.IsFalse(server.TryRequest(new Request_WorldText("안녕"),out _));Assert.AreEqual(3,packets.Count);
                server.UnbindLocalWorldTextService(context);Assert.IsFalse(server.TryRequest(new Request_WorldText("해제 후"),out _));
                Assert.IsFalse(server.LocalServer.HasHandler(new Request_WorldWhisperSetting(true)));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void SelectingRoleChangesBothPersonalWindowsAndOriginalRequestsNeverChangeThePrimarySave()
        {
            var active=SceneManager.GetActiveScene();var dirty=active.isDirty;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);scene.name="Town_Runtime";
            try
            {
                var go=new GameObject("role selection fixture");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<WorldSessionHud>();
                var session=go.GetComponent<BattleTransitionController>();var primary=new NativeCharacterProfile{name="본인",jobId=100};
                typeof(BattleTransitionController).GetField("character",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(session,primary);
                var panel=go.GetComponent<TownServicesPanel>() ?? go.AddComponent<TownServicesPanel>();var server=go.GetComponent<ServerManager>() ?? go.AddComponent<ServerManager>();
                var playerGo=new GameObject("role fixture actor");SceneManager.MoveGameObjectToScene(playerGo,scene);playerGo.AddComponent<TopDownPlayerController>();
                var ui=go.GetComponent<NativeWorldWindows>() ?? go.AddComponent<NativeWorldWindows>();
                var other=new NativeCharacterProfile{name="보유캐릭터",jobId=103,equipmentItemIds=new[]{0,0,321,0,0,0,0}};
                var packet=new Notify_WorldRoster(new[]{new WorldRosterEntry{RecordId=100,Name="본인",JobId=100,Profile=primary},new WorldRosterEntry{RecordId=200,Name="보유캐릭터",JobId=103,JobName="매지션",Profile=other,OriginalFlag=1,MaxHp=180,MaxMp=65}});
                server.UnbindLocalWorldCharacters(ui);
                WorldCharacterData[] saved=null;
                var authority=new WorldCharacterService(()=>ui.IsAvailable,packet.Capture,()=>saved,(WorldCharacterData[] data,out string reason)=>{saved=WorldCharacterData.Copy(data);reason="";return true;},p=>server.SendToClient(p));
                server.BindLocalWorldCharacters(this,authority);
                Assert.IsTrue(ui.BindWorldRoster(packet));Assert.IsTrue(ui.SelectWorldRole(1));Assert.AreEqual(200,ui.SelectedWorldRecordId);
                Assert.IsTrue(ui.HandleFunctionKey(3));Assert.AreEqual("보유캐릭터",panel.CharacterInfoPlayerName);Assert.AreEqual(200,panel.InspectedWorldRecordId);
                Assert.IsTrue(ui.HandleFunctionKey(4));Assert.AreEqual(200,panel.InspectedWorldRecordId);
                EquipmentRequest requested=null;using(server.RegisterRequestHandler<EquipmentRequest>(p=>{requested=p;return new LocalCommandResponse(false,"거부");}))
                {Assert.IsFalse(panel.RequestOriginalEquipment(2,-1,0));Assert.AreEqual(200,requested.UnitRecordId);Assert.AreEqual(321,requested.PreviousItemId);Assert.AreEqual(ushort.MaxValue,requested.ItemId);}
                Assert.AreEqual(321,ui.SelectedWorldUnit.Profile.EquipmentItemAt(2));Assert.AreEqual("본인",session.PlayerName);Assert.AreEqual(100,session.PlayerJobId);
                panel.Close();Assert.IsTrue(ui.SetWorldRoleFlag(1));Assert.AreEqual(2,saved[1].Flag);Assert.AreEqual(2,ui.SelectedWorldUnit.OriginalFlag);
                Assert.IsTrue(ui.ReorderWorldRoles(1,0));Assert.AreEqual(200,saved[0].RecordId);
                Assert.AreEqual(200,ui.SelectedWorldRecordId);Assert.IsTrue(ui.SelectWorldRole(0));Assert.AreEqual(200,ui.SelectedWorldRecordId);
                Assert.AreEqual("본인",session.PlayerName);Assert.AreEqual(100,session.PlayerJobId);
            }
            finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);}
            Assert.AreEqual(dirty,active.isDirty);
        }
        [Test] public void CharacterManagerFramesRequestsPublishesReceiptsAndRollsBackRegistrationConflicts()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("character authority manager");SceneManager.MoveGameObjectToScene(go,scene);var server=go.AddComponent<ServerManager>();
                WorldCharacterData[] saved=null;var received=new List<ServerPacket>();server.ResponseReceived+=received.Add;
                var service=new WorldCharacterService(()=>true,()=>new[]{new WorldRosterEntry{RecordId=100,JobId=100,Name="본인"},new WorldRosterEntry{RecordId=200,JobId=103,Name="동료"}},()=>saved,(WorldCharacterData[] data,out string reason)=>{saved=WorldCharacterData.Copy(data);reason="";return true;},p=>server.SendToClient(p));
                using(server.RegisterRequestHandler<CharacterAdjustmentClassRequest>(p=>new LocalCommandResponse(false)))
                {
                    Assert.Throws<InvalidOperationException>(()=>server.BindLocalWorldCharacters(this,service));
                    Assert.IsFalse(server.LocalServer.HasHandler(new Request_WorldCharacters()));
                    Assert.IsFalse(server.LocalServer.HasHandler(new Request_WorldRosterFlag(200,1)));
                    Assert.IsTrue(server.LocalServer.HasHandler(new CharacterAdjustmentClassRequest(200,103)));
                }
                server.BindLocalWorldCharacters(this,service);server.BindLocalWorldCharacters(this,service);
                Assert.IsTrue(server.TryRequest(new Request_WorldRosterFlag(200,1),out _));Assert.AreEqual(1,server.OriginalFrameCount);
                Assert.AreEqual(2,received.Count);Assert.IsInstanceOf<Notify_WorldRoster>(received[0]);Assert.IsInstanceOf<LocalCommandResponse>(received[1]);Assert.AreEqual(1,saved[1].Flag);
                server.UnbindLocalWorldCharacters(new object());Assert.NotNull(server.LocalWorldCharacters);
                server.UnbindLocalWorldCharacters(this);Assert.IsNull(server.LocalWorldCharacters);Assert.IsFalse(server.TryRequest(new Request_WorldRosterFlag(200,2),out _));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void OriginalFontSelectorCyclesFourModesAndAlternateBitmapIsASeparateAtlas()
        {
            const string key="DarkSaver.WorldUI.font";bool had=PlayerPrefs.HasKey(key);int old=PlayerPrefs.GetInt(key);
            try
            {
                PlayerPrefs.SetInt(key,0);
                var basic=NativeStatusFont.WorldGlyph('가',0);var alternate=NativeStatusFont.WorldGlyph('가',1);Assert.NotNull(basic);Assert.NotNull(alternate);Assert.AreNotSame(basic.texture,alternate.texture);
                foreach(string name in new[]{"dark14","dark14-1","굴림","솔체"}){Assert.AreEqual(name,NativeWorldPreferences.FontName);NativeWorldPreferences.CycleFont();}
                Assert.AreEqual(0,NativeWorldPreferences.FontMode);
            }
            finally{if(had)PlayerPrefs.SetInt(key,old);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
    }
}
