using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<ItemUseRequest,WorldView,ItemUseNotice> ItemUseAuthority { get; set; }
        public event Action<WorldReply> Notify_ItemUsed;
        public WorldReply Notify_ItemUse(ItemUseNotice notice)=>ItemUseResult(s=>s.ReceiveItemUse(notice));
        private WorldReply SendItemUse(string owned,string request)=>ItemUseResult(s=>s.ExecuteItemUse(owned,request,ItemUseAuthority));
        private WorldReply ItemUseResult(Func<EntryAccountService,WorldReply> apply)
        {
            var reply=WorldRequest(apply);if(reply.Accepted){Notify_ItemUsed?.Invoke(reply);Notify_MapEntered?.Invoke(reply);}return reply;
        }
    }
}
