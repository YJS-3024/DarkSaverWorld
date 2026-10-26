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
    public sealed class ScreenshotUpdatesTests
    {
        [TestCase("World1010_Map","Field_Runtime")]
        [TestCase("Brugos01_Map","Brugos01_Runtime")]
        [TestCase("Map_101_;","Training101_Runtime")]
        public void PersonalEquipmentOpensOutsideTownAndPreservesWoundedVitalsItemsGoldAndSave(string mapName,string runtimeName)
        {
            var map=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);map.name=mapName;
            var runtime=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);runtime.name=runtimeName;
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
                var battle=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);battle.name="Battle";
                try
                {
                    Assert.IsFalse(panel.Open(TownService.Inventory,player));
                    Assert.IsFalse(session.TryChangeEquipment(2,1,session.CaptureCharacterProfile(),out _));
                    Assert.AreEqual(0,session.EquipmentItemAt(2));Assert.AreEqual(77,session.CurrentGold);
                }
                finally{EditorSceneManager.CloseScene(battle,true);}
            }
            finally{EditorSceneManager.CloseScene(runtime,true);EditorSceneManager.CloseScene(map,true);if(Directory.Exists(directory))Directory.Delete(directory,true);}
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
