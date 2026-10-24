using DarkSaver.Core;
using DarkSaver.Items;
using UnityEngine.SceneManagement;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private NativeInventory inventory;
        public NativeInventory CaptureInventory() => inventory?.Clone();
        public long? CurrentGold => inventory?.gold;
        public int? OwnedItemTypeCount => inventory?.items?.Length;
        public int OwnedItemCount(int id) => inventory?.items == null ? 0 : inventory.Count(id);
        public bool TryGetOwnedItemAt(int index, out int id, out int quantity)
        {
            id = quantity = 0;
            if (inventory?.items == null || index < 0 || index >= inventory.items.Length) return false;
            var stack = inventory.items[index];
            if (stack == null) return false;
            id = stack.itemId; quantity = stack.quantity; return true;
        }
        private bool usingBattleItem;
        internal bool OwnsActiveBattle(BattleSceneRoot source) => isActiveAndEnabled && !transitioning && source!=null
            && activeBattle==source && source.gameObject.scene.name==BattleSceneName && source.gameObject.scene.isLoaded;
        public bool CanUseBattleRecoveryItem(BattleSceneRoot source,int itemId,short targetId,NativeInventory baseline,out string reason)
        {
            reason="현재 이 물건을 사용할 수 없습니다.";
            if(usingBattleItem || !isActiveAndEnabled || transitioning || activeBattle==null || source!=activeBattle
                || source.gameObject.scene.name!=BattleSceneName || !source.gameObject.scene.isLoaded
                || inventory==null || baseline==null || !inventory.IsValid || !baseline.IsValid
                || !string.Equals(UnityEngine.JsonUtility.ToJson(inventory),UnityEngine.JsonUtility.ToJson(baseline),System.StringComparison.Ordinal)
                || inventory.Count(itemId)<=0)return false;
            var items=GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog;
            var magic=GetComponent<DarkSaver.UI.MagicCatalogPanel>()?.Catalog;
            if(items==null || !items.TryGet(itemId,out var item))return false;
            if(SinglePlayerItemRecoveryRules.TryGet(item,magic,out int hp,out int mp,out int range,out _))
            { if(!source.CanApplyItemRecovery(targetId,hp,mp,range,SinglePlayerItemRecoveryRules.ApCost))return false; }
            else
            {
                if(!SinglePlayerItemMagicRules.TryGet(item,magic,out var linked))return false;
                TemporaryMagicCatalogRules.Configure(source,magic);
                if(!source.CanApplyItemMagic(targetId,linked.Id,(int)linked.Range,SinglePlayerItemRecoveryRules.ApCost))return false;
            }
            reason=string.Empty;return true;
        }
        public bool TryUseBattleRecoveryItem(BattleSceneRoot source,int itemId,short targetId,NativeInventory baseline,out string reason)
        {
            if(!processingSessionRequest)
            {
                if(source!=activeBattle){reason="현재 전투가 아닙니다.";return false;}
                return SubmitSession(new DarkSaver.Network.LocalBattleItemUseRequest(itemId,targetId,baseline),out reason);
            }
            if(!CanUseBattleRecoveryItem(source,itemId,targetId,baseline,out reason)
                || !inventory.TryConsume(itemId,1,out var changed))return false;
            var itemCatalog=GetComponent<DarkSaver.UI.ItemCatalogPanel>().Catalog;
            itemCatalog.TryGet(itemId,out var item);
            bool recovery=SinglePlayerItemRecoveryRules.TryGet(item,GetComponent<DarkSaver.UI.MagicCatalogPanel>().Catalog,out int hp,out int mp,out int range,out _);
            SinglePlayerItemMagicRules.TryGet(item,GetComponent<DarkSaver.UI.MagicCatalogPanel>().Catalog,out var linked);
            var previous=inventory;usingBattleItem=true;
            try
            {
                // Commit the prepared inventory before publishing vitals events; reentrant use is guarded.
                inventory=changed;
                if(!(recovery ? source.TryApplyItemRecovery(targetId,hp,mp,range,SinglePlayerItemRecoveryRules.ApCost,(int)item.GetNumericValue(17))
                    : source.TryApplyItemMagic(targetId,linked.Id,(int)linked.Range,SinglePlayerItemRecoveryRules.ApCost)))
                {inventory=previous;return false;}
                reason="사용했습니다.";return true;
            }
            finally {usingBattleItem=false;}
        }

        public bool CanTrade(NativeShopOffer offer, int quantity, bool buying, ItemCatalog catalog, out string message)
        {
            message = "거래할 수 없습니다. 품목·가격·소지 상태를 확인하세요.";
            var interior=DarkSaver.World.ShopInteriorContext.Current;
            if(interior!=null && (offer==null || catalog==null || !catalog.TryGet(offer.itemId,out var entry) || !interior.OffersPosition(entry.GetNumericValue(3))))
            {message="이 상점에서 취급하지 않는 품목입니다.";return false;}
            if(UnityEngine.Object.FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling==true)return false;
            if(!buying && offer!=null && inventory!=null && inventory.IsValid && (character?.EquippedCount(offer.itemId)??0)>0
                && inventory.Count(offer.itemId)-(character?.EquippedCount(offer.itemId)??0)<quantity)
            {message="장착 중인 수량은 판매할 수 없습니다.";return false;}
            return HasCreatedCharacter && inventory != null && !transitioning && DarkSaver.World.TownSceneContext.IsReady
                && !SceneManager.GetSceneByName("Battle").isLoaded && offer != null && catalog != null
                && catalog.TryGet(offer.itemId, out _)
                && inventory.CanTrade(offer.itemId, quantity, buying ? offer.buyPrice : offer.sellPrice, buying, out message);
        }
        public bool TryTrade(NativeShopOffer offer, int quantity, bool buying, ItemCatalog catalog, out string message)
        {
            if(!processingSessionRequest)
            {
                if(offer==null || catalog!=serverShopCatalog){message="서버 상점 정보가 다릅니다.";return false;}
                return SubmitSession(new DarkSaver.Network.LocalTradeRequest(offer.itemId,quantity,buying),out message);
            }
            if (!CanTrade(offer, quantity, buying, catalog, out message) || !inventory.TryTrade(offer.itemId, quantity,
                    buying ? offer.buyPrice : offer.sellPrice, buying, out var changed)) return false;
            inventory = changed; message = buying ? "구매했습니다. 저장 버튼으로 보관하세요." : "판매했습니다. 저장 버튼으로 보관하세요.";
            return true;
        }
    }
}
