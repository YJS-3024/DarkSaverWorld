using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Items;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private int inventoryPage, selectedInventoryId = -1;
        private ItemCatalogEntry detailSource;
        private string detailText;
        private float detailWidth, detailHeight;
        private int detailTroopType = -1;
        private NativeBattleWindows nativeInventoryCatalog;
        private NativeBattleWindows minimizedInventoryCatalog;
        private NativeBattleWindows.Control inventoryRestoreControl;
        public bool IsInventoryMinimized { get; private set; }
        private NativeBattleWindows.Window MinimizedInventoryWindow {
            get {
                if (minimizedInventoryCatalog == null) minimizedInventoryCatalog = Resources.Load<NativeBattleWindows>("UI/TownInventoryMinimized/NativeTownInventoryMinimized");
                return minimizedInventoryCatalog != null ? minimizedInventoryCatalog.Find(4) : null;
            }
        }
        public bool SetInventoryMinimized(bool minimized)
        {
            if (!IsOpen || Service != TownService.Inventory || !CanOpen || IsInventoryMinimized == minimized
                || NativeInventoryWindow == null || MinimizedInventoryWindow == null) return false;
            ui?.CancelItemDrags(); IsInventoryMinimized = minimized; return true;
        }
        private void DrawMinimizedInventory()
        {
            var window = MinimizedInventoryWindow;
            if (window == null) { IsInventoryMinimized = false; return; }
            if (inventoryRestoreControl == null) inventoryRestoreControl = new NativeBattleWindows.Control {
                rect = new Rect(0,0,window.size.x,window.size.y),
                frames = new[] {window.background,window.background,window.background}
            };
            var position = new Vector2((Rendering.GameScreen.Width-window.size.x)*.5f,64+(227-window.size.y)*.5f);
            ui.NativeImageButton("inventory-restore",position,inventoryRestoreControl,()=>SetInventoryMinimized(false),true,"장비 창 복원 (로컬 클릭)");
        }
        private NativeBattleWindows.Window NativeInventoryWindow {
            get {
                if (nativeInventoryCatalog == null) nativeInventoryCatalog = Resources.Load<NativeBattleWindows>("UI/TownInventory/NativeTownInventory");
                return nativeInventoryCatalog != null ? nativeInventoryCatalog.Find(4) : null;
            }
        }
        private ItemCatalog InventoryCatalog => GetComponent<ItemCatalogPanel>()?.Catalog ?? shopItems;
        public NativeItemStack[] CaptureOwnedItems() => GetComponent<BattleTransitionController>()?.CaptureInventory()?.items;
        public ItemCatalogEntry SelectedInventoryItem => GetComponent<BattleTransitionController>()?.OwnedItemCount(selectedInventoryId) > 0
            && InventoryCatalog != null && InventoryCatalog.TryGet(selectedInventoryId, out var item) ? item : null;
        public bool SelectOwnedItem(int itemId)
        {
            if (!IsOpen || Service != TownService.Inventory || !CanOpen
                || (GetComponent<BattleTransitionController>()?.OwnedItemCount(itemId) ?? 0) <= 0) return false;
            selectedInventoryId = itemId; equipmentBaseline=GetComponent<BattleTransitionController>()?.CaptureCharacterProfile(); return true;
        }
        public static string ItemDetails(ItemCatalogEntry item)
        {
            if (item == null) return "아이템 정보가 없습니다.";
            // Original table values, not calculated equipment bonuses or server rules.
            string Value(int column) => item.GetNumericValue(column).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return ItemCatalogPanel.DisplayName(item) + "\n" + (item.Description ?? "").Replace('_', ' ')
                + "\n장착 위치 값 " + Value(3) + " / 요구 레벨 " + Value(4) + "\n내구 " + Value(5)
                + "\n원본 슬롯 후보(0~6): " + ItemUsageConditions.EquipmentSlotsSummary(item)
                + "\n병종 허용: " + ItemUsageConditions.PermissionSummary(item)
                + (item.GetNumericValue(4) < 0 ? "\n음수 요구 레벨은 원본 값이며 의미 미확정" : "")
                + "\n이동 " + Value(18) + " / 체력 " + Value(19)
                + "\n공격 " + Value(20) + " / 방어 " + Value(21) + " / 공격 범위 " + Value(22)
                + "\n마법력 " + Value(23) + "\n마법 공격 " + Value(24) + " / 마법 방어 " + Value(25)
                + "\n마법 범위 " + Value(26) + " / 지휘 범위 " + Value(27)
                + "\n명중 " + Value(28) + " / 회피 " + Value(29) + " / 크리티컬 " + Value(30)
                + "\n마법 명중 " + Value(31) + " / 마법 회피 " + Value(32)
                + "\n원본 마렙치 " + Value(33)
                + "\n이동 속도 향상 " + Value(34) + " / 공격 속도 향상 " + Value(35)
                + "\n휴식 속도 향상 " + Value(36) + " / 마법 속도 향상 " + Value(37)
                + "\n원본 데이터 값입니다. 장비 보정은 확인된 장착 상태를 기준으로 합산합니다.";
        }
        private void DrawItemDetails(string key, Rect area, ItemCatalogEntry item, bool nativePaper = false)
        {
            var icon = ItemIcons.Get(item);
            if (icon != null) {
                ui.Portrait(key + "-icon", new Rect(area.x, area.y, 32, 32), icon);
                area.x += 36; area.width -= 36;
            }
            var session = GetComponent<BattleTransitionController>();
            int troopType = session != null && session.TryGetPlayerTroopType(out int foundType) ? foundType : -1;
            bool changed = detailSource != item || detailWidth != area.width || detailTroopType != troopType;
            if (changed) {
                detailSource = item; detailWidth = area.width; detailTroopType = troopType;
                string permission = ItemUsageConditions.TryGetPermission(item, troopType, out bool allowed)
                    ? ItemUsageConditions.TypeName(troopType) + (allowed ? " / 원본 허용" : " / 원본 제한") : "미확인";
                detailText = ItemDetails(item) + "\n현재 병종 조건: " + permission
                    + "\n장착 시 위치와 베이스 레벨 조건도 적용됩니다.";
                detailHeight = area.height;
            }
            if (nativePaper) {
                var window = NativeShopWindow;
                ui.NativeBitmapScrollText(key, area, detailText, window.controls[0], window.controls[1], changed);
                return;
            }
            float height = Mathf.Max(area.height, detailHeight);
            var parent = ui.Scroll(key + "-scroll", area, height, changed);
            ui.Label(key, new Rect(0, 0, area.width - 24, height), detailText, 12, parent,
                TextAnchor.UpperLeft, nativePaper ? new Color(.17f, .09f, .035f) : (Color?)null);
            var label=ui.Find<Text>(key);
            float measured=label.preferredHeight;
            if(!float.IsNaN(measured) && !float.IsInfinity(measured) && measured>0)
            {
                detailHeight=Mathf.Max(area.height,Mathf.Ceil(measured)+4);
                parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,detailHeight);
                label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,detailHeight);
            }
        }
        public int InventoryPageIndex => inventoryPage;
        public int InventoryPageCount => InventoryGridLayout.PageCount(GetComponent<BattleTransitionController>()?.OwnedItemTypeCount ?? 0);
        public bool SetInventoryPage(int page)
        {
            if (!IsOpen || Service != TownService.Inventory || !CanOpen || page < 0 || page >= InventoryPageCount) return false;
            inventoryPage = page; return true;
        }
        public bool TryGetInventoryCell(int slot, out int id, out int quantity)
        {
            id = quantity = 0;
            if (!IsOpen || Service != TownService.Inventory || !CanOpen || slot < 0 || slot >= InventoryGridLayout.PageSize) return false;
            var session = GetComponent<BattleTransitionController>();
            return session != null && session.TryGetOwnedItemAt(inventoryPage * InventoryGridLayout.PageSize + slot, out id, out quantity);
        }
        private void DrawInventory(Rect panel)
        {
            var window = NativeInventoryWindow;
            if (window == null) {
                ui.Label("inventory-unavailable", new Rect(panel.x + 20, 110, panel.width - 40, 60), "원본 장비·소지품 UI 리소스를 확인하세요.");
                return;
            }
            var session = GetComponent<BattleTransitionController>();
            int? count = session?.OwnedItemTypeCount;
            int pages = InventoryGridLayout.PageCount(count ?? 0);
            inventoryPage = Mathf.Clamp(inventoryPage, 0, pages - 1);
            for (int slot = 0; slot < InventoryGridLayout.PageSize; slot++) {
                bool owned = TryGetInventoryCell(slot, out int id, out int quantity);
                var item = owned && InventoryCatalog != null && InventoryCatalog.TryGet(id, out var found) ? found : null;
                string hint = !owned ? "빈 칸" : (item != null ? ItemCatalogPanel.DisplayName(item) : "미확인 아이템 #" + id) + " ×" + quantity;
                ui.ItemSlotButton("owned-" + slot, InventoryGridLayout.Cell(slot, panel.position + new Vector2(146, 42)),
                    () => SelectOwnedItem(id), ItemIcons.Get(item), owned ? quantity : 0, owned && selectedInventoryId == id, hint);
            }
            ui.NativeImageButton("inventory-minimize", panel.position + window.controls[0].rect.position, window.controls[0], () => SetInventoryMinimized(true), MinimizedInventoryWindow != null, "최소화");
            ui.NativeImageButton("close", panel.position + window.controls[1].rect.position, window.controls[1], Close, true, "닫기");
            ui.NativeImageButton("inventory-confirm", panel.position + window.controls[2].rect.position, window.controls[2], Close, true, "닫기");
            // Extra pages/details/save controls are local conveniences, outside the original window.
            ui.Panel("inventory-local-detail", new Rect(panel.x, 300, panel.width, 70)); ui.Raise("inventory-local-detail");
            var area = new Rect(panel.x + 8, 304, panel.width - 16, 62);
            var selected = SelectedInventoryItem;
            if (selected != null) DrawItemDetails("inventory-detail", area, selected);
            else ui.Label("inventory-detail-empty", area, count == null ? "이 저장에는 소지품 상태가 없습니다."
                : count == 0 ? "소지품이 없습니다." : selectedInventoryId >= 0 ? "선택한 아이템의 상세 정보가 없습니다." : "소지품을 선택하세요.");
            ui.Raise("inventory-detail-icon"); ui.Raise("inventory-detail-scroll"); ui.Raise("inventory-detail-empty");
            ui.NativeLibraryButton("inventory-prev", new Vector2(panel.x + 8, 382), 40, () => SetInventoryPage(inventoryPage - 1), inventoryPage > 0, "이전 페이지 (로컬)");
            ui.NativeLibraryButton("inventory-next", new Vector2(panel.x + 30, 382), 39, () => SetInventoryPage(inventoryPage + 1), inventoryPage + 1 < pages, "다음 페이지 (로컬)");
            ui.Label("inventory-page", new Rect(panel.x + 52, 382, 52, 28), (inventoryPage + 1) + "/" + pages, 12);
            ui.Raise("inventory-prev"); ui.Raise("inventory-next"); ui.Raise("inventory-page");
            DrawEquipmentSlots(panel);
            // Original renderer uses right anchor (104,193); keep digits in its gold box.
            ui.NativeBitmapText("inventory-gold", new Rect(panel.x + 50, panel.y + 193, 54, 14), session?.CurrentGold?.ToString() ?? "미확인",
                new Color(1,1,.25f), alignment: TextAnchor.MiddleRight);
        }
    }
}
