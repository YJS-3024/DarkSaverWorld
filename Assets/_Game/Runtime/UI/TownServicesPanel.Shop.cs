using DarkSaver.Combat;
using DarkSaver.Items;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        [SerializeField] private ItemCatalog shopItems;
        [SerializeField] private NativeShopOffer[] shopOffers = System.Array.Empty<NativeShopOffer>();
        private int shopPage;
        private bool shopConfiguredExplicitly;
        private bool settingAutomaticStock;
        private string automaticStockTown;
        private NativeShopOffer pendingTrade;
        private int pendingTradeQuantity;
        private bool pendingTradeBuying;
        private NativeBattleWindows confirmationCatalog;
        public int ConfiguredShopOfferCount => shopOffers.Length;
        public bool HasShopItemCatalog => shopItems != null;
        private void Start() => EnsureAnalyzedTownBindings();
        private void EnsureAnalyzedTownBindings()
        {
            if (shopItems == null) shopItems = GetComponent<ItemCatalogPanel>()?.Catalog;
            string townKey = DarkSaver.World.TownSceneContext.Current?.Key;
            var interior=DarkSaver.World.ShopInteriorContext.Current;
            string stockKey=interior?.Key ?? townKey;
            if (shopItems == null || townKey == null || shopConfiguredExplicitly || automaticStockTown == stockKey) return;
            if (automaticStockTown == null && shopOffers.Length != 0) { shopConfiguredExplicitly = true; return; }
            var stock = new System.Collections.Generic.List<NativeShopOffer>();
            if (DarkSaver.Core.OriginalRestorationPolicy.AllowSinglePlayerRules && townKey == "Kaosia")
                for (int i = 0; i < shopItems.Count; i++)
                    if (interior!=null
                        ? interior.TryCreateOffer(shopItems.GetAt(i),out var offer)
                        : KaosiaShopRules.TryCreateOffer(shopItems.GetAt(i),out offer)) stock.Add(offer);
            settingAutomaticStock = true;
            try { if (ConfigureShop(shopItems, stock.ToArray())) automaticStockTown = stockKey; }
            finally { settingAutomaticStock = false; }
        }
        public bool HasPendingTrade => pendingTrade != null;
        public string PendingTradeText { get; private set; }
        public bool RequestTrade(int itemId, int quantity, bool buying)
        {
            if (HasPendingTrade || !CanTrade(itemId, quantity, buying, out shopMessage)) return false;
            var quote = System.Array.Find(shopOffers, offer => offer.itemId == itemId);
            pendingTrade = new NativeShopOffer {itemId = itemId, buyPrice = quote.buyPrice, sellPrice = quote.sellPrice};
            pendingTradeQuantity = quantity; pendingTradeBuying = buying;
            long total = checked((buying ? quote.buyPrice : quote.sellPrice) * quantity);
            PendingTradeText = ItemCatalogPanel.DisplayName(SelectedOrCatalogItem(itemId)) + " x" + quantity + " / " + total
                + (buying ? " 구입하시겠습니까?" : " 판매하시겠습니까?");
            return true;
        }
        private ItemCatalogEntry SelectedOrCatalogItem(int id) => shopItems != null && shopItems.TryGet(id, out var item) ? item : null;
        public bool CancelPendingTrade()
        {
            bool pending = HasPendingTrade;
            pendingTrade = null; pendingTradeQuantity = 0; PendingTradeText = null; return pending;
        }
        public bool ConfirmPendingTrade()
        {
            if (!HasPendingTrade) return false;
            var snapshot = pendingTrade; int quantity = pendingTradeQuantity; bool buying = pendingTradeBuying;
            CancelPendingTrade();
            var quote = System.Array.Find(shopOffers, offer => offer.itemId == snapshot.itemId);
            if (quote == null || (buying ? quote.buyPrice != snapshot.buyPrice : quote.sellPrice != snapshot.sellPrice)) {
                shopMessage = "가격이 변경되어 거래를 취소했습니다."; return false;
            }
            return Trade(snapshot.itemId, quantity, buying);
        }
        private void DrawTradeConfirmation()
        {
            if (!HasPendingTrade) return;
            if (confirmationCatalog == null) confirmationCatalog = Resources.Load<NativeBattleWindows>("UI/TownTradeConfirmation/NativeTradeConfirmation");
            var window = confirmationCatalog != null ? confirmationCatalog.Find(29) : null;
            if (window == null) throw new System.InvalidOperationException("Converted native trade confirmation missing.");
            ui.NativeTexturePanel("trade-confirm-blocker", new Rect(0, 0, Rendering.GameScreen.Width, Rendering.GameScreen.Height), null);
            var rect = new Rect((Rendering.GameScreen.Width - 528) / 2, 180, 528, 75);
            ui.NativeTexturePanel("trade-confirm-window", rect, window.background);
            ui.NativeBitmapText("trade-confirm-text", new Rect(rect.x + 12, rect.y + 4, 504, 32), PendingTradeText,
                new Color(.03f, .03f, .03f), 2, TextAnchor.MiddleCenter);
            ui.Raise("trade-confirm-text");
            ui.NativeImageButton("trade-confirm-yes", rect.position + window.controls[0].rect.position, window.controls[0], () => ConfirmPendingTrade(), true, "예");
            ui.NativeImageButton("trade-confirm-no", rect.position + window.controls[1].rect.position, window.controls[1], () => CancelPendingTrade(), true, "아니오");
        }
        private NativeBattleWindows nativeShopCatalog;
        private int selectedShopOwnedId = -1;
        public ItemCatalogEntry SelectedShopInspectionItem => SelectedShopItem
            ?? (GetComponent<BattleTransitionController>()?.OwnedItemCount(selectedShopOwnedId) > 0
                && InventoryCatalog != null && InventoryCatalog.TryGet(selectedShopOwnedId, out var item) ? item : null);
        public bool SelectShopOwnedItem(int id)
        {
            if (HasPendingTrade || !IsOpen || Service != TownService.Shop || !CanOpen
                || (GetComponent<BattleTransitionController>()?.OwnedItemCount(id) ?? 0) <= 0 || InventoryCatalog == null
                || !InventoryCatalog.TryGet(id, out _)) return false;
            selectedShopOwnedId = id; selectedShopId = -1; return true;
        }
        private void ClearShopInspection()
        {
            selectedShopId = selectedShopOwnedId = -1; detailSource = null;
        }
        private NativeBattleWindows.Window NativeShopWindow {
            get {
                if (nativeShopCatalog == null) nativeShopCatalog = Resources.Load<NativeBattleWindows>("UI/TownShop/NativeTownShop");
                return nativeShopCatalog != null ? nativeShopCatalog.Find(10) : null;
            }
        }
        private void DrawNativeShopList(Rect panel, NativeBattleWindows.Window window)
        {
            var session = GetComponent<BattleTransitionController>();
            Vector2 origin = panel.position;
            var ownedArea = new Rect(origin + new Vector2(20,40), new Vector2(242,137));
            var saleArea = new Rect(origin + new Vector2(278,40), new Vector2(102,137));
            for (int slot = 0; slot < InventoryGridLayout.PageSize; slot++) {
                int id = 0, quantity = 0;
                bool owned = session != null && session.TryGetOwnedItemAt(slot, out id, out quantity);
                var item = owned && InventoryCatalog != null && InventoryCatalog.TryGet(id, out var found) ? found : null;
                int ownedId = id;
                ui.ItemSlotButton("shop-owned-" + slot, InventoryGridLayout.Cell(slot, origin + new Vector2(20, 40)),
                    () => SelectShopOwnedItem(ownedId),
                    ItemIcons.Get(item), owned ? quantity : 0, selectedShopOwnedId == id, item != null ? ItemCatalogPanel.DisplayName(item) + " ×" + quantity : "");
                ui.ItemDragSource("shop-owned-" + slot, owned ? id : 0, ItemIcons.Get(item), saleArea,
                    draggedId => TradeDroppedItem(draggedId, false));
            }
            for (int slot = 0; slot < ShopVisibleCount; slot++) {
                bool populated = TryGetShopCell(slot, out var item);
                ui.ItemSlotButton("shop-item-" + slot, ShopCell(slot, origin + new Vector2(278, 40)),
                    () => { if (item != null) SelectShopItem(item.Id); }, populated ? ItemIcons.Get(item) : null,
                    populated ? 1 : 0, populated && selectedShopId == item.Id, populated ? ItemCatalogPanel.DisplayName(item) : "");
                ui.ItemDragSource("shop-item-" + slot, populated ? item.Id : 0, populated ? ItemIcons.Get(item) : null, ownedArea,
                    draggedId => TradeDroppedItem(draggedId, true));
            }
            ui.NativeImageButton("shop-prev", origin + window.controls[0].rect.position, window.controls[0], () => SetShopPage(shopPage - 1), shopPage > 0, "이전 줄");
            ui.NativeImageButton("shop-next", origin + window.controls[1].rect.position, window.controls[1], () => SetShopPage(shopPage + 1), shopPage + 1 < ShopPageCount, "다음 줄");
            ui.NativeImageButton("close", origin + window.controls[2].rect.position, window.controls[2], Close, true, "닫기");
            ui.NativeBitmapText("shop-gold", new Rect(panel.x + 54, panel.y + 266, 60, 20), session?.CurrentGold?.ToString() ?? "미확인",
                new Color(1, 1, .25f), alignment: TextAnchor.MiddleRight);
            var selected = SelectedShopInspectionItem;
            if (selected != null && string.IsNullOrEmpty(shopMessage)) {
                DrawItemDetails("shop-detail", new Rect(panel.x + 20, panel.y + 190, 375, 60), selected, nativePaper: true);
                ui.Raise("shop-detail-icon"); ui.Raise("shop-detail-scroll");
            }
            else ui.Label("shop-message", new Rect(panel.x + 20, panel.y + 202, 375, 48), shopMessage ?? (shopOffers.Length == 0 ? "이 상점에서 취급하는 물품이 없습니다." : "클릭: 정보 조회 / 반대쪽 물건 영역으로 드래그: 구매·판매"), 12, color: new Color(.17f, .09f, .035f));
            ui.Raise("shop-gold"); ui.Raise("shop-page"); ui.Raise("shop-message");
        }
        public bool TradeDroppedItem(int itemId, bool buying)
        {
            if (!CanTrade(itemId, 1, buying, out shopMessage)) return false;
            return Trade(itemId, 1, buying);
        }
        public const int ShopColumns = 3, ShopRows = 4, ShopVisibleCount = ShopColumns * ShopRows;
        public int ShopPageIndex => shopPage;
        // The original advances one row, not a full twelve-cell page.
        public int ShopPageCount => shopOffers.Length <= ShopVisibleCount ? 1 : (shopOffers.Length - ShopVisibleCount - 1) / ShopColumns + 2;
        public static Rect ShopCell(int slot, Vector2 origin)
        {
            if (slot < 0 || slot >= ShopVisibleCount) throw new System.ArgumentOutOfRangeException(nameof(slot));
            return new Rect(origin.x + slot % ShopColumns * 35, origin.y + slot / ShopColumns * 35, 32, 32);
        }
        public bool TryGetShopCell(int slot, out ItemCatalogEntry item)
        {
            item = null;
            if (!IsOpen || Service != TownService.Shop || !CanOpen || shopItems == null || slot < 0 || slot >= ShopVisibleCount) return false;
            int index = shopPage * ShopColumns + slot;
            return index < shopOffers.Length && shopItems.TryGet(shopOffers[index].itemId, out item);
        }
        public bool SetShopPage(int page)
        {
            if (HasPendingTrade || !IsOpen || Service != TownService.Shop || !CanOpen || page < 0 || page >= ShopPageCount) return false;
            ui?.CancelItemDrags(); shopPage = page; ClearShopInspection(); return true;
        }
        private int selectedShopId = -1;
        public ItemCatalogEntry SelectedShopItem => shopItems != null && shopItems.TryGet(selectedShopId, out var item)
            && System.Array.Exists(shopOffers, offer => offer.itemId == selectedShopId) ? item : null;
        public bool SelectShopItem(int itemId)
        {
            if (HasPendingTrade || !IsOpen || Service != TownService.Shop || !CanOpen || shopItems == null
                || !shopItems.TryGet(itemId, out _) || !System.Array.Exists(shopOffers, offer => offer.itemId == itemId)) return false;
            selectedShopId = itemId; selectedShopOwnedId = -1; return true;
        }
        private string shopMessage;
        private string shopQuantity = "1";
        public bool ConfigureShop(ItemCatalog catalog, NativeShopOffer[] offers)
        {
            if (catalog == null || offers == null) return false;
            var copy = new NativeShopOffer[offers.Length]; var ids = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < offers.Length; i++)
            {
                var offer = offers[i];
                if (offer == null || !catalog.TryGet(offer.itemId, out _) || !ids.Add(offer.itemId)
                    || offer.buyPrice < -1 || offer.sellPrice < -1) return false;
                copy[i] = new NativeShopOffer {itemId = offer.itemId, buyPrice = offer.buyPrice, sellPrice = offer.sellPrice};
            }
            GetComponent<BattleTransitionController>()?.ConfigureLocalShop(catalog,copy);
            CancelPendingTrade();
            ui?.CancelItemDrags();
            shopConfiguredExplicitly = !settingAutomaticStock; shopItems = catalog; shopOffers = copy; shopPage = 0; ClearShopInspection(); shopMessage = null; return true;
        }
        public bool Trade(int itemId, bool buying) => Trade(itemId, 1, buying);

        public bool CanTrade(int itemId, int quantity, bool buying, out string reason)
        {
            reason = "마을 상점의 거래 품목을 확인하세요.";
            if (!IsOpen || Service != TownService.Shop || !CanOpen) return false;
            var session = GetComponent<BattleTransitionController>();
            foreach (var offer in shopOffers)
                if (offer.itemId == itemId)
                    return session != null && session.CanTrade(offer, quantity, buying, shopItems, out reason);
            return false;
        }

        public bool Trade(int itemId, int quantity, bool buying)
        {
            if (HasPendingTrade || !IsOpen || Service != TownService.Shop || !CanOpen || quantity <= 0) return false;
            foreach (var offer in shopOffers)
                if (offer.itemId == itemId)
                {
                    var session = GetComponent<BattleTransitionController>();
                    return session != null && session.TryTrade(offer, quantity, buying, shopItems, out shopMessage);
                }
            return false;
        }

        private void TradeEnteredQuantity(int itemId, bool buying)
        {
            if (!TryReadShopQuantity(out var quantity, out shopMessage)) return;
            RequestTrade(itemId, quantity, buying);
        }
        private bool TryReadShopQuantity(out int quantity, out string reason)
        {
            reason = "수량은 1 이상의 정수로 입력하세요.";
            if (!int.TryParse(shopQuantity, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out quantity) || quantity <= 0) return false;
            reason = null; return true;
        }
        private void DrawTradeButton(string key, Rect area, int id, long price, bool buying)
        {
            if (price < 0) return;
            bool allowed = TryReadShopQuantity(out int quantity, out string reason)
                && CanTrade(id, quantity, buying, out reason);
            ui.Button(key, area, (buying ? "구매 " : "판매 ") + price,
                () => TradeEnteredQuantity(id, buying), allowed, tooltip: reason);
        }
        private void DrawShop(Rect panel)
        {
            var session = GetComponent<BattleTransitionController>();
            var gold = session != null ? session.CurrentGold : null;
            ui.Label("shop-gold", new Rect(panel.x + 20, 104, panel.width - 180, 24), !gold.HasValue ? "소지 상태 미확인" : "소지금 " + gold.Value);
            ui.Label("shop-quantity-label", new Rect(panel.xMax - 146, 104, 48, 24), "수량");
            ui.TextInput("shop-quantity", new Rect(panel.xMax - 94, 102, 74, 28), shopQuantity, 10, value => shopQuantity = value);
            if (shopItems == null || shopOffers.Length == 0)
                ui.Label("shop-empty", new Rect(panel.x + 20, 138, panel.width - 40, 100), "이 상점에서 취급하는 물품이 없습니다.\n다른 상점을 이용해 주세요.");
            else
            {
                var selected = SelectedShopItem;
                if (selected != null)
                {
                    DrawItemDetails("shop-detail", new Rect(panel.x + 20, 136, panel.width - 40, 136), selected);
                    ui.Button("shop-list", new Rect(panel.x + 20, 282, 110, 26), "품목 목록", () => selectedShopId = -1);
                    var quote = System.Array.Find(shopOffers, offer => offer.itemId == selected.Id);
                    DrawTradeButton("shop-detail-buy", new Rect(panel.xMax - 164, 282, 70, 26), selected.Id, quote.buyPrice, true);
                    DrawTradeButton("shop-detail-sell", new Rect(panel.xMax - 90, 282, 70, 26), selected.Id, quote.sellPrice, false);
                }
                else {
                for (int slot = 0; slot < ShopVisibleCount; slot++)
                {
                    bool populated = TryGetShopCell(slot, out var item);
                    ui.ItemSlotButton("shop-item-" + slot, ShopCell(slot, new Vector2(panel.x + 20, 136)),
                        () => { if (item != null) SelectShopItem(item.Id); }, populated ? ItemIcons.Get(item) : null,
                        populated ? 1 : 0, false, populated ? ItemCatalogPanel.DisplayName(item) + " / 보유 " + (session?.OwnedItemCount(item.Id) ?? 0) : "");
                }
                ui.Label("shop-hint", new Rect(panel.x + 142, 136, panel.width - 162, 100), "품목을 선택하면 상세 정보와\n구매·판매 버튼이 표시됩니다.\n이전·다음은 한 줄씩 이동합니다.");
                ui.NativeLibraryButton("shop-prev", new Vector2(panel.x + 20, 282), 40, () => SetShopPage(shopPage - 1), shopPage > 0, "이전 줄");
                ui.NativeLibraryButton("shop-next", new Vector2(panel.x + 55, 282), 39, () => SetShopPage(shopPage + 1), shopPage + 1 < ShopPageCount, "다음 줄");
                ui.Label("shop-page", new Rect(panel.x + 172, 282, 90, 26), (shopPage + 1) + "/" + ShopPageCount);
                }
            }
            TryReadShopQuantity(out _, out var quantityReason);
            ui.Label("shop-message", new Rect(panel.x + 20, 316, panel.width - 174, 46), quantityReason ?? shopMessage ?? "");
        }
    }
}
