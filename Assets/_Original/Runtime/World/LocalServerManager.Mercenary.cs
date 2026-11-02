using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<MercenaryRequest,WorldView,MercenaryNotice> MercenaryAuthority { get; set; }
        public event Action<WorldReply> Notify_MercenaryChanged;
        public WorldReply Notify_Mercenary(MercenaryNotice notice)=>MercenaryResult(s=>s.ReceiveMercenaryTransfer(notice));
        private WorldReply MercenaryResult(Func<EntryAccountService,WorldReply> apply)
        {var reply=WorldRequest(apply);if(reply.Accepted)Notify_MercenaryChanged?.Invoke(reply);return reply;}
        private WorldReply SendMercenary(bool hire,int job,string token,string request)=>MercenaryResult(s=>MercenaryAuthority!=null
            ?s.ExecuteMercenaryTransfer(hire,job,token,request,MercenaryAuthority)
            :hire?s.HireWorldMercenary(job,token,request):s.DismissWorldMercenary(token,request));
    }
}
