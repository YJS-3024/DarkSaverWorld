using System;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<byte[]> Notify_WorldTextPacket;
        public event Action<WorldClientTextRequest> Notify_WorldTextRequested;
        public bool Request_SpaceMove()
        {
            if(!isActiveAndEnabled||service==null||!service.TryWorldClientSession(out var character,out var session))return false;
            var request=new WorldClientTextRequest{id=Guid.NewGuid().ToString("N"),character=character,session=session,command="/공간이동",packet=NativeWorldTextPackets.SpaceMove()};
            bool Current()=>isActiveAndEnabled&&service.IsWorldClientSession(character,session);
            Notify_WorldTextPacket?.Invoke((byte[])request.packet.Clone());if(!Current())return false;Notify_WorldTextRequested?.Invoke(request.Copy());return Current();
        }
    }
}
