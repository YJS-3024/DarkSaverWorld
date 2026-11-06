using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<byte[]> Notify_ClientSettingsPacket;
        public event Action<bool> Notify_WhisperReceptionChanged;
        private string clientSettingsSession,clientSettingsCharacter;private bool? whisperReception;
        public bool? WhisperReception=>isActiveAndEnabled&&service!=null&&service.IsWorldClientSession(clientSettingsCharacter,clientSettingsSession)?whisperReception:null;
        private readonly HashSet<string> clientSettingsNotices=new HashSet<string>();
        public bool Notify_ClientSettingsState(ClientWhisperNotice notice)
        {
            if(!isActiveAndEnabled||service==null||notice==null||!service.IsWorldClientSession(notice.character,notice.session)||!Guid.TryParseExact(notice.id,"N",out _)
                ||!OriginalClientSettings.TryReadWhisper(notice.packet,out bool allowed))return false;
            if(clientSettingsSession!=notice.session){clientSettingsSession=notice.session;clientSettingsNotices.Clear();}
            if(!clientSettingsNotices.Add(notice.id))return false;
            clientSettingsCharacter=notice.character;whisperReception=allowed;Notify_WhisperReceptionChanged?.Invoke(allowed);return true;
        }
        public bool Request_WhisperReception(bool allowed)
        {
            if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out var character,out var session))return false;
            if(clientSettingsSession!=session){clientSettingsSession=session;clientSettingsNotices.Clear();}
            clientSettingsCharacter=character;whisperReception=allowed;Notify_ClientSettingsPacket?.Invoke(OriginalClientSettings.WhisperPacket(allowed));return isActiveAndEnabled&&service.IsWorldClientSession(character,session);
        }
    }
}
