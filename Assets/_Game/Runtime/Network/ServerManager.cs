using System;
using UnityEngine;

namespace DarkSaver.Network
{
    // Unity main-thread, synchronous local delivery only. No sockets or original data access.
    // Server handlers subscribe to RequestReceived; client handlers to ResponseReceived.
    // This component does not invent validation, damage, AP costs, rewards or acknowledgements.
    [DisallowMultipleComponent]
    public sealed class ServerManager : MonoBehaviour
    {
        private ServerManager_Receiver receiver;
        private ServerManager_Sender sender;
        public ServerManager_Receiver Receiver => receiver ??= new ServerManager_Receiver(this);
        public ServerManager_Sender Sender => sender ??= new ServerManager_Sender(this);

        // Compatibility facade: existing callers/scenes keep the same component/API.
        public event Action<ClientPacket> RequestReceived
        { add => Receiver.RequestReceived += value; remove => Receiver.RequestReceived -= value; }
        public event Action<ServerPacket> ResponseReceived
        { add => Sender.ResponseSent += value; remove => Sender.ResponseSent -= value; }

        // True means delivered to subscriber(s), not that gameplay accepted the request.
        // Subscriber exceptions propagate to the caller; nothing silently retries a packet.
        public bool SendToServer(ClientPacket packet) => SendToServer(packet, null);

        // Local dispatch boundary: after transport checks, before synchronous server
        // callbacks can respond. Not a server acknowledgement or an original opcode.
        public bool SendToServer(ClientPacket packet, Action onDispatch)
            => Receiver.Receive(packet, onDispatch);

        public bool SendToClient(ServerPacket packet)
            => Sender.Send(packet);

        private void OnDestroy()
        { receiver?.ClearSubscribers(); sender?.ClearSubscribers(); }
    }
}
