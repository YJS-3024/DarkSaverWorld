using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Jobs;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Tests
{
    [NUnit.Framework.Category("LegacySinglePlayer")]
    [NUnit.Framework.Ignore("싱글판 구현 삭제: 원작 서버 기반 검사로 교체 필요")]
    public sealed class EquipmentExecutionTests
    {
        // Archived single-player behavior is tested only inside an explicit fixture.

        private static JobCatalog Jobs=>AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
        private static ItemCatalog Items=>AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
        private static NativeCharacterProfile Profile()=>new NativeCharacterProfile {name="Gear",jobId=100,hasLevels=true,baseLevel=12,jobLevel=5};
        [Test]
        public void UnknownSlotsStayUnknownAndCopiesCannotShareEquipmentArrays()
        {
            var profile=Profile();Assert.IsTrue(profile.IsValid);for(int i=0;i<7;i++)Assert.AreEqual(-1,profile.EquipmentItemAt(i));
            var inventory=new NativeInventory {items=new[]{new NativeItemStack {itemId=1,quantity=1}}};
            Assert.IsTrue(profile.TryBuildEquipmentSlot(2,1,inventory,out var changed));Assert.IsNull(profile.equipmentItemIds);
            Assert.AreEqual(1,changed.EquipmentItemAt(2));Assert.AreEqual(-1,changed.EquipmentItemAt(3));
            var clone=changed.Clone();clone.equipmentItemIds[2]=0;Assert.AreEqual(1,changed.EquipmentItemAt(2));
            Assert.IsFalse(changed.TryBuildEquipmentSlot(3,1,inventory,out _));
            profile.equipmentItemIds=new[]{1};Assert.IsFalse(profile.IsValid);Assert.AreEqual(-1,profile.EquipmentItemAt(3));
        }
        [Test]
        public void NativeOppositeSlotAndTroopRestrictionsAreSeparateFromLocalBaseLevelPolicy()
        {
            var profile=Profile();Jobs.TryGet(100,out var job);Items.TryGet(1,out var dagger);Items.TryGet(10,out var requiredSix);
            Assert.IsFalse(UnresolvedEquipmentServerRules.IsEligible(profile,job,Items,2,dagger)); // Opposite slot is unknown.
            profile.equipmentItemIds=new[]{-1,-1,-1,0,-1,-1,-1};
            Assert.IsTrue(UnresolvedEquipmentServerRules.IsEligible(profile,job,Items,2,dagger));
            Assert.IsTrue(UnresolvedEquipmentServerRules.IsEligible(profile,job,Items,2,requiredSix)); // base12/job5: local base-level policy.
            profile.baseLevel=5;Assert.IsFalse(UnresolvedEquipmentServerRules.IsEligible(profile,job,Items,2,requiredSix));
            Assert.IsFalse(UnresolvedEquipmentServerRules.IsEligible(profile,job,Items,1,dagger));
            profile.baseLevel=12;profile.equipmentItemIds[3]=1;
            Assert.IsFalse(UnresolvedEquipmentServerRules.IsEligible(profile,job,Items,2,dagger));
        }
        [Test]
        public void ActualEquipConfirmKeepsOwnedQuantitiesLocksSalesAndRejectsStaleWindows()
        {
            WithTown((session,panel,player)=> {
                var baseline=session.CaptureCharacterProfile();
                Assert.IsTrue(session.TryChangeEquipment(3,0,baseline,out _));
                Assert.IsFalse(session.TryChangeEquipment(2,1,baseline,out _));
                baseline=session.CaptureCharacterProfile();Assert.IsTrue(session.TryChangeEquipment(2,1,baseline,out _));
                Assert.AreEqual(2,session.OwnedItemCount(1));Assert.AreEqual(1,session.EquipmentItemAt(2));Assert.AreEqual(0,session.EquipmentItemAt(3));
                Assert.IsTrue(session.TryGetPlayerPhysicalStats(out int a,out int d,out int attack,out int defense));Assert.AreEqual(a+40,attack);Assert.AreEqual(d,defense);
                Assert.AreEqual(17,session.PlayerHp);Assert.AreEqual(3,session.PlayerMp);Assert.AreEqual(77,session.CurrentGold);
                var quote=new NativeShopOffer {itemId=1,buyPrice=1,sellPrice=1};
                session.ConfigureLocalShop(Items, new[] { quote });
                Assert.IsFalse(session.TryTrade(quote,2,false,Items,out _));
                Assert.IsFalse(session.TryTrade(quote,1,false,Items,out _));Assert.AreEqual(2,session.OwnedItemCount(1));
                Assert.IsFalse(session.TryTrade(quote,1,false,Items,out _));
                Assert.IsTrue(session.TryChangeEquipment(2,0,session.CaptureCharacterProfile(),out _));
                Assert.IsTrue(session.TryTrade(quote,2,false,Items,out _));Assert.AreEqual(0,session.OwnedItemCount(1));
            });
        }
        [Test]
        public void EquipmentRoundTripsDiskAndCannotClaimMoreCopiesThanOwned()
        {
            WithTown((session,panel,player)=> {
                Assert.IsTrue(session.TryChangeEquipment(-1,0,session.CaptureCharacterProfile(),out _));
                Assert.IsTrue(session.TryChangeEquipment(2,1,session.CaptureCharacterProfile(),out _));
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                string directory=Path.GetFullPath("Tools/CombatRulesChecks/Equipment-"+Guid.NewGuid().ToString("N"));
                string file=Path.Combine(directory,"save.json");
                try
                {
                    var store=new NativeSaveStore(file);Assert.IsTrue(store.TrySave(save,out _));Assert.IsTrue(store.TryLoad(out var loaded,out _));
                    Assert.AreEqual(1,loaded.character.EquipmentItemAt(2));Assert.AreEqual(0,loaded.character.EquipmentItemAt(3));
                    loaded.inventory.items=Array.Empty<NativeItemStack>();Assert.IsFalse(NativeSaveStore.IsValid(loaded));
                    Assert.IsTrue(save.character.IsEquipmentOwned(save.inventory));
                }
                finally {if(File.Exists(file))File.Delete(file);if(Directory.Exists(directory))Directory.Delete(directory);}
            });
        }
        [Test]
        public void EquipmentBattleStatsDoNotAccumulateAndMissingCatalogCannotPartiallyApply()
        {
            var profile=Profile();profile.equipmentItemIds=new[]{-1,-1,1,0,-1,-1,-1};profile.allocatedAttack=2;
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("Gear combat fixture");SceneManager.MoveGameObjectToScene(go,scene);var battle=go.AddComponent<BattleSceneRoot>();
                Assert.IsTrue(battle.ConfigureTraversalCatalogs(new[]{Jobs}));Assert.IsTrue(battle.ConfigurePlayerJob(100));
                Assert.IsTrue(battle.ConfigurePlayerJobBaseStats(profile,Items));battle.TryGetUnit(100,out var first);
                Jobs.TryGet(100,out var job);Assert.AreEqual(job.GetNumericValue(6)+42,first.Attack);
                Assert.IsTrue(battle.ConfigurePlayerJobBaseStats(profile,Items));battle.TryGetUnit(100,out var second);Assert.AreEqual(first.Attack,second.Attack);
                Assert.IsFalse(battle.ConfigurePlayerJobBaseStats(profile));battle.TryGetUnit(100,out var after);Assert.AreEqual(first.Attack,after.Attack);Assert.AreEqual(first.Hp,after.Hp);
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]
        public void InventorySelectionDoesNotCommitAndUnequipAllMakesUnknownSlotsExplicitlyEmpty()
        {
            WithTown((session,panel,player)=> {
                Assert.IsTrue(panel.Open(TownService.Inventory,player));Assert.IsTrue(panel.SelectOwnedItem(1));Assert.IsTrue(panel.SelectEquipmentSlot(3));
                Assert.AreEqual(-1,session.EquipmentItemAt(3));Assert.IsTrue(panel.ConfirmSelectedEquipment(true));Assert.AreEqual(0,session.EquipmentItemAt(3));
                Assert.IsTrue(panel.SelectEquipmentSlot(2));Assert.IsTrue(panel.ConfirmSelectedEquipment());Assert.AreEqual(1,session.EquipmentItemAt(2));
                Assert.IsTrue(panel.ConfirmSelectedEquipment(true,true));for(int i=0;i<7;i++)Assert.AreEqual(0,session.EquipmentItemAt(i));
                Assert.AreEqual(2,session.OwnedItemCount(1));panel.Close();Assert.IsFalse(panel.ConfirmSelectedEquipment());
            });
        }
        [Test]
        public void AllSixLocalBonusesClampNegativeTotalsAndRejectOverflow()
        {
            var catalog=ScriptableObject.CreateInstance<ItemCatalog>();
            try
            {
                var values=new long[ItemCatalogEntry.NumericColumnCount];values[0]=500;values[3]=1;values[6]=1;
                values[19]=25;values[23]=-100;values[20]=7;values[21]=-100;values[24]=9;values[25]=-100;
                catalog.Initialize(new[]{new ItemCatalogEntry(values,"0","","Fixture","","weapon")});
                var profile=Profile();profile.equipmentItemIds=new[]{0,0,500,0,0,0,0};Jobs.TryGet(100,out var job);
                Assert.IsTrue(UnresolvedEquipmentServerRules.TryResolve(profile,job,catalog,out var stats));
                Assert.AreEqual(job.GetNumericValue(2)+25,stats.Hp);Assert.AreEqual(0,stats.Mp);Assert.AreEqual(job.GetNumericValue(6)+7,stats.Attack);
                Assert.AreEqual(0,stats.Defense);Assert.AreEqual(job.GetNumericValue(10)+9,stats.MagicAttack);Assert.AreEqual(0,stats.MagicDefense);
                values[20]=long.MaxValue;catalog.Initialize(new[]{new ItemCatalogEntry(values,"0","","Overflow","","weapon")});
                Assert.IsFalse(UnresolvedEquipmentServerRules.TryResolve(profile,job,catalog,out _));Assert.AreEqual(500,profile.EquipmentItemAt(2));
            }
            finally {UnityEngine.Object.DestroyImmediate(catalog);}
        }
        [Test]
        public void EquipmentDoesNotHealAndMaxMpReductionClampsOnlyCurrentMp()
        {
            WithTown((session,panel,player)=> {
                var catalog=ScriptableObject.CreateInstance<ItemCatalog>();
                try
                {
                    var values=new long[ItemCatalogEntry.NumericColumnCount];values[0]=500;values[3]=1;values[6]=1;values[19]=25;values[23]=-100;
                    catalog.Initialize(new[]{new ItemCatalogEntry(values,"0","","Fixture","","weapon")});session.GetComponent<ItemCatalogPanel>().Configure(catalog,player);
                    var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);save.inventory.items=new[]{new NativeItemStack {itemId=500,quantity=1}};
                    Assert.IsTrue(session.RestoreSaveState(save));Assert.IsTrue(session.TryChangeEquipment(2,500,session.CaptureCharacterProfile(),out _));
                    Assert.AreEqual(17,session.PlayerHp);Assert.AreEqual(225,session.PlayerMaxHp);Assert.AreEqual(0,session.PlayerMp);Assert.AreEqual(0,session.PlayerMaxMp);
                    Assert.IsTrue(session.TryChangeEquipment(2,0,session.CaptureCharacterProfile(),out _));
                    Assert.AreEqual(17,session.PlayerHp);Assert.AreEqual(200,session.PlayerMaxHp);Assert.AreEqual(0,session.PlayerMp);Assert.AreEqual(45,session.PlayerMaxMp);
                }
                finally {UnityEngine.Object.DestroyImmediate(catalog);}
            });
        }

        private static void WithTown(Action<BattleTransitionController,TownServicesPanel,TopDownPlayerController> check)
        {
            var original=SceneManager.GetActiveScene();var town=SceneManager.GetSceneByName("Town_Runtime");bool created=!town.isLoaded;
            if(created){town=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);town.name="Town_Runtime";}
            var go=new GameObject("Equipment fixture");SceneManager.MoveGameObjectToScene(go,town);
            try
            {
                var player=go.AddComponent<TopDownPlayerController>();var session=go.AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.InitializeCharacter(Profile()));Assert.IsTrue(session.ConfigureFormationSupport(new[]{Jobs},new Dictionary<int,DirectionalSpriteAnimator>()));
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);save.hasVitals=true;save.hp=17;save.maxHp=200;save.mp=3;save.maxMp=45;
                save.inventory=new NativeInventory {gold=77,items=new[]{new NativeItemStack {itemId=1,quantity=2}}};Assert.IsTrue(session.RestoreSaveState(save));
                go.AddComponent<ItemCatalogPanel>().Configure(Items,player);var panel=go.AddComponent<TownServicesPanel>();check(session,panel,player);
            }
            finally {UnityEngine.Object.DestroyImmediate(go);if(created)EditorSceneManager.CloseScene(town,true);SceneManager.SetActiveScene(original);}
        }
    }
}
