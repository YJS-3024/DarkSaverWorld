using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<byte[]> Notify_ShopPacket;
        public event Action<NativeShopRequest> Notify_ShopItemRequested;
        private NativeShopInventory nativeShopInventory;private string nativeShopSession;private readonly HashSet<string> nativeShopRequests=new HashSet<string>(),nativeShopNotices=new HashSet<string>();
        public int? ShopClientWindow(string token)=>nativeShopInventory?.Token==token&&nativeShopInventory.Matches(WorldSnapshot)?nativeShopInventory.NativeWindowId:(int?)null;
        private void MaintainNativeShopSession(WorldView view)
        {if(nativeShopSession!=view?.currencySession){nativeShopSession=view?.currencySession;nativeShopInventory=null;nativeShopRequests.Clear();nativeShopNotices.Clear();}}
        public WorldReply Notify_ShopInventory(NativeShopInventoryNotice notice)
        {
            var view=WorldSnapshot;if(!isActiveAndEnabled||service?.TryWorldClientSession(out _,out _)!=true)return new WorldReply(WorldFailure.Unauthenticated,view);
            MaintainNativeShopSession(view);var inventory=NativeShopInventory.Begin(view,notice);
            if(inventory==null||nativeShopNotices.Contains(notice.id))return new WorldReply(WorldFailure.StaleRequest,view);
            nativeShopInventory=inventory;nativeShopNotices.Add(notice.id);return new WorldReply(WorldFailure.None,view);
        }
        public WorldReply Request_ShopItem(NativeItemDragContext source,bool buy,string token,string request,int offset=0)
        {
            var view=WorldSnapshot;if(!isActiveAndEnabled||service?.TryWorldClientSession(out _,out _)!=true)return new WorldReply(WorldFailure.Unauthenticated,view);
            MaintainNativeShopSession(view);
            if(!NativeShopInventory.Available(view,token)||source==null||!source.Matches(view,offset)||buy!=(source.Owned==null)||!Guid.TryParseExact(request,"N",out _)||nativeShopRequests.Contains(request))return new WorldReply(WorldFailure.StaleRequest,view);
            if(buy&&view.record.inventory.Length>27)return new WorldReply(WorldFailure.InventoryFull,view);
            if(!buy&&nativeShopInventory?.Matches(view)==true&&!nativeShopInventory.Allows(source.Item))return new WorldReply(WorldFailure.ShopCategoryMismatch,view);
            ushort itemToken=0;if(!buy&&nativeShopInventory?.TryToken(view,source.Owned,source.Item,out itemToken)!=true)return new WorldReply(WorldFailure.RulesUnavailable,view);
            var packet=buy?NativeShopPackets.Buy(source.Item):NativeShopPackets.Sell(source.Item,itemToken);if(packet==null)return new WorldReply(WorldFailure.InvalidInput,view);
            nativeShopRequests.Add(request);var sent=new NativeShopRequest{id=request,character=view.characterId,session=view.currencySession,token=token,npc=view.facility.npc,map=view.record.map,
                owned=source.Owned,item=source.Item,buy=buy,itemToken=buy?(ushort?)null:itemToken,packet=packet};
            bool Current(){var now=WorldSnapshot;return isActiveAndEnabled&&NativeShopInventory.Available(now,token)&&now.characterId==sent.character&&now.currencySession==sent.session;}
            Notify_ShopPacket?.Invoke((byte[])packet.Clone());if(!Current())return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);
            Notify_ShopItemRequested?.Invoke(sent.Copy());var current=WorldSnapshot;
            if(!Current()||!NativeShopInventory.Available(current,token)||current.characterId!=sent.character||current.currencySession!=sent.session)return new WorldReply(WorldFailure.StaleRequest,current);
            // The original client sends4B/4C directly on a valid drop. Only an
            // explicitly configured local server may price/approve and persist it;
            // no extra client confirmation or default sale price is introduced.
            if(ShopQuoteAuthority==null||ShopTradeAuthority==null)return new WorldReply(WorldFailure.None,current);
            var quoted=Request_ShopQuote(buy,source.Item,source.Owned,token,request);if(!quoted.Accepted)return quoted;
            var offer=quoted.View?.facility?.shopQuote;
            return offer==null?new WorldReply(WorldFailure.RulesUnavailable,quoted.View):Request_ConfirmShopTrade(offer.id,token,Guid.NewGuid().ToString("N"));
        }
        public bool Request_CancelShopClient(string token)
        {if(nativeShopInventory==null||token!=null&&nativeShopInventory.Token!=token)return false;nativeShopInventory=null;return true;}
    }
}
