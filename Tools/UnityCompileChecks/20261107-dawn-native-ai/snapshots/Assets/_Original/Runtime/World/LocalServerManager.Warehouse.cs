using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<WarehouseRequest,WorldView,WarehouseNotice> WarehouseAuthority { get; set; }
        public event Action<WorldReply> Notify_WarehouseChanged;
        public WorldReply Request_WarehouseState(string token,string request)=>WarehouseResult(s=>s.ExecuteWarehouseOperation(WarehouseOperation.State,false,null,token,request,WarehouseAuthority));
        public WorldReply Notify_Warehouse(WarehouseNotice notice)=>WarehouseResult(s=>s.ReceiveWarehouseOperation(notice));
        private WorldReply WarehouseResult(Func<EntryAccountService,WorldReply> apply)
        {var reply=WorldRequest(apply);if(reply.Accepted)Notify_WarehouseChanged?.Invoke(reply);return reply;}
        private WorldReply SendWarehouseMove(bool deposit,string owned,string token,string request)=>WarehouseResult(s=>WarehouseAuthority==null?s.MoveWorldStorage(deposit,owned,token,request):s.ExecuteWarehouseOperation(WarehouseOperation.Move,deposit,owned,token,request,WarehouseAuthority));
        private WorldReply SendWarehouseConfirm(string token,string request)=>WarehouseResult(s=>WarehouseAuthority==null?s.ConfirmWorldStorage(token,request):s.ExecuteWarehouseOperation(WarehouseOperation.Confirm,false,null,token,request,WarehouseAuthority));
    }
}
