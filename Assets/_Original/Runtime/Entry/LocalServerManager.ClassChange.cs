using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<ClassOptionsRequest,WorldView,ClassOptionsNotice> ClassOptionsAuthority { get; set; }
        public Func<ClassChangeRequest,WorldView,ClassChangeNotice> ClassChangeAuthority { get; set; }
        public event Action<WorldReply> Notify_ClassChanged;
        public WorldReply Request_SelectAdjustmentCommander(int index,string identity,long revision)=>WorldRequest(s=>s.SelectAdjustmentCommander(index,identity,revision));
        public WorldReply Request_ClassOptions()=>WorldRequest(s=>s.ExecuteClassOptions(Guid.NewGuid().ToString("N"),ClassOptionsAuthority));
        public WorldReply Notify_ClassOptions(ClassOptionsNotice notice)=>WorldRequest(s=>s.ReceiveClassOptions(notice));
        public WorldReply Request_CloseClassOptions()=>WorldRequest(s=>s.CloseClassOptions());
        public WorldReply Request_ChangeClass(int slot,int index,string options,string request)=>ClassChangeResult(s=>s.ExecuteClassChange(slot,index,options,request,ClassChangeAuthority));
        public WorldReply Notify_ClassChange(ClassChangeNotice notice)=>ClassChangeResult(s=>s.ReceiveClassChange(notice));
        private WorldReply ClassChangeResult(Func<EntryAccountService,WorldReply> apply)
        {
            var reply=WorldRequest(apply);
            if(reply.Accepted)
            {
                Notify_ClassChanged?.Invoke(reply);Notify_GrowthChanged?.Invoke(reply);
                if(ClassOptionsAuthority!=null)Request_ClassOptions();
                return new WorldReply(reply.Failure,WorldSnapshot);
            }
            return reply;
        }
    }
}
