using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<WorldPrivateTextRequest> Notify_PrivateTextRequested;
        private readonly NativePrivateChatInbox privateChats=new NativePrivateChatInbox();
        private readonly HashSet<string> privateChatNotices=new HashSet<string>();private string privateChatSession;
        private bool PrivateChatContext(out string character,out string session)
        {
            if(!ChatContext(out character,out session))return false;
            if(privateChatSession!=session){privateChatSession=session;privateChats.Clear();privateChatNotices.Clear();}return true;
        }
        public NativePrivateChatView[] PrivateChats=>PrivateChatContext(out _,out _)?privateChats.Views():Array.Empty<NativePrivateChatView>();
        public string PrivateChatSession=>PrivateChatContext(out _,out var session)?session:null;
        // Native452D60 calls425CF0 with EM_LIMITTEXT(60). This adapter counts
        // CP949 bytes; actual Win32/IME composition is verified separately.
        public bool AcceptsPrivateText(string value)=>memoCodec!=null&&memoCodec.TryEncode(value,out var bytes)&&bytes.Length<=60;
        public bool Notify_PrivateMessage(WorldPrivateMessageNotice notice)
        {
            if(!PrivateChatContext(out string character,out string session)||notice==null||!notice.approved||notice.character!=character||notice.session!=session
                ||!notice.nativeSuppression.HasValue||!notice.nativeRoomNumber.HasValue||notice.nativeRoomNumber<short.MinValue||notice.nativeRoomNumber>ushort.MaxValue
                ||!Guid.TryParseExact(notice.id,"N",out _)||privateChatNotices.Contains(notice.id)
                ||memoCodec==null||!memoCodec.TryEncode(notice.text,out _))return false;
            if(!privateChats.Receive(notice.text,notice.nativeSuppression.Value,notice.nativeRoomNumber.Value,memoCodec))return false;
            privateChatNotices.Add(notice.id);return true;
        }
        public bool Request_ClosePrivateChat(string thread,string session)
        {return PrivateChatContext(out _,out var current)&&session==current&&privateChats.Close(thread);}
        public bool Request_ScrollPrivateChat(string thread,string session,int direction)
        {return PrivateChatContext(out _,out var current)&&session==current&&privateChats.Scroll(thread,direction);}
        public bool Request_PrivateText(string thread,string session,string text)
        {
            if(!PrivateChatContext(out string character,out string current)||session!=current||memoCodec==null)return false;
            string recipient=privateChats.Recipient(thread);if(recipient==null)return false;
            // This request precedes the empty-edit check in original4537A0.
            if(WhisperReception==false)Notify_ClientSettingsPacket?.Invoke(OriginalClientSettings.WhisperPacket(true));
            if(string.IsNullOrEmpty(text)||!AcceptsPrivateText(text))return false;
            string processed=chatFilter.Apply(text,false);if(!memoCodec.TryEncode(recipient,out var name)||!memoCodec.TryEncode(processed,out var body))return false;
            var packet=NativePrivateChatInbox.Reply(name,body);if(packet==null)return false;
            var request=new WorldPrivateTextRequest{id=Guid.NewGuid().ToString("N"),character=character,session=current,thread=thread,recipient=recipient.TrimEnd(' '),text=processed,packet=packet};
            Notify_WorldTextPacket?.Invoke((byte[])packet.Clone());Notify_PrivateTextRequested?.Invoke(request.Copy());return true;
        }
    }
}
