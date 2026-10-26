using DarkSaver.EditorTools.CanvasTesting;
using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class TownShopTests
    {
        [Test]
        public void TradeFeedbackUsesNativeShopBodyWithoutRemovedSaveStrip()
        {
            var go=new GameObject("Shop feedback fixture");
            try
            {
                var panel=go.AddComponent<TownServicesPanel>();
                InGameUi ui=null;InGameUi.Ensure(ref ui,panel);
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(TownServicesPanel).GetField("ui",flags).SetValue(panel,ui);
                var trade=typeof(TownServicesPanel).GetField("shopMessage",flags);
                var draw=typeof(TownServicesPanel).GetMethod("DrawNativeShopList",flags);
                var window=Resources.Load<NativeBattleWindows>("UI/TownShop/NativeTownShop")?.Find(10);
                Assert.IsNotNull(window);Assert.AreEqual(3,window.controls.Length);
                trade.SetValue(panel,"Trade failed");
                ui.Begin();draw.Invoke(panel,new object[]{new Rect(110,64,419,303),window});ui.End();
                var root=CanvasProbe.For(ui);
                var status=DarkSaver.EditorTools.CanvasTesting.UQueryExtensions.Q<DarkSaver.EditorTools.CanvasTesting.Label>(root,"shop-message");
                Assert.That(status.text,Is.EqualTo("Trade failed"));
                Assert.That(status.style.top.value.value,Is.GreaterThanOrEqualTo(64));
                Assert.That(status.style.top.value.value+status.style.height.value.value,Is.LessThanOrEqualTo(367));
                Assert.That(DarkSaver.EditorTools.CanvasTesting.UQueryExtensions.Q(root,"town-save-message"),Is.Null);
                Assert.That(DarkSaver.EditorTools.CanvasTesting.UQueryExtensions.Q(root,"shop-trade-status"),Is.Null);
                trade.SetValue(panel,null);
                ui.Begin();draw.Invoke(panel,new object[]{new Rect(110,64,419,303),window});ui.End();
                Assert.That(status.text,Does.Not.Contain("Trade failed"));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [TestCase(0, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(7, 1)]
        [TestCase(12, 1)]
        [TestCase(13, 2)]
        [TestCase(15, 2)]
        [TestCase(16, 3)]
        public void ShopPagesRejectBoundsResetSelectionAndNeverTrade(int count, int pages)
        {
            var active = SceneManager.GetActiveScene();
            var town = SceneManager.GetSceneByName("Town_Runtime"); bool created = !town.isLoaded;
            if (created) { town = EditModeSceneFixture.Create("Town_Runtime"); }
            var root = new GameObject("Shop page fixture"); SceneManager.MoveGameObjectToScene(root, town);
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            try {
                var entries = new ItemCatalogEntry[Math.Max(1, count)]; var offers = new NativeShopOffer[count];
                for (int i = 0; i < entries.Length; i++) {
                    var values = new long[ItemCatalogEntry.NumericColumnCount]; values[0] = i + 1;
                    entries[i] = new ItemCatalogEntry(values, "1", "", "fixture", "", "fixture");
                    if (i < count) offers[i] = new NativeShopOffer {itemId = i + 1, buyPrice = 1, sellPrice = 1};
                }
                catalog.Initialize(entries);
                var player = root.AddComponent<TopDownPlayerController>();
                var session = root.AddComponent<BattleTransitionController>();
                Assert.That(session.InitializeCharacter(new NativeCharacterProfile {name = "fixture", jobId = 100}), Is.True);
                var before = session.CaptureInventory();
                var panel = root.AddComponent<TownServicesPanel>();
                Assert.That(panel.ConfigureShop(catalog, offers), Is.True);
                Assert.That(panel.SetShopPage(0), Is.False);
                Assert.That(panel.Open(TownService.Shop, player), Is.True);
                Assert.That(panel.ShopPageCount, Is.EqualTo(pages));
                if (count > 0) Assert.That(panel.SelectShopItem(1), Is.True);
                Assert.That(panel.SetShopPage(-1), Is.False);
                Assert.That(panel.SetShopPage(pages), Is.False);
                if (count > 0) Assert.That(panel.SelectedShopItem.Id, Is.EqualTo(1));
                Assert.That(panel.SetShopPage(pages - 1), Is.True);
                Assert.That(panel.ShopPageIndex, Is.EqualTo(pages - 1));
                Assert.That(panel.SelectedShopItem, Is.Null);
                int start = (pages - 1) * TownServicesPanel.ShopColumns;
                for (int slot = 0; slot < TownServicesPanel.ShopVisibleCount; slot++) {
                    bool populated = start + slot < count;
                    Assert.That(panel.TryGetShopCell(slot, out var item), Is.EqualTo(populated));
                    if (populated) Assert.That(item.Id, Is.EqualTo(start + slot + 1));
                }
                Assert.That(panel.TryGetShopCell(-1, out _), Is.False);
                Assert.That(panel.TryGetShopCell(12, out _), Is.False);
                Assert.That(session.CurrentGold, Is.EqualTo(before.gold));
                Assert.That(session.OwnedItemTypeCount, Is.EqualTo(before.items.Length));
                panel.Close(); Assert.That(panel.SetShopPage(0), Is.False);
                Assert.That(panel.TryGetShopCell(0, out _), Is.False);
                Assert.That(panel.Open(TownService.Shop, player), Is.True);
                Assert.That(panel.ShopPageIndex, Is.Zero); panel.Close();
            } finally {
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(catalog);
                if (created) EditModeSceneFixture.Close(town);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }

        [TestCase(0, 278, 40)]
        [TestCase(2, 348, 40)]
        [TestCase(3, 278, 75)]
        [TestCase(11, 348, 145)]
        public void OriginalShopGridGeometry(int slot, int x, int y)
        {
            Assert.That(TownServicesPanel.ShopCell(slot, new Vector2(278, 40)), Is.EqualTo(new Rect(x, y, 32, 32)));
            Assert.Throws<ArgumentOutOfRangeException>(() => TownServicesPanel.ShopCell(-1, Vector2.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => TownServicesPanel.ShopCell(12, Vector2.zero));
        }

        [TestCase(100L, 0, 2, 30L, true, true)]
        [TestCase(100L, 0, 4, 30L, true, false)]
        [TestCase(0L, 2, 3, 10L, false, false)]
        [TestCase(0L, 2, 2, 10L, false, true)]
        [TestCase(0L, 0, 1, 0L, true, true)]
        [TestCase(100L, 1, 0, 1L, true, false)]
        [TestCase(100L, 1, 1, -1L, true, false)]
        [TestCase(long.MaxValue, 1, 2, long.MaxValue, true, false)]
        [TestCase(long.MaxValue, 1, 1, 1L, false, false)]
        [TestCase(100L, int.MaxValue, 1, 0L, true, false)]
        public void TradeAvailabilityMatchesExecutionAndDoesNotMutateSource(long gold, int owned, int quantity, long price, bool buying, bool expected)
        {
            var source = new NativeInventory { gold = gold, items = owned == 0 ? Array.Empty<NativeItemStack>()
                : new[] {new NativeItemStack {itemId = 1, quantity = owned}} };
            var original = source.items;
            for (int i = 0; i < 8; i++) {
                Assert.That(source.CanTrade(1, quantity, price, buying, out var reason), Is.EqualTo(expected));
                Assert.That(reason, Is.Not.Null.And.Not.Empty);
            }
            Assert.That(source.gold, Is.EqualTo(gold)); Assert.That(source.Count(1), Is.EqualTo(owned));
            Assert.That(source.items, Is.SameAs(original));
            Assert.That(source.TryTrade(1, quantity, price, buying, out _), Is.EqualTo(expected));
            Assert.That(source.gold, Is.EqualTo(gold)); Assert.That(source.Count(1), Is.EqualTo(owned));
        }

        [Test]
        public void InventoryTradeIsAtomicAndChecksOverflowFundsAndOwnedQuantity()
        {
            var source = new NativeInventory {gold = 100};
            Assert.That(source.TryTrade(1, 2, 30, true, out var bought), Is.True);
            Assert.That((bought.gold, bought.Count(1)), Is.EqualTo((40L, 2)));
            Assert.That((source.gold, source.Count(1)), Is.EqualTo((100L, 0)));
            Assert.That(bought.TryTrade(1, 3, 1, false, out _), Is.False);
            Assert.That(bought.TryTrade(1, 2, 30, true, out _), Is.False);
            Assert.That(bought.TryTrade(1, 0, 1, true, out _), Is.False);
            Assert.That(bought.TryTrade(1, 1, -1, false, out _), Is.False);
            Assert.That(bought.TryTrade(1, 2, long.MaxValue, true, out _), Is.False);
            Assert.That(bought.TryTrade(1, 2, 10, false, out var sold), Is.True);
            Assert.That((sold.gold, sold.items.Length), Is.EqualTo((60L, 0)));
            var maximum = new NativeInventory { gold = long.MaxValue, items = new[] {new NativeItemStack {itemId = 1, quantity = 1}} };
            Assert.That(maximum.TryTrade(1, 1, 1, false, out _), Is.False);
            Assert.That(new NativeInventory {items = new[] {new NativeItemStack {itemId = 1, quantity = 0}}}.IsValid, Is.False);
        }

        [Test]
        public void ShopUiSessionTradeAndDiskRestorePreserveInventoryAndUnknownLegacyState()
        {
            var before = SceneManager.GetActiveScene(); var town = SceneManager.GetSceneByName("Town_Runtime");
            bool created = !town.isLoaded;
            if (created) { town = EditModeSceneFixture.Create("Town_Runtime"); }
            var root = new GameObject("Shop check"); SceneManager.MoveGameObjectToScene(root, town);
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            var folder = Path.Combine(Application.temporaryCachePath, "TownShop-" + Guid.NewGuid().ToString("N"));
            try
            {
                var values = new long[ItemCatalogEntry.NumericColumnCount]; values[0] = 1;
                values[19] = 88; values[20] = 17; values[21] = -3; values[28] = 9; values[29] = 4;
                values[33] = 77; values[34] = 3; values[35] = 4; values[36] = -2; values[37] = 6;
                var ownedOnlyValues = new long[ItemCatalogEntry.NumericColumnCount]; ownedOnlyValues[0] = 2;
                catalog.Initialize(new[] {new ItemCatalogEntry(values, "30", "", "검증 아이템", "", "fixture"),
                    new ItemCatalogEntry(ownedOnlyValues, "0", "", "조회 전용", "", "fixture"),
                    new ItemCatalogEntry(new long[ItemCatalogEntry.NumericColumnCount], "0", "", "영번_아이템", "", "fixture")});
                var player = root.AddComponent<TopDownPlayerController>();
                var session = root.AddComponent<BattleTransitionController>();
                Assert.That(session.InitializeCharacter(new NativeCharacterProfile {name = "검증", jobId = 100}), Is.True);
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                save.inventory.gold = 100; Assert.That(session.RestoreSaveState(save), Is.True); // Isolated test funding only.
                var panel = root.AddComponent<TownServicesPanel>();
                var offers = new[] {new NativeShopOffer {itemId = 1, buyPrice = 30, sellPrice = 10}};
                Assert.That(panel.ConfigureShop(catalog, offers), Is.True); offers[0].buyPrice = 0;
                Assert.That(panel.Trade(1, true), Is.False);
                Assert.That(panel.CanTrade(1, 1, true, out _), Is.False);
                Assert.That(panel.Open(TownService.Shop, player), Is.True);
                Assert.That(panel.CanTrade(1, 3, true, out _), Is.True);
                Assert.That(panel.CanTrade(1, 4, true, out var reason), Is.False);
                StringAssert.Contains("소지금", reason);
                Assert.That(panel.CanTrade(1, 1, false, out reason), Is.False);
                StringAssert.Contains("보유", reason);
                Assert.That(panel.CanTrade(999, 1, true, out _), Is.False);
                Assert.That(panel.SelectedShopItem, Is.Null);
                Assert.That(panel.SelectShopItem(999), Is.False);
                Assert.That(panel.SelectShopItem(1), Is.True);
                Assert.That(panel.SelectedShopItem.Name, Is.EqualTo("검증 아이템"));
                Assert.That(session.CaptureInventory().gold, Is.EqualTo(100)); // Inspection never trades.
                string details = TownServicesPanel.ItemDetails(panel.SelectedShopItem);
                StringAssert.Contains("체력 88", details);
                StringAssert.Contains("공격 17 / 방어 -3", details);
                StringAssert.Contains("명중 9 / 회피 4", details);
                StringAssert.DoesNotContain("%", details); // Source units/effects are not invented.
                Assert.That(panel.TradeDroppedItem(999, true), Is.False);
                Assert.That(panel.TradeDroppedItem(1, false), Is.False);
                Assert.That(session.CurrentGold, Is.EqualTo(100));
                Assert.That(panel.TradeDroppedItem(1, true), Is.True);
                Assert.That(session.CurrentGold, Is.EqualTo(70)); Assert.That(session.OwnedItemCount(1), Is.EqualTo(1));
                Assert.That(panel.HasPendingTrade, Is.False);
                Assert.That(panel.TradeDroppedItem(1, false), Is.True);
                Assert.That(session.CurrentGold, Is.EqualTo(80)); Assert.That(session.OwnedItemCount(1), Is.Zero);
                Assert.That(session.RestoreSaveState(save), Is.True);
                StringAssert.Contains("원본 마렙치 77", details);
                StringAssert.Contains("이동 속도 향상 3 / 공격 속도 향상 4", details);
                StringAssert.Contains("휴식 속도 향상 -2 / 마법 속도 향상 6", details);
                var ownedOnly = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                ownedOnly.inventory.items = new[] {new NativeItemStack {itemId = 2, quantity = 3}};
                Assert.That(session.RestoreSaveState(ownedOnly), Is.True);
                Assert.That(panel.SelectShopOwnedItem(2), Is.True);
                Assert.That(panel.SelectedShopItem, Is.Null);
                Assert.That(panel.SelectedShopInspectionItem.Id, Is.EqualTo(2));
                Assert.That(panel.SelectShopOwnedItem(999), Is.False);
                Assert.That(panel.RequestTrade(2, 1, false), Is.False);
                Assert.That(panel.Trade(2, false), Is.False);
                Assert.That(session.OwnedItemCount(2), Is.EqualTo(3));
                Assert.That(session.CurrentGold, Is.EqualTo(100));
                Assert.That(session.RestoreSaveState(save), Is.True);
                Assert.That(panel.SelectedShopInspectionItem, Is.Null);
                Assert.That(panel.SelectShopItem(1), Is.True);
                Assert.That(panel.RequestTrade(1, 1, true), Is.True);
                Assert.That(panel.HasPendingTrade, Is.True);
                StringAssert.Contains("30", panel.PendingTradeText);
                Assert.That(session.CurrentGold, Is.EqualTo(100));
                Assert.That(panel.RequestTrade(1, 1, true), Is.False);
                Assert.That(panel.Trade(1, true), Is.False);
                Assert.That(panel.SelectShopItem(1), Is.False);
                Assert.That(panel.SelectShopOwnedItem(1), Is.False);
                Assert.That(panel.SetShopPage(0), Is.False);
                Assert.That(panel.CancelPendingTrade(), Is.True);
                Assert.That(panel.ConfirmPendingTrade(), Is.False);
                Assert.That(session.CurrentGold, Is.EqualTo(100));
                Assert.That(panel.RequestTrade(1, 1, true), Is.True);
                Assert.That(panel.ConfirmPendingTrade(), Is.True);
                Assert.That(session.CurrentGold, Is.EqualTo(70));
                Assert.That(panel.ConfirmPendingTrade(), Is.False);
                Assert.That(session.RestoreSaveState(save), Is.True);
                Assert.That(panel.RequestTrade(1, 1, true), Is.True);
                var emptyFunds = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                emptyFunds.inventory.gold = 0;
                Assert.That(session.RestoreSaveState(emptyFunds), Is.True);
                Assert.That(panel.ConfirmPendingTrade(), Is.False); // Revalidate at confirmation.
                Assert.That(session.OwnedItemCount(1), Is.Zero);
                Assert.That(session.RestoreSaveState(save), Is.True);
                Assert.That(panel.RequestTrade(1, 1, true), Is.True);
                panel.Close(); Assert.That(panel.HasPendingTrade, Is.False);
                Assert.That(panel.Open(TownService.Shop, player), Is.True);
                Assert.That(panel.ConfirmPendingTrade(), Is.False);
                Assert.That(panel.SelectShopItem(1), Is.True);
                Assert.That(panel.Trade(1, true), Is.True);
                Assert.That(session.CaptureInventory().gold, Is.EqualTo(70));
                Assert.That(panel.CanTrade(1, 1, false, out _), Is.True);
                Assert.That(panel.CanTrade(1, 2, false, out _), Is.False);
                var copy = session.CaptureInventory(); copy.items[0].quantity = 100;
                Assert.That(session.CaptureInventory().Count(1), Is.EqualTo(1));
                Assert.That(panel.Trade(1, false), Is.True);
                Assert.That(session.CaptureInventory().gold, Is.EqualTo(80));
                Assert.That(panel.Trade(1, false), Is.False);
                Assert.That(panel.ConfigureShop(catalog, new[] {new NativeShopOffer {itemId = 999}}), Is.False);
                Assert.That(panel.Trade(1, true), Is.True);
                var store = new NativeSaveStore(Path.Combine(folder, "save.json"));
                Assert.That(store.TrySave(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one), out _), Is.True);
                Assert.That(store.TryLoad(out var loaded, out _), Is.True);
                Assert.That(loaded.inventory.gold, Is.EqualTo(50)); Assert.That(loaded.inventory.Count(1), Is.EqualTo(1));
                panel.Close(); Assert.That(session.RestoreSaveState(loaded), Is.True);
                loaded.inventory.items[0].quantity = 99; Assert.That(session.CaptureInventory().Count(1), Is.EqualTo(1));
                Assert.That(panel.Open(TownService.Inventory, player), Is.True);
                Assert.That(panel.SelectedShopItem, Is.Null);
                Assert.That(panel.SelectedInventoryItem, Is.Null);
                Assert.That(panel.SelectShopItem(1), Is.False);
                Assert.That(panel.SelectOwnedItem(999), Is.False);
                Assert.That(panel.SelectOwnedItem(1), Is.True);
                Assert.That(panel.SelectedInventoryItem.Name, Is.EqualTo("검증 아이템"));
                var owned = panel.CaptureOwnedItems(); owned[0].quantity = 100;
                Assert.That(session.CaptureInventory().Count(1), Is.EqualTo(1));
                panel.Close();
                var npcObject = new GameObject("Facility NPC fixture"); npcObject.transform.SetParent(root.transform);
                var npc = npcObject.AddComponent<TownNpcInteraction>();
                Assert.That(npc.ConfigureFacility("상점 검증", new[] {"대화"}, TownService.Shop), Is.True);
                Assert.That(npc.ConfigureFacility("잘못된 설정", new[] {""}, TownService.Inventory), Is.False);
                Assert.That(npc.TryInteract(player, panel), Is.True);
                Assert.That(panel.Service, Is.EqualTo(TownService.Dialogue));
                Assert.That(panel.NextLine(), Is.True); Assert.That(panel.Service, Is.EqualTo(TownService.Shop));
                Assert.That(player.UiInputBlocked, Is.True);
                panel.Close(); Assert.That(player.UiInputBlocked, Is.False);
                var legacy = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                legacy.hasInventory = false; legacy.inventory = null;
                Assert.That(session.RestoreSaveState(legacy), Is.True); Assert.That(session.CaptureInventory(), Is.Null);
                Assert.That(panel.Open(TownService.Shop, player), Is.True); Assert.That(panel.Trade(1, true), Is.False);
                panel.Close();
                legacy.hasInventory = true; legacy.inventory = new NativeInventory {gold = 1000};
                Assert.That(session.RestoreSaveState(legacy), Is.True);
                Assert.That(panel.Trade(1, 3, true), Is.False);
                Assert.That(panel.Open(TownService.Shop, player), Is.True);
                Assert.That(panel.Trade(1, 3, true), Is.True);
                Assert.That(session.CaptureInventory().gold, Is.EqualTo(910));
                Assert.That(panel.Trade(1, 2, false), Is.True);
                Assert.That(session.CaptureInventory().gold, Is.EqualTo(930));
                Assert.That(session.CaptureInventory().Count(1), Is.EqualTo(1));
                Assert.That(panel.Trade(1, 0, true), Is.False);
                Assert.That(panel.Trade(1, int.MaxValue, true), Is.False);
                Assert.That(session.CaptureInventory().gold, Is.EqualTo(930));
                Assert.That(panel.SaveTown(), Is.False); // Fixture lacks a loaded Kaosia map: never save an ambiguous world.
                Assert.That(player.UiInputBlocked, Is.True);
                var formation = root.AddComponent<BattleFormationPanel>();
                Assert.That(panel.OpenFormation(), Is.True);
                Assert.That(panel.IsOpen, Is.False); Assert.That(formation.IsOpen, Is.True);
                Assert.That(player.UiInputBlocked, Is.True);
                formation.Cancel(); Assert.That(player.UiInputBlocked, Is.False);
                var zeroState = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                zeroState.inventory = new NativeInventory { gold = 0, items = new[] {new NativeItemStack { itemId = 0, quantity = 1 }} };
                Assert.That(session.RestoreSaveState(zeroState), Is.False); // Catalog ID 0 is not a valid owned item.
                Assert.That(panel.Open(TownService.Inventory, player), Is.True);
                Assert.That(panel.SelectedInventoryItem, Is.Null);
                Assert.That(panel.SelectOwnedItem(0), Is.False);
                Assert.That(panel.SelectOwnedItem(1), Is.True);
                Assert.That(panel.SelectedInventoryItem.Id, Is.EqualTo(1));
                panel.Close(); Assert.That(panel.SelectedInventoryItem, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(catalog);
                if (created) EditModeSceneFixture.Close(town);
                if (before.IsValid() && before.isLoaded) SceneManager.SetActiveScene(before);
                if (File.Exists(Path.Combine(folder, "save.json"))) File.Delete(Path.Combine(folder, "save.json"));
                if (Directory.Exists(folder)) Directory.Delete(folder);
            }
        }
    }
}
