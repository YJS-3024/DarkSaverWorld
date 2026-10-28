namespace DarkSaver.Network
{
    // In-process contracts; NPC ids are Unity entity ids, not recovered wire ids.
    public sealed class Request_OpenShop : ClientPacket
    {
        public string NpcId { get; }
        public Request_OpenShop(string npcId) { NpcId = npcId; }
    }
    public sealed class Response_OpenShop : LocalCommandResponse
    {
        public string NpcId { get; }
        public string ShopKey { get; }
        private readonly DarkSaver.Items.NativeShopOffer[] offers;
        public DarkSaver.Items.NativeShopOffer[] CaptureOffers() => System.Array.ConvertAll(offers,
            item => new DarkSaver.Items.NativeShopOffer { itemId = item.itemId, buyPrice = item.buyPrice, sellPrice = item.sellPrice });
        public Response_OpenShop(string npcId, string shopKey, bool accepted, string reason = null, DarkSaver.Items.NativeShopOffer[] stock = null) : base(accepted, reason)
        {
            NpcId = npcId; ShopKey = shopKey;
            offers = accepted && stock != null ? System.Array.ConvertAll(stock,
                item => item == null ? throw new System.ArgumentException("Null shop offer.")
                    : new DarkSaver.Items.NativeShopOffer { itemId = item.itemId, buyPrice = item.buyPrice, sellPrice = item.sellPrice })
                : System.Array.Empty<DarkSaver.Items.NativeShopOffer>();
        }
    }
}
