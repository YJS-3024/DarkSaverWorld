using System;

namespace DarkSaver.Items
{
    [Serializable] public sealed class NativeShopOffer
    {
        public int itemId;
        // -1 means unavailable/unknown; zero is an explicitly free quote.
        public long buyPrice = -1, sellPrice = -1;
    }
}
