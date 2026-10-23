using System;

namespace DarkSaver.Network
{
    // Server perspective: client -> local server. Owned by one ServerManager;
    // not a new scene component, socket listener or original binary decoder.
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
            if (handler == null)
            {
                var battle = owner.GetComponent<DarkSaver.Combat.BattleSceneRoot>();
                if (onDispatch == null || battle == null || !battle.TemporaryLocalRulesEnabled
                    || (packet is not AttackRequest && packet is not MagicRequest)) return false;
                onDispatch(); return true;
            }
            onDispatch?.Invoke();
            handler(packet); // Exceptions propagate; never retry or manufacture an acknowledgement.
            return true;
        }

        internal void ClearSubscribers() => RequestReceived = null;
    }
}
