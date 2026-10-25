using System.IO;
using DarkSaver.Core;
using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class LocalServerExecutionTests
    {
        [Test]
        public void RegistrationPreservesExistingSaveAndRequiresAuthenticationBeforeResume()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-membership-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var paths=new[]{Path.Combine(directory,"one.json"),Path.Combine(directory,"two.json"),Path.Combine(directory,"three.json")};
                var store=new NativeSaveStore(paths[0]);
                Assert.That(store.TrySave(new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene="Kaosia_Map",runtimeScene="Town_Runtime",
                    worldX=21,worldY=21,hasCharacter=true,character=new NativeCharacterProfile {name="기존인물",jobId=100},clearedScenarios=Array.Empty<string>()},out _),Is.True);
                string before=File.ReadAllText(paths[0]);var server=new LocalCharacterServer(Path.Combine(directory,"login.json"),paths);
                Assert.That(server.Register(new AccountRegisterRequest("hero_01","sample-password!","sample-password!")).Accepted,Is.True);
                Assert.That(File.ReadAllText(paths[0]),Is.EqualTo(before));
                Assert.That(server.Login(new LoginRequest()).Accepted,Is.False);
                Assert.That(server.Login(new LoginRequest("hero_01","wrong-password!")).Accepted,Is.False);
                Assert.That(server.List(new CharacterListRequest()).Accepted,Is.False);
                var login=(LoginResponse)server.Login(new LoginRequest("hero_01","sample-password!"));
                Assert.That(login.Accepted,Is.True);Assert.That(login.Authenticated,Is.True);Assert.That(login.RequiresCharacterSelection,Is.True);
                Assert.That(login.ResumePath,Is.Null);
                Assert.That(server.Select(new CharacterSelectRequest(paths[0])).Accepted,Is.True);
                Assert.That(NativeSaveStore.TakeResume(out var saved,out _),Is.True);Assert.That(saved.worldX,Is.EqualTo(21));Assert.That(saved.worldY,Is.EqualTo(21));
            }
            finally{NativeSaveStore.ClearResume();NativeCharacterProfile.ClearPending();Directory.Delete(directory,true);}
        }
        [Test]
        public void RegistrationPageReturnsToLoginAndRejectsWrongPasswordBeforeCharacterSelection()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-signup-ui-"+Guid.NewGuid().ToString("N"));
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("signup page");SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var menu=root.AddComponent<DarkSaver.UI.StartMenuController>();
                Assert.That(menu.ConfigureLocalLogin(Path.Combine(directory,"login.json"),System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0,NativeCharacterSlots.Count),i=>Path.Combine(directory,"slot-"+i+".json")))),Is.True);
                menu.StartGame();Assert.That(menu.OpenRegistration(),Is.True);Assert.That(menu.IsRegistering,Is.True);
                menu.CancelRegistration();Assert.That(menu.IsLoggingIn,Is.True);Assert.That(Directory.Exists(directory),Is.False);
                Assert.That(menu.OpenRegistration(),Is.True);
                Assert.That(menu.RegisterAccount("hero_01","sample-password!","different-password!"),Is.False);
                Assert.That(menu.IsRegistering,Is.True);
                Assert.That(menu.RegisterAccount("hero_01","sample-password!","sample-password!"),Is.True);
                Assert.That(menu.IsLoggingIn,Is.True);Assert.That(menu.IsRegistering,Is.False);
                Assert.That(menu.Login("hero_01","wrong-password!"),Is.False);Assert.That(menu.IsSelectingCharacter,Is.False);
                Assert.That(menu.Login("hero_01","sample-password!"),Is.True);Assert.That(menu.IsSelectingCharacter,Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);
                NativeSaveStore.ClearResume();NativeCharacterProfile.ClearPending();if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        [Test]
        public void CharacterHandshakeAndCreateDoNotOverwriteExistingSaveOrPrepareTwice()
        {
            string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ds-protocol-"+Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            try
            {
                var server=new LocalCharacterServer(System.IO.Path.Combine(directory,"login.json"),new[]{System.IO.Path.Combine(directory,"a.json"),System.IO.Path.Combine(directory,"b.json"),System.IO.Path.Combine(directory,"c.json")});
                Assert.That(server.List(new CharacterListRequest()).Accepted,Is.False);
                Assert.That(server.Login(new LoginRequest()).Accepted,Is.True);
                string occupied=System.IO.Path.Combine(directory,"occupied.json");System.IO.File.WriteAllText(occupied,"invalid retained save");
                Assert.That(server.Create(new CharacterCreateRequest("통신검증",100,occupied)).Accepted,Is.False);
                Assert.That(System.IO.File.ReadAllText(occupied),Is.EqualTo("invalid retained save"));
                string path=System.IO.Path.Combine(directory,"new.json");
                Assert.That(server.Create(new CharacterCreateRequest("통신검증",100,path)).Accepted,Is.True);
                Assert.That(server.Create(new CharacterCreateRequest("다른이름",100,path)).Accepted,Is.False);
                Assert.That(DarkSaver.Core.NativeCharacterProfile.TakePending(out var profile,out var selectedPath),Is.True);
                Assert.That(profile.name,Is.EqualTo("통신검증"));Assert.That(selectedPath,Is.EqualTo(path));
            }
            finally
            {
                DarkSaver.Core.NativeCharacterProfile.ClearPending();DarkSaver.Core.NativeSaveStore.ClearResume();
                System.IO.Directory.Delete(directory,true);
            }
        }
        [Test]
        public void LocalSessionRequestsOwnTheirEconomicAndCharacterSnapshots()
        {
            var profile=new DarkSaver.Core.NativeCharacterProfile {name="검증",jobId=100,allocatedAttack=3};
            var equipment=new LocalEquipmentChangeRequest(2,5,profile);profile.allocatedAttack=90;
            Assert.That(equipment.CaptureBaseline().allocatedAttack,Is.EqualTo(3));
            var inventory=new DarkSaver.Core.NativeInventory {gold=500};
            var item=new LocalBattleItemUseRequest(1,100,inventory);inventory.gold=0;
            Assert.That(item.CaptureBaseline().gold,Is.EqualTo(500));
        }
        [Test]
        public void AttackExecutesOnceInLocalServerAndPublishesDetachedResults()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("Local server execution");SceneManager.MoveGameObjectToScene(go,scene);
                var sender=go.AddComponent<BattleRequestSender>();var battle=go.GetComponent<BattleSceneRoot>();
                battle.SetTemporaryLocalRules(true);battle.ConfigureActionPointCosts(1,1);
                battle.AdvanceActionPoints(GameConstants.BattleMaximumAp);
                battle.ConfigurePhysicalAccuracy(100, 100, 0);
                battle.ConfigurePhysicalAccuracy(800, 100, 0);
                var order=new List<Type>();var manager=go.GetComponent<ServerManager>();
                manager.ResponseReceived+=p=>order.Add(p.GetType());
                int applied=0;battle.CombatResultApplied+=_=>applied++;
                var packet=new AttackRequest(800,100,0,0);int hp=battle.EnemyHp;
                Assert.That(sender.TrySendAttack(packet,out var reason),Is.True,reason);
                Assert.That(applied,Is.EqualTo(1));Assert.That(battle.EnemyHp,Is.LessThan(hp));
                Assert.That(order.IndexOf(typeof(AttackEffectNotification)),Is.LessThan(order.IndexOf(typeof(LocalCombatResolvedNotification))));
                Assert.That(order[order.Count-1],Is.EqualTo(typeof(LocalCommandResponse)));
                Assert.That(manager.OriginalFrameCount,Is.GreaterThanOrEqualTo(3));
                var before=battle.EnemyHp;
                Assert.That(sender.TrySendAttack(packet,out _),Is.False); // AP has not recharged.
                Assert.That(battle.EnemyHp,Is.EqualTo(before));Assert.That(applied,Is.EqualTo(1));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]
        public void RegisteredAuthorityRejectsUnsupportedRequestAndDisposedBindingStopsExecution()
        {
            var go=new GameObject("Local dispatcher ownership");
            try
            {
                var server=go.AddComponent<ServerManager>();int executions=0;
                using(var binding=server.RegisterRequestHandler<WorldMoveRequest>(p=>{executions++;return new LocalCommandResponse(false,"blocked");}))
                {
                    Assert.That(server.TryRequest(new WorldMoveRequest(1,2),out var result),Is.False);
                    Assert.That(result.Reason,Is.EqualTo("blocked"));Assert.That(executions,Is.EqualTo(1));
                    Assert.Throws<InvalidOperationException>(()=>server.RegisterRequestHandler<WorldMoveRequest>(p=>new LocalCommandResponse(true)));
                }
                Assert.That(server.TryRequest(new WorldMoveRequest(),out _),Is.False);Assert.That(executions,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
