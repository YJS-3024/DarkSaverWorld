using System;
using System.Linq;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<byte[]> Notify_CommanderNamePacket;
        public event Action<CommanderNameRequest> Notify_CommanderNameRequested;
        private NativeCommanderNameEdit commanderNameEdit;
        public NativeCommanderNameEdit Request_EditCommanderName()
        {
            commanderNameEdit?.Cancel();commanderNameEdit=null;
            if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out _,out _))return null;
            return commanderNameEdit=NativeCommanderNameEdit.Begin(service.WorldSnapshot());
        }
        public bool Request_CancelCommanderName(NativeCommanderNameEdit edit)
        {if(edit==null||!ReferenceEquals(edit,commanderNameEdit))return false;edit.Cancel();commanderNameEdit=null;return true;}
        public bool Request_SetCommanderName(NativeCommanderNameEdit edit,string name)
        {
            if(edit==null||!ReferenceEquals(edit,commanderNameEdit)||!isActiveAndEnabled||service==null
                ||!service.IsWorldClientSession(edit.Character,edit.Session)||name==null||name.Any(char.IsControl)
                ||memoCodec==null||!memoCodec.TryEncode(name,out byte[] encoded))return false;
            var request=edit.Submit(service.WorldSnapshot(),name,encoded);if(request==null)return false;
            commanderNameEdit=null;
            Notify_CommanderNamePacket?.Invoke((byte[])request.packet.Clone());Notify_CommanderNameRequested?.Invoke(request.Copy());
            //458750 immediately restores focus/hides the edit; subsequent roster reception supplies the name.
            return true;
        }
    }
}
