using System;

namespace DarkSaver.Network
{
    // Server perspective: local server -> client. Responses remain independent
    // packets; sending a request does not implicitly generate a response.
    public sealed class ServerManager_Sender
    {
        private readonly ServerManager owner;
        internal ServerManager_Sender(ServerManager owner) { this.owner = owner; }

        public event Action<ServerPacket> ResponseSent;

        public bool Send(ServerPacket packet)
        {
            var handler = ResponseSent;
            if (owner == null || !owner.isActiveAndEnabled || packet == null || handler == null) return false;
            handler(packet); // Local synchronous delivery, not successful client application.
            return true;
        }

        internal void ClearSubscribers() => ResponseSent = null;
    }
}
