using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<BankTransferRequest,WorldView,BankTransferNotice> BankTransferAuthority { get; set; }
        public event Action<WorldReply> Notify_BankTransferChanged;
        public WorldReply Notify_BankTransfer(BankTransferNotice notice)=>BankTransferResult(s=>s.ReceiveBankTransfer(notice));
        private WorldReply SendBankTransfer(bool deposit,ulong amount,string token,string request)
            =>BankTransferResult(s=>BankTransferAuthority==null?s.BankWorld(deposit,amount,token,request):s.ExecuteBankTransfer(deposit,amount,token,request,BankTransferAuthority));
        private WorldReply BankTransferResult(Func<EntryAccountService,WorldReply> apply)
        {var reply=WorldRequest(apply);if(reply.Accepted){Notify_BankTransferChanged?.Invoke(reply);Notify_CurrencyChanged?.Invoke(reply);}return reply;}
    }
}
