using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<WorldMessageNotice> Notify_WorldMessageReceived;
        private string worldMessageSession;
        private readonly HashSet<string> worldMessageNotices=new HashSet<string>();
        // Trusted, decoded server message. Private kind9 belongs to its own native window and is not a main-log row.
        public bool Notify_WorldMessage(WorldMessageNotice notice)
        {
            if(!isActiveAndEnabled||service==null||notice==null||!notice.approved||!NativeWorldChatLog.IsMainKind(notice.kind)
                ||!Guid.TryParseExact(notice.id,"N",out _)||!service.IsWorldClientSession(notice.character,notice.session)
                ||memoCodec==null||!memoCodec.TryEncode(notice.text,out _))return false;
            if(worldMessageSession!=notice.session){worldMessageSession=notice.session;worldMessageNotices.Clear();}
            if(!worldMessageNotices.Add(notice.id))return false;
            Notify_WorldMessageReceived?.Invoke(notice.Copy());return true;
        }
    }
}
