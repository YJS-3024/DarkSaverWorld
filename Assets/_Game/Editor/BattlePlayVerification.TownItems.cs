using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        [MenuItem("DarkSaver/Verification/Run town item details and trade (isolated)")]
        public static void BeginTownItems()
        {
            SessionState.SetBool(Key + "townHiring", false);
            SessionState.SetBool(Key + "townItems", true);
            SessionState.SetBool(Key + "classChange", false);
            SessionState.SetBool(Key + "mercenaryDeployment", false);
            try { BeginTownTraining(); }
            catch { SessionState.SetBool(Key + "townItems", false); throw; }
        }

        static void PrepareTownItems(BattleTransitionController session, TopDownPlayerController player)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(DarkSaver.Editor.ItemCatalogConverter.Output);
            RequireMercenary(catalog != null, "Native item catalog missing");
            ItemCatalogEntry item = null;
            for (int i = 0; i < catalog.Count; i++) {
                var row = catalog.GetAt(i);
                if (row.Id > 0 && !string.IsNullOrEmpty(row.Name)) { item = row; break; }
            }
            RequireMercenary(item != null, "Native named item missing");
            var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
            state.hasInventory = true; state.inventory = new NativeInventory { gold = 1000 };
            RequireMercenary(session.RestoreSaveState(state), "Isolated inventory fixture rejected");
            var panel = session.GetComponent<TownServicesPanel>();
            RequireMercenary(panel.ConfigureShop(catalog, new[] { new NativeShopOffer { itemId = item.Id, buyPrice = 30, sellPrice = 10 } })
                && panel.Open(TownService.Shop, player) && panel.SelectShopItem(item.Id), "Shop inspection unavailable");
            SessionState.SetInt(Key + "itemId", item.Id);
            SessionState.SetInt(Key + "stage", 50);
            Note("Native item detail fixture; quotes 30/10 and gold1000 are isolated, not original town stock/prices");
        }

        static T TownItemElement<T>(TownServicesPanel panel, string name) where T : VisualElement
        {
            foreach (var document in panel.GetComponentsInChildren<UIDocument>()) {
                var element = document.rootVisualElement.Q<T>(name);
                if (element != null && element.panel != null && element.resolvedStyle.display != DisplayStyle.None
                    && element.worldBound.width > 0) return element;
            }
            return null;
        }

        static void ClickTownItem(TownServicesPanel panel, string key)
        {
            var button = TownItemElement<Button>(panel, key);
            RequireMercenary(button != null && button.enabledInHierarchy, "Town button not ready: " + key);
            var point = button.worldBound.center;
            var picked = button.panel.Pick(point);
            RequireMercenary(picked == button || button.Contains(picked), "Town button obstructed: " + key);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point })) button.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point })) button.SendEvent(up);
        }

        static void TickTownItems(int stage)
        {
            if (SessionState.GetBool(Key + "townItems", false)) { TickTownDragItems(stage); return; }
            if (stage >= 70 && stage < 90) { TickTownHiring(stage); return; }
            if (EditorApplication.timeSinceStartup < nextAction) return;
            nextAction = EditorApplication.timeSinceStartup + .3;
            var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
            var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
            if (session == null || player == null || session.IsTransitioning) return;
            var panel = session.GetComponent<TownServicesPanel>();
            int id = SessionState.GetInt(Key + "itemId", -1);
            void Step() => SessionState.SetInt(Key + "stage", stage + 1);
            void Unchanged(long gold, int quantity) => RequireMercenary(session.CaptureInventory().gold == gold
                && session.CaptureInventory().Count(id) == quantity, "Item inspection/transaction changed unexpected resources");
            ScrollView Scroll(string key) => TownItemElement<ScrollView>(panel, key);
            if (stage == 50) {
                RequireMercenary(TownItemElement<VisualElement>(panel, "town-service-native")?.style.backgroundImage.value.texture != null,
                    "Item selection replaced native shop window");
                RequireMercenary(TownItemElement<Button>(panel, "shop-detail-buy")?.enabledInHierarchy == true
                    && TownItemElement<Button>(panel, "shop-detail-sell")?.enabledInHierarchy == false,
                    "Initial purchase/sale availability mismatch");
                var scroll = Scroll("shop-detail-scroll"); if (scroll == null) return;
                RequireMercenary(scroll.verticalScrollerVisibility == ScrollerVisibility.Hidden
                    && !TownItemElement<Button>(panel, "shop-detail-up").enabledInHierarchy
                    && TownItemElement<Button>(panel, "shop-detail-down").enabledInHierarchy,
                    "Native detail controls or top boundary missing");
                RequireMercenary(TownItemElement<VisualElement>(panel, "shop-detail-icon")?.resolvedStyle.backgroundImage.sprite != null,
                    "Native shop item icon missing");
                RequireMercenary(scroll.contentContainer.layout.height > scroll.contentViewport.layout.height, "Shop detail cannot scroll");
                Unchanged(1000, 0); ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-detail-top.png")); Step(); return;
            }
            if (stage == 51) {
                ClickTownItem(panel, "shop-detail-down");
                RequireMercenary(Scroll("shop-detail-scroll").scrollOffset.y > 0, "Native down button did not scroll");
                Scroll("shop-detail-scroll").scrollOffset = new Vector2(0, 10000); Step(); return;
            }
            if (stage == 52) {
                RequireMercenary(Scroll("shop-detail-scroll").scrollOffset.y > 0, "Shop detail scroll did not move");
                var scroll = Scroll("shop-detail-scroll");
                RequireMercenary(!TownItemElement<Button>(panel, "shop-detail-down").enabledInHierarchy
                    && TownItemElement<Button>(panel, "shop-detail-up").enabledInHierarchy, "Native detail bottom boundary mismatch");
                Unchanged(1000, 0); ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-detail-bottom.png")); Step(); return;
            }
            if (stage == 53) { ClickTownItem(panel, "shop-list"); Step(); return; }
            if (stage == 54) {
                RequireMercenary(panel.SelectedShopItem == null, "Shop list return retained selection");
                RequireMercenary(!TownItemElement<Button>(panel, "shop-prev").enabledInHierarchy
                    && !TownItemElement<Button>(panel, "shop-next").enabledInHierarchy, "Single-offer navigation enabled");
                ClickTownItem(panel, "shop-item-0"); SessionState.SetInt(Key + "stage", 90); return;
            }
            if (stage == 90) {
                ClickTownItem(panel, "shop-detail-buy"); Unchanged(1000, 0);
                RequireMercenary(panel.HasPendingTrade, "Buy bypassed confirmation");
                SessionState.SetInt(Key + "stage", 96); return;
            }
            if (stage == 96) {
                RequireMercenary(TownItemElement<VisualElement>(panel, "trade-confirm-window")?.style.backgroundImage.value.texture != null, "Native confirmation missing");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-buy-confirmation.png"));
                SessionState.SetInt(Key + "stage", 97); return;
            }
            if (stage == 97) {
                ClickTownItem(panel, "trade-confirm-no"); Unchanged(1000, 0);
                RequireMercenary(!panel.HasPendingTrade && player.UiInputBlocked, "Cancel released shop lock or retained trade");
                SessionState.SetInt(Key + "stage", 99); return;
            }
            if (stage == 99) {
                ClickTownItem(panel, "shop-detail-buy"); SessionState.SetInt(Key + "stage", 100); return;
            }
            if (stage == 100) {
                ClickTownItem(panel, "trade-confirm-yes"); Unchanged(970, 1);
                RequireMercenary(!panel.ConfirmPendingTrade(), "Repeated confirm traded twice");
                RequireMercenary(panel.Open(TownService.Inventory, player) && panel.SelectOwnedItem(id), "Owned inspection unavailable");
                SessionState.SetInt(Key + "stage", 55); return;
            }
            if (stage == 55) {
                var scroll = Scroll("inventory-detail-scroll"); if (scroll == null) return;
                RequireMercenary(TownItemElement<VisualElement>(panel, "inventory-detail-icon")?.resolvedStyle.backgroundImage.sprite != null,
                    "Native owned item icon missing");
                RequireMercenary(scroll.contentContainer.layout.height > scroll.contentViewport.layout.height, "Inventory detail cannot scroll");
                Unchanged(970, 1); ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-inventory-detail-top.png")); Step(); return;
            }
            if (stage == 56) { Scroll("inventory-detail-scroll").scrollOffset = new Vector2(0, 10000); Step(); return; }
            if (stage == 57) {
                RequireMercenary(Scroll("inventory-detail-scroll").scrollOffset.y > 0, "Inventory scroll did not move");
                var scroll = Scroll("inventory-detail-scroll");
                var label = scroll.Q<Label>("inventory-detail");
                float measured = label.MeasureTextSize(label.text, label.layout.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y;
                RequireMercenary(Mathf.Abs(scroll.contentContainer.layout.height - Mathf.Max(scroll.contentViewport.layout.height, Mathf.Ceil(measured) + 4)) < 8,
                    "Inventory detail has excess trailing blank space");
                Unchanged(970, 1); ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-inventory-detail-bottom.png")); Step(); return;
            }
            if (stage == 58) {
                RequireMercenary(panel.Open(TownService.Shop, player) && panel.SelectShopItem(id), "Shop detail reopen failed"); Step(); return;
            }
            if (stage == 59) {
                var scroll = Scroll("shop-detail-scroll"); if (scroll == null) return;
                RequireMercenary(scroll.scrollOffset.y < 1, "Reopened detail retained old scroll");
                ClickTownItem(panel, "shop-detail-sell"); Unchanged(970, 1);
                RequireMercenary(panel.HasPendingTrade, "Sell bypassed confirmation");
                SessionState.SetInt(Key + "stage", 98); return;
            }
            if (stage == 98) {
                ClickTownItem(panel, "trade-confirm-yes"); Unchanged(980, 0);
                RequireMercenary(panel.SaveTown(), "Isolated trade save failed");
                var store = new NativeSaveStore(Path.Combine(output, "verification-save.json"));
                RequireMercenary(store.TryLoad(out var saved, out _) && saved.inventory.gold == 980 && saved.inventory.Count(id) == 0, "Trade disk persistence mismatch");
                TownItemElement<TextField>(panel, "shop-quantity").value = "-1";
                SessionState.SetInt(Key + "stage", 60); return;
            }
            if (stage == 60) {
                RequireMercenary(!TownItemElement<Button>(panel, "shop-detail-buy").enabledInHierarchy
                    && !TownItemElement<Button>(panel, "shop-detail-sell").enabledInHierarchy, "Invalid quantity buttons enabled");
                Unchanged(980, 0); ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-invalid-quantity.png"));
                SessionState.SetInt(Key + "stage", 63); return;
            }
            if (stage == 61) {
                var buy = TownItemElement<Button>(panel, "shop-detail-buy");
                RequireMercenary(!buy.enabledInHierarchy && buy.tooltip.Contains("소지금"), "Insufficient funds button/reason mismatch");
                RequireMercenary(!TownItemElement<Button>(panel, "shop-detail-sell").enabledInHierarchy, "Unowned sale enabled");
                Unchanged(980, 0); ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-insufficient-funds.png"));
                SessionState.SetInt(Key + "stage", 64); return;
            }
            if (stage == 63) {
                TownItemElement<TextField>(panel, "shop-quantity").value = "34";
                SessionState.SetInt(Key + "stage", 61); return;
            }
            if (stage == 64) {
                TownItemElement<TextField>(panel, "shop-quantity").value = "1";
                SessionState.SetInt(Key + "stage", 62); return;
            }
            if (stage == 62) {
                RequireMercenary(TownItemElement<Button>(panel, "shop-detail-buy").enabledInHierarchy
                    && !TownItemElement<Button>(panel, "shop-detail-sell").enabledInHierarchy, "Quantity correction did not refresh buttons");
                Unchanged(980, 0);
                panel.Close();
                var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(DarkSaver.Editor.ItemCatalogConverter.Output);
                var stacks = new System.Collections.Generic.List<NativeItemStack>();
                for (int i = 0; i < catalog.Count && stacks.Count < 29; i++)
                    if (catalog.GetAt(i).Id > 0) stacks.Add(new NativeItemStack {itemId = catalog.GetAt(i).Id, quantity = stacks.Count + 1});
                RequireMercenary(stacks.Count == 29, "Grid fixture items missing");
                var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
                state.inventory.items = stacks.ToArray();
                RequireMercenary(session.RestoreSaveState(state) && panel.Open(TownService.Inventory, player), "Grid fixture rejected");
                SessionState.SetInt(Key + "gridLast", stacks[28].itemId);
                SessionState.SetInt(Key + "stage", 65); return;
            }
            if (stage == 65) {
                RequireMercenary(panel.InventoryPageCount == 2 && panel.InventoryPageIndex == 0, "Grid page count mismatch");
                for (int slot = 0; slot < 28; slot++)
                    RequireMercenary(TownItemElement<Button>(panel, "owned-" + slot)?.enabledInHierarchy == true, "Grid first page cell missing");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-inventory-grid-page1.png"));
                SessionState.SetInt(Key + "stage", 68); return;
            }
            if (stage == 68) {
                ClickTownItem(panel, "inventory-next"); SessionState.SetInt(Key + "stage", 66); return;
            }
            if (stage == 66) {
                RequireMercenary(panel.InventoryPageIndex == 1 && !TownItemElement<Button>(panel, "inventory-next").enabledInHierarchy,
                    "Last grid page navigation mismatch");
                for (int slot = 1; slot < 28; slot++)
                    RequireMercenary(!TownItemElement<Button>(panel, "owned-" + slot).enabledInHierarchy, "Empty grid cell enabled");
                ClickTownItem(panel, "owned-0"); SessionState.SetInt(Key + "stage", 67); return;
            }
            if (stage == 67) {
                RequireMercenary(panel.SelectedInventoryItem?.Id == SessionState.GetInt(Key + "gridLast", -1), "Reused grid cell selected stale first-page item");
                RequireMercenary(session.CurrentGold == 980 && session.OwnedItemTypeCount == 29, "Grid browsing mutated resources");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-inventory-grid-page2.png"));
                SessionState.SetInt(Key + "stage", 69); return;
            }
            if (stage == 69) {
                panel.Close(); RequireMercenary(!player.UiInputBlocked, "Town input lock leaked");
                var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(DarkSaver.Editor.ItemCatalogConverter.Output);
                var offers = new NativeShopOffer[13];
                for (int slot = 0; slot < offers.Length; slot++) {
                    RequireMercenary(session.TryGetOwnedItemAt(slot, out int ownedId, out _), "Shop grid fixture item missing");
                    offers[slot] = new NativeShopOffer {itemId = ownedId, buyPrice = 30, sellPrice = 10};
                }
                RequireMercenary(panel.ConfigureShop(catalog, offers) && panel.Open(TownService.Shop, player), "Shop grid fixture rejected");
                SessionState.SetInt(Key + "shopGridLast", offers[12].itemId);
                SessionState.SetInt(Key + "stage", 91); return;
            }
            if (stage == 91) {
                RequireMercenary(TownItemElement<VisualElement>(panel, "town-service-native")?.style.backgroundImage.value.texture != null, "Native shop background missing");
                RequireMercenary(panel.ShopPageCount == 2 && panel.ShopPageIndex == 0, "Shop row count mismatch");
                for (int slot = 0; slot < 12; slot++)
                    RequireMercenary(TownItemElement<Button>(panel, "shop-item-" + slot)?.enabledInHierarchy == true, "Shop cell missing");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-grid-row1.png")); Step(); return;
            }
            if (stage == 92) { ClickTownItem(panel, "shop-next"); Step(); return; }
            if (stage == 93) {
                RequireMercenary(panel.ShopPageIndex == 1 && !TownItemElement<Button>(panel, "shop-next").enabledInHierarchy, "Shop last row navigation mismatch");
                RequireMercenary(panel.TryGetShopCell(0, out var first) && session.TryGetOwnedItemAt(3, out int fourth, out _) && first.Id == fourth, "Shop did not advance exactly three items");
                for (int slot = 10; slot < 12; slot++)
                    RequireMercenary(!TownItemElement<Button>(panel, "shop-item-" + slot).enabledInHierarchy, "Empty shop cell enabled");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-grid-row2.png")); Step(); return;
            }
            if (stage == 94) { ClickTownItem(panel, "shop-item-9"); Step(); return; }
            if (stage == 95) {
                RequireMercenary(panel.SelectedShopItem?.Id == SessionState.GetInt(Key + "shopGridLast", -1), "Recycled shop cell selected stale item");
                RequireMercenary(session.CurrentGold == 980 && session.OwnedItemTypeCount == 29, "Shop browsing mutated resources");
                ClickTownItem(panel, "shop-owned-13"); SessionState.SetInt(Key + "stage", 101); return;
            }
            if (stage == 101) {
                RequireMercenary(session.TryGetOwnedItemAt(13, out int ownedOnly, out _) && panel.SelectedShopInspectionItem?.Id == ownedOnly
                    && panel.SelectedShopItem == null, "Unlisted owned item inspection missing");
                RequireMercenary(TownItemElement<Button>(panel, "shop-detail-buy") == null && TownItemElement<Button>(panel, "shop-detail-sell") == null,
                    "Unlisted owned item exposed a trade button");
                RequireMercenary(session.CurrentGold == 980 && session.OwnedItemCount(ownedOnly) == 14, "Owned inspection changed resources");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-owned-inspection.png"));
                SessionState.SetInt(Key + "stage", 102); return;
            }
            if (stage == 102) {
                RequireMercenary(TownItemElement<Button>(panel, "close")?.style.backgroundImage.value.texture != null, "Native town close artwork missing");
                ClickTownItem(panel, "close"); RequireMercenary(!panel.IsOpen && !player.UiInputBlocked, "Native close/input lock leaked");
                Finish("PASS: persistent native shop/details -> confirmation/cancel/buy/sell -> disk save -> quantity/funds guards -> inventory/shop navigation -> unlisted owned inspection without invented quote -> input release");
            }
        }
    }
}
