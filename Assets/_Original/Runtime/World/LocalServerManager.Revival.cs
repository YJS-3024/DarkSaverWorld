using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<RevivalQuoteRequest,WorldView,RevivalQuote> RevivalQuoteAuthority { get; set; }
        public Func<RevivalRequest,WorldView,RevivalNotice> RevivalAuthority { get; set; }
        public event Action<WorldReply> Notify_RevivalChanged;
        public WorldReply Request_RevivalQuote(string token,string id)=>WorldRequest(s=>s.ExecuteRevivalQuote(token,id,RevivalQuoteAuthority));
        public WorldReply Notify_RevivalQuote(RevivalQuote quote)=>WorldRequest(s=>s.ReceiveRevivalQuote(quote));
        public WorldReply Notify_Revival(RevivalNotice notice)=>RevivalResult(s=>s.ReceiveTempleRevival(notice));
        private WorldReply RevivalResult(Func<EntryAccountService,WorldReply> apply)
        {var result=WorldRequest(apply);if(result.Accepted)Notify_RevivalChanged?.Invoke(result);return result;}
        private WorldReply SendRevival(string token,string id)=>RevivalResult(s=>RevivalQuoteAuthority!=null||RevivalAuthority!=null?s.ExecuteTempleRevival(token,id,RevivalAuthority):s.ReviveWorld(token,id));
    }
}
