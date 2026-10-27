using System;
using System.Reflection;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Magic;
using DarkSaver.UI;
using DarkSaver.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    [NUnit.Framework.Category("LegacySinglePlayer")]
    [NUnit.Framework.Ignore("싱글판 구현 삭제: 원작 서버 기반 검사로 교체 필요")]
    public sealed class BattleItemUseTests
    {
        // Archived single-player behavior is tested only inside an explicit fixture.

        [TestCase(300,15,0,false)] [TestCase(301,15,0,false)] [TestCase(302,20,0,false)]
        [TestCase(303,300,0,false)] [TestCase(304,999,0,true)] [TestCase(305,0,40,false)]
        [TestCase(316,15,0,false)] [TestCase(317,15,0,false)] [TestCase(318,20,0,false)]
        [TestCase(319,300,0,false)] [TestCase(320,999,0,true)] [TestCase(321,0,40,false)]
        [TestCase(963,999,0,false)] [TestCase(1163,1500,0,false)]
        public void ExplicitRecoveryPolicyKeepsNativeLinkRangeAndConfirmedAmountSeparate(int id,int hp,int mp,bool confirmed)
        {
            var items=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
            var magic=AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
            Assert.IsTrue(items.TryGet(id,out var item));
            Assert.IsTrue(UnresolvedItemRecoveryServerRules.TryGet(item,magic,out int actualHp,out int actualMp,out int range,out bool actualConfirmed));
            Assert.AreEqual(hp,actualHp);Assert.AreEqual(mp,actualMp);Assert.AreEqual(6,range);Assert.AreEqual(confirmed,actualConfirmed);
        }
        [TestCase(307)] [TestCase(311)] [TestCase(313)] [TestCase(315)]
        public void OtherConsumablesDoNotBecomeRecoveryByNameOrCategory(int id)
        {
            var items=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
            var magic=AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
            Assert.IsTrue(items.TryGet(id,out var item));Assert.IsFalse(UnresolvedItemRecoveryServerRules.TryGet(item,magic,out _,out _,out _,out _));
        }
        [Test]
        public void ConsumptionCopiesWithoutGoldChangesAndRejectsInvalidRequests()
        {
            var inventory=new NativeInventory {gold=77,items=new[]{new NativeItemStack {itemId=300,quantity=2}}};
            Assert.IsFalse(inventory.TryConsume(300,0,out _));Assert.IsFalse(inventory.TryConsume(300,3,out _));Assert.IsFalse(inventory.TryConsume(999,1,out _));
            Assert.IsTrue(inventory.TryConsume(300,1,out var next));Assert.AreEqual(1,next.Count(300));Assert.AreEqual(2,inventory.Count(300));Assert.AreEqual(77,next.gold);
            Assert.IsTrue(next.TryConsume(300,1,out var empty));Assert.AreEqual(0,empty.items.Length);Assert.AreEqual(1,next.Count(300));Assert.AreEqual(77,empty.gold);
        }
        [Test]
        public void FullApStaleInventoryForeignBattleAndInvalidTargetsCannotPartiallyConsume()
        {
            WithBattle((battle,session)=> {
                var baseline=session.CaptureInventory();string before=JsonUtility.ToJson(baseline);
                Assert.IsTrue(battle.ApplyHp(100,20));int mp=battle.PlayerMp;
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,300,100,baseline,out _)); // Starting AP is not full.
                battle.AdvanceActionPoints(20);
                Assert.IsFalse(session.TryUseBattleRecoveryItem(null,300,100,baseline,out _));
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,300,800,baseline,out _));
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,307,100,baseline,out _));
                Assert.AreEqual(before,JsonUtility.ToJson(session.CaptureInventory()));Assert.AreEqual(20,battle.PlayerHp);
                int ap=battle.PlayerAp;int visuals=0;
                Action<CombatRequest> effect=request=>{visuals++;Assert.AreEqual(3001,request.MagicId);};battle.MagicPresentationStarted+=effect;
                Assert.IsTrue(session.TryUseBattleRecoveryItem(battle,300,100,baseline,out _));
                Assert.AreEqual(1,visuals);Assert.AreEqual(35,battle.PlayerHp);Assert.AreEqual(mp,battle.PlayerMp);Assert.AreEqual(ap-6,battle.PlayerAp);
                Assert.AreEqual(2,session.OwnedItemCount(300));Assert.AreEqual(77,session.CurrentGold);
                battle.AdvanceActionPoints(20);
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,300,100,baseline,out _)); // stale successful request cannot replay
                Assert.AreEqual(2,session.OwnedItemCount(300));Assert.AreEqual(35,battle.PlayerHp);
                Assert.IsTrue(battle.ApplyHp(100,battle.PlayerMaxHp));
                var fresh=session.CaptureInventory();int ready=battle.PlayerAp;
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,300,100,fresh,out _));Assert.AreEqual(ready,battle.PlayerAp);
                battle.MagicPresentationStarted-=effect;
                Assert.IsTrue(battle.ApplyMp(100,0));
                Assert.IsTrue(session.TryUseBattleRecoveryItem(battle,305,100,fresh,out _));
                Assert.AreEqual(battle.PlayerMaxMp,battle.PlayerMp);Assert.AreEqual(0,session.OwnedItemCount(305));
                Assert.AreEqual(3,baseline.Count(300));Assert.AreEqual(1,baseline.Count(305));
            });
        }
        [Test]
        public void RecoveryClampsAtIntegerLimitAndCannotReviveOrIgnoreRange()
        {
            var world=new CombatWorld();var unit=new CombatUnit {Id=100,Hp=1,MaxHp=int.MaxValue,Mp=0,MaxMp=int.MaxValue};
            var ally=new CombatUnit {Id=200,Hp=1,MaxHp=100,Mp=0,MaxMp=0,X=8};world.Add(unit);world.Add(ally);
            var flow=new CombatFlow(world,new CombatServerBoundary(),true);flow.TemporaryRulesEnabled=true;flow.AdvanceActionPoints(20);
            Assert.IsFalse(flow.TryApplyItemRecovery(100,200,50,0,6,6));Assert.AreEqual(1,ally.Hp);
            Assert.IsFalse(flow.TryApplyItemRecovery(100,100,-1,0,6,6));
            Assert.IsTrue(flow.TryApplyItemRecovery(100,100,int.MaxValue,int.MaxValue,6,6));
            Assert.AreEqual(int.MaxValue,unit.Hp);Assert.AreEqual(int.MaxValue,unit.Mp);
            flow.AdvanceActionPoints(20);ally.Hp=0;
            Assert.IsFalse(flow.TryApplyItemRecovery(100,200,50,0,10,6));Assert.AreEqual(0,ally.Hp);
        }

        [Test]
        public void VitalsCallbackCannotReenterTheSameInventoryTransaction()
        {
            WithBattle((battle,session)=> {
                battle.AdvanceActionPoints(20);battle.ApplyHp(100,20);var baseline=session.CaptureInventory();
                var flow=(CombatFlow)typeof(BattleSceneRoot).GetField("flow",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
                int callbacks=0;flow.VitalsDisplayChanged+=(id,delta,mp)=> {
                    callbacks++;Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,300,100,session.CaptureInventory(),out _));
                };
                Assert.IsTrue(session.TryUseBattleRecoveryItem(battle,300,100,baseline,out _));
                Assert.AreEqual(1,callbacks);Assert.AreEqual(2,session.OwnedItemCount(300));Assert.AreEqual(35,battle.PlayerHp);
            });
        }
        [Test]
        public void QuickAndAttackItemsConsumeOneStackWithoutMpOrLearnedSpellRequirement()
        {
            WithBattle((battle,session)=> {
                battle.AdvanceActionPoints(20); battle.ApplyMp(100,0);
                Assert.IsTrue(session.TryUseBattleRecoveryItem(battle,310,100,session.CaptureInventory(),out _));
                Assert.IsTrue(battle.TryGetTemporaryQuick(100,out int percent,out _));Assert.AreEqual(65,percent);
                Assert.AreEqual(0,battle.PlayerMp);Assert.AreEqual(0,session.OwnedItemCount(310));
                battle.AdvanceActionPoints(10); var before=session.CaptureInventory();int hp=battle.EnemyHp;
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,311,100,before,out _));
                Assert.AreEqual(1,session.OwnedItemCount(311));Assert.AreEqual(hp,battle.EnemyHp);
                Assert.IsTrue(session.TryUseBattleRecoveryItem(battle,311,800,before,out _));
                Assert.Less(battle.EnemyHp,hp);Assert.AreEqual(0,battle.PlayerMp);Assert.AreEqual(0,session.OwnedItemCount(311));
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,311,800,before,out _));
            });
        }

        [TestCase(307,30801)] [TestCase(308,30802)] [TestCase(309,30803)] [TestCase(310,2203)]
        [TestCase(311,101)] [TestCase(312,301)] [TestCase(313,1601)] [TestCase(314,701)]
        [TestCase(315,3601)] [TestCase(322,3501)] [TestCase(323,2203)] [TestCase(398,201)]
        public void NonRecoveryConsumablesKeepExactNativeMagicLinks(int id,int linked)
        {
            var items=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
            var magic=AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
            Assert.IsTrue(items.TryGet(id,out var item));
            Assert.IsTrue(OriginalItemMagicLink.TryGet(item,magic,out var entry));Assert.AreEqual(linked,entry.Id);
        }

        [Test]
        public void PoisonCureAndMovementItemsCommitOnlyForAnEligibleTarget()
        {
            WithBattle((battle,session)=> {
                battle.AdvanceActionPoints(20);battle.ApplyMp(100,0);
                var before=session.CaptureInventory();int hp=battle.EnemyHp;
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,313,100,before,out _));
                Assert.IsTrue(session.TryUseBattleRecoveryItem(battle,313,800,before,out _));
                Assert.AreEqual(0,session.OwnedItemCount(313));Assert.AreEqual(hp,battle.EnemyHp);Assert.AreEqual(0,battle.PlayerMp);
                Assert.IsTrue(battle.TryGetTemporaryStatus(800,TemporaryStatusEffect.Poison,out _,out _));
                battle.AdvanceActionPoints(2);Assert.Less(battle.EnemyHp,hp);
                var flow=(CombatFlow)typeof(BattleSceneRoot).GetField("flow",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
                var world=(CombatWorld)typeof(CombatFlow).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(flow);
                world.Get(100).ActionPoints.ApplyAbsolute(world.Get(100).ActionPoints.Maximum);
                before=session.CaptureInventory();int ap=battle.PlayerAp;
                Assert.IsFalse(session.TryUseBattleRecoveryItem(battle,307,100,before,out _));
                Assert.AreEqual(1,session.OwnedItemCount(307));Assert.AreEqual(ap,battle.PlayerAp);
                Assert.IsTrue(flow.ConfigureTemporaryStatusMagic(1601,0,5,TemporaryStatusEffect.Poison,8,20));
                Assert.IsTrue(battle.TryGetUnit(800,out var enemy));
                Assert.IsTrue(world.Get(800).ActionPoints.ApplyAbsolute(enemy.MaxAp));
                Assert.IsTrue(flow.TryExecute(new CombatRequest(800,100,CombatActionType.Magic,1601),out _,out _));
                Assert.IsTrue(session.TryUseBattleRecoveryItem(battle,307,100,before,out _));
                Assert.IsFalse(battle.TryGetTemporaryStatus(100,TemporaryStatusEffect.Poison,out _,out _));
                Assert.AreEqual(0,session.OwnedItemCount(307));Assert.AreEqual(0,battle.PlayerMp);
            });
        }

        private static void WithBattle(Action<BattleSceneRoot,BattleTransitionController> check)
        {
            Assert.IsFalse(SceneManager.GetSceneByName("Battle").isLoaded);
            var original=SceneManager.GetActiveScene();bool dirty=original.isDirty;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);scene.name="ItemUseFixture";
            try
            {
                var go=new GameObject("Isolated item use");SceneManager.MoveGameObjectToScene(go,scene);
                var session=go.AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.InitializeCharacter(new NativeCharacterProfile {name="Item fixture",jobId=100}));
                var state=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                state.inventory=new NativeInventory {gold=77,items=new[]{new NativeItemStack {itemId=300,quantity=3},new NativeItemStack {itemId=305,quantity=1},new NativeItemStack {itemId=310,quantity=1},new NativeItemStack {itemId=311,quantity=1},new NativeItemStack {itemId=313,quantity=1},new NativeItemStack {itemId=307,quantity=1}}};
                Assert.IsTrue(session.RestoreSaveState(state));
                var player=go.AddComponent<TopDownPlayerController>();
                go.AddComponent<ItemCatalogPanel>().Configure(AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset"),player);
                go.AddComponent<MagicCatalogPanel>().Configure(AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset"),player);
                var battle=go.AddComponent<BattleSceneRoot>();
                Assert.IsTrue(battle.TryConfigureGrid(4,4,new[]{new CombatGridPlacement(100,new CombatGridCell(0,0)),new CombatGridPlacement(800,new CombatGridCell(3,3))},(_,_)=>true,(_,_)=>true));
                battle.SetTemporaryLocalRules(true);scene.name="Battle";
                // Isolated ownership fixture; real entry/return is checked separately in Play.
                typeof(BattleTransitionController).GetField("activeBattle",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(session,battle);
                check(battle,session);
            }
            finally {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
            Assert.AreEqual(dirty,original.isDirty);
        }
    }
}
