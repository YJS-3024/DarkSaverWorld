using System;

namespace DarkSaver.Network
{
    // Server perspective: client -> local server. Owned by one ServerManager;
    // not a new scene component or socket listener. Known payloads use the shared codec.
    public sealed class ServerManager_Receiver
    {
        private readonly ServerManager owner;
        internal ServerManager_Receiver(ServerManager owner) { this.owner = owner; }

        public event Action<ClientPacket> RequestReceived;

        // Delivery is not gameplay acceptance. The existing handler/dispatch
        // callback retains responsibility for validated action processing.
        public bool Receive(ClientPacket packet, Action onDispatch = null)
        {
            var handler = RequestReceived;
            if (owner == null || !owner.isActiveAndEnabled || packet == null) return false;
            if (owner.LocalServer.HasHandler(packet))
            {
                onDispatch?.Invoke();
                owner.TryRequest(packet, out _);
                handler?.Invoke(packet);
                return true;
            }
            if (handler == null) return false;
            packet=(ClientPacket)owner.Transfer(packet);
            owner.LocalServer.Record(packet);
            onDispatch?.Invoke();
            handler(packet); // Exceptions propagate; never retry or manufacture an acknowledgement.
            return true;
        }

        internal void ClearSubscribers() => RequestReceived = null;
    }
}
