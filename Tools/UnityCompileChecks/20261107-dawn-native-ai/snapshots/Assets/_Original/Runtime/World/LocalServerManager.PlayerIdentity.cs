using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Notify_PlayerIdentity(PlayerIdentityNotice notice)=>WorldRequest(s=>
            notice?.spouse!=null&&(memoCodec==null||!memoCodec.TryEncode(notice.spouse,out _))?new WorldReply(WorldFailure.InvalidInput,s.WorldSnapshot()):s.ReceivePlayerIdentity(notice));
    }
}
