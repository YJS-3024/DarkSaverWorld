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
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}        public bool TryUseBattleRecoveryItem(BattleSceneRoot source,int itemId,short targetId,NativeInventory baseline,out string reason)
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}
        public static bool IsTempleReady => DarkSaver.World.FieldSceneContext.Current?.Key == "KaosiaTemple"
            && SceneManager.GetSceneByName("KaosiaTemple_Map").isLoaded;
        // User-approved local service: free primary-character revival and full HP/MP.
        public bool TryTempleRecovery(out string reason)
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}
        public bool CanTrade(NativeShopOffer offer, int quantity, bool buying, ItemCatalog catalog, out string message)
{message=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}        public bool TryTrade(NativeShopOffer offer, int quantity, bool buying, ItemCatalog catalog, out string message)
{message=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}    }
}
