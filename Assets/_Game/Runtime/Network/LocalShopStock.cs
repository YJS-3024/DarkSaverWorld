using System;
using System.Collections.Generic;
using DarkSaver.Items;

namespace DarkSaver.Network
{
    [Serializable] public sealed class LocalShopStockEntry
    {
        public string key, evidence;
        public NativeShopOffer[] offers;
    }
    [Serializable] public sealed class LocalShopStock
    {
        public int version;
        public LocalShopStockEntry[] shops;
        public bool TryGetOffers(string key, ItemCatalog catalog, out NativeShopOffer[] result)
        {
            result = null;
            if (version != 1 || shops == null || catalog == null || string.IsNullOrEmpty(key)) return false;
            LocalShopStockEntry selected = null;
            foreach (var shop in shops)
            {
                if (shop == null) return false;
                if (shop.key != key) continue;
                if (selected != null) return false;
                selected = shop;
            }
            if (selected?.offers == null) return false;
            var copy = new NativeShopOffer[selected.offers.Length];
            var ids = new HashSet<int>();
            for (int i = 0; i < copy.Length; i++)
            {
                var offer = selected.offers[i];
                if (offer == null || offer.itemId <= 0 || !ids.Add(offer.itemId)
                    || !catalog.TryGet(offer.itemId, out _) || offer.buyPrice < 0 || offer.sellPrice < -1) return false;
                copy[i] = new NativeShopOffer { itemId = offer.itemId, buyPrice = offer.buyPrice, sellPrice = offer.sellPrice };
            }
            result = copy;
            return true;
        }
    }
}
