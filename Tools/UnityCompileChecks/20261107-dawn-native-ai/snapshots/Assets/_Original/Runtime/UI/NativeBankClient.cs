using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    public static class NativeBankLayout
    {
        public const int WalletX=115,WalletY=47,SavingsX=311,SavingsY=19;
        public static bool CanOpenAmount(bool deposit,bool loaded,ulong wallet,ulong savings)=>loaded&&(deposit?NativeCurrencyText.WalletValue(wallet)!=0:savings!=0);
    }
    public sealed class NativeBankQuery
    {
        public string id,character,session,token,npc;public int map;public long revision;
        public NativeBankQuery Copy()=>(NativeBankQuery)MemberwiseClone();
    }
    public sealed class NativeBankBalanceNotice
    {
        // Transport ownership/correlation fields are local metadata, not bytes in94.
        public string id,query,character,session,token;public long expectedRevision;public bool approved;public byte[] packet;
    }
    public sealed class NativeBankAmountRequest
    {
        public string id,character,session,token;public bool deposit;public int amount;public byte[] packet;
        public NativeBankAmountRequest Copy(){var result=(NativeBankAmountRequest)MemberwiseClone();result.packet=packet?.Clone()as byte[];return result;}
    }
    public sealed class NativeBankWindow
    {
        private readonly NativeBankQuery query;public NativeBankQuery Query=>query.Copy();public bool Loaded{get;private set;}
        private NativeBankWindow(NativeBankQuery value){query=value.Copy();}
        public static bool Available(WorldView view,string token)=>view?.record!=null&&view.facility?.kind==FacilityKind.Bank
            &&!string.IsNullOrEmpty(token)&&!string.IsNullOrEmpty(view.facility.npc)&&view.facility.token==token&&!view.moving&&view.pendingPortal==null&&(view.presentation==null||view.presentation.completed)&&view.battle==null&&view.training==null;
        public static NativeBankWindow Begin(WorldView view,string token,string id)
        {
            if(!Available(view,token)||!Guid.TryParseExact(id,"N",out _)||string.IsNullOrEmpty(view.characterId)||string.IsNullOrEmpty(view.currencySession))return null;
            return new NativeBankWindow(new NativeBankQuery{id=id,character=view.characterId,session=view.currencySession,token=token,npc=view.facility.npc,map=view.record.map,revision=view.record.currency?.revision??0});
        }
        public bool Matches(WorldView view)=>Available(view,Query.token)&&view.characterId==Query.character&&view.currencySession==Query.session
            &&view.record.map==Query.map&&view.facility.npc==Query.npc;
        public void MarkLoaded(){Loaded=true;}
    }
    public sealed class NativeBankAmountEdit
    {
        private readonly NativeBankQuery query;public NativeBankQuery Query=>query.Copy();public long Revision{get;}public bool Deposit{get;}public bool Closed{get;private set;}
        private NativeBankAmountEdit(NativeBankWindow bank,WorldView view,bool deposit){query=bank.Query;Revision=view.record.currency?.revision??0;Deposit=deposit;}
        public static NativeBankAmountEdit Begin(NativeBankWindow bank,WorldView view,bool deposit)=>bank?.Loaded==true&&bank.Matches(view)?new NativeBankAmountEdit(bank,view,deposit):null;
        public bool Matches(NativeBankWindow bank,WorldView view)=>!Closed&&bank?.Loaded==true&&bank.Query.id==Query.id&&bank.Matches(view)&&(view.record.currency?.revision??0)==Revision;
        public void Close(){Closed=true;}
    }
    public static class NativeBankPackets
    {
        public static byte[] Query()=>new byte[]{0x93};
        public static byte[] Amount(bool deposit,int amount)=>new[]{deposit?(byte)0x95:(byte)0x96,(byte)amount,(byte)(amount>>8),(byte)(amount>>16),(byte)(amount>>24)};
        public static bool TryBalance(byte[] payload,out ulong balance)
        {
            balance=0;if(payload?.Length!=9||payload[0]!=0x94)return false;
            for(int i=0;i<8;i++)balance|=(ulong)payload[i+1]<<(i*8);return true;
        }
        // Match atoi's defined, representable prefix conversions. CRT overflow is
        // deliberately rejected rather than guessing a server or Windows CRT limit.
        public static bool TryAmount(string text,out int value)
        {
            value=0;if(text==null)return false;int at=0;while(at<text.Length&&" \t\r\n\v\f".IndexOf(text[at])>=0)at++;
            bool negative=at<text.Length&&text[at]=='-';if(at<text.Length&&(text[at]=='-'||text[at]=='+'))at++;
            long total=0,limit=negative?2147483648L:int.MaxValue;
            while(at<text.Length&&text[at]>='0'&&text[at]<='9'){total=total*10+text[at++]-'0';if(total>limit)return false;}
            value=negative?(int)-total:(int)total;return true;
        }
    }
}
