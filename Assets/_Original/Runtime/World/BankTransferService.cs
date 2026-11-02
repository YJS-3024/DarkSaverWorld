using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure BankTransferReady(string token)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Moving)return WorldFailure.Moving;
            if(world.Kind!=FacilityKind.Bank||token==null||world.Token!=token)return WorldFailure.StaleRequest;
            if(world.Combat!=null||world.PresentationLocked||world.PendingPortal!=null)return WorldFailure.Busy;
            var npc=world.Map(world.Record.map).npcs.FirstOrDefault(n=>n.id==world.Npc&&n.kind==(int)FacilityKind.Bank);
            if(!world.CanInteract(npc))return WorldFailure.TooFar;
            if(!world.Record.goldKnown||!world.Record.bankKnown)return WorldFailure.RulesUnavailable;
            return WorldFailure.None;
        }
        public WorldReply RequestBankTransfer(bool deposit,ulong amount,string token,string request)
        {
            var f=BankTransferReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(amount==0)return WorldResult(WorldFailure.InvalidInput);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingBankTransfer!=null)return WorldResult(WorldFailure.Busy);
            if(amount>(deposit?world.Record.gold:world.Record.bank))return WorldResult(WorldFailure.InsufficientFunds);
            long revision=world.Record.currency?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            world.BankTransferState=world.Record.Copy();
            world.PendingBankTransfer=new BankTransferRequest{id=request,character=worldCharacter,session=world.CurrencySession,token=token,npc=world.Npc,map=world.Record.map,
                deposit=deposit,amount=amount,currencyRevision=revision};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveBankTransfer(BankTransferNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=BankTransferReady(notice.token);if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingBankTransfer;var baseline=world.BankTransferState;
            if(pending==null||baseline==null||pending.id!=notice.request||notice.character!=worldCharacter||notice.session!=world.CurrencySession||notice.map!=world.Record.map||notice.npc!=world.Npc
                ||!Guid.TryParseExact(notice.id,"N",out _)||notice.expectedRevision!=pending.currencyRevision||notice.expectedRevision!=(world.Record.currency?.revision??0)
                ||notice.expectedRevision==long.MaxValue||world.CurrencyNotices.Contains(notice.id)||world.Record.currency?.id==notice.id)return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.Record,baseline)){world.ClearBankTransfer();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearBankTransfer();return WorldResult(WorldFailure.InvalidInput);}
            if(pending.deposit!=notice.deposit||pending.amount!=notice.amount)return WorldResult(WorldFailure.InvalidInput);
            if(!notice.gold.HasValue||!notice.bank.HasValue||!notice.fee.HasValue||!notice.feeSource.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            if(!Enum.IsDefined(typeof(BankFeeSource),notice.feeSource.Value))return WorldResult(WorldFailure.InvalidInput);
            // Use a wider exact integer representation for validation; a gross intermediate may exceed UInt64
            // even when the server's explicit fee leaves a representable final amount.
            decimal wallet=baseline.gold,savings=baseline.bank;
            if(pending.deposit){wallet-=pending.amount;savings+=pending.amount;}
            else{wallet+=pending.amount;savings-=pending.amount;}
            if(notice.feeSource==BankFeeSource.Wallet)wallet-=notice.fee.Value;else savings-=notice.fee.Value;
            if(wallet<0||savings<0)return WorldResult(WorldFailure.InsufficientFunds);
            if(wallet>ulong.MaxValue||savings>ulong.MaxValue||notice.gold.Value!=(ulong)wallet||notice.bank.Value!=(ulong)savings)return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.gold=notice.gold.Value;next.bank=notice.bank.Value;
            next.currency=new CurrencyReceipt{id=notice.id,revision=notice.expectedRevision+1};
            next.lastBankTransfer=new BankTransferReceipt{id=notice.id,request=notice.request,currencyRevision=notice.expectedRevision+1,deposit=pending.deposit,amount=pending.amount,fee=notice.fee.Value,feeSource=notice.feeSource.Value};
            f=CommitWorld(next,baseline);if(f!=WorldFailure.None)return WorldResult(f);
            world.Record=next;world.CurrencyNotices.Add(notice.id);world.ClearBankTransfer();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteBankTransfer(bool deposit,ulong amount,string token,string request,Func<BankTransferRequest,WorldView,BankTransferNotice> authority)
        {
            var f=BankTransferReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestBankTransfer(deposit,amount,token,request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.bankTransferRequest.Copy(),queued.View);
                if(notice!=null)
                {
                    if(notice.request!=request){if(world.PendingBankTransfer?.id==request)world.ClearBankTransfer();return WorldResult(WorldFailure.StaleRequest);}
                    var result=ReceiveBankTransfer(notice);if(!result.Accepted&&world.PendingBankTransfer?.id==request)world.ClearBankTransfer();return WorldResult(result.Failure);
                }
            }
            catch(Exception){/* A failed local authority must not leave later transfers locked. */}
            if(world.PendingBankTransfer?.id==request)world.ClearBankTransfer();return WorldResult(WorldFailure.RulesUnavailable);
        }
    }
}
