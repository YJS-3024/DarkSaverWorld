using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<CurrencyRequest,WorldView,CurrencyNotice> CurrencyAuthority { get; set; }
        public event Action<WorldReply> Notify_CurrencyChanged;
        public WorldReply Request_CurrencyState()=>CurrencyResult(s=>s.ExecuteCurrencyState(Guid.NewGuid().ToString("N"),CurrencyAuthority));
        public WorldReply Notify_Currency(CurrencyNotice notice)=>CurrencyResult(s=>s.ReceiveCurrencyState(notice));
        private WorldReply CurrencyResult(Func<EntryAccountService,WorldReply> apply)
        {var result=WorldRequest(apply);if(result.Accepted)Notify_CurrencyChanged?.Invoke(result);return result;}
    }
}
