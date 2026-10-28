using System;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private IDisposable shopOpenBinding;
        public void BindLocalShopService()
        {
            if (shopOpenBinding != null) return;
            shopOpenBinding = RegisterRequestHandler<Request_OpenShop>(request =>
            {
                var interior = ShopInteriorContext.Current;
                var session = GetComponent<BattleTransitionController>();
                var panel = GetComponent<TownServicesPanel>();
                var player = FindFirstObjectByType<TopDownPlayerController>();
                ShopCounter2D npc = null;
                foreach (var candidate in FindObjectsByType<ShopCounter2D>(FindObjectsSortMode.None))
                    if (candidate.GetEntityId().ToString() == request.NpcId) { npc = candidate; break; }
                bool allowed = interior != null && session != null && session.HasCreatedCharacter
                    && !session.HasPendingTutorial && panel != null && !panel.IsOpen && panel.CanOpenService(TownService.Shop)
                    && player != null && player.isActiveAndEnabled && !player.UiInputBlocked
                    && npc != null && npc.isActiveAndEnabled
                    && SceneManager.GetSceneByName(interior.Region.MapScene).isLoaded
                    && npc.gameObject.scene.name == interior.Region.RuntimeScene
                    && OriginalRestorationPolicy.AllowsStaffEvidence(npc.GetComponent<OriginalTownStaff>()?.Source?.evidence)
                    && npc.CanInteractFrom(player.transform.position);
                if (!allowed) return new Response_OpenShop(request.NpcId, null, false, "현재 위치나 상태에서는 상점을 열 수 없습니다.");
                var source = Resources.Load<TextAsset>("World/KaosiaShopStock");
                var stock = source == null ? null : JsonUtility.FromJson<LocalShopStock>(source.text);
                var catalog = GetComponent<ItemCatalogPanel>()?.Catalog;
                // Unconfigured interiors remain empty; malformed configured stock is rejected.
                var offers = Array.Empty<DarkSaver.Items.NativeShopOffer>();
                if (stock != null && Array.Exists(stock.shops ?? Array.Empty<LocalShopStockEntry>(), row => row?.key == interior.Key)
                    && !stock.TryGetOffers(interior.Key, catalog, out offers))
                    return new Response_OpenShop(request.NpcId, null, false, "상점 판매 목록을 확인할 수 없습니다.");
                if (source == null || stock == null)
                    return new Response_OpenShop(request.NpcId, null, false, "상점 설정을 불러올 수 없습니다.");
                return new Response_OpenShop(request.NpcId, interior.Key, true, stock: offers);
            });
        }
        private void ClearShopBindings() { shopOpenBinding?.Dispose(); shopOpenBinding = null; }
    }
}
