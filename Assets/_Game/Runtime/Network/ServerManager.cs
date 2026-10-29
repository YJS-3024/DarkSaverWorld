using System;
using UnityEngine;

namespace DarkSaver.Network
{
    // In-process server dispatch on the Unity main thread. Confirmed payloads are
    // framed/decoded locally; remaining contracts are explicit native extensions.
    // Registered local handlers own rule execution; client handlers use ResponseReceived.
    // RequestReceived remains the compatibility route for external/unregistered handlers.
    [DisallowMultipleComponent]
    public sealed partial class ServerManager : MonoBehaviour
    {
        private ServerManager_Receiver receiver;
        public int OriginalFrameCount { get; private set; }
        // Known payloads cross the exact byte framing boundary even though no socket is used.
        internal LocalPacket Transfer(LocalPacket packet)
        {
            if(!OriginalLocalPacketCodec.TryEncodePayload(packet,out var payload))return packet;
            var frame=OriginalLocalPacketCodec.EncodeFrame(payload);
            if(!OriginalLocalPacketCodec.TryDecodeFrame(frame,out var decoded)
                || !OriginalLocalPacketCodec.TryDecodePayload(packet.Direction,decoded,out var copy,(packet as MagicRequest)?.ActingUnitId))
                throw new InvalidOperationException("Local original-protocol round trip failed.");
            OriginalFrameCount++;return copy;
        }
        public LocalServerDispatcher LocalServer { get; } = new LocalServerDispatcher();
        public IDisposable RegisterRequestHandler<T>(Func<T, LocalCommandResponse> handler) where T : ClientPacket => LocalServer.Register(handler);

        // Acceptance is explicit and distinct from delivery. All calls are main-thread local.
        public bool TryRequest(ClientPacket packet, out LocalCommandResponse response)
        {
            response = null;
            if (!isActiveAndEnabled || packet == null || !LocalServer.HasHandler(packet)) return false;
            packet=(ClientPacket)Transfer(packet);
            LocalServer.Record(packet);
            if (!LocalServer.TryDispatch(packet, out response)) return false;
            SendToClient(response);
            return response.Accepted;
        }
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
        { ClearMercenaryBindings();ClearWorldCharacterBinding();ClearWorldTextBinding(); ClearWarehouseBinding();ClearShopBindings();UnbindLocalTrainingService();ClearBuildingBindings();ClearTutorialBinding();ClearAccountBindings();receiver?.ClearSubscribers(); sender?.ClearSubscribers(); LocalServer.Clear(); }
    }
}
