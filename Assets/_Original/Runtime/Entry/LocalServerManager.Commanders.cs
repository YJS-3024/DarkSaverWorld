using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<CommanderRequest,WorldView,CommanderNotice> CommanderAuthority {get;set;}
        public event Action<WorldReply> Notify_Commanders;
        public event Action<byte[]> Notify_CommanderPacket;
        public WorldReply Request_CommanderState()=>CommanderResult(s=>s.ExecuteCommanderOperation(CommanderOperation.State,0,0,Guid.NewGuid().ToString("N"),CommanderAuthority));
        public WorldReply Request_InspectCommander(int slot)=>CommanderResult(s=>s.InspectCommander(slot));
        public WorldReply Request_ToggleCommanderDeployment(int slot)=>SendCommanderEdit(CommanderOperation.ToggleDeployment,slot,0);
        public WorldReply Request_SwapCommanders(int source,int destination)=>SendCommanderEdit(CommanderOperation.Swap,source,destination);
        public WorldReply Notify_CommanderRoster(CommanderRosterPush notice)=>CommanderResult(s=>s.ReceiveCommanderRoster(notice));
        public WorldReply Notify_CommanderState(CommanderNotice notice)=>CommanderResult(s=>s.ReceiveCommanderState(notice));
        public WorldReply Request_EquipCommanderItem(int index,string identity,long revision,string owned,int slot,string request,int inventorySlot=-1)
            =>CommanderResult(s=>s.EquipCommanderItem(index,identity,revision,owned,slot,request,inventorySlot));
        public WorldReply Request_CancelCommander(string request)=>CommanderResult(s=>s.CancelCommanderOperation(request));
        private WorldReply CommanderResult(Func<EntryAccountService,WorldReply> apply)
        {var r=WorldRequest(apply);if(r.Accepted)Notify_Commanders?.Invoke(r);return r;}
        private WorldReply SendCommanderEdit(CommanderOperation operation,int source,int destination)
        {
            byte[] packet=null;
            var result=CommanderResult(s=>s.ExecuteCommanderOperation(operation,source,destination,Guid.NewGuid().ToString("N"),(request,view)=>
            {
                packet=request.NativePacket;
                return CommanderAuthority!=null?CommanderAuthority(request,view):new CommanderNotice{approved=true,id=Guid.NewGuid().ToString("N"),request=request.Copy(),state=OriginalCommanderPackets.Edit(request)};
            }));
            if(result.Accepted&&packet!=null)Notify_CommanderPacket?.Invoke((byte[])packet.Clone());return result;
        }
    }
}
