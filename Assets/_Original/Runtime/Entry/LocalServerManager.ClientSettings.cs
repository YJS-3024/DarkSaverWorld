using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<byte[]> Notify_ClientSettingsPacket;
        public event Action<bool> Notify_WhisperReceptionChanged;
        private string clientSettingsSession;
        public bool? WhisperReception {get;private set;}
        private readonly HashSet<string> clientSettingsNotices=new HashSet<string>();
        public bool Notify_ClientSettingsState(ClientWhisperNotice notice)
        {
            if(!isActiveAndEnabled||service==null||notice==null||!service.IsWorldClientSession(notice.character,notice.session)||!Guid.TryParseExact(notice.id,"N",out _)
                ||!OriginalClientSettings.TryReadWhisper(notice.packet,out bool allowed))return false;
            if(clientSettingsSession!=notice.session){clientSettingsSession=notice.session;clientSettingsNotices.Clear();}
            if(!clientSettingsNotices.Add(notice.id))return false;
            WhisperReception=allowed;Notify_WhisperReceptionChanged?.Invoke(allowed);return true;
        }
        public bool Request_WhisperReception(bool allowed)
        {
            if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out _,out _))return false;
            WhisperReception=allowed;Notify_ClientSettingsPacket?.Invoke(OriginalClientSettings.WhisperPacket(allowed));return true;
        }
    }
}
