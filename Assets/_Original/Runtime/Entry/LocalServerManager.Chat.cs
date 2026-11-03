using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private string chatRecipientSession,lastChatRecipient;
        private NativeWorldChatFilter chatFilter;
        private readonly HashSet<string> chatTermNotices=new HashSet<string>();
        public string LastChatRecipient=>isActiveAndEnabled&&service!=null&&service.TryWorldClientSession(out _,out var session)&&session==chatRecipientSession?lastChatRecipient:null;
        private bool ChatContext(out string character,out string session)
        {
            character=session=null;if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out character,out session))return false;
            if(chatRecipientSession!=session){chatRecipientSession=session;lastChatRecipient=null;chatFilter=new NativeWorldChatFilter();chatTermNotices.Clear();}return true;
        }
        public bool Notify_WorldChatTerms(WorldChatTermsNotice notice)
        {
            if(!ChatContext(out var character,out var session)||notice==null||!notice.approved||notice.character!=character||notice.session!=session
                ||!Guid.TryParseExact(notice.id,"N",out _)||chatTermNotices.Contains(notice.id)||!chatFilter.SetTerms(notice.terms,memoCodec))return false;
            chatTermNotices.Add(notice.id);return true;
        }
        public bool Request_WorldText(string text,int channel)
        {
            if(channel<1||channel>3||string.IsNullOrEmpty(text)||memoCodec==null||!ChatContext(out var character,out var session)||!memoCodec.TryEncode(text,out _))return false;
            string processed=chatFilter.Apply(text);if(!memoCodec.TryEncode(processed,out byte[] encoded))return false;
            bool Current()=>isActiveAndEnabled&&service.IsWorldClientSession(character,session);
            var packet=NativeWorldTextPackets.Build(encoded,channel);
            if(packet!=null)
            {
                var request=new WorldClientTextRequest{id=Guid.NewGuid().ToString("N"),character=character,session=session,command=processed,packet=packet};
                Notify_WorldTextPacket?.Invoke((byte[])packet.Clone());if(!Current())return false;Notify_WorldTextRequested?.Invoke(request.Copy());if(!Current())return false;
            }
            // 430FA0 examines the full CString even when429F70's low byte wraps to0.
            if(channel==1&&NativeWorldChatComposer.WhisperPrefix(processed,out string recipient))
            {
                if(WhisperReception==false)Notify_ClientSettingsPacket?.Invoke(OriginalClientSettings.WhisperPacket(true));
                if(!Current())return false;if(recipient!=null)lastChatRecipient=recipient;
            }
            return Current();
        }
    }
}
