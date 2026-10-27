using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Items;
using DarkSaver.Jobs;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DarkSaver.Tests
{
    [NUnit.Framework.Category("LegacySinglePlayer")]
    [NUnit.Framework.Ignore("싱글판 구현 삭제: 원작 서버 기반 검사로 교체 필요")]
    public sealed class ScreenshotUpdatesTests
    {
        // Archived single-player behavior is tested only inside an explicit fixture.

        [TestCase("World1010_Map","Field_Runtime")]
        [TestCase("Brugos01_Map","Brugos01_Runtime")]
        [TestCase("Map_101_;","Training101_Runtime")]
        public void PersonalEquipmentOpensOutsideTownAndPreservesWoundedVitalsItemsGoldAndSave(string mapName,string runtimeName)
        {
            var map=EditModeSceneFixture.Create(mapName);
            var runtime=EditModeSceneFixture.Create(runtimeName);
            string directory=Path.Combine(Path.GetTempPath(),"ds-personal-"+Guid.NewGuid().ToString("N"));
            try
            {
                var root=new GameObject("personal gear field fixture");SceneManager.MoveGameObjectToScene(root,runtime);
                var player=root.AddComponent<TopDownPlayerController>();var session=root.AddComponent<BattleTransitionController>();var panel=root.AddComponent<TownServicesPanel>();
                var jobs=AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                var items=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
                root.AddComponent<ItemCatalogPanel>().Configure(items,player);Assert.IsTrue(session.ConfigureFormationSupport(new[]{jobs},new Dictionary<int,DirectionalSpriteAnimator>()));
                var data=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene=mapName,runtimeScene=runtimeName,
                    worldX=8.5f,worldY=9.5f,hasCharacter=true,character=new NativeCharacterProfile {name="개인장비",jobId=100,hasLevels=true,baseLevel=100,jobLevel=1,
                    hasClassAbilities=true,learnedMagicIds=Array.Empty<int>(),usableSkillIds=Array.Empty<int>(),equipmentItemIds=new int[7]},
                    hasVitals=true,hp=17,maxHp=200,mp=3,maxMp=45,hasInventory=true,inventory=new NativeInventory {gold=77,items=new[]{new NativeItemStack {itemId=1,quantity=2}}},clearedScenarios=Array.Empty<string>()};
                Assert.IsTrue(session.RestoreSaveState(data));Assert.IsTrue(panel.Open(TownService.Inventory,player));Assert.IsTrue(player.UiInputBlocked);
                Assert.IsFalse(panel.Open(TownService.Shop,player));Assert.IsFalse(panel.Open(TownService.ClassChange,player));Assert.AreEqual(TownService.Inventory,panel.Service);
                var baseline=session.CaptureCharacterProfile();Assert.IsTrue(session.TryChangeEquipment(2,1,baseline,out var reason),reason);
                Assert.AreEqual(17,session.PlayerHp);Assert.AreEqual(3,session.PlayerMp);Assert.AreEqual(77,session.CurrentGold);Assert.AreEqual(2,session.OwnedItemCount(1));
                Assert.IsFalse(session.TryChangeEquipment(2,0,baseline,out _),"stale equipment candidate must fail");
                var store=new NativeSaveStore(Path.Combine(directory,"save.json"));var snapshot=session.CaptureSaveState(mapName,runtimeName,new Vector2(8.5f,9.5f));
                Assert.IsTrue(store.TrySave(snapshot,out reason),reason);Assert.IsTrue(store.TryLoad(out var loaded,out reason),reason);
                Assert.AreEqual(mapName,loaded.mapScene);Assert.AreEqual(1,loaded.character.EquipmentItemAt(2));Assert.AreEqual(17,loaded.hp);Assert.AreEqual(77,loaded.inventory.gold);
                Assert.IsTrue(session.TryChangeEquipment(2,0,session.CaptureCharacterProfile(),out _));Assert.AreEqual(0,session.EquipmentItemAt(2));
                panel.Close();Assert.IsFalse(player.UiInputBlocked);
                Assert.IsTrue(panel.Open(TownService.CharacterInfo,player));panel.Close();
                var battle=EditModeSceneFixture.Create("Battle");
                try
                {
                    Assert.IsFalse(panel.Open(TownService.Inventory,player));
                    Assert.IsFalse(session.TryChangeEquipment(2,1,session.CaptureCharacterProfile(),out _));
                    Assert.AreEqual(0,session.EquipmentItemAt(2));Assert.AreEqual(77,session.CurrentGold);
                }
                finally{EditModeSceneFixture.Close(battle);}
            }
            finally{EditModeSceneFixture.Close(runtime);EditModeSceneFixture.Close(map);if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [TestCase(400)] [TestCase(50)] [TestCase(0)]
        public void BattleCheckpointPersistsDetachedCurrentVitalsAndRetainsLiveEncounter(int remainingHp)
        {
            var directory=Path.Combine(Path.GetTempPath(),"darksaver-checkpoint-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var scene=EditModeSceneFixture.Create("CheckpointSession_"+Guid.NewGuid().ToString("N"));
            Scene battleScene=default;
            var enemy=ScriptableObject.CreateInstance<BattleEnemyDefinition>();
            try
            {
                var definition=Scenario01Controller.Definition;
                var source=definition.units.Single(u=>u.id==700);
                var progress=Scenario01Progress.Create(definition);
                progress.phase=Scenario01Progress.Playing;progress.wave=source.wave;progress.pendingBattle=700;
                var root=new GameObject("checkpoint session");SceneManager.MoveGameObjectToScene(root,scene);
                var session=root.AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.InitializeCharacter(new NativeCharacterProfile {name="checkpoint",jobId=100}));
                var state=session.CaptureSaveState("Brugos01_Map","Brugos01_Runtime",new Vector2(13.5f,8.5f));
                state.version=NativeSaveStore.ScenarioVersion;state.hasScenario01=true;state.scenario01=progress;
                Assert.IsTrue(session.RestoreSaveState(state));
                var path=Path.Combine(directory,"slot.json");var store=new NativeSaveStore(path);
                Assert.IsTrue(store.TrySave(state,out var reason),reason);session.ConfigureSaveFile(path);
                battleScene=EditModeSceneFixture.Create("Battle");
                var battleObject=new GameObject("checkpoint battle");SceneManager.MoveGameObjectToScene(battleObject,battleScene);
                var battle=battleObject.AddComponent<BattleSceneRoot>();enemy.InitializeScenarioUnit(source);
                Assert.IsTrue(battle.ConfigureEnemy(enemy));Assert.IsTrue(battle.ApplyHp(700,remainingHp));
                if(remainingHp==0)Assert.IsTrue(battle.ApplyDeath(700,false));
                Assert.IsTrue(battle.ApplyHp(100,17));Assert.IsTrue(battle.ApplyMp(100,3));
                typeof(BattleTransitionController).GetField("activeBattle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session,battle);
                Assert.IsFalse(session.SaveWorld(),"manual world save remains blocked during combat");
                Assert.IsTrue(session.SaveBattleCheckpoint(out reason),reason);
                Assert.IsTrue(store.TryLoad(out var saved,out reason),reason);
                Assert.AreEqual(remainingHp,saved.scenario01.enemies.Single(e=>e.id==700).hp);
                Assert.AreEqual(17,saved.hp);Assert.AreEqual(3,saved.mp);
                Assert.AreEqual(13.5f,saved.worldX);Assert.AreEqual(8.5f,saved.worldY);
                Assert.AreEqual(-1,saved.scenario01.pendingBattle);
                Assert.AreEqual(remainingHp==0,saved.scenario01.defeated.Contains(700));
                Assert.AreEqual(700,session.CaptureScenario01().pendingBattle,"checkpoint must not finalize the live encounter");
                Assert.IsEmpty(session.CaptureScenario01().enemies);
                Assert.IsTrue(session.SaveBattleCheckpoint(out reason),reason);
                Assert.AreEqual(0,session.ClearedScenarioCount);Assert.AreEqual(state.inventory.gold,session.CurrentGold);
                // A failed atomic write must not publish the candidate or corrupt the last slot.
                var preserved=File.ReadAllBytes(path);var blocker=Path.Combine(directory,"not-a-directory");File.WriteAllText(blocker,"blocked");
                session.ConfigureSaveFile(Path.Combine(blocker,"slot.json"));
                Assert.IsFalse(session.SaveBattleCheckpoint(out _));
                CollectionAssert.AreEqual(preserved,File.ReadAllBytes(path));
                Assert.AreEqual(700,session.CaptureScenario01().pendingBattle);
                if(remainingHp>0)
                {
                    var valid=File.ReadAllBytes(path);session.ConfigureSaveFile(path);
                    Assert.IsTrue(battle.ApplyHp(700,0));
                    Assert.IsFalse(session.SaveBattleCheckpoint(out _),"HP0 without a death notification must not create victory or an unspawnable enemy save");
                    CollectionAssert.AreEqual(valid,File.ReadAllBytes(path));
                    Assert.IsTrue(battle.ApplyHp(700,remainingHp));
                }
                Assert.IsTrue(session.TryCaptureBattleReturn(battle,null,out reason),reason);
                Assert.AreEqual(battle.PlayerMaxMp,session.PlayerMp,"confirmed battle-end policy restores MP; checkpoints preserve spent MP");
                Assert.AreEqual(-1,session.CaptureScenario01().pendingBattle);
            }
            finally
            {
                if(battleScene.IsValid())EditModeSceneFixture.Close(battleScene);
                EditModeSceneFixture.Close(scene);UnityEngine.Object.DestroyImmediate(enemy);
                Directory.Delete(directory,true);
            }
        }
        [Test]
        public void KisraUses450AndResumedHpIsNeverRefilled()
        {
            var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/01/Content").text);var source=definition.units.Single(u=>u.id==700);
            var enemy=ScriptableObject.CreateInstance<BattleEnemyDefinition>();
            try
            {
                Assert.AreEqual(300,source.hp);Assert.AreEqual(450,source.CombatHp);
                enemy.InitializeScenarioUnit(source);Assert.IsTrue(enemy.TryCreateUnit(out var fresh));Assert.AreEqual(450,fresh.Hp);Assert.AreEqual(150,fresh.Mp);Assert.AreEqual(126,fresh.Attack);Assert.AreEqual(55,fresh.Defense);
                foreach(int hp in new[]{150,300,450})
                {enemy.InitializeScenarioUnit(source,new Scenario01EnemyState {id=700,hp=hp,mp=140,x=source.x,y=source.y});Assert.IsTrue(enemy.TryCreateUnit(out var resumed));Assert.AreEqual(hp,resumed.Hp);Assert.AreEqual(450,resumed.MaxHp);}
            }
            finally{UnityEngine.Object.DestroyImmediate(enemy);}
        }
    }
}
