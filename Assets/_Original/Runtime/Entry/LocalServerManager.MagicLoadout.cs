using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<MagicLoadoutRequest,WorldView,MagicLoadoutNotice> MagicLoadoutAuthority { get; set; }
        public event Action<WorldReply> Notify_MagicLoadoutChanged;
        public WorldReply Request_MagicLoadoutState()=>MagicLoadoutResult(s=>s.ExecuteMagicLoadout(MagicLoadoutOperation.State,-1,Guid.NewGuid().ToString("N"),ClassMagicAuthority()));
        public WorldReply Request_AddBattleMagic(int index,string request)=>MagicLoadoutResult(s=>s.ExecuteMagicLoadout(MagicLoadoutOperation.Add,index,request,ClassMagicAuthority()));
        public WorldReply Request_RemoveBattleMagic(int index,string request)=>MagicLoadoutResult(s=>s.ExecuteMagicLoadout(MagicLoadoutOperation.Remove,index,request,ClassMagicAuthority()));
        public WorldReply Request_ConfirmMagicLoadout(string request)=>MagicLoadoutResult(s=>s.ExecuteMagicLoadout(MagicLoadoutOperation.Confirm,-1,request,ClassMagicAuthority()));
        public WorldReply Request_CancelMagicLoadout()=>WorldRequest(s=>s.CancelMagicLoadout());
        public WorldReply Notify_MagicLoadout(MagicLoadoutNotice notice)=>MagicLoadoutResult(s=>s.ReceiveMagicLoadout(notice));
        private WorldReply MagicLoadoutResult(Func<EntryAccountService,WorldReply> apply)
        {
            var reply=WorldRequest(apply);
            if(reply.Accepted){Notify_MagicLoadoutChanged?.Invoke(reply);if(reply.View?.magicLoadout==null&&reply.View?.record.magicLoadout!=null)Notify_GrowthChanged?.Invoke(reply);}
            return reply;
        }
    }
}
