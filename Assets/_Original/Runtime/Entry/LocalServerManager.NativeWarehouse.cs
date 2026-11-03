using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private NativeWarehouseWindow nativeWarehouse;private readonly HashSet<string> nativeWarehouseNotices=new HashSet<string>(),nativeWarehouseRequests=new HashSet<string>();private string nativeWarehouseSession;
        public event Action<byte[]> Notify_WarehousePacket;
        public event Action<NativeWarehouseRequest> Notify_WarehouseItemRequested;
        public NativeWarehouseWindow WarehouseClient(string token)=>isActiveAndEnabled&&nativeWarehouse?.Token==token&&nativeWarehouse.Matches(WorldSnapshot)?nativeWarehouse:null;
        public WorldReply Notify_WarehouseOpened(NativeWarehouseOpenNotice notice)
        {
            var view=WorldSnapshot;if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out _,out _))return new WorldReply(WorldFailure.Unauthenticated,view);
            if(nativeWarehouseSession!=view?.currencySession){nativeWarehouseSession=view?.currencySession;nativeWarehouseNotices.Clear();nativeWarehouseRequests.Clear();nativeWarehouse=null;}
            var window=NativeWarehouseWindow.Begin(view,notice);
            if(window==null||nativeWarehouseNotices.Contains(window.Open))return new WorldReply(WorldFailure.StaleRequest,view);
            nativeWarehouse=window;nativeWarehouseNotices.Add(window.Open);Notify_WarehousePacket?.Invoke(NativeWarehousePackets.Query(window.Key));
            return new WorldReply(isActiveAndEnabled&&ReferenceEquals(nativeWarehouse,window)&&window.Matches(WorldSnapshot)?WorldFailure.None:WorldFailure.StaleRequest,WorldSnapshot);
        }
        public WorldReply Notify_WarehouseContents(NativeWarehouseContentsNotice notice)
        {
            var view=WorldSnapshot;var window=notice==null?null:WarehouseClient(notice.token);
            if(window==null||!notice.approved||notice.open!=window.Open||notice.character!=window.Character||notice.session!=window.Session||!Guid.TryParseExact(notice.id,"N",out _)||nativeWarehouseNotices.Contains(notice.id))return new WorldReply(WorldFailure.StaleRequest,view);
            if(!service.TryWorldItemIds(out var items))return new WorldReply(WorldFailure.Unauthenticated,view);
            var known=new HashSet<int>(items);if(!window.Receive(notice.packet,known.Contains))return new WorldReply(WorldFailure.InvalidInput,view);
            nativeWarehouseNotices.Add(notice.id);return new WorldReply(WorldFailure.None,view);
        }
        public NativeWarehouseGesture Request_BeginWarehouseDrag(string token,bool deposit,string owned,int row)=>service?.TryWorldClientSession(out _,out _)==true?WarehouseClient(token)?.BeginDrag(WorldSnapshot,deposit,owned,row):null;
        public bool WarehouseDragCurrent(NativeWarehouseGesture gesture)=>gesture!=null&&WarehouseClient(gesture.Token)?.Matches(gesture,WorldSnapshot)==true;
        public WorldReply Request_WarehouseItem(NativeWarehouseGesture gesture,string request)
        {
            var view=WorldSnapshot;var window=gesture==null?null:WarehouseClient(gesture.Token);
            if(service?.TryWorldClientSession(out _,out _)!=true)return new WorldReply(WorldFailure.Unauthenticated,view);
            if(window==null||!window.Matches(gesture,view)||!Guid.TryParseExact(request,"N",out _)||nativeWarehouseRequests.Contains(request))return new WorldReply(WorldFailure.StaleRequest,view);
            if(!gesture.Deposit&&view.record.inventory.Length>27)return new WorldReply(WorldFailure.InventoryFull,view);
            var packet=NativeWarehousePackets.Move(gesture.Deposit,window.Key,gesture.Item,gesture.ItemToken);if(packet==null)return new WorldReply(WorldFailure.InvalidInput,view);
            nativeWarehouseRequests.Add(request);var sent=new NativeWarehouseRequest{id=request,open=window.Open,character=window.Character,session=window.Session,token=window.Token,
                owned=gesture.Owned,deposit=gesture.Deposit,item=gesture.Item,key=window.Key,itemToken=gesture.ItemToken,packet=packet};
            bool Current()=>isActiveAndEnabled&&ReferenceEquals(nativeWarehouse,window)&&window.Matches(WorldSnapshot);
            Notify_WarehousePacket?.Invoke((byte[])packet.Clone());if(!Current())return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);
            Notify_WarehouseItemRequested?.Invoke(sent.Copy());return new WorldReply(Current()?WorldFailure.None:WorldFailure.StaleRequest,WorldSnapshot);
        }
        public bool Request_CancelWarehouseClient(string token)
        {if(nativeWarehouse==null||token!=null&&nativeWarehouse.Token!=token)return false;nativeWarehouse=null;return true;}
    }
}
