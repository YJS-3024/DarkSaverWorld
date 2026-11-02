using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<ShopRequest,WorldView,ShopQuote> ShopQuoteAuthority { get; set; }
        public Func<ShopRequest,WorldView,ShopNotice> ShopTradeAuthority { get; set; }
        public event Action<WorldReply> Notify_ShopChanged;
        public bool UsesShopApproval=>ShopQuoteAuthority!=null||ShopTradeAuthority!=null;
        public WorldReply Request_ShopQuote(bool buy,int item,string owned,string token,string request)=>ShopResult(s=>s.ExecuteShopQuote(buy,item,owned,token,request,ShopQuoteAuthority));
        public WorldReply Notify_ShopQuote(ShopQuote quote)=>ShopResult(s=>s.ReceiveShopQuote(quote));
        public WorldReply Request_ConfirmShopTrade(string quote,string token,string request)=>ShopResult(s=>s.ExecuteShopTrade(quote,token,request,ShopTradeAuthority));
        public WorldReply Notify_ShopTrade(ShopNotice notice)=>ShopResult(s=>s.ReceiveShopTrade(notice));
        private WorldReply ShopResult(Func<EntryAccountService,WorldReply> apply)
        {var reply=WorldRequest(apply);if(reply.Accepted)Notify_ShopChanged?.Invoke(reply);return reply;}
        private WorldReply SendShop(bool buy,int item,string owned,string token,string request)
        {
            if(!UsesShopApproval)return ShopResult(s=>buy?s.BuyWorldItem(item,token,request):s.SellWorldItem(owned,token,request));
            var quote=WorldSnapshot?.facility?.shopQuote;
            if(quote==null||quote.request.buy!=buy||buy&&quote.request.item!=item||!buy&&quote.request.owned!=owned)
                return ShopResult(s=>new WorldReply(WorldFailure.RulesUnavailable,s.WorldSnapshot()));
            return Request_ConfirmShopTrade(quote.id,token,request);
        }
    }
}
