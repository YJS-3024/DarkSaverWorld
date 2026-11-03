using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<byte[]> Notify_BankPacket;
        public event Action<NativeBankQuery> Notify_BankBalanceRequested;
        public event Action<NativeBankAmountRequest> Notify_BankAmountRequested;
        private NativeBankWindow nativeBank;private NativeBankAmountEdit nativeBankAmount;
        private readonly HashSet<string> nativeBankAmounts=new HashSet<string>();private string nativeBankSession;
        public bool BankBalanceReady(string token)=>isActiveAndEnabled&&nativeBank?.Loaded==true&&nativeBank.Query.token==token&&nativeBank.Matches(WorldSnapshot);
        public WorldReply Request_BankBalance(string token)
        {
            if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out _,out _))return new WorldReply(WorldFailure.Unauthenticated);
            var current=service.WorldSnapshot();if(nativeBankSession!=current?.currencySession){nativeBankSession=current?.currencySession;nativeBankAmounts.Clear();}
            if(!NativeBankWindow.Available(current,token))return new WorldReply(WorldFailure.StaleRequest,current);
            if(nativeBank!=null&&nativeBank.Matches(current))return new WorldReply(WorldFailure.None,current);
            Request_CancelBankClient(null);
            var bank=NativeBankWindow.Begin(current,token,Guid.NewGuid().ToString("N"));if(bank==null)return new WorldReply(WorldFailure.StaleRequest,current);
            void Send(){nativeBank=bank;Notify_BankPacket?.Invoke(NativeBankPackets.Query());Notify_BankBalanceRequested?.Invoke(bank.Query.Copy());}
            WorldReply result;
            if(CurrencyAuthority==null)
            {result=WorldRequest(s=>{var queued=s.RequestCurrencyState(bank.Query.id);if(queued.Accepted)Send();return new WorldReply(queued.Failure,s.WorldSnapshot());});return result;}
            bool loaded=false;
            result=CurrencyResult(s=>s.ExecuteCurrencyState(bank.Query.id,(request,view)=>
            {
                Send();if(!ReferenceEquals(nativeBank,bank)||!bank.Matches(service.WorldSnapshot()))return null;
                var notice=CurrencyAuthority(request,view);if(!ReferenceEquals(nativeBank,bank)||!bank.Matches(service.WorldSnapshot()))return null;
                loaded=notice?.approved==true&&notice.bank.HasValue&&notice.request==request.id&&notice.character==request.character&&notice.session==request.session&&notice.expectedRevision==request.revision;
                return notice?.approved==true&&!notice.bank.HasValue?null:notice;
            }));
            if(result.Accepted&&loaded&&ReferenceEquals(nativeBank,bank))bank.MarkLoaded();
            else if(!result.Accepted&&ReferenceEquals(nativeBank,bank))nativeBank=null;
            return result;
        }
        public WorldReply Notify_BankBalance(NativeBankBalanceNotice notice)
        {
            var bank=nativeBank;var view=WorldSnapshot;
            if(!isActiveAndEnabled||bank==null||!bank.Matches(view)||notice==null||notice.query!=bank.Query.id||notice.character!=bank.Query.character
                ||notice.session!=bank.Query.session||notice.token!=bank.Query.token||!Guid.TryParseExact(notice.id,"N",out _)
                ||notice.expectedRevision!=(view.record.currency?.revision??0))return new WorldReply(WorldFailure.StaleRequest,view);
            ulong balance=0;if(notice.approved&&!NativeBankPackets.TryBalance(notice.packet,out balance))return new WorldReply(WorldFailure.InvalidInput,view);
            // A wallet push can supersede the local read correlation before94
            // arrives.94 is still an absolute bank-only update for this window;
            // it must not erase that newer wallet or wait forever for a lost read.
            var result=Notify_Currency(new CurrencyNotice{id=notice.id,request=view.currencyRequest?.id==bank.Query.id?bank.Query.id:null,character=notice.character,session=notice.session,
                expectedRevision=notice.expectedRevision,approved=notice.approved,bank=notice.approved?(ulong?)balance:null});
            if(result.Accepted&&ReferenceEquals(nativeBank,bank))bank.MarkLoaded();return result;
        }
        public bool Request_CancelBankClient(string token)
        {
            if(nativeBank==null||token!=null&&nativeBank.Query.token!=token)return false;
            nativeBankAmount?.Close();nativeBankAmount=null;var query=nativeBank.Query;nativeBank=null;
            if(service?.WorldSnapshot()?.currencyRequest?.id==query.id)service.CancelCurrencyStateRequest(query.id);return true;
        }
        public NativeBankAmountEdit Request_EditBankAmount(bool deposit,string token)
        {
            nativeBankAmount?.Close();nativeBankAmount=null;
            return BankBalanceReady(token)?nativeBankAmount=NativeBankAmountEdit.Begin(nativeBank,WorldSnapshot,deposit):null;
        }
        public bool Request_CancelBankAmount(NativeBankAmountEdit edit)
        {if(edit==null||!ReferenceEquals(edit,nativeBankAmount))return false;edit.Close();nativeBankAmount=null;return true;}
        public WorldReply Request_BankAmount(NativeBankAmountEdit edit,string text,string request)
        {
            var view=WorldSnapshot;
            if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out _,out _)||edit==null||!ReferenceEquals(edit,nativeBankAmount)||!edit.Matches(nativeBank,view)
                ||!Guid.TryParseExact(request,"N",out _)||nativeBankAmounts.Contains(request))return new WorldReply(WorldFailure.StaleRequest,view);
            if(!NativeBankPackets.TryAmount(text,out int amount))return new WorldReply(WorldFailure.InvalidInput,view);
            edit.Close();nativeBankAmount=null;nativeBankAmounts.Add(request);bool sent=false;
            void Send()
            {
                if(sent)return;sent=true;var r=new NativeBankAmountRequest{id=request,character=edit.Query.character,session=edit.Query.session,token=edit.Query.token,
                    deposit=edit.Deposit,amount=amount,packet=NativeBankPackets.Amount(edit.Deposit,amount)};
                Notify_BankPacket?.Invoke((byte[])r.packet.Clone());Notify_BankAmountRequested?.Invoke(r.Copy());
            }
            WorldReply Dispatched()
            {var current=service.WorldSnapshot();return new WorldReply(current?.currencySession==edit.Query.session&&current.characterId==edit.Query.character?WorldFailure.None:WorldFailure.StaleRequest,current);}
            if(amount<=0){Send();return Dispatched();}
            WorldReply Queue(EntryAccountService state)
            {state.TryQueueNativeBankTransfer(edit.Deposit,(ulong)amount,edit.Query.token,request);Send();return Dispatched();}
            var result=BankTransferResult(s=>BankTransferAuthority==null?Queue(s)
                :s.ExecuteBankTransfer(edit.Deposit,(ulong)amount,edit.Query.token,request,(operation,state)=>{Send();return BankTransferAuthority(operation,state);}));
            Send();return result;
        }
    }
}
