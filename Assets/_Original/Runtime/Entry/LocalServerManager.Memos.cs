using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private NativeMemoInbox memoInbox;private NativeCodePage949 memoCodec;private string memoSession,memoCharacter;
        private readonly HashSet<string> memoNotices=new HashSet<string>();
        public Func<string,string,NativeMemoNotice> MemoAuthority {get;set;}
        public event Action<byte[]> Notify_MemoPacket;
        public event Action Notify_MemosChanged;
        public void ConfigureClientText(NativeCodePage949 codec){memoCodec=codec;}
        private bool MemoContext()
        {
            if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out var character,out var session))return false;
            memoCharacter=character;
            if(memoSession!=session){memoSession=session;memoInbox=new NativeMemoInbox();memoNotices.Clear();}
            return true;
        }
        public NativeMemoView MemoState
        {get{if(!MemoContext())return null;return new NativeMemoView{character=memoCharacter,session=memoSession,revision=memoInbox.Revision,page=memoInbox.Page,rows=memoInbox.Rows};}}
        public bool MemoViewChanged(string session,long revision,int page)=>MemoContext()&&(memoSession!=session||memoInbox.Revision!=revision||memoInbox.Page!=page);
        public NativeMemo[] MemoSnapshot=>MemoContext()?memoInbox.Rows:null;
        public long MemoRevision=>MemoContext()?memoInbox.Revision:-1;
        public int MemoPage=>MemoContext()?memoInbox.Page:0;
        public bool Request_ViewMemos()
        {
            if(!MemoContext())return false;
            if(!memoInbox.Query())return true;
            var inbox=memoInbox;string character=memoCharacter,session=memoSession;var authority=MemoAuthority;
            bool Current()=>MemoContext()&&ReferenceEquals(inbox,memoInbox)&&service.IsWorldClientSession(character,session);
            Notify_MemoPacket?.Invoke(NativeMemoPackets.ViewRequest());if(!Current())return false;
            if(authority!=null)
            {
                NativeMemoNotice notice;
                try{notice=authority(character,session);}catch{if(Current())inbox.InvalidateQuery();return false;}
                if(!Current())return false;
                if(notice!=null&&!Notify_MemoPayload(notice)){if(Current())inbox.InvalidateQuery();return false;}
            }
            return true;
        }
        public bool Notify_MemoRefreshNeeded(string character,string session)
        {if(!MemoContext()||!service.IsWorldClientSession(character,session))return false;memoInbox.InvalidateQuery();return true;}
        public bool Notify_MemoTransfer(NativeMemoTransferNotice notice)
        {
            if(!MemoContext()||notice==null||!notice.approved||!service.IsWorldClientSession(notice.character,notice.session)||!Guid.TryParseExact(notice.id,"N",out _)||memoNotices.Contains(notice.id))return false;
            if(!NativeMemoTransfer.TryDecode(notice.body,out var payload))return false;
            return Notify_MemoPayload(new NativeMemoNotice{id=notice.id,character=notice.character,session=notice.session,approved=true,payload=payload});
        }
        public bool Notify_MemoPayload(NativeMemoNotice notice)
        {
            if(!MemoContext()||notice==null||!notice.approved||!service.IsWorldClientSession(notice.character,notice.session)||!Guid.TryParseExact(notice.id,"N",out _)||memoNotices.Contains(notice.id))return false;
            if(!memoInbox.Append(notice.payload,memoCodec))return false;memoNotices.Add(notice.id);Notify_MemosChanged?.Invoke();return true;
        }
        public bool Request_MemoPage(int page)
        {if(!MemoContext()||!memoInbox.SetPage(page))return false;Notify_MemosChanged?.Invoke();return true;}
        public bool Request_DeleteMemo(int row,long revision,byte id,string session,int page)
        {
            if(!MemoContext()||memoSession!=session||memoInbox.Page!=page||!memoInbox.Delete(row,revision,id,out var packet))return false;
            Notify_MemoPacket?.Invoke(packet);Notify_MemosChanged?.Invoke();return true;
        }
    }
}
